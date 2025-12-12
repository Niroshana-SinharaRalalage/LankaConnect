namespace LankaConnect.Application.Communications.Common;

/// <summary>
/// DTO for EmailGroup entity
/// Phase 6A.25: Email Groups Management
/// </summary>
public record EmailGroupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid OwnerId { get; init; }
    public string OwnerName { get; init; } = string.Empty;
    public string EmailAddresses { get; init; } = string.Empty;
    public int EmailCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
