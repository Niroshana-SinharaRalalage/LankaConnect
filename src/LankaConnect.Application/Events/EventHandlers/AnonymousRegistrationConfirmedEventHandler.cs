using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.24: Handles AnonymousRegistrationConfirmedEvent to send confirmation email to anonymous attendees.
/// For paid events, email is sent by PaymentCompletedEventHandler after payment.
/// </summary>
public class AnonymousRegistrationConfirmedEventHandler : INotificationHandler<DomainEventNotification<AnonymousRegistrationConfirmedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILogger<AnonymousRegistrationConfirmedEventHandler> _logger;

    public AnonymousRegistrationConfirmedEventHandler(
        IEmailService emailService,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ILogger<AnonymousRegistrationConfirmedEventHandler> logger)
    {
        _emailService = emailService;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<AnonymousRegistrationConfirmedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "AnonymousRegistrationConfirmed"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("AttendeeEmail", domainEvent.AttendeeEmail))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AnonymousRegistrationConfirmed START: Event={EventId}, Email={Email}, Quantity={Quantity}",
                domainEvent.EventId, domainEvent.AttendeeEmail, domainEvent.Quantity);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Retrieve event data
                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "AnonymousRegistrationConfirmed: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Fetch registration to get attendee details and check if paid event
                var registration = await _registrationRepository.GetAnonymousByEventAndEmailAsync(
                    domainEvent.EventId, domainEvent.AttendeeEmail, cancellationToken);

                if (registration == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "AnonymousRegistrationConfirmed: Registration not found - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, domainEvent.AttendeeEmail, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Skip email for paid events - PaymentCompletedEventHandler will send it after payment
                if (registration.PaymentStatus == PaymentStatus.Pending)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "AnonymousRegistrationConfirmed: Skipping paid event - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, domainEvent.AttendeeEmail, stopwatch.ElapsedMilliseconds);
                    return;
                }

            // Prepare attendee details for email
            var attendeeDetails = new List<Dictionary<string, object>>();
            if (registration.HasDetailedAttendees())
            {
                foreach (var attendee in registration.Attendees)
                {
                    attendeeDetails.Add(new Dictionary<string, object>
                    {
                        { "Name", attendee.Name },
                        { "AgeCategory", attendee.AgeCategory.ToString() },
                        { "Gender", attendee.Gender?.ToString() ?? "" }
                    });
                }
            }

            // Get contact name from first attendee or fallback
            var contactName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                ? registration.Attendees.First().Name
                : "Guest";

            // Prepare email parameters with enhanced attendee details
            var parameters = new Dictionary<string, object>
            {
                { "UserName", contactName },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "EventEndDate", @event.EndDate.ToString("MMMM dd, yyyy") },
                { "EventLocation", @event.Location != null ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}" : "Online Event" },
                { "Quantity", domainEvent.Quantity },
                { "RegistrationDate", domainEvent.RegistrationDate.ToString("MMMM dd, yyyy h:mm tt") },
                // Add attendee details
                { "Attendees", attendeeDetails },
                { "HasAttendeeDetails", attendeeDetails.Any() }
            };

            // Add contact information if available
            if (registration.Contact != null)
            {
                parameters["ContactEmail"] = registration.Contact.Email;
                parameters["ContactPhone"] = registration.Contact.PhoneNumber ?? "";
                parameters["HasContactInfo"] = true;
            }
            else
            {
                parameters["HasContactInfo"] = false;
            }

                // Send templated email to the attendee email
                var result = await _emailService.SendTemplatedEmailAsync(
                    EmailTemplateNames.AnonymousRsvpConfirmation,
                    domainEvent.AttendeeEmail,
                    parameters,
                    cancellationToken);

                stopwatch.Stop();

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "AnonymousRegistrationConfirmed FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.AttendeeEmail, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "AnonymousRegistrationConfirmed COMPLETE: Email sent successfully - Email={Email}, AttendeeCount={AttendeeCount}, Duration={ElapsedMs}ms",
                        domainEvent.AttendeeEmail, attendeeDetails.Count, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "AnonymousRegistrationConfirmed CANCELED: Operation was canceled - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.AttendeeEmail, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "AnonymousRegistrationConfirmed FAILED: Exception occurred - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.AttendeeEmail, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
