using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Common;

public record EventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public Guid OrganizerId { get; init; }
    public int Capacity { get; init; }
    public int CurrentRegistrations { get; init; }
    public EventStatus Status { get; init; }
    public EventCategory Category { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Phase 6A.46: User-facing display label based on event lifecycle
    /// Computed based on PublishedAt, StartDate, EndDate, and Status
    /// Values: "New", "Upcoming", "Cancelled", "Completed", "Inactive", or status name
    /// </summary>
    public string DisplayLabel { get; init; } = string.Empty;

    // Location information (nullable - not all events have physical locations)
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }

    // Legacy Ticket pricing (nullable - free events, backward compatibility)
    public decimal? TicketPriceAmount { get; init; }
    public Currency? TicketPriceCurrency { get; init; }
    public bool IsFree { get; init; }

    // Session 21: Dual Pricing (Adult/Child) - nullable
    public decimal? AdultPriceAmount { get; init; }
    public Currency? AdultPriceCurrency { get; init; }
    public decimal? ChildPriceAmount { get; init; }
    public Currency? ChildPriceCurrency { get; init; }
    public int? ChildAgeLimit { get; init; }

    /// <summary>
    /// Indicates whether this event uses age-based dual pricing (adult/child)
    /// True if ChildPrice is set, False for single pricing or free events
    /// </summary>
    public bool HasDualPricing { get; init; }

    // Phase 6D: Group Tiered Pricing - nullable
    /// <summary>
    /// Phase 6D: Pricing type (Single, AgeDual, GroupTiered)
    /// Null for free events or legacy events without Pricing configured
    /// </summary>
    public string? PricingType { get; init; }

    /// <summary>
    /// Phase 6D: Group pricing tiers for quantity-based discounts
    /// Only populated when PricingType is "GroupTiered"
    /// </summary>
    public IReadOnlyList<GroupPricingTierDto> GroupPricingTiers { get; init; } = Array.Empty<GroupPricingTierDto>();

    /// <summary>
    /// Phase 6D: Indicates whether this event uses group-based tiered pricing
    /// True if PricingType is GroupTiered and tiers are configured
    /// </summary>
    public bool HasGroupPricing { get; init; }

    // Media galleries (Epic 2 Phase 2)
    public IReadOnlyList<EventImageDto> Images { get; init; } = Array.Empty<EventImageDto>();
    public IReadOnlyList<EventVideoDto> Videos { get; init; } = Array.Empty<EventVideoDto>();

    /// <summary>
    /// Phase 6A.25: Badge Management System
    /// Badges assigned to this event for overlay display
    /// </summary>
    public IReadOnlyList<EventBadgeDto> Badges { get; init; } = Array.Empty<EventBadgeDto>();

    /// <summary>
    /// Phase 6A.32: Email Groups Integration
    /// IDs of email groups associated with this event for invitations
    /// </summary>
    public IReadOnlyList<Guid> EmailGroupIds { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Phase 6A.32: Email Groups Integration
    /// Summary details of email groups associated with this event
    /// Includes IsActive flag to detect soft-deleted groups
    /// </summary>
    public IReadOnlyList<EmailGroupSummaryDto> EmailGroups { get; init; } = Array.Empty<EmailGroupSummaryDto>();

    /// <summary>
    /// Phase 6A.X: Event Organizer Contact Details
    /// Optional contact information published by the event organizer
    /// </summary>
    public bool PublishOrganizerContact { get; init; }
    public string? OrganizerContactName { get; init; }
    public string? OrganizerContactPhone { get; init; }
    public string? OrganizerContactEmail { get; init; }
}

/// <summary>
/// DTO for event image in gallery
/// Phase 6A.13: Added IsPrimary for main image selection
/// </summary>
public record EventImageDto
{
    public Guid Id { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool IsPrimary { get; init; } // Phase 6A.13: Primary image flag
    public DateTime UploadedAt { get; init; }
}

/// <summary>
/// DTO for event video in gallery
/// </summary>
public record EventVideoDto
{
    public Guid Id { get; init; }
    public string VideoUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public TimeSpan? Duration { get; init; }
    public string Format { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime UploadedAt { get; init; }
}

/// <summary>
/// Phase 6A.32: Email Groups Integration
/// Summary DTO for email groups associated with events
/// Includes IsActive flag to detect soft-deleted groups
/// </summary>
public record EmailGroupSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
