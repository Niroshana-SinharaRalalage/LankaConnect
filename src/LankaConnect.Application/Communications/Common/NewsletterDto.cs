using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Application.Communications.Common;

/// <summary>
/// Data transfer object for Newsletter
/// </summary>
public class NewsletterDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public Guid? EventId { get; set; }
    public string? EventTitle { get; set; }
    public NewsletterStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IncludeNewsletterSubscribers { get; set; }
    public List<Guid> EmailGroupIds { get; set; } = new();

    // Phase 6A.74 Enhancement 1: Location Targeting
    public List<Guid> MetroAreaIds { get; set; } = new();
    public bool TargetAllLocations { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
