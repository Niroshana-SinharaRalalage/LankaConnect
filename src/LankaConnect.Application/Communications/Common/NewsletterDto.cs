using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Application.Communications.Common;

/// <summary>
/// DTO for Newsletter entity
/// Phase 6A.74: Newsletter/News Alert system
/// </summary>
public record NewsletterDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid CreatedByUserId { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
    public Guid? EventId { get; init; }
    public string? EventTitle { get; init; }
    public NewsletterStatus Status { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IncludeNewsletterSubscribers { get; init; }
    public bool TargetAllLocations { get; init; }

    /// <summary>
    /// Phase 6A.74 Part 14: Indicates if this is an announcement-only newsletter
    /// - When true: Auto-activated on creation, NOT visible on public /newsletters page
    /// - When false: Normal published newsletter (Draft → Active → visible on public page)
    /// </summary>
    public bool IsAnnouncementOnly { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Phase 6A.74 Part 13 Issue #1: Total number of recipients who received this newsletter email
    /// Null if newsletter hasn't been sent yet
    /// </summary>
    public int? TotalRecipientCount { get; init; }

    /// <summary>
    /// Phase 6A.74 Part 13+: Number of recipients from newsletter's email groups
    /// Null if newsletter hasn't been sent yet
    /// </summary>
    public int? NewsletterEmailGroupCount { get; init; }

    /// <summary>
    /// Phase 6A.74 Part 13+: Number of recipients from event's email groups (if linked)
    /// Null if newsletter hasn't been sent yet
    /// </summary>
    public int? EventEmailGroupCount { get; init; }

    /// <summary>
    /// Phase 6A.74 Part 13+: Number of newsletter subscribers
    /// Null if newsletter hasn't been sent yet
    /// </summary>
    public int? SubscriberCount { get; init; }

    /// <summary>
    /// Phase 6A.74 Part 13+: Number of event registrations (if linked)
    /// Null if newsletter hasn't been sent yet
    /// </summary>
    public int? EventRegistrationCount { get; init; }

    /// <summary>
    /// Phase 6A.74 Part 13+: Number of emails successfully sent
    /// Null if newsletter hasn't been sent yet
    /// </summary>
    public int? SuccessfulSends { get; init; }

    /// <summary>
    /// Phase 6A.74 Part 13+: Number of emails that failed to send
    /// Null if newsletter hasn't been sent yet
    /// </summary>
    public int? FailedSends { get; init; }

    /// <summary>
    /// Legacy: Number of recipients from email groups (backwards compatibility)
    /// Null if newsletter hasn't been sent yet
    /// </summary>
    public int? EmailGroupRecipientCount { get; init; }

    /// <summary>
    /// Legacy: Number of recipients from newsletter subscribers (backwards compatibility)
    /// Null if newsletter hasn't been sent yet
    /// </summary>
    public int? SubscriberRecipientCount { get; init; }

    /// <summary>
    /// Email groups associated with this newsletter
    /// </summary>
    public IReadOnlyList<Guid> EmailGroupIds { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Summary details of email groups
    /// </summary>
    public IReadOnlyList<EmailGroupSummaryDto> EmailGroups { get; init; } = Array.Empty<EmailGroupSummaryDto>();

    /// <summary>
    /// Metro areas targeted by this newsletter (for location-based targeting)
    /// </summary>
    public IReadOnlyList<Guid> MetroAreaIds { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Summary details of metro areas
    /// </summary>
    public IReadOnlyList<MetroAreaSummaryDto> MetroAreas { get; init; } = Array.Empty<MetroAreaSummaryDto>();
}

/// <summary>
/// Summary DTO for email groups (lightweight)
/// </summary>
public record EmailGroupSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

/// <summary>
/// Summary DTO for metro areas (lightweight)
/// </summary>
public record MetroAreaSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
}
