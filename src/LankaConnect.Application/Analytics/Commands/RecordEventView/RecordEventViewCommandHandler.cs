using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Analytics;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Analytics.Commands.RecordEventView;

/// <summary>
/// Handler for RecordEventViewCommand
/// Records event views with deduplication and creates analytics records
/// Implements fire-and-forget pattern for non-blocking performance
/// </summary>
public class RecordEventViewCommandHandler : ICommandHandler<RecordEventViewCommand>
{
    private readonly IEventAnalyticsRepository _analyticsRepository;
    private readonly IEventViewRecordRepository _viewRecordRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecordEventViewCommandHandler> _logger;

    // Deduplication window: 5 minutes (don't count multiple views from same user/IP within 5 min)
    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromMinutes(5);

    public RecordEventViewCommandHandler(
        IEventAnalyticsRepository analyticsRepository,
        IEventViewRecordRepository viewRecordRepository,
        IUnitOfWork unitOfWork,
        ILogger<RecordEventViewCommandHandler> logger)
    {
        _analyticsRepository = analyticsRepository;
        _viewRecordRepository = viewRecordRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RecordEventViewCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RecordEventView"))
        using (LogContext.PushProperty("EntityType", "EventAnalytics"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("IpAddress", request.IpAddress ?? "unknown"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RecordEventView START: EventId={EventId}, UserId={UserId}, IpAddress={IpAddress}",
                request.EventId, request.UserId, request.IpAddress ?? "unknown");

            try
            {
                // Validation
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RecordEventView FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event ID cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(request.IpAddress))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RecordEventView FAILED: Missing IP address - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("IP address is required");
                }

                // Check if we should count this view (deduplication)
                var shouldCount = await _analyticsRepository.ShouldCountViewAsync(
                    request.EventId,
                    request.UserId,
                    request.IpAddress,
                    DeduplicationWindow,
                    cancellationToken);

                _logger.LogInformation(
                    "RecordEventView: Deduplication check - EventId={EventId}, ShouldCount={ShouldCount}, DeduplicationWindow={WindowMinutes}min",
                    request.EventId, shouldCount, DeduplicationWindow.TotalMinutes);

                // Get or create analytics record
                var analytics = await _analyticsRepository.GetByEventIdAsync(request.EventId, cancellationToken);

                bool isNewRecord = analytics == null;

                if (analytics == null)
                {
                    // Create new analytics record
                    analytics = EventAnalytics.Create(request.EventId);

                    if (shouldCount)
                    {
                        analytics.RecordView(request.UserId, request.IpAddress);
                    }

                    await _analyticsRepository.AddAsync(analytics, cancellationToken);

                    _logger.LogInformation(
                        "RecordEventView: Created new analytics record - EventId={EventId}, ViewCounted={ViewCounted}",
                        request.EventId, shouldCount);
                }
                else
                {
                    // Update existing analytics (only if should count)
                    if (shouldCount)
                    {
                        analytics.RecordView(request.UserId, request.IpAddress);
                        _analyticsRepository.Update(analytics);

                        _logger.LogInformation(
                            "RecordEventView: Updated analytics record - EventId={EventId}, ViewCounted={ViewCounted}",
                            request.EventId, shouldCount);
                    }
                }

                // Always create view record for detailed tracking (even if duplicate)
                var viewRecord = new EventViewRecord
                {
                    Id = Guid.NewGuid(),
                    EventId = request.EventId,
                    UserId = request.UserId,
                    IpAddress = request.IpAddress,
                    UserAgent = request.UserAgent,
                    ViewedAt = DateTime.UtcNow
                };

                await _viewRecordRepository.AddAsync(viewRecord, cancellationToken);

                _logger.LogInformation(
                    "RecordEventView: View record created - EventId={EventId}, RecordId={RecordId}",
                    request.EventId, viewRecord.Id);

                // Commit transaction
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RecordEventView COMPLETE: EventId={EventId}, IsNewRecord={IsNewRecord}, ViewCounted={ViewCounted}, Duration={ElapsedMs}ms",
                    request.EventId, isNewRecord, shouldCount, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RecordEventView FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
