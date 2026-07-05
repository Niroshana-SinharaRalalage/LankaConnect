using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Analytics.Common;
using LankaConnect.Products.LankaEvents.Domain.Analytics;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Application.Analytics.Queries.GetOrganizerDashboard;

/// <summary>
/// Handler for GetOrganizerDashboardQuery
/// Retrieves aggregated analytics for an organizer
/// </summary>
public class GetOrganizerDashboardQueryHandler : IQueryHandler<GetOrganizerDashboardQuery, OrganizerDashboardDto?>
{
    private readonly IEventAnalyticsRepository _analyticsRepository;

    public GetOrganizerDashboardQueryHandler(IEventAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<Result<OrganizerDashboardDto?>> Handle(GetOrganizerDashboardQuery request, CancellationToken cancellationToken)
    {
        if (request.OrganizerId == Guid.Empty)
            return Result<OrganizerDashboardDto?>.Failure("Organizer ID cannot be empty");

        var dashboardData = await _analyticsRepository.GetOrganizerDashboardDataAsync(request.OrganizerId, cancellationToken);

        if (dashboardData == null)
            return Result<OrganizerDashboardDto?>.Success(null);

        var dto = new OrganizerDashboardDto
        {
            OrganizerId = dashboardData.OrganizerId,
            TotalEvents = dashboardData.TotalEvents,
            TotalViews = dashboardData.TotalViews,
            TotalUniqueViewers = dashboardData.TotalUniqueViewers,
            TotalRegistrations = dashboardData.TotalRegistrations,
            AverageConversionRate = dashboardData.AverageConversionRate,
            LastActivityAt = dashboardData.LastActivityAt,
            TopEvents = dashboardData.TopEvents.Select(e => new EventAnalyticsSummaryDto
            {
                EventId = e.EventId,
                Title = e.Title,
                EventDate = e.EventDate,
                Views = e.Views,
                Registrations = e.Registrations,
                ConversionRate = e.ConversionRate
            }).ToList(),
            UpcomingEvents = dashboardData.UpcomingEvents.Select(e => new EventAnalyticsSummaryDto
            {
                EventId = e.EventId,
                Title = e.Title,
                EventDate = e.EventDate,
                Views = e.Views,
                Registrations = e.Registrations,
                ConversionRate = e.ConversionRate
            }).ToList()
        };

        return Result<OrganizerDashboardDto?>.Success(dto);
    }
}
