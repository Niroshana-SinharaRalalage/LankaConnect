using System.Diagnostics;
using Hangfire;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.SendEventReminder;

/// <summary>
/// Handler for SendEventReminder command
/// Allows organizers to manually trigger reminder emails from the Communications tab
/// Phase 6A.76: Manual reminder triggering feature
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// </summary>
public class SendEventReminderCommandHandler : IRequestHandler<SendEventReminderCommand, Result<int>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IEventReminderRepository _eventReminderRepository;
    private readonly ILogger<SendEventReminderCommandHandler> _logger;

    public SendEventReminderCommandHandler(
        IEventRepository eventRepository,
        ICurrentUserService currentUserService,
        IBackgroundJobClient backgroundJobClient,
        IEventReminderRepository eventReminderRepository,
        ILogger<SendEventReminderCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _currentUserService = currentUserService;
        _backgroundJobClient = backgroundJobClient;
        _eventReminderRepository = eventReminderRepository;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(SendEventReminderCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SendEventReminder"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("ReminderType", request.ReminderType))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SendEventReminder START: EventId={EventId}, ReminderType={ReminderType}",
                request.EventId, request.ReminderType);

            try
            {
                // Check for cancellation at the start
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Fetch event with registrations to get count
                var @event = await _eventRepository.GetWithRegistrationsAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SendEventReminder FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<int>.Failure("Event not found");
                }

                _logger.LogInformation(
                    "SendEventReminder: Event loaded - EventId={EventId}, Title={Title}, Status={Status}, OrganizerId={OrganizerId}",
                    @event.Id, @event.Title.Value, @event.Status, @event.OrganizerId);

                // 2. Authorization: Verify organizer or admin
                var userId = _currentUserService.UserId;
                var isAdmin = _currentUserService.IsAdmin;

                if (@event.OrganizerId != userId && !isAdmin)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SendEventReminder FAILED: Permission denied - EventId={EventId}, UserId={UserId}, OrganizerId={OrganizerId}, IsAdmin={IsAdmin}, Duration={ElapsedMs}ms",
                        request.EventId, userId, @event.OrganizerId, isAdmin, stopwatch.ElapsedMilliseconds);

                    return Result<int>.Failure("You are not authorized to send reminders for this event");
                }

                // 3. Verify event status (Active or Published only)
                if (@event.Status != EventStatus.Active && @event.Status != EventStatus.Published)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SendEventReminder FAILED: Invalid status - EventId={EventId}, Status={Status}, Duration={ElapsedMs}ms",
                        request.EventId, @event.Status, stopwatch.ElapsedMilliseconds);

                    return Result<int>.Failure($"Cannot send reminders for events with status: {@event.Status}");
                }

                // 4. Get registration count
                var registrations = @event.Registrations;
                if (registrations == null || registrations.Count == 0)
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "SendEventReminder: No registrations - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<int>.Success(0);
                }

                _logger.LogInformation(
                    "SendEventReminder: Registrations loaded - EventId={EventId}, TotalRegistrations={Count}",
                    request.EventId, registrations.Count);

                // 5. Validate reminder type
                var validReminderTypes = new[] { "1day", "2day", "7day", "custom" };
                var reminderType = validReminderTypes.Contains(request.ReminderType)
                    ? request.ReminderType
                    : "custom";

                // 6. Check idempotency before queuing - count how many will actually be sent
                var eligibleCount = 0;
                foreach (var registration in registrations)
                {
                    if (registration == null || registration.Id == Guid.Empty) continue;

                    var alreadySent = await _eventReminderRepository.IsReminderAlreadySentAsync(
                        request.EventId, registration.Id, reminderType, cancellationToken);

                    if (!alreadySent)
                    {
                        eligibleCount++;
                    }
                }

                _logger.LogInformation(
                    "SendEventReminder: Idempotency check complete - EventId={EventId}, Eligible={Eligible}, Total={Total}",
                    request.EventId, eligibleCount, registrations.Count);

                if (eligibleCount == 0)
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "SendEventReminder: All reminders already sent - EventId={EventId}, Count={Count}, ReminderType={ReminderType}, Duration={ElapsedMs}ms",
                        request.EventId, registrations.Count, reminderType, stopwatch.ElapsedMilliseconds);

                    // Return -1 to indicate all were already sent (frontend will show appropriate message)
                    return Result<int>.Success(-1);
                }

                // 7. Queue background job to send reminders
                var jobId = _backgroundJobClient.Enqueue<EventReminderJob>(
                    job => job.SendRemindersForEventAsync(request.EventId, reminderType, CancellationToken.None));

                _logger.LogInformation(
                    "SendEventReminder: Background job queued - JobId={JobId}, EventId={EventId}, ReminderType={ReminderType}, Eligible={Eligible}",
                    jobId, request.EventId, reminderType, eligibleCount);

                stopwatch.Stop();

                _logger.LogInformation(
                    "SendEventReminder COMPLETE: EventId={EventId}, ReminderType={ReminderType}, JobId={JobId}, Eligible={Eligible}/{Total}, Duration={ElapsedMs}ms",
                    request.EventId, reminderType, jobId, eligibleCount, registrations.Count, stopwatch.ElapsedMilliseconds);

                // 8. Return eligible count (how many will actually receive the reminder)
                return Result<int>.Success(eligibleCount);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "SendEventReminder FAILED: Unexpected error - EventId={EventId}, ReminderType={ReminderType}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.EventId, request.ReminderType, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
