using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Application.Communications.DTOs;

/// <summary>
/// DTO for Newsletter entity
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public record NewsletterDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid CreatedByUserId { get; init; }
    public string? CreatorName { get; init; } // For display in UI
    public Guid? EventId { get; init; }
    public string? EventTitle { get; init; } // For display in UI
    public NewsletterStatus Status { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IncludeNewsletterSubscribers { get; init; }
    public List<Guid> EmailGroupIds { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
