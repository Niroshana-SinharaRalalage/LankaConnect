using System.Diagnostics;
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
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.SendEventNotification;

/// <summary>
/// Handler for SendEventNotification command
/// Queues background job to send event notification emails
/// Phase 6A.61: Background job integration
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
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
        using (LogContext.PushProperty("Operation", "SendEventNotification"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SendEventNotification START: EventId={EventId}",
                request.EventId);

            try
            {
                // Check for cancellation at the start
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Fetch event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SendEventNotification FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<int>.Failure("Event not found");
                }

                _logger.LogInformation(
                    "SendEventNotification: Event loaded - EventId={EventId}, Title={Title}, Status={Status}, OrganizerId={OrganizerId}",
                    @event.Id, @event.Title.Value, @event.Status, @event.OrganizerId);

                // 2. Authorization: Verify organizer
                var userId = _currentUserService.UserId;
                if (@event.OrganizerId != userId)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SendEventNotification FAILED: Permission denied - EventId={EventId}, UserId={UserId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.EventId, userId, @event.OrganizerId, stopwatch.ElapsedMilliseconds);

                    return Result<int>.Failure("You are not authorized to send notifications for this event");
                }

                // 3. Verify event status (Active or Published only)
                if (@event.Status != EventStatus.Active && @event.Status != EventStatus.Published)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SendEventNotification FAILED: Invalid status - EventId={EventId}, Status={Status}, Duration={ElapsedMs}ms",
                        request.EventId, @event.Status, stopwatch.ElapsedMilliseconds);

                    return Result<int>.Failure($"Cannot send notifications for events with status: {@event.Status}");
                }

                // 4. Create history record (placeholder counts, updated by background job)
                var historyResult = EventNotificationHistory.Create(request.EventId, userId, 0);
                if (!historyResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SendEventNotification FAILED: History creation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, historyResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<int>.Failure(historyResult.Error);
                }

                var history = await _historyRepository.AddAsync(historyResult.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "SendEventNotification: History record created - HistoryId={HistoryId}, EventId={EventId}",
                    history.Id, request.EventId);

                // 5. Queue background job (pass historyId to update stats later)
                var jobId = _backgroundJobClient.Enqueue<EventNotificationEmailJob>(
                    job => job.ExecuteAsync(history.Id, CancellationToken.None));

                _logger.LogInformation(
                    "SendEventNotification: Background job queued - JobId={JobId}, HistoryId={HistoryId}, EventId={EventId}",
                    jobId, history.Id, request.EventId);

                stopwatch.Stop();

                _logger.LogInformation(
                    "SendEventNotification COMPLETE: EventId={EventId}, HistoryId={HistoryId}, JobId={JobId}, Duration={ElapsedMs}ms",
                    request.EventId, history.Id, jobId, stopwatch.ElapsedMilliseconds);

                // 6. Return success (count updated by background job)
                return Result<int>.Success(0); // Count updated by background job
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "SendEventNotification FAILED: Unexpected error - EventId={EventId}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
