namespace LankaConnect.Modules.Identity.Application.DTOs;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Phase 6A.90: DTO for admin user management list view
/// </summary>
public record AdminUserDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string IdentityProvider { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsEmailVerified { get; init; }
    public bool IsAccountLocked { get; init; }
    public DateTime? AccountLockedUntil { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ProfilePhotoUrl { get; init; }
}

/// <summary>
/// Phase 6A.90: DTO for detailed admin user view
/// </summary>
public record AdminUserDetailsDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
    public string Role { get; init; } = string.Empty;
    public string IdentityProvider { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsEmailVerified { get; init; }
    public bool IsAccountLocked { get; init; }
    public DateTime? AccountLockedUntil { get; init; }
    public int FailedLoginAttempts { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public string? PendingUpgradeRole { get; init; }
    public DateTime? UpgradeRequestedAt { get; init; }
    public UserLocationDto? Location { get; init; }
}

/// <summary>
/// Phase 6A.90: DTO for admin user statistics
/// </summary>
public record AdminUserStatisticsDto
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int InactiveUsers { get; init; }
    public int LockedAccounts { get; init; }
    public int UnverifiedEmails { get; init; }
    public Dictionary<string, int> UsersByRole { get; init; } = new();
    public int PendingUpgradeRequests { get; init; }
}

// Wave 4.6.c.3 (2026-06-24): PagedResultDto<T> extracted to
// LankaConnect.BuildingBlocks.Application.Common.Models.PagedResultDto so legacy consumers
// (UserMappingProfile, GetSupportTicketsPagedQuery, etc.) can keep using it
// without taking an Identity.Application ProjectReference (which would create
// a circular ref Identity.Application -> LankaConnect.Application -> Identity.Application).
