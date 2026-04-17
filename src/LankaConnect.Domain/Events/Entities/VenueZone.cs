using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a physical zone/section within a venue layout.
/// Each zone maps to a ticket tier for pricing and can contain multiple seats.
/// Examples: "VIP Section", "Section A", "Balcony".
/// </summary>
public class VenueZone : BaseEntity
{
    private readonly List<Seat> _seats = new();

    public Guid VenueLayoutId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public Guid? TicketTierId { get; private set; }
    public int SortOrder { get; private set; }

    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();

    /// <summary>Count of enabled (non-disabled) seats in this zone.</summary>
    public int EnabledSeatCount => _seats.Count(s => s.IsEnabled);

    // EF Core parameterless constructor
    private VenueZone() { }

    private VenueZone(
        Guid venueLayoutId,
        string name,
        string color,
        Guid? ticketTierId,
        int sortOrder)
    {
        VenueLayoutId = venueLayoutId;
        Name = name;
        Color = color;
        TicketTierId = ticketTierId;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Creates a new venue zone within a layout.
    /// </summary>
    public static Result<VenueZone> Create(
        Guid venueLayoutId,
        string name,
        string color,
        Guid? ticketTierId,
        int sortOrder)
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
            ticketTierId,
            sortOrder));
    }

    /// <summary>
    /// Updates zone properties. Cannot change the parent layout.
    /// </summary>
    public Result Update(string name, string color, Guid? ticketTierId, int sortOrder)
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
        TicketTierId = ticketTierId;
        SortOrder = sortOrder;
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
}
