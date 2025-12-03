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

    // Media galleries (Epic 2 Phase 2)
    public IReadOnlyList<EventImageDto> Images { get; init; } = Array.Empty<EventImageDto>();
    public IReadOnlyList<EventVideoDto> Videos { get; init; } = Array.Empty<EventVideoDto>();
}

/// <summary>
/// DTO for event image in gallery
/// </summary>
public record EventImageDto
{
    public Guid Id { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
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
