using Hangfire;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.SendEventReminder;

/// <summary>
/// Phase 6A.76: Handler for SendEventReminder command.
/// Allows organizers to manually trigger reminder emails from the Communications tab.
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
        try
        {
            _logger.LogInformation(
                "[Phase 6A.76] Sending manual event reminder for event {EventId}, type={ReminderType}",
                request.EventId, request.ReminderType);

            // 1. Fetch event with registrations to get count
            var @event = await _eventRepository.GetWithRegistrationsAsync(request.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("[Phase 6A.76] Event {EventId} not found", request.EventId);
                return Result<int>.Failure("Event not found");
            }

            // 2. Authorization: Verify organizer or admin
            var userId = _currentUserService.UserId;
            var isAdmin = _currentUserService.IsAdmin;

            if (@event.OrganizerId != userId && !isAdmin)
            {
                _logger.LogWarning(
                    "[Phase 6A.76] Unauthorized attempt to send reminder by user {UserId} for event {EventId}",
                    userId, request.EventId);
                return Result<int>.Failure("You are not authorized to send reminders for this event");
            }

            // 3. Verify event status (Active or Published only)
            if (@event.Status != EventStatus.Active && @event.Status != EventStatus.Published)
            {
                _logger.LogWarning(
                    "[Phase 6A.76] Attempt to send reminder for event {EventId} with status {Status}",
                    request.EventId, @event.Status);
                return Result<int>.Failure($"Cannot send reminders for events with status: {@event.Status}");
            }

            // 4. Get registration count
            var registrations = @event.Registrations;
            if (registrations == null || registrations.Count == 0)
            {
                _logger.LogInformation(
                    "[Phase 6A.76] Event {EventId} has no registrations, no reminders to send",
                    request.EventId);
                return Result<int>.Success(0);
            }

            // 5. Validate reminder type
            var validReminderTypes = new[] { "1day", "2day", "7day", "custom" };
            var reminderType = validReminderTypes.Contains(request.ReminderType)
                ? request.ReminderType
                : "custom";

            // 6. Phase 6A.76: Check idempotency before queuing - count how many will actually be sent
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

            if (eligibleCount == 0)
            {
                _logger.LogInformation(
                    "[Phase 6A.76] All {Count} attendees have already received {ReminderType} reminder for event {EventId}",
                    registrations.Count, reminderType, request.EventId);

                // Return -1 to indicate all were already sent (frontend will show appropriate message)
                return Result<int>.Success(-1);
            }

            // 7. Queue background job to send reminders
            var jobId = _backgroundJobClient.Enqueue<EventReminderJob>(
                job => job.SendRemindersForEventAsync(request.EventId, reminderType, CancellationToken.None));

            _logger.LogInformation(
                "[Phase 6A.76] Queued manual reminder job {JobId} for event {EventId}, type={ReminderType}, eligible={Eligible}/{Total}",
                jobId, request.EventId, reminderType, eligibleCount, registrations.Count);

            // 8. Return eligible count (how many will actually receive the reminder)
            return Result<int>.Success(eligibleCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.76] Error queueing manual reminder - EventId: {EventId}, ReminderType: {ReminderType}",
                request.EventId, request.ReminderType);

            return Result<int>.Failure($"Failed to send reminder. Error: {ex.Message}");
        }
    }
}
