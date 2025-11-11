namespace LankaConnect.Application.Users.DTOs;

/// <summary>
/// DTO for pending role upgrade approval request
/// Phase 6A.5: Admin Approval Workflow
/// </summary>
public record PendingRoleUpgradeDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string CurrentRole { get; init; } = string.Empty;
    public string RequestedRole { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; }
}
