using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Users.Queries.GetAdminUsersPaged;

/// <summary>
/// Query to get paginated list of users for admin management
/// Phase 6A.90: Admin User Management
/// </summary>
public record GetAdminUsersPagedQuery : IQuery<PagedResultDto<AdminUserDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public UserRole? RoleFilter { get; init; }
    public bool? IsActiveFilter { get; init; }
}
