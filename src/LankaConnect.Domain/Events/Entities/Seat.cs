using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a physical seat within a venue zone.
/// Tracks structural state only (enabled/disabled, accessible).
/// Effective runtime status (Available/Held/Reserved) is derived from
/// SeatHold and SeatReservation tables at query time.
/// </summary>
public class Seat : BaseEntity
{
    public Guid VenueZoneId { get; private set; }
    public string Row { get; private set; } = string.Empty;
    public int Number { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public bool IsAccessible { get; private set; }

    // Optional coordinates for custom layouts (future)
    public double? X { get; private set; }
    public double? Y { get; private set; }

    // EF Core parameterless constructor
    private Seat() { }

    private Seat(
        Guid venueZoneId,
        string row,
        int number,
        string label,
        int sortOrder,
        bool isAccessible)
    {
        VenueZoneId = venueZoneId;
        Row = row;
        Number = number;
        Label = label;
        SortOrder = sortOrder;
        IsEnabled = true;
        IsAccessible = isAccessible;
    }

    /// <summary>
    /// Creates a new seat within a venue zone.
    /// </summary>
    public static Result<Seat> Create(
        Guid venueZoneId,
        string row,
        int number,
        string label,
        int sortOrder,
        bool isAccessible = false)
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
            venueZoneId,
            row.Trim(),
            number,
            label.Trim(),
            sortOrder,
            isAccessible));
    }

    /// <summary>
    /// Disables the seat (organizer action). Disabled seats cannot be held or reserved.
    /// </summary>
    public Result Disable()
    {
        if (!IsEnabled)
            return Result.Failure("Seat is already disabled");

        IsEnabled = false;
        MarkAsUpdated();
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
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Sets the accessibility flag for ADA compliance.
    /// </summary>
    public Result SetAccessible(bool isAccessible)
    {
        IsAccessible = isAccessible;
        MarkAsUpdated();
        return Result.Success();
    }
}
