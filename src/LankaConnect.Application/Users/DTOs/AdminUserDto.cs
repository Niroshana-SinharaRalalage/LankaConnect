namespace LankaConnect.Application.Users.DTOs;

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

/// <summary>
/// Phase 6A.90: Paged result DTO
/// </summary>
public record PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
