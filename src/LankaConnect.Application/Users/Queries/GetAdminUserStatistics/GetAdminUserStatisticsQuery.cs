using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;

namespace LankaConnect.Application.Users.Queries.GetAdminUserStatistics;

/// <summary>
/// Query to get user statistics for admin dashboard
/// Phase 6A.90: Admin User Management
/// </summary>
public record GetAdminUserStatisticsQuery : IQuery<AdminUserStatisticsDto>
{
}
