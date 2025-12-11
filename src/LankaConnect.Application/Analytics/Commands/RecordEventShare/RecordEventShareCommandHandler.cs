using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Analytics;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Analytics.Commands.RecordEventShare;

/// <summary>
/// Handler for RecordEventShareCommand
/// Records social shares for event analytics and tracking
/// </summary>
public class RecordEventShareCommandHandler : ICommandHandler<RecordEventShareCommand>
{
    private readonly IEventAnalyticsRepository _analyticsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecordEventShareCommandHandler(
        IEventAnalyticsRepository analyticsRepository,
        IUnitOfWork unitOfWork)
    {
        _analyticsRepository = analyticsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RecordEventShareCommand request, CancellationToken cancellationToken)
    {
        // Validation
        if (request.EventId == Guid.Empty)
            return Result.Failure("Event ID cannot be empty");

        // Get or create analytics record
        var analytics = await _analyticsRepository.GetByEventIdAsync(request.EventId, cancellationToken);

        if (analytics == null)
        {
            // Create new analytics record
            analytics = EventAnalytics.Create(request.EventId);
            analytics.RecordShare();
            await _analyticsRepository.AddAsync(analytics, cancellationToken);
        }
        else
        {
            // Update existing analytics
            analytics.RecordShare();
            _analyticsRepository.Update(analytics);
        }

        // Commit transaction
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
