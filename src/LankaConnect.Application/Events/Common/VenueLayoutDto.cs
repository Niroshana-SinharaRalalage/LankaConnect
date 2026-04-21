using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// DTO for VenueLayout aggregate (includes zones and seats).
/// </summary>
public record VenueLayoutDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? EventId { get; init; }
    public string LayoutType { get; init; } = string.Empty;
    public bool IsTemplate { get; init; }
    public Guid CreatedByUserId { get; init; }
    public int TotalCapacity { get; init; }
    public List<VenueZoneDto> Zones { get; init; } = new();

    /// <summary>
    /// Slice 5 Chunk 6: banquet / dining tables attached to the layout. Empty when
    /// the layout has no tables (typical for pure theater layouts).
    /// </summary>
    public List<VenueTableDto> Tables { get; init; } = new();

    /// <summary>
    /// Slice 5 Chunk 7: non-seating decorative / structural elements (stage, aisle,
    /// text label, etc.). Empty when the layout has no decorations.
    /// </summary>
    public List<VenueDecorationDto> Decorations { get; init; } = new();

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Slice 5 Chunk 5b: PostgreSQL xmin exposed so clients can send it back in the
    /// <c>If-Match</c> header on PUT / PATCH / DELETE. Without this field the write
    /// endpoints are not reachable from the frontend.
    /// </summary>
    public uint RowVersion { get; init; }
}

/// <summary>
/// DTO for a zone within a venue layout.
/// </summary>
public record VenueZoneDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public Guid? TicketTierId { get; init; }
    public string? TicketTierName { get; init; }
    public int SortOrder { get; init; }
    public int EnabledSeatCount { get; init; }
    public int TotalSeatCount { get; init; }
    public List<SeatDto> Seats { get; init; } = new();

    /// <summary>
    /// Slice 5 Chunk 8: every tier currently assigned to this zone via the
    /// polymorphic <c>tier_assignments</c> junction. Superset of the legacy
    /// <c>TicketTierId</c> scalar (which is now always null — see Slice 4 notes).
    /// </summary>
    public List<Guid> TicketTierIds { get; init; } = new();
}

/// <summary>
/// DTO for a seat within a zone (structural info only).
/// </summary>
public record SeatDto
{
    public Guid Id { get; init; }
    public string Row { get; init; } = string.Empty;
    public int Number { get; init; }
    public string Label { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsAccessible { get; init; }
    public double? X { get; init; }
    public double? Y { get; init; }
}

/// <summary>
/// DTO for a banquet / dining table within a venue layout (Slice 5 Chunk 6).
/// </summary>
public record VenueTableDto
{
    public Guid Id { get; init; }
    public Guid? VenueZoneId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Shape { get; init; } = string.Empty;
    public string Geometry { get; init; } = "{}";
    public int Capacity { get; init; }
    public int SortOrder { get; init; }
    public int EnabledSeatCount { get; init; }
    public int TotalSeatCount { get; init; }
    public List<SeatDto> Seats { get; init; } = new();

    /// <summary>
    /// Slice 5 Chunk 8: every tier currently assigned to this table via the
    /// polymorphic <c>tier_assignments</c> junction.
    /// </summary>
    public List<Guid> TicketTierIds { get; init; } = new();
}

/// <summary>
/// DTO for a decorative / structural element within a venue layout (Slice 5 Chunk 7).
/// </summary>
public record VenueDecorationDto
{
    public Guid Id { get; init; }
    public string Kind { get; init; } = string.Empty;
    public string? Label { get; init; }
    public string Geometry { get; init; } = "{}";
    public string Properties { get; init; } = "{}";
    public int SortOrder { get; init; }
}

/// <summary>
/// DTO for seat availability (structural + runtime status).
/// Used by the user-facing seat selection UI.
/// </summary>
public record SeatAvailabilityDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Row { get; init; } = string.Empty;
    public int Number { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsAccessible { get; init; }
    public double? X { get; init; }
    public double? Y { get; init; }

    /// <summary>
    /// Runtime status derived from SeatHold and SeatReservation tables.
    /// Available = no hold or reservation, Held = active hold, Reserved = confirmed reservation.
    /// </summary>
    public string Status { get; init; } = "Available";

    /// <summary>
    /// Zone info for grouping in the seat map.
    /// </summary>
    public Guid ZoneId { get; init; }
    public string ZoneName { get; init; } = string.Empty;
    public string ZoneColor { get; init; } = string.Empty;
    public Guid? TicketTierId { get; init; }
}
