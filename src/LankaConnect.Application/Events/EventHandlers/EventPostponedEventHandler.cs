using LankaConnect.Modules.Identity.Contracts; // W4.6.d.2.b: IUserRepository -> IIdentityQueries/IIdentityCommands
using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.100: Handles EventPostponedEvent to send postponement notifications to all registered attendees.
/// Migrated from inline HTML to ITypedEmailService with EventPostponedEmailParams.
/// </summary>
public class EventPostponedEventHandler : INotificationHandler<DomainEventNotification<EventPostponedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IIdentityQueries _identityQueries;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILogger<EventPostponedEventHandler> _logger;

    public EventPostponedEventHandler(
        ITypedEmailService typedEmailService,
        IIdentityQueries identityQueries,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ILogger<EventPostponedEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _identityQueries = identityQueries;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EventPostponedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "EventPostponed"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "EventPostponed START: EventId={EventId}, PostponedAt={PostponedAt}, Reason={Reason}",
                domainEvent.EventId, domainEvent.PostponedAt, domainEvent.Reason);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Retrieve event data
                _logger.LogInformation(
                    "EventPostponed: Loading event - EventId={EventId}",
                    domainEvent.EventId);

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "EventPostponed: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "EventPostponed: Event loaded - EventTitle={EventTitle}",
                    @event.Title.Value);

                // Get all confirmed registrations for this event
                var registrations = await _registrationRepository.GetByEventAsync(domainEvent.EventId, cancellationToken);
                var confirmedRegistrations = registrations
                    .Where(r => r.Status == RegistrationStatus.Confirmed)
                    .ToList();

                if (!confirmedRegistrations.Any())
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "EventPostponed: No confirmed registrations found, skipping notifications - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "EventPostponed: Found confirmed registrations - Count={Count}",
                    confirmedRegistrations.Count);

                // Phase 6A.100: Send typed emails individually instead of bulk with inline HTML
                int successCount = 0;
                int failCount = 0;

                foreach (var registration in confirmedRegistrations)
                {
                    // Skip anonymous registrations - they don't have email in user repository
                    if (!registration.UserId.HasValue)
                    {
                        _logger.LogInformation(
                            "EventPostponed: Skipping anonymous registration - RegistrationId={RegistrationId}",
                            registration.Id);
                        continue;
                    }

                    var user = await _identityQueries.GetContactInfoAsync(registration.UserId.Value, cancellationToken);
                    if (user == null)
                    {
                        _logger.LogWarning(
                            "EventPostponed: User not found for registration - UserId={UserId}, RegistrationId={RegistrationId}",
                            registration.UserId.Value, registration.Id);
                        continue;
                    }

                    try
                    {
                        // Phase 6A.100: Use typed email params instead of inline HTML
                        var emailParams = EventPostponedEmailParams.Create(
                            userId: user.Id,
                            userName: $"{user.FirstName} {user.LastName}",
                            userEmail: user.Email,
                            eventId: @event.Id,
                            eventTitle: @event.Title.Value,
                            originalStartDate: domainEvent.PostponedAt,
                            timeZoneId: @event.TimeZoneId,
                            reason: domainEvent.Reason,
                            postponedAt: domainEvent.PostponedAt);

                        var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                        if (result.Success)
                        {
                            successCount++;
                            _logger.LogDebug(
                                "EventPostponed: Email sent to {Email} - Duration={DurationMs}ms",
                                user.Email, result.DurationMs);
                        }
                        else
                        {
                            failCount++;
                            _logger.LogWarning(
                                "EventPostponed: Failed to send email to {Email} - Errors={Errors}",
                                user.Email, string.Join(", ", result.Errors));
                        }
                    }
                    catch (Exception emailEx)
                    {
                        failCount++;
                        _logger.LogError(emailEx,
                            "EventPostponed: Exception sending email to {Email}",
                            user.Email);
                    }
                }

                stopwatch.Stop();

                if (successCount == 0 && failCount > 0)
                {
                    _logger.LogError(
                        "EventPostponed FAILED: All emails failed - EventId={EventId}, Failed={Failed}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, failCount, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "EventPostponed COMPLETE: Emails sent - EventId={EventId}, Success={Success}, Failed={Failed}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, successCount, failCount, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "EventPostponed CANCELED: Operation was canceled - EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "EventPostponed FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }
    }
}
