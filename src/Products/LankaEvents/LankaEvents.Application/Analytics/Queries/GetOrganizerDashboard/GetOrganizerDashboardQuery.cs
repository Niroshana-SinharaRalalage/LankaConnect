using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Analytics.Common;
namespace LankaConnect.Products.LankaEvents.Application.Analytics.Queries.GetOrganizerDashboard;

/// <summary>
/// Query to get aggregated analytics for an organizer
/// Used for organizer dashboard
/// </summary>
public record GetOrganizerDashboardQuery(Guid OrganizerId) : IQuery<OrganizerDashboardDto?>;
