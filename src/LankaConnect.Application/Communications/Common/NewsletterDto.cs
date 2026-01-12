using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Application.Communications.Common;

/// <summary>
/// Newsletter data transfer object
/// Phase 6A.74: Newsletter representation
/// </summary>
public class NewsletterDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public Guid? EventId { get; set; }
    public NewsletterStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IncludeNewsletterSubscribers { get; set; }
    public bool TargetAllLocations { get; set; }
    public List<Guid> EmailGroupIds { get; set; } = new();
    public List<Guid> MetroAreaIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
