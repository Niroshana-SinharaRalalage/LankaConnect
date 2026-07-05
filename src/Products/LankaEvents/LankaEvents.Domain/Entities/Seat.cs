using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.Entities;

/// <summary>
/// Represents a physical seat within a venue zone OR at a banquet table.
/// Exactly ONE of <see cref="VenueZoneId"/> / <see cref="VenueTableId"/> must be set
/// — enforced in the domain factories and by a DB CHECK constraint (see
/// <c>AddSeatingDomainExpansion</c> migration).
/// Tracks structural state only (enabled/disabled, accessible). Effective runtime
/// status (Available/Held/Reserved) is derived from SeatHold and SeatReservation
/// tables at query time.
/// </summary>
public class Seat : LegacyBaseEntity
{
    /// <summary>FK to parent zone. Null when seat belongs to a table instead.</summary>
    public Guid? VenueZoneId { get; private set; }

    /// <summary>FK to parent table. Null when seat belongs to a zone instead.</summary>
    public Guid? VenueTableId { get; private set; }

    public string Row { get; private set; } = string.Empty;
    public int Number { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public bool IsAccessible { get; private set; }

    /// <summary>
    /// Radial position on a round/square/rect table, in degrees (0°=3 o'clock,
    /// sweeping clockwise). Null for zone-based (row/col) seats.
    /// </summary>
    public double? AngleDeg { get; private set; }

    /// <summary>Canvas X coordinate. Populated by canvas editor or seat generators.</summary>
    public double? X { get; private set; }

    /// <summary>Canvas Y coordinate. Populated by canvas editor or seat generators.</summary>
    public double? Y { get; private set; }

    // EF Core parameterless constructor
    private Seat() { }

    private Seat(
        Guid? venueZoneId,
        Guid? venueTableId,
        string row,
        int number,
        string label,
        int sortOrder,
        bool isAccessible,
        double? angleDeg,
        double? x,
        double? y)
    {
        VenueZoneId = venueZoneId;
        VenueTableId = venueTableId;
        Row = row;
        Number = number;
        Label = label;
        SortOrder = sortOrder;
        IsEnabled = true;
        IsAccessible = isAccessible;
        AngleDeg = angleDeg;
        X = x;
        Y = y;
    }

    /// <summary>
    /// Creates a new seat within a venue zone (row/col or irregular polygon).
    /// Back-compat overload — equivalent to <see cref="CreateInZone"/> with no canvas coords.
    /// </summary>
    public static Result<Seat> Create(
        Guid venueZoneId,
        string row,
        int number,
        string label,
        int sortOrder,
        bool isAccessible = false)
        => CreateInZone(venueZoneId, row, number, label, sortOrder, isAccessible, x: null, y: null);

    /// <summary>
    /// Creates a new seat within a venue zone. Zone-based seats are identified by
    /// <c>Row</c> + <c>Number</c> (e.g. "A5"); canvas coordinates are optional.
    /// </summary>
    public static Result<Seat> CreateInZone(
        Guid venueZoneId,
        string row,
        int number,
        string label,
        int sortOrder,
        bool isAccessible = false,
        double? x = null,
        double? y = null)
    {
        if (venueZoneId == Guid.Empty)
            return Result<Seat>.Failure("Venue zone ID is required");

        if (string.IsNullOrWhiteSpace(row))
            return Result<Seat>.Failure("Row identifier is required");

        if (number <= 0)
            return Result<Seat>.Failure("Seat number must be greater than 0");

        if (string.IsNullOrWhiteSpace(label))
            return Result<Seat>.Failure("Seat label is required");

        if (sortOrder < 0)
            return Result<Seat>.Failure("Sort order cannot be negative");

        return Result<Seat>.Success(new Seat(
            venueZoneId: venueZoneId,
            venueTableId: null,
            row: row.Trim(),
            number: number,
            label: label.Trim(),
            sortOrder: sortOrder,
            isAccessible: isAccessible,
            angleDeg: null,
            x: x,
            y: y));
    }

    /// <summary>
    /// Creates a new seat at a banquet/dining table. Table-based seats are
    /// identified by their <paramref name="number"/> around the table and their
    /// radial <paramref name="angleDeg"/>. Row is used for grouping (e.g. a table
    /// label "T3") so existing row-based reporting continues to work.
    /// </summary>
    public static Result<Seat> CreateAtTable(
        Guid venueTableId,
        string tableLabel,
        int seatNumber,
        string label,
        int sortOrder,
        double angleDeg,
        double? x = null,
        double? y = null,
        bool isAccessible = false)
    {
        if (venueTableId == Guid.Empty)
            return Result<Seat>.Failure("Venue table ID is required");

        if (string.IsNullOrWhiteSpace(tableLabel))
            return Result<Seat>.Failure("Table label is required");

        if (seatNumber <= 0)
            return Result<Seat>.Failure("Seat number must be greater than 0");

        if (string.IsNullOrWhiteSpace(label))
            return Result<Seat>.Failure("Seat label is required");

        if (sortOrder < 0)
            return Result<Seat>.Failure("Sort order cannot be negative");

        if (double.IsNaN(angleDeg) || double.IsInfinity(angleDeg))
            return Result<Seat>.Failure("Angle must be a finite number");

        // Normalize angle into [0, 360) so equality-compares are stable across regenerations.
        var normalized = ((angleDeg % 360) + 360) % 360;

        return Result<Seat>.Success(new Seat(
            venueZoneId: null,
            venueTableId: venueTableId,
            row: tableLabel.Trim(),
            number: seatNumber,
            label: label.Trim(),
            sortOrder: sortOrder,
            isAccessible: isAccessible,
            angleDeg: normalized,
            x: x,
            y: y));
    }

    /// <summary>
    /// Disables the seat (organizer action). Disabled seats cannot be held or reserved.
    /// </summary>
    public Result Disable()
    {
        if (!IsEnabled)
            return Result.Failure("Seat is already disabled");

        IsEnabled = false;
        return Result.Success();
    }

    /// <summary>
    /// Enables a previously disabled seat.
    /// </summary>
    public Result Enable()
    {
        if (IsEnabled)
            return Result.Failure("Seat is already enabled");

        IsEnabled = true;
        return Result.Success();
    }

    /// <summary>
    /// Sets the accessibility flag for ADA compliance.
    /// </summary>
    public Result SetAccessible(bool isAccessible)
    {
        IsAccessible = isAccessible;
        return Result.Success();
    }

    /// <summary>
    /// True when this seat lives in a zone (row/col seating).
    /// </summary>
    public bool IsZoneSeat => VenueZoneId.HasValue;

    /// <summary>
    /// True when this seat lives at a table (radial/table seating).
    /// </summary>
    public bool IsTableSeat => VenueTableId.HasValue;
}
