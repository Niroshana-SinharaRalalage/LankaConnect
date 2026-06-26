using LankaConnect.Modules.Identity.Contracts; // W4.7.d.2
using System.Diagnostics;
using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Shared.Email.Contracts;
using OrganizerContactInfo = LankaConnect.Shared.Email.Helpers.OrganizerContactInfo;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.51+: Handles CommitmentUpdatedEvent to send update confirmation email to user
/// when they change their commitment quantity or details.
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class CommitmentUpdatedEventHandler : INotificationHandler<DomainEventNotification<CommitmentUpdatedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IIdentityQueries _identityQueries;
    private readonly IEventRepository _eventRepository;
    private readonly IFormQueries _formQueries;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<CommitmentUpdatedEventHandler> _logger;

    public CommitmentUpdatedEventHandler(
        IServiceScopeFactory scopeFactory,
        IIdentityQueries identityQueries,
        IEventRepository eventRepository,
        IFormQueries formQueries,
        IEmailUrlHelper emailUrlHelper,
        ILogger<CommitmentUpdatedEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _identityQueries = identityQueries;
        _eventRepository = eventRepository;
        _formQueries = formQueries;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<CommitmentUpdatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "CommitmentUpdated"))
        using (LogContext.PushProperty("EntityType", "SignUpCommitment"))
        using (LogContext.PushProperty("UserId", domainEvent.UserId))
        using (LogContext.PushProperty("SignUpItemId", domainEvent.SignUpItemId))
        {
            var stopwatch = Stopwatch.StartNew();

            // Phase 6A.121: Support dual nullable fields (PhysicalQuantity or SlotsClaimed)
            var oldQuantity = domainEvent.OldPhysicalQuantity ?? domainEvent.OldSlotsClaimed ?? 0;
            var newQuantity = domainEvent.NewPhysicalQuantity ?? domainEvent.NewSlotsClaimed ?? 0;
            var quantityType = domainEvent.NewPhysicalQuantity.HasValue ? "units" : "slots";

            _logger.LogInformation(
                "CommitmentUpdated START: UserId={UserId}, ItemDescription={ItemDescription}, OldQuantity={OldQuantity}, NewQuantity={NewQuantity} ({QuantityType})",
                domainEvent.UserId, domainEvent.ItemDescription, oldQuantity, newQuantity, quantityType);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user details
                var user = await _identityQueries.GetContactInfoAsync(domainEvent.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CommitmentUpdated: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        domainEvent.UserId, stopwatch.ElapsedMilliseconds);
                    return; // Fail-silent: don't throw to prevent transaction rollback
                }

                // Get event details via repository navigation method
                var @event = await _eventRepository.GetEventBySignUpItemIdAsync(domainEvent.SignUpItemId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CommitmentUpdated: Event not found - SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                        domainEvent.SignUpItemId, stopwatch.ElapsedMilliseconds);
                    return; // Fail-silent
                }

                // Phase 7C.2: Project event's primary + optional secondary location into the
                // 8 decomposed email keys. Fixes GPS-coordinate leak from @event.Location?.ToString().
                var locationProjection = @event.ProjectEmailLocation();

                // Phase 6A.87: Use typed email parameters for compile-time safety
                // Phase 6A.121: Use whichever quantity field is populated (PhysicalQuantity or SlotsClaimed)
                var emailParams = SignupCommitmentEmailParams.CreateUpdate(
                    userId: user.Id,
                    userName: user.FirstName,
                    userEmail: user.Email,
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Untitled Event",
                    signupItem: domainEvent.ItemDescription,
                    quantity: newQuantity,  // Phase 6A.121: Calculated from dual fields above
                    eventStartDate: @event.StartDate.GetValueOrDefault(), // Phase 8YA-2 TODO: param class should accept DateTime?
                    timeZoneId: @event.TimeZoneId,
                    eventLocation: locationProjection.LegacyFlatString,
                    eventDetailsUrl: $"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups"
                );

                // Phase 7C.2: Populate decomposed LocationName / LocationAddress / secondary block fields.
                emailParams.WithLocationDetails(locationProjection);

                // Phase 6A.122: Set OldQuantity and NewQuantity for update email template
                // Bug fix: These were calculated (lines 55-56) but never assigned to emailParams,
                // causing the email to show "PREVIOUS QUANTITY: 0, NEW QUANTITY: 0" always.
                emailParams.OldQuantity = oldQuantity;
                emailParams.NewQuantity = newQuantity;

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
                var forms = await _formQueries.GetByOwnerAsync(FormOwnerEntityTypeDto.Event, @event.Id, cancellationToken);
                var hasActiveForms = forms.Any(f => f.Status == FormStatusDto.Active);
                if (hasActiveForms)
                {
                    emailParams.WithSignupForms($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#signup-forms");
                }

                // Phase 6A.122: Fire-and-forget email - don't block HTTP response waiting for email
                // Root cause of slow signup operations: Azure Communication Services takes 2-16 seconds
                // User should not wait for email to send before seeing success
                stopwatch.Stop();
                _logger.LogInformation(
                    "CommitmentUpdated COMPLETE: Signup updated successfully, dispatching email async - UserId={UserId}, OldQty={OldQty}, NewQty={NewQty}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, oldQuantity, newQuantity, stopwatch.ElapsedMilliseconds);

                // Phase 6A.127: Create new DI scope for fire-and-forget email dispatch.
                // The HTTP request scope (and its DbContext) is disposed by the time Task.Run executes,
                // causing ObjectDisposedException in EmailTemplateRepository.GetByNameAsync().
                var capturedParams = emailParams;
                var capturedEmail = user.Email;
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
                                "CommitmentUpdated EMAIL SENT: Email={Email}, EventId={EventId}",
                                capturedEmail, capturedEventId);
                        }
                        else
                        {
                            _logger.LogError(
                                "CommitmentUpdated EMAIL FAILED: Email={Email}, Errors={Errors}",
                                capturedEmail, string.Join(", ", emailResult.Errors));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "CommitmentUpdated EMAIL EXCEPTION: Email={Email}, EventId={EventId}",
                            capturedEmail, capturedEventId);
                    }
                }, CancellationToken.None);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "CommitmentUpdated CANCELED: Operation was canceled - UserId={UserId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpItemId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "CommitmentUpdated FAILED: Exception occurred - UserId={UserId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpItemId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
