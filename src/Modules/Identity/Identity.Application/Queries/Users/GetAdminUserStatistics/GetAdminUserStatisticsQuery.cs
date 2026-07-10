using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Identity.Application.DTOs;
namespace LankaConnect.Modules.Identity.Application.Queries.Users.GetAdminUserStatistics;

/// <summary>
/// Query to get user statistics for admin dashboard
/// Phase 6A.90: Admin User Management
/// </summary>
public record GetAdminUserStatisticsQuery : IQuery<AdminUserStatisticsDto>
{
}
