using System.Diagnostics;
using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.100: Handles AttendeesAddedEvent to send confirmation email and regenerate ticket PDF.
/// This handler is triggered after additional attendees are successfully added to a paid registration.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// Unified to always use ITypedEmailService with AttendeesAddedEmailParams.
/// </summary>
public class AttendeesAddedEventHandler : INotificationHandler<DomainEventNotification<AttendeesAddedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly ITicketService _ticketService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IFormQueries _formQueries;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<AttendeesAddedEventHandler> _logger;

    public AttendeesAddedEventHandler(
        ITypedEmailService typedEmailService,
        ITicketService ticketService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IFormQueries formQueries,
        IEmailUrlHelper emailUrlHelper,
        ILogger<AttendeesAddedEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _ticketService = ticketService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _formQueries = formQueries;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<AttendeesAddedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var correlationId = Guid.NewGuid();

        using (LogContext.PushProperty("Operation", "AttendeesAdded"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("RegistrationId", domainEvent.RegistrationId))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[Phase 6A.100] AttendeesAdded START: CorrelationId={CorrelationId}, EventId={EventId}, RegistrationId={RegistrationId}, " +
                "PreviousCount={PreviousCount}, AddedCount={AddedCount}, NewTotal={NewTotal}, AdditionalAmount={AdditionalAmount}",
                correlationId, domainEvent.EventId, domainEvent.RegistrationId,
                domainEvent.PreviousAttendeeCount, domainEvent.AddedAttendeeCount,
                domainEvent.NewTotalAttendeeCount, domainEvent.AdditionalAmountPaid);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Step 1: Load event data
                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning(
                        "[Phase 6A.100] AttendeesAdded: Event not found - CorrelationId={CorrelationId}, EventId={EventId}",
                        correlationId, domainEvent.EventId);
                    return;
                }

                // Step 2: Load registration with updated attendees
                var registration = await _registrationRepository.GetByIdAsync(domainEvent.RegistrationId, cancellationToken);
                if (registration == null)
                {
                    _logger.LogWarning(
                        "[Phase 6A.100] AttendeesAdded: Registration not found - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}",
                        correlationId, domainEvent.RegistrationId);
                    return;
                }

                // Step 3: Determine recipient
                string recipientName;
                string recipientEmail = domainEvent.ContactEmail;

                if (domainEvent.UserId.HasValue)
                {
                    var user = await _userRepository.GetByIdAsync(domainEvent.UserId.Value, cancellationToken);
                    if (user != null)
                    {
                        recipientName = $"{user.FirstName} {user.LastName}";
                        recipientEmail = user.Email.Value;
                    }
                    else
                    {
                        recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                            ? registration.Attendees.First().Name
                            : "Guest";
                    }
                }
                else
                {
                    recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                        ? registration.Attendees.First().Name
                        : "Guest";
                }

                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    _logger.LogError(
                        "[Phase 6A.100] AttendeesAdded: No email address - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}",
                        correlationId, domainEvent.RegistrationId);
                    return;
                }

                // Step 4: Regenerate ticket PDF with updated attendees
                var ticketResult = await _ticketService.RegenerateTicketPdfForRegistrationAsync(
                    domainEvent.RegistrationId,
                    cancellationToken);

                string ticketCode = "";
                Guid? ticketId = null;

                if (ticketResult.IsSuccess)
                {
                    ticketCode = ticketResult.Value.TicketCode;
                    ticketId = ticketResult.Value.TicketId;
                    _logger.LogInformation(
                        "[Phase 6A.100] AttendeesAdded: Ticket regenerated - CorrelationId={CorrelationId}, TicketCode={TicketCode}",
                        correlationId, ticketCode);
                }
                else
                {
                    _logger.LogWarning(
                        "[Phase 6A.100] AttendeesAdded: Ticket regeneration failed - CorrelationId={CorrelationId}, Errors={Errors}",
                        correlationId, string.Join(", ", ticketResult.Errors));
                }

                // Step 5: Build typed email parameters
                var newAttendeesHtml = new System.Text.StringBuilder();
                var newAttendeesText = new System.Text.StringBuilder();

                var allAttendees = registration.Attendees.ToList();
                var newAttendees = allAttendees.TakeLast(domainEvent.AddedAttendeeCount).ToList();

                // Phase 8: Include tier name when present
                // Slice 7 S7.7: Append seat label for assigned-seating events
                foreach (var attendee in newAttendees)
                {
                    var tierSuffix = !string.IsNullOrWhiteSpace(attendee.TicketTierName)
                        ? $" <span style=\"color: #8B1538; font-weight: 600;\">({attendee.TicketTierName})</span>"
                        : "";
                    var tierText = !string.IsNullOrWhiteSpace(attendee.TicketTierName)
                        ? $" [{attendee.TicketTierName}]"
                        : "";
                    var seatSuffix = !string.IsNullOrWhiteSpace(attendee.SeatLabel)
                        ? $" <span style=\"color: #2563EB; font-weight: 600;\">(Seat {attendee.SeatLabel})</span>"
                        : "";
                    var seatText = !string.IsNullOrWhiteSpace(attendee.SeatLabel)
                        ? $" [Seat {attendee.SeatLabel}]"
                        : "";
                    newAttendeesHtml.AppendLine($@"<div class=""attendee-item"">
                        <div class=""attendee-icon new"">&#10003;</div>
                        <span class=""attendee-name"">{attendee.Name}{tierSuffix}{seatSuffix}</span>
                        <span class=""attendee-badge"">NEW</span>
                    </div>");
                    newAttendeesText.AppendLine($"- {attendee.Name} ({attendee.AgeCategory}){tierText}{seatText}");
                }

                var allAttendeesHtml = new System.Text.StringBuilder();
                var allAttendeesText = new System.Text.StringBuilder();
                int index = 1;
                foreach (var attendee in allAttendees)
                {
                    var isNew = newAttendees.Contains(attendee);
                    var iconClass = isNew ? "new" : "";
                    var badge = isNew ? @"<span class=""attendee-badge"">NEW</span>" : "";
                    var initial = attendee.Name.Length > 0 ? attendee.Name[0].ToString().ToUpper() : index.ToString();

                    var allTierSuffix = !string.IsNullOrWhiteSpace(attendee.TicketTierName)
                        ? $" <span style=\"color: #8B1538; font-weight: 600;\">({attendee.TicketTierName})</span>"
                        : "";
                    var allTierText = !string.IsNullOrWhiteSpace(attendee.TicketTierName)
                        ? $" [{attendee.TicketTierName}]"
                        : "";
                    // Slice 7 S7.7: Append seat label for assigned-seating events
                    var allSeatSuffix = !string.IsNullOrWhiteSpace(attendee.SeatLabel)
                        ? $" <span style=\"color: #2563EB; font-weight: 600;\">(Seat {attendee.SeatLabel})</span>"
                        : "";
                    var allSeatText = !string.IsNullOrWhiteSpace(attendee.SeatLabel)
                        ? $" [Seat {attendee.SeatLabel}]"
                        : "";
                    allAttendeesHtml.AppendLine($@"<div class=""attendee-item"">
                        <div class=""attendee-icon {iconClass}"">{(isNew ? "&#10003;" : initial)}</div>
                        <span class=""attendee-name"">{attendee.Name}{allTierSuffix}{allSeatSuffix}</span>
                        {badge}
                    </div>");
                    allAttendeesText.AppendLine($"- {attendee.Name} ({attendee.AgeCategory}){allTierText}{allSeatText}");
                    index++;
                }

                // Phase 6A.100: Use AttendeesAddedEmailParams for type-safe email sending
                var emailParams = AttendeesAddedEmailParams.Create(
                    userId: domainEvent.UserId,
                    registrationId: domainEvent.RegistrationId,
                    eventId: domainEvent.EventId,
                    userName: recipientName,
                    userEmail: recipientEmail,
                    eventTitle: @event.Title.Value,
                    eventStartDate: @event.StartDate.GetValueOrDefault(), // Phase 8YA-2 TODO: param class should accept DateTime?
                    timeZoneId: @event.TimeZoneId,
                    eventLocation: GetEventLocationString(@event),
                    previousCount: domainEvent.PreviousAttendeeCount,
                    addedCount: domainEvent.AddedAttendeeCount,
                    newTotalCount: domainEvent.NewTotalAttendeeCount,
                    additionalAmount: domainEvent.AdditionalAmountPaid,
                    totalPaid: domainEvent.TotalAmountPaid,
                    newAttendees: newAttendeesText.ToString().TrimEnd(),
                    newAttendeesHtml: newAttendeesHtml.ToString(),
                    allAttendees: allAttendeesText.ToString().TrimEnd(),
                    allAttendeesHtml: allAttendeesHtml.ToString(),
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id),
                    ticketUrl: ticketId.HasValue ? _emailUrlHelper.BuildTicketViewUrl(ticketId.Value) : null,
                    ticketCode: ticketCode
                );

                // Phase 7C.2b: emit decomposed location keys for the template rewrite.
                emailParams.WithLocationDetails(@event.ProjectEmailLocation());

                // Phase 6A.100 Fix: Add organizer contact if available
                if (@event.HasOrganizerContact())
                {
                    emailParams.WithOrganizerContacts(
                        @event.OrganizerContacts
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                            .ToList());
                }

                // Phase 6A.100 Fix: Add signup lists URL if event has signup lists
                if (@event.HasSignUpLists())
                {
                    emailParams.WithSignUpLists(
                        _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#sign-ups");
                }

                // Phase 6A.112: Check if event has active signup forms
                var forms = await _formQueries.GetByOwnerAsync(FormOwnerEntityTypeDto.Event, @event.Id, cancellationToken);
                var hasActiveForms = forms.Any(f => f.Status == FormStatusDto.Active);

                if (hasActiveForms)
                {
                    emailParams.WithSignupForms($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#signup-forms");
                }

                // Phase 7F-A: populate FlexibleRegistration params via the shared formatter so
                // Mode-B add-attendees confirmations render the head-count breakdown alongside
                // the per-attendee table that Mode A still relies on. (Today, AttendeesAdded
                // is fired only on Mode A — but populating the params keeps the email contract
                // consistent and ready for paid-B add-attendees in Phase 7F-D.)
                try
                {
                    var flex = LankaConnect.Application.Events.Common.HeadCountEmailFormatter.Compute(registration);
                    emailParams.HasDetailedAttendees = flex.hasDetailedAttendees;
                    emailParams.HasHeadCount = flex.hasHeadCount;
                    emailParams.HasHeadCountBreakdown = flex.hasHeadCountBreakdown;
                    emailParams.HasTierBreakdown = flex.hasTierBreakdown;
                    emailParams.HeadCountTotal = flex.headCountTotal;
                    emailParams.HeadCountBreakdownLine = flex.headCountBreakdownLine;
                    emailParams.TierBreakdownLine = flex.tierBreakdownLine;
                    emailParams.LeadAttendeeName = flex.leadAttendeeName;
                    emailParams.RegistrationBreakdownHtml = flex.registrationBreakdownHtml;
                }
                catch (Exception flexEx)
                {
                    _logger.LogWarning(flexEx,
                        "[Phase 7F-A] FlexibleRegistration params computation failed for AttendeesAdded email " +
                        "RegistrationId={RegistrationId} — continuing with default Mode-A fields.",
                        registration.Id);
                }

                _logger.LogInformation(
                    "[Phase 6A.100] AttendeesAdded: Sending via ITypedEmailService - CorrelationId={CorrelationId}, Template={Template}",
                    correlationId, emailParams.TemplateName);

                // Phase 6A.100: Send via typed email service
                var typedResult = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                stopwatch.Stop();

                if (!typedResult.Success)
                {
                    _logger.LogError(
                        "[Phase 6A.100] AttendeesAdded FAILED: Email sending failed - CorrelationId={CorrelationId}, Email={Email}, " +
                        "Errors={Errors}, Duration={ElapsedMs}ms",
                        correlationId, recipientEmail, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "[Phase 6A.100] AttendeesAdded COMPLETE: Email sent successfully - CorrelationId={CorrelationId}, " +
                        "Email={Email}, RegistrationId={RegistrationId}, AddedCount={AddedCount}, HasTicket={HasTicket}, " +
                        "Duration={ElapsedMs}ms",
                        correlationId, recipientEmail, domainEvent.RegistrationId, domainEvent.AddedAttendeeCount,
                        emailParams.HasTicket, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[Phase 6A.100] AttendeesAdded CANCELED - CorrelationId={CorrelationId}, Duration={ElapsedMs}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[Phase 6A.100] AttendeesAdded FAILED: Unhandled exception - CorrelationId={CorrelationId}, " +
                    "EventId={EventId}, RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                    correlationId, domainEvent.EventId, domainEvent.RegistrationId, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Safely extracts event location string with defensive null handling.
    /// </summary>
    private static string GetEventLocationString(Event @event)
    {
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;

        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        if (string.IsNullOrWhiteSpace(street))
            return city!;

        if (string.IsNullOrWhiteSpace(city))
            return street;

        return $"{street}, {city}";
    }
}
