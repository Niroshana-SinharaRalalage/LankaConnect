using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Analytics;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Analytics.Commands.RecordEventShare;

/// <summary>
/// Handler for RecordEventShareCommand
/// Records social shares for event analytics and tracking
/// </summary>
public class RecordEventShareCommandHandler : ICommandHandler<RecordEventShareCommand>
{
    private readonly IEventAnalyticsRepository _analyticsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecordEventShareCommandHandler> _logger;

    public RecordEventShareCommandHandler(
        IEventAnalyticsRepository analyticsRepository,
        IUnitOfWork unitOfWork,
        ILogger<RecordEventShareCommandHandler> logger)
    {
        _analyticsRepository = analyticsRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RecordEventShareCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RecordEventShare"))
        using (LogContext.PushProperty("EntityType", "EventAnalytics"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RecordEventShare START: EventId={EventId}",
                request.EventId);

            try
            {
                // Validation
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RecordEventShare FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event ID cannot be empty");
                }

                // Get or create analytics record
                var analytics = await _analyticsRepository.GetByEventIdAsync(request.EventId, cancellationToken);

                bool isNewRecord = analytics == null;

                if (analytics == null)
                {
                    // Create new analytics record
                    analytics = EventAnalytics.Create(request.EventId);
                    analytics.RecordShare();
                    await _analyticsRepository.AddAsync(analytics, cancellationToken);

                    _logger.LogInformation(
                        "RecordEventShare: Created new analytics record - EventId={EventId}",
                        request.EventId);
                }
                else
                {
                    // Update existing analytics
                    analytics.RecordShare();
                    _analyticsRepository.Update(analytics);

                    _logger.LogInformation(
                        "RecordEventShare: Updated existing analytics record - EventId={EventId}",
                        request.EventId);
                }

                // Commit transaction
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RecordEventShare COMPLETE: EventId={EventId}, IsNewRecord={IsNewRecord}, Duration={ElapsedMs}ms",
                    request.EventId, isNewRecord, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RecordEventShare FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
