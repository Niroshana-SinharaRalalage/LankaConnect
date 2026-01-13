using Hangfire;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.SendEventNotification;

/// <summary>
/// Phase 6A.61: Handler for SendEventNotification command
/// Queues background job to send event notification emails
/// </summary>
public class SendEventNotificationCommandHandler : IRequestHandler<SendEventNotificationCommand, Result<int>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventNotificationHistoryRepository _historyRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendEventNotificationCommandHandler> _logger;

    public SendEventNotificationCommandHandler(
        IEventRepository eventRepository,
        ICurrentUserService currentUserService,
        IEventNotificationHistoryRepository historyRepository,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<SendEventNotificationCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _currentUserService = currentUserService;
        _historyRepository = historyRepository;
        _backgroundJobClient = backgroundJobClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(SendEventNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[Phase 6A.61] Sending event notification for event {EventId}", request.EventId);

            // 1. Fetch event
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("[Phase 6A.61] Event {EventId} not found", request.EventId);
                return Result<int>.Failure("Event not found");
            }

            // 2. Authorization: Verify organizer
            var userId = _currentUserService.UserId;
            if (@event.OrganizerId != userId)
            {
                _logger.LogWarning("[Phase 6A.61] Unauthorized attempt to send notification by user {UserId} for event {EventId}",
                    userId, request.EventId);
                return Result<int>.Failure("You are not authorized to send notifications for this event");
            }

            // 3. Verify event status (Active or Published only)
            if (@event.Status != EventStatus.Active && @event.Status != EventStatus.Published)
            {
                _logger.LogWarning("[Phase 6A.61] Attempt to send notification for event {EventId} with status {Status}",
                    request.EventId, @event.Status);
                return Result<int>.Failure($"Cannot send notifications for events with status: {@event.Status}");
            }

            // 4. Create history record (placeholder counts, updated by background job)
            var historyResult = EventNotificationHistory.Create(request.EventId, userId, 0);
            if (!historyResult.IsSuccess)
            {
                _logger.LogError("[Phase 6A.61] Failed to create history record: {Error}", historyResult.Error);
                return Result<int>.Failure(historyResult.Error);
            }

            var history = await _historyRepository.AddAsync(historyResult.Value, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("[Phase 6A.61] Created history record {HistoryId} for event {EventId}",
                history.Id, request.EventId);

            // 5. Queue background job (pass historyId to update stats later)
            var jobId = _backgroundJobClient.Enqueue<EventNotificationEmailJob>(
                job => job.ExecuteAsync(history.Id, CancellationToken.None));

            _logger.LogInformation("[Phase 6A.61] Queued notification job {JobId} for history {HistoryId}",
                jobId, history.Id);

            // 6. Return success (count updated by background job)
            return Result<int>.Success(0); // Count updated by background job
        }
        catch (Exception ex)
        {
            // Phase 6A.61 Hotfix: Enhanced error logging and messaging
            _logger.LogError(ex,
                "[Phase 6A.61 Hotfix] Error queueing notification - " +
                "EventId: {EventId}, ExceptionType: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                request.EventId, ex.GetType().FullName, ex.Message, ex.StackTrace);

            // Provide detailed error message to help debugging (safe for staging/production)
            var errorMessage = $"Failed to send notification. Error: {ex.Message}";
            return Result<int>.Failure(errorMessage);
        }
    }
}
