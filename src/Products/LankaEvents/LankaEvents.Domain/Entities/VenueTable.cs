using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Domain.Entities;

/// <summary>
/// Represents a banquet / dining / head table inside a venue layout. Tables own
/// their seats directly (via <see cref="Seats"/>) — tables are independent from
/// <see cref="VenueZone"/>, though a zone FK (<see cref="VenueZoneId"/>) can be
/// supplied for grouping (e.g. "VIP tables" zone).
///
/// Seats around a table are placed radially for <see cref="TableShape.Round"/>
/// and along sides for <see cref="TableShape.Square"/> / <see cref="TableShape.Rect"/>.
/// Geometry is stored as raw JSON to give the canvas editor full freedom without
/// forcing EF Core to serialize a mutable value-object graph (avoids Phase 6A.130).
/// </summary>
public class VenueTable : LegacyBaseEntity
{
    public const int MaxLabelLength = 50;
    public const int MinCapacity = 1;
    public const int MaxCapacity = 30;

    private readonly List<Seat> _seats = new();

    public Guid VenueLayoutId { get; private set; }

    /// <summary>Optional group / section this table belongs to. Null means ungrouped.</summary>
    public Guid? VenueZoneId { get; private set; }

    public string Label { get; private set; } = string.Empty;
    public TableShape Shape { get; private set; }

    /// <summary>
    /// JSON document describing the table geometry on the canvas, as a string (not
    /// a parsed object). Shape-specific payloads:
    /// <list type="bullet">
    ///   <item>Round: <c>{ "centerX", "centerY", "radius", "rotation" }</c></item>
    ///   <item>Square: <c>{ "centerX", "centerY", "side", "rotation" }</c></item>
    ///   <item>Rect: <c>{ "centerX", "centerY", "width", "height", "rotation" }</c></item>
    /// </list>
    /// Defaults to <c>"{}"</c> when omitted — editor fills in at save time.
    /// </summary>
    public string Geometry { get; private set; } = "{}";

    /// <summary>Total seat count for this table. 1..30 to guard against user typos.</summary>
    public int Capacity { get; private set; }

    public int SortOrder { get; private set; }

    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();

    /// <summary>Count of enabled (non-disabled) seats currently attached to the table.</summary>
    public int EnabledSeatCount => _seats.Count(s => s.IsEnabled);

    // EF Core parameterless constructor
    private VenueTable() { }

    private VenueTable(
        Guid venueLayoutId,
        Guid? venueZoneId,
        string label,
        TableShape shape,
        string geometry,
        int capacity,
        int sortOrder)
    {
        VenueLayoutId = venueLayoutId;
        VenueZoneId = venueZoneId;
        Label = label;
        Shape = shape;
        Geometry = geometry;
        Capacity = capacity;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Creates a new banquet / dining table inside a layout. Does NOT auto-generate
    /// seats — callers invoke <see cref="GenerateRoundTableSeats"/> or
    /// <see cref="GenerateRectTableSeats"/> after the table is attached to a layout.
    /// </summary>
    public static Result<VenueTable> Create(
        Guid venueLayoutId,
        string label,
        TableShape shape,
        int capacity,
        int sortOrder,
        Guid? venueZoneId = null,
        string? geometry = null)
    {
        if (venueLayoutId == Guid.Empty)
            return Result<VenueTable>.Failure("Venue layout ID is required");

        if (string.IsNullOrWhiteSpace(label))
            return Result<VenueTable>.Failure("Table label is required");

        if (label.Trim().Length > MaxLabelLength)
            return Result<VenueTable>.Failure($"Table label cannot exceed {MaxLabelLength} characters");

        if (capacity < MinCapacity || capacity > MaxCapacity)
            return Result<VenueTable>.Failure(
                $"Table capacity must be between {MinCapacity} and {MaxCapacity}");

        if (shape == TableShape.Square && capacity % 4 != 0)
            return Result<VenueTable>.Failure(
                "Square-table capacity must be a multiple of 4 (one seat per side minimum)");

        if (sortOrder < 0)
            return Result<VenueTable>.Failure("Sort order cannot be negative");

        var normalizedGeometry = NormalizeGeometry(geometry);

        return Result<VenueTable>.Success(new VenueTable(
            venueLayoutId: venueLayoutId,
            venueZoneId: venueZoneId,
            label: label.Trim(),
            shape: shape,
            geometry: normalizedGeometry,
            capacity: capacity,
            sortOrder: sortOrder));
    }

    /// <summary>
    /// Updates mutable table properties. Cannot reassign to a different layout.
    /// Caller must regenerate seats if capacity/shape changed.
    /// </summary>
    public Result Update(
        string label,
        TableShape shape,
        int capacity,
        int sortOrder,
        Guid? venueZoneId,
        string? geometry)
    {
        if (string.IsNullOrWhiteSpace(label))
            return Result.Failure("Table label is required");

        if (label.Trim().Length > MaxLabelLength)
            return Result.Failure($"Table label cannot exceed {MaxLabelLength} characters");

        if (capacity < MinCapacity || capacity > MaxCapacity)
            return Result.Failure(
                $"Table capacity must be between {MinCapacity} and {MaxCapacity}");

        if (shape == TableShape.Square && capacity % 4 != 0)
            return Result.Failure(
                "Square-table capacity must be a multiple of 4 (one seat per side minimum)");

        if (sortOrder < 0)
            return Result.Failure("Sort order cannot be negative");

        Label = label.Trim();
        Shape = shape;
        Capacity = capacity;
        SortOrder = sortOrder;
        VenueZoneId = venueZoneId;
        Geometry = NormalizeGeometry(geometry);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Generates <see cref="Capacity"/> seats distributed radially around a round
    /// table. Seat 1 is placed at <paramref name="startAngleDeg"/> (default 0° =
    /// 3 o'clock) and subsequent seats sweep clockwise. Existing seats are cleared
    /// before regeneration.
    /// </summary>
    /// <remarks>
    /// Angle normalization matches <see cref="Seat.CreateAtTable"/> — seats always
    /// land in <c>[0, 360)</c> so persistence round-trips are stable.
    /// </remarks>
    public Result GenerateRoundTableSeats(double startAngleDeg = 0)
    {
        if (Shape != TableShape.Round)
            return Result.Failure("GenerateRoundTableSeats requires a Round table shape");

        if (double.IsNaN(startAngleDeg) || double.IsInfinity(startAngleDeg))
            return Result.Failure("Start angle must be a finite number");

        _seats.Clear();

        var step = 360.0 / Capacity;
        for (var i = 0; i < Capacity; i++)
        {
            var seatNumber = i + 1;
            var angle = startAngleDeg + (i * step);
            var label = $"{Label}-S{seatNumber}";

            var seatResult = Seat.CreateAtTable(
                venueTableId: Id,
                tableLabel: Label,
                seatNumber: seatNumber,
                label: label,
                sortOrder: i,
                angleDeg: angle);

            if (seatResult.IsFailure)
                return Result.Failure($"Failed to generate seat {seatNumber}: {seatResult.Error}");

            _seats.Add(seatResult.Value);
        }

        return Result.Success();
    }

    /// <summary>
    /// Generates seats along the sides of a square/rect table, distributing
    /// <see cref="Capacity"/> evenly. Seats are labeled "<c>{Label}-S{n}</c>"
    /// starting at side 1 (top). Angles use side midpoints: top=270°, right=0°,
    /// bottom=90°, left=180°. Existing seats are cleared before regeneration.
    /// </summary>
    public Result GenerateRectTableSeats()
    {
        if (Shape is not (TableShape.Square or TableShape.Rect))
            return Result.Failure("GenerateRectTableSeats requires a Square or Rect table shape");

        if (Shape == TableShape.Square && Capacity % 4 != 0)
            return Result.Failure("Square-table capacity must be a multiple of 4");

        _seats.Clear();

        // Distribute capacity across 4 sides as evenly as possible. For Square (capacity % 4 == 0)
        // each side gets capacity/4; for Rect long sides get the extra seats first.
        var perSide = Capacity / 4;
        var remainder = Capacity % 4;
        var sideCounts = new[]
        {
            perSide + (remainder > 0 ? 1 : 0), // top
            perSide + (remainder > 1 ? 1 : 0), // right
            perSide + (remainder > 2 ? 1 : 0), // bottom
            perSide,                           // left
        };
        var sideAngles = new[] { 270.0, 0.0, 90.0, 180.0 };

        var seatIndex = 0;
        for (var side = 0; side < 4; side++)
        {
            var count = sideCounts[side];
            for (var i = 0; i < count; i++)
            {
                seatIndex++;
                var label = $"{Label}-S{seatIndex}";

                var seatResult = Seat.CreateAtTable(
                    venueTableId: Id,
                    tableLabel: Label,
                    seatNumber: seatIndex,
                    label: label,
                    sortOrder: seatIndex - 1,
                    angleDeg: sideAngles[side]);

                if (seatResult.IsFailure)
                    return Result.Failure($"Failed to generate seat {seatIndex}: {seatResult.Error}");

                _seats.Add(seatResult.Value);
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Adds a seat to this table. Internal — used by the aggregate root when
    /// attaching seats generated outside the table entity itself.
    /// </summary>
    internal void AddSeat(Seat seat)
    {
        _seats.Add(seat);
    }

    /// <summary>
    /// Clears all seats. Used before regenerating or when the table's capacity changes.
    /// </summary>
    internal void ClearSeats()
    {
        _seats.Clear();
    }

    /// <summary>
    /// Slice 8 S8.9b: rebuilds this table's seat collection from a source list,
    /// preserving every seat's <see cref="Seat.Number"/>, <see cref="Seat.Label"/>,
    /// <see cref="Seat.SortOrder"/>, <see cref="Seat.AngleDeg"/>,
    /// <see cref="Seat.X"/>, <see cref="Seat.Y"/>, <see cref="Seat.IsAccessible"/>,
    /// and <see cref="Seat.IsEnabled"/> flags but with fresh server-side IDs and
    /// a <see cref="Seat.VenueTableId"/> pointing at this (clone) table.
    ///
    /// Used by <see cref="VenueLayout.CloneAsTemplate"/> only — see
    /// <see cref="VenueZone.RebuildSeatsFrom"/> for the rationale.
    /// </summary>
    internal void RebuildSeatsFrom(IEnumerable<Seat> sourceSeats)
    {
        if (sourceSeats is null) return;
        _seats.Clear();
        foreach (var src in sourceSeats)
        {
            var seatResult = Seat.CreateAtTable(
                venueTableId: Id,
                tableLabel: Label,
                seatNumber: src.Number,
                label: src.Label,
                sortOrder: src.SortOrder,
                angleDeg: src.AngleDeg ?? 0.0,
                x: src.X,
                y: src.Y,
                isAccessible: src.IsAccessible);
            if (!seatResult.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Failed to clone table seat (Number={src.Number}, Table={Label}): {seatResult.Error}");
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
        // Cheap structural check — keep the validator in the domain small; full
        // schema validation is the canvas editor / API layer's job.
        if (trimmed[0] != '{' && trimmed[0] != '[')
            return "{}";
        return trimmed;
    }
}
