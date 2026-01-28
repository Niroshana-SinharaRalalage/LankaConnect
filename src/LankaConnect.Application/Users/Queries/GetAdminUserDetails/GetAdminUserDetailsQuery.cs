using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;

namespace LankaConnect.Application.Users.Queries.GetAdminUserDetails;

/// <summary>
/// Query to get detailed user information for admin view
/// Phase 6A.90: Admin User Management
/// </summary>
public record GetAdminUserDetailsQuery : IQuery<AdminUserDetailsDto>
{
    public Guid UserId { get; init; }

    public GetAdminUserDetailsQuery(Guid userId)
    {
        UserId = userId;
    }
}
