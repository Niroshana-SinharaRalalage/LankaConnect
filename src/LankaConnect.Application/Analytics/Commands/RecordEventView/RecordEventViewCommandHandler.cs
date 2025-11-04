using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Analytics;
using LankaConnect.Domain.Common;

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

    // Deduplication window: 5 minutes (don't count multiple views from same user/IP within 5 min)
    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromMinutes(5);

    public RecordEventViewCommandHandler(
        IEventAnalyticsRepository analyticsRepository,
        IEventViewRecordRepository viewRecordRepository,
        IUnitOfWork unitOfWork)
    {
        _analyticsRepository = analyticsRepository;
        _viewRecordRepository = viewRecordRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RecordEventViewCommand request, CancellationToken cancellationToken)
    {
        // Validation
        if (request.EventId == Guid.Empty)
            return Result.Failure("Event ID cannot be empty");

        if (string.IsNullOrWhiteSpace(request.IpAddress))
            return Result.Failure("IP address is required");

        // Check if we should count this view (deduplication)
        var shouldCount = await _analyticsRepository.ShouldCountViewAsync(
            request.EventId,
            request.UserId,
            request.IpAddress,
            DeduplicationWindow,
            cancellationToken);

        // Get or create analytics record
        var analytics = await _analyticsRepository.GetByEventIdAsync(request.EventId, cancellationToken);

        if (analytics == null)
        {
            // Create new analytics record
            analytics = EventAnalytics.Create(request.EventId);

            if (shouldCount)
            {
                analytics.RecordView(request.UserId, request.IpAddress);
            }

            await _analyticsRepository.AddAsync(analytics, cancellationToken);
        }
        else
        {
            // Update existing analytics (only if should count)
            if (shouldCount)
            {
                analytics.RecordView(request.UserId, request.IpAddress);
                _analyticsRepository.Update(analytics);
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

        // Commit transaction
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
