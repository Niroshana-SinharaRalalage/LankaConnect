using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Analytics.Common;
using LankaConnect.Products.LankaEvents.Domain.Analytics;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Application.Analytics.Queries.GetEventAnalytics;

/// <summary>
/// Handler for GetEventAnalyticsQuery
/// Retrieves analytics data for a specific event
/// </summary>
public class GetEventAnalyticsQueryHandler : IQueryHandler<GetEventAnalyticsQuery, EventAnalyticsDto?>
{
    private readonly IEventAnalyticsRepository _analyticsRepository;

    public GetEventAnalyticsQueryHandler(IEventAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<Result<EventAnalyticsDto?>> Handle(GetEventAnalyticsQuery request, CancellationToken cancellationToken)
    {
        if (request.EventId == Guid.Empty)
            return Result<EventAnalyticsDto?>.Failure("Event ID cannot be empty");

        var analytics = await _analyticsRepository.GetByEventIdAsync(request.EventId, cancellationToken);

        if (analytics == null)
            return Result<EventAnalyticsDto?>.Success(null);

        var dto = new EventAnalyticsDto
        {
            EventId = analytics.EventId,
            TotalViews = analytics.TotalViews,
            UniqueViewers = analytics.UniqueViewers,
            RegistrationCount = analytics.RegistrationCount,
            ConversionRate = analytics.ConversionRate,
            LastViewedAt = analytics.LastViewedAt,
            CreatedAt = analytics.CreatedAt,
            UpdatedAt = analytics.UpdatedAt
        };

        return Result<EventAnalyticsDto?>.Success(dto);
    }
}
