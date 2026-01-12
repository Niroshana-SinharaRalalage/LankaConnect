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
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

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
