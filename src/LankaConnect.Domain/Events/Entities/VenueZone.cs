using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a physical zone/section within a venue layout.
/// Zones are mapped to ticket tiers via the polymorphic <see cref="TierAssignment"/> table
/// (Slice 4 Release N); the legacy <c>TicketTierId</c> FK was dropped from the domain model.
/// Examples: "VIP Section", "Section A", "Balcony".
/// </summary>
public class VenueZone : BaseEntity
{
    private readonly List<Seat> _seats = new();

    public Guid VenueLayoutId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    /// <summary>
    /// Canvas shape the zone is rendered with. Default is <see cref="ZoneShape.Rect"/>
    /// so preexisting rows continue to behave as before.
    /// </summary>
    public ZoneShape Shape { get; private set; } = ZoneShape.Rect;

    /// <summary>
    /// JSON document describing the zone geometry on the canvas, as a string.
    /// Shape-specific payloads:
    /// <list type="bullet">
    ///   <item>Rect: <c>{ "x", "y", "width", "height", "rotation" }</c></item>
    ///   <item>Curve: <c>{ "centerX", "centerY", "radius", "startAngleDeg", "sweepAngleDeg", "rowCount" }</c></item>
    ///   <item>Polygon: <c>{ "points": [{"x","y"}, ...] }</c></item>
    /// </list>
    /// Defaults to <c>"{}"</c> — editor fills in at save time.
    /// </summary>
    public string Geometry { get; private set; } = "{}";

    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();

    /// <summary>Count of enabled (non-disabled) seats in this zone.</summary>
    public int EnabledSeatCount => _seats.Count(s => s.IsEnabled);

    // EF Core parameterless constructor
    private VenueZone() { }

    private VenueZone(
        Guid venueLayoutId,
        string name,
        string color,
        int sortOrder,
        ZoneShape shape,
        string geometry)
    {
        VenueLayoutId = venueLayoutId;
        Name = name;
        Color = color;
        SortOrder = sortOrder;
        Shape = shape;
        Geometry = geometry;
    }

    /// <summary>
    /// Creates a new venue zone within a layout (back-compat — defaults Shape to Rect,
    /// empty Geometry).
    /// </summary>
    public static Result<VenueZone> Create(
        Guid venueLayoutId,
        string name,
        string color,
        int sortOrder)
        => Create(venueLayoutId, name, color, sortOrder, ZoneShape.Rect, geometry: null);

    /// <summary>
    /// Creates a new venue zone with an explicit canvas shape + geometry.
    /// </summary>
    public static Result<VenueZone> Create(
        Guid venueLayoutId,
        string name,
        string color,
        int sortOrder,
        ZoneShape shape,
        string? geometry)
    {
        if (venueLayoutId == Guid.Empty)
            return Result<VenueZone>.Failure("Venue layout ID is required");

        if (string.IsNullOrWhiteSpace(name))
            return Result<VenueZone>.Failure("Zone name is required");

        if (name.Trim().Length > 100)
            return Result<VenueZone>.Failure("Zone name cannot exceed 100 characters");

        if (string.IsNullOrWhiteSpace(color))
            return Result<VenueZone>.Failure("Zone color is required");

        if (sortOrder < 0)
            return Result<VenueZone>.Failure("Sort order cannot be negative");

        return Result<VenueZone>.Success(new VenueZone(
            venueLayoutId,
            name.Trim(),
            color.Trim(),
            sortOrder,
            shape,
            NormalizeGeometry(geometry)));
    }

    /// <summary>
    /// Updates zone properties (back-compat — does not touch Shape/Geometry).
    /// </summary>
    public Result Update(string name, string color, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Zone name is required");

        if (name.Trim().Length > 100)
            return Result.Failure("Zone name cannot exceed 100 characters");

        if (string.IsNullOrWhiteSpace(color))
            return Result.Failure("Zone color is required");

        if (sortOrder < 0)
            return Result.Failure("Sort order cannot be negative");

        Name = name.Trim();
        Color = color.Trim();
        SortOrder = sortOrder;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Updates zone properties AND canvas shape/geometry (used by the canvas editor).
    /// </summary>
    public Result Update(
        string name,
        string color,
        int sortOrder,
        ZoneShape shape,
        string? geometry)
    {
        var baseResult = Update(name, color, sortOrder);
        if (baseResult.IsFailure) return baseResult;

        Shape = shape;
        Geometry = NormalizeGeometry(geometry);
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Adds a seat to this zone. Called internally by VenueLayout during seat generation.
    /// </summary>
    internal void AddSeat(Seat seat)
    {
        _seats.Add(seat);
    }

    /// <summary>
    /// Clears all seats from this zone. Used before regenerating seats.
    /// </summary>
    internal void ClearSeats()
    {
        _seats.Clear();
    }

    /// <summary>
    /// Slice 8 S8.9b: rebuilds this zone's seat collection from a source list,
    /// preserving every seat's <see cref="Seat.Row"/>, <see cref="Seat.Number"/>,
    /// <see cref="Seat.Label"/>, <see cref="Seat.SortOrder"/>, <see cref="Seat.X"/>,
    /// <see cref="Seat.Y"/>, <see cref="Seat.IsAccessible"/>, and
    /// <see cref="Seat.IsEnabled"/> flags but with fresh server-side IDs and a
    /// <see cref="Seat.VenueZoneId"/> pointing at this (clone) zone.
    ///
    /// Used by <see cref="VenueLayout.CloneAsTemplate"/> only — keeps the seat
    /// fidelity contract on the aggregate root rather than exposing an
    /// arbitrary "create seat at exact (row,number,label,...)" public surface
    /// that would invite domain-rule bypasses.
    /// </summary>
    internal void RebuildSeatsFrom(IEnumerable<Seat> sourceSeats)
    {
        if (sourceSeats is null) return;
        _seats.Clear();
        foreach (var src in sourceSeats)
        {
            var seatResult = Seat.CreateInZone(
                venueZoneId: Id,
                row: src.Row,
                number: src.Number,
                label: src.Label,
                sortOrder: src.SortOrder,
                isAccessible: src.IsAccessible,
                x: src.X,
                y: src.Y);
            if (!seatResult.IsSuccess)
            {
                // Source seat passed validation when originally created; if we
                // can't recreate it the source aggregate is malformed. Surface
                // loudly rather than silently dropping seats from the clone.
                throw new InvalidOperationException(
                    $"Failed to clone zone seat (Row={src.Row}, Number={src.Number}): {seatResult.Error}");
            }
            var newSeat = seatResult.Value;
            if (!src.IsEnabled)
            {
                newSeat.Disable();
            }
            _seats.Add(newSeat);
        }
    }

    private static string NormalizeGeometry(string? geometry)
    {
        if (string.IsNullOrWhiteSpace(geometry)) return "{}";
        var trimmed = geometry.Trim();
        if (trimmed[0] != '{' && trimmed[0] != '[') return "{}";
        return trimmed;
    }
}
