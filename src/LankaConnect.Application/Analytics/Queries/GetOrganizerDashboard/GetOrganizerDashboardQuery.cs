using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Analytics.Common;

namespace LankaConnect.Application.Analytics.Queries.GetOrganizerDashboard;

/// <summary>
/// Query to get aggregated analytics for an organizer
/// Used for organizer dashboard
/// </summary>
public record GetOrganizerDashboardQuery(Guid OrganizerId) : IQuery<OrganizerDashboardDto?>;
