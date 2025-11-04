using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Analytics.Common;
using LankaConnect.Domain.Analytics;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Analytics.Queries.GetOrganizerDashboard;

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
