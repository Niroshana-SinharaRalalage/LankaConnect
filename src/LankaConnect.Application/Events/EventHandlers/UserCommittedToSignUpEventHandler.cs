using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using OrganizerContactInfo = LankaConnect.Shared.Email.Helpers.OrganizerContactInfo;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.51: Handles UserCommittedToSignUpEvent to send confirmation email to user
/// when they commit to bringing an item to an event.
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class UserCommittedToSignUpEventHandler : INotificationHandler<DomainEventNotification<UserCommittedToSignUpEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventFormRepository _eventFormRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<UserCommittedToSignUpEventHandler> _logger;

    public UserCommittedToSignUpEventHandler(
        IServiceScopeFactory scopeFactory,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEventFormRepository eventFormRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<UserCommittedToSignUpEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _eventFormRepository = eventFormRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<UserCommittedToSignUpEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "UserCommittedToSignUp"))
        using (LogContext.PushProperty("EntityType", "SignUpCommitment"))
        using (LogContext.PushProperty("UserId", domainEvent.UserId))
        using (LogContext.PushProperty("SignUpListId", domainEvent.SignUpListId))
        {
            var stopwatch = Stopwatch.StartNew();

            // Phase 6A.121: Support dual nullable fields (PhysicalQuantity or SlotsClaimed)
            var quantity = domainEvent.PhysicalQuantity ?? domainEvent.SlotsClaimed ?? 0;
            var quantityType = domainEvent.PhysicalQuantity.HasValue ? "units" : "slots";

            _logger.LogInformation(
                "UserCommittedToSignUp START: UserId={UserId}, Quantity={Quantity} {QuantityType}, ItemDescription={ItemDescription}, SignUpListId={SignUpListId}",
                domainEvent.UserId, quantity, quantityType, domainEvent.ItemDescription, domainEvent.SignUpListId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user details. Phase 6A.140: when the commitment was created anonymously
                // (deterministic GUID — no row in Users), the lookup returns null. Previously
                // this caused fail-silent skip → anonymous committers got zero confirmation
                // email. We now fall back to the form-submitted ContactEmail / ContactName
                // carried on the domain event itself.
                var user = await _userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);

                // Resolve the recipient and greeting name for the email. For member commits
                // (smart-resolved real UserId) `user` is non-null and wins. For pure-anonymous
                // commits `user` is null but the event payload carries the typed-in contact.
                string? resolvedEmail = user?.Email.Value ?? domainEvent.ContactEmail;
                string resolvedGreetingName = user?.FirstName ?? domainEvent.ContactName ?? "there";
                Guid resolvedUserIdForEmailParams = user?.Id ?? Guid.Empty;

                if (string.IsNullOrWhiteSpace(resolvedEmail))
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UserCommittedToSignUp: No recipient email available (user lookup null AND no ContactEmail on event) - UserId={UserId}, Duration={ElapsedMs}ms",
                        domainEvent.UserId, stopwatch.ElapsedMilliseconds);
                    return; // Genuinely nothing to send — fail-silent.
                }

                if (user == null)
                {
                    _logger.LogInformation(
                        "UserCommittedToSignUp: Anonymous commit — using ContactEmail from domain event - UserId={UserId}",
                        domainEvent.UserId);
                }

                // Get event details via repository navigation method
                var @event = await _eventRepository.GetEventBySignUpListIdAsync(domainEvent.SignUpListId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UserCommittedToSignUp: Event not found - SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        domainEvent.SignUpListId, stopwatch.ElapsedMilliseconds);
                    return; // Fail-silent
                }

                // Phase 7C.2: Project event's primary + optional secondary location into the
                // 8 decomposed email keys. LegacyFlatString is the "Street, City" / "Online Event"
                // string that old templates rendered via {{EventLocation}}. Using the projection
                // here also fixes the GPS-coordinate leak — @event.Location?.ToString() returns
                // "{Street}, {City}, {State}, {ZipCode}, {Country} ({Coordinates})" which the
                // admin UI + diaspora sync still depend on, so we stop calling it at the handler.
                var locationProjection = @event.ProjectEmailLocation();

                // Phase 6A.87: Use typed email parameters for compile-time safety
                // Phase 6A.121: Use whichever quantity field is populated (PhysicalQuantity or SlotsClaimed)
                // Phase 6A.140: user / userEmail / userName resolved above to support
                // anonymous commits (real user wins, else fall back to event ContactEmail).
                var emailParams = SignupCommitmentEmailParams.CreateConfirmation(
                    userId: resolvedUserIdForEmailParams,
                    userName: resolvedGreetingName,
                    userEmail: resolvedEmail,
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
                    signupItem: domainEvent.ItemDescription,
                    quantity: quantity,  // Phase 6A.121: Calculated from dual fields above
                    eventStartDate: @event.StartDate.GetValueOrDefault(), // Phase 8YA-2 TODO: signups on TBD events — decide whether to allow + how to render
                    timeZoneId: @event.TimeZoneId,
                    eventLocation: locationProjection.LegacyFlatString,
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id)
                );

                // Phase 7C.2: Populate decomposed LocationName / LocationAddress / secondary block fields.
                emailParams.WithLocationDetails(locationProjection);

                // Phase 7D.1: Route to volunteer-specific template when the signup list is
                // a volunteer list. The Handlebars parameter shape is identical so no other
                // population logic changes.
                if (domainEvent.Kind == SignUpKind.Volunteers)
                {
                    emailParams.AsVolunteerConfirmation();
                }

                // Phase 6A.103: Add event image if available
                var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
                emailParams.WithEventImage(primaryImage?.ImageUrl ?? @event.Images.FirstOrDefault()?.ImageUrl ?? "");

                // Phase 6A.87+ Fix: Populate organizer contacts if available
                emailParams.WithOrganizerContacts(
                    @event.OrganizerContacts
                        .OrderBy(c => c.SortOrder)
                        .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                        .ToList());

                // Phase 6A.87+ Fix: Populate signup lists URL if event has signup lists
                if (@event.SignUpLists?.Count > 0)
                {
                    emailParams.WithSignUpLists($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups");
                }

                // Phase 6A.129: Add signup forms URL if event has active forms
                var forms = await _eventFormRepository.GetByEventIdAsync(@event.Id, cancellationToken);
                var hasActiveForms = forms.Any(f => f.Status == EventFormStatus.Active);
                if (hasActiveForms)
                {
                    emailParams.WithSignupForms($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#signup-forms");
                }

                // Phase 6A.122: Fire-and-forget email - don't block HTTP response waiting for email
                // Root cause of slow signup operations: Azure Communication Services takes 2-16 seconds
                stopwatch.Stop();
                _logger.LogInformation(
                    "UserCommittedToSignUp COMPLETE: Signup confirmed, dispatching email async - UserId={UserId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, @event.Id, stopwatch.ElapsedMilliseconds);

                // Phase 6A.127: Create new DI scope for fire-and-forget email dispatch.
                // The HTTP request scope (and its DbContext) is disposed by the time Task.Run executes,
                // causing ObjectDisposedException in EmailTemplateRepository.GetByNameAsync().
                // Fix: resolve a fresh ITypedEmailService from a new scope inside Task.Run.
                var capturedParams = emailParams;
                var capturedEmail = resolvedEmail;
                var capturedEventId = @event.Id;
                var capturedScopeFactory = _scopeFactory;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = capturedScopeFactory.CreateScope();
                        var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();
                        var emailResult = await emailService.SendEmailAsync(capturedParams, CancellationToken.None);
                        if (emailResult.Success)
                        {
                            _logger.LogInformation(
                                "UserCommittedToSignUp EMAIL SENT: Email={Email}, EventId={EventId}",
                                capturedEmail, capturedEventId);
                        }
                        else
                        {
                            _logger.LogError(
                                "UserCommittedToSignUp EMAIL FAILED: Email={Email}, Errors={Errors}",
                                capturedEmail, string.Join(", ", emailResult.Errors));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "UserCommittedToSignUp EMAIL EXCEPTION: Email={Email}, EventId={EventId}",
                            capturedEmail, capturedEventId);
                    }
                }, CancellationToken.None);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "UserCommittedToSignUp CANCELED: Operation was canceled - UserId={UserId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpListId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "UserCommittedToSignUp FAILED: Exception occurred - UserId={UserId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpListId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
