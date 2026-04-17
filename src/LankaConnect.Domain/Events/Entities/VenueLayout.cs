using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Aggregate root representing a venue seating layout.
/// Owns zones and seats (structural definition only).
/// Runtime seat status (Available/Held/Reserved) is derived from SeatHold/SeatReservation tables.
/// Can be a reusable template or assigned to a specific event.
/// </summary>
public class VenueLayout : BaseEntity
{
    private readonly List<VenueZone> _zones = new();

    public string Name { get; private set; } = string.Empty;
    public Guid? EventId { get; private set; }
    public LayoutType LayoutType { get; private set; }
    public bool IsTemplate { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    /// <summary>Optimistic concurrency token (EF Core xmin/rowversion).</summary>
    public uint RowVersion { get; private set; }

    public IReadOnlyList<VenueZone> Zones => _zones.AsReadOnly();

    /// <summary>Total number of enabled seats across all zones.</summary>
    public int TotalCapacity => _zones.Sum(z => z.EnabledSeatCount);

    // EF Core parameterless constructor
    private VenueLayout() { }

    private VenueLayout(
        string name,
        LayoutType layoutType,
        Guid createdByUserId,
        Guid? eventId,
        bool isTemplate)
    {
        Name = name;
        LayoutType = layoutType;
        CreatedByUserId = createdByUserId;
        EventId = eventId;
        IsTemplate = isTemplate;
    }

    /// <summary>
    /// Creates a new venue layout.
    /// </summary>
    public static Result<VenueLayout> Create(
        string name,
        LayoutType layoutType,
        Guid createdByUserId,
        Guid? eventId = null,
        bool isTemplate = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<VenueLayout>.Failure("Layout name is required");

        if (name.Trim().Length > 200)
            return Result<VenueLayout>.Failure("Layout name cannot exceed 200 characters");

        if (!Enum.IsDefined(typeof(LayoutType), layoutType))
            return Result<VenueLayout>.Failure("Invalid layout type");

        if (createdByUserId == Guid.Empty)
            return Result<VenueLayout>.Failure("Creator user ID is required");

        if (isTemplate && eventId.HasValue)
            return Result<VenueLayout>.Failure("Templates cannot be assigned to a specific event");

        return Result<VenueLayout>.Success(new VenueLayout(
            name.Trim(),
            layoutType,
            createdByUserId,
            eventId,
            isTemplate));
    }

    // ──────────────────────────────────────
    // Zone Management
    // ──────────────────────────────────────

    /// <summary>
    /// Adds a new zone to this layout.
    /// </summary>
    public Result<VenueZone> AddZone(
        string name,
        string color,
        Guid? ticketTierId,
        int sortOrder)
    {
        // Check for duplicate zone names (case-insensitive)
        if (_zones.Any(z => z.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Result<VenueZone>.Failure($"A zone named '{name.Trim()}' already exists in this layout");

        var zoneResult = VenueZone.Create(Id, name, color, ticketTierId, sortOrder);
        if (zoneResult.IsFailure)
            return Result<VenueZone>.Failure(zoneResult.Error);

        _zones.Add(zoneResult.Value);
        MarkAsUpdated();
        return zoneResult;
    }

    /// <summary>
    /// Updates an existing zone's properties.
    /// </summary>
    public Result UpdateZone(
        Guid zoneId,
        string name,
        string color,
        Guid? ticketTierId,
        int sortOrder)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == zoneId);
        if (zone == null)
            return Result.Failure("Zone not found in this layout");

        // Check duplicate name excluding the zone being updated
        if (_zones.Any(z => z.Id != zoneId
            && z.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Result.Failure($"A zone named '{name.Trim()}' already exists in this layout");

        var result = zone.Update(name, color, ticketTierId, sortOrder);
        if (result.IsSuccess)
            MarkAsUpdated();

        return result;
    }

    /// <summary>
    /// Removes a zone from the layout. Zone must have no reserved seats.
    /// Note: Reservation checks are done at the application layer (via SeatReservation table).
    /// </summary>
    public Result RemoveZone(Guid zoneId)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == zoneId);
        if (zone == null)
            return Result.Failure("Zone not found in this layout");

        _zones.Remove(zone);
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Gets a zone by ID.
    /// </summary>
    public VenueZone? GetZone(Guid zoneId) => _zones.FirstOrDefault(z => z.Id == zoneId);

    // ──────────────────────────────────────
    // Seat Generation
    // ──────────────────────────────────────

    /// <summary>
    /// Generates theater-style seats for a zone: rows × seats per row.
    /// Row labels: A, B, C, ... (or custom start). Seat labels: "A1", "A2", "B1", etc.
    /// </summary>
    public Result GenerateTheaterSeats(
        Guid zoneId,
        int rows,
        int seatsPerRow,
        string startRowLabel = "A")
    {
        var zone = _zones.FirstOrDefault(z => z.Id == zoneId);
        if (zone == null)
            return Result.Failure("Zone not found in this layout");

        if (rows <= 0)
            return Result.Failure("Number of rows must be greater than 0");

        if (rows > 100)
            return Result.Failure("Number of rows cannot exceed 100");

        if (seatsPerRow <= 0)
            return Result.Failure("Seats per row must be greater than 0");

        if (seatsPerRow > 100)
            return Result.Failure("Seats per row cannot exceed 100");

        if (string.IsNullOrWhiteSpace(startRowLabel))
            return Result.Failure("Start row label is required");

        // Clear existing seats in this zone before regeneration
        zone.ClearSeats();

        int startCharCode = char.ToUpper(startRowLabel.Trim()[0]);
        int sortIndex = 0;

        for (int r = 0; r < rows; r++)
        {
            string rowLabel = ((char)(startCharCode + r)).ToString();
            // Wrap after Z: AA, AB, etc. (simple version — single char for now)
            if (startCharCode + r > 'Z')
                rowLabel = "A" + ((char)('A' + (startCharCode + r - 'Z' - 1))).ToString();

            for (int s = 1; s <= seatsPerRow; s++)
            {
                string seatLabel = $"{rowLabel}{s}";
                var seatResult = Seat.Create(zone.Id, rowLabel, s, seatLabel, sortIndex);
                if (seatResult.IsFailure)
                    return Result.Failure($"Failed to create seat {seatLabel}: {seatResult.Error}");

                zone.AddSeat(seatResult.Value);
                sortIndex++;
            }
        }

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Generates banquet-style seats for a zone: tables × seats per table.
    /// Seat labels: "T1-S1", "T1-S2", etc.
    /// </summary>
    public Result GenerateBanquetSeats(
        Guid zoneId,
        int tables,
        int seatsPerTable,
        int startTableNumber = 1)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == zoneId);
        if (zone == null)
            return Result.Failure("Zone not found in this layout");

        if (tables <= 0)
            return Result.Failure("Number of tables must be greater than 0");

        if (tables > 200)
            return Result.Failure("Number of tables cannot exceed 200");

        if (seatsPerTable <= 0)
            return Result.Failure("Seats per table must be greater than 0");

        if (seatsPerTable > 20)
            return Result.Failure("Seats per table cannot exceed 20");

        if (startTableNumber <= 0)
            return Result.Failure("Start table number must be greater than 0");

        // Clear existing seats in this zone before regeneration
        zone.ClearSeats();

        int sortIndex = 0;

        for (int t = 0; t < tables; t++)
        {
            int tableNum = startTableNumber + t;
            string rowLabel = $"T{tableNum}";

            for (int s = 1; s <= seatsPerTable; s++)
            {
                string seatLabel = $"T{tableNum}-S{s}";
                var seatResult = Seat.Create(zone.Id, rowLabel, s, seatLabel, sortIndex);
                if (seatResult.IsFailure)
                    return Result.Failure($"Failed to create seat {seatLabel}: {seatResult.Error}");

                zone.AddSeat(seatResult.Value);
                sortIndex++;
            }
        }

        MarkAsUpdated();
        return Result.Success();
    }

    // ──────────────────────────────────────
    // Seat Management
    // ──────────────────────────────────────

    /// <summary>
    /// Disables a specific seat (organizer action).
    /// </summary>
    public Result DisableSeat(Guid seatId)
    {
        var seat = FindSeat(seatId);
        if (seat == null)
            return Result.Failure("Seat not found in this layout");

        var result = seat.Disable();
        if (result.IsSuccess)
            MarkAsUpdated();

        return result;
    }

    /// <summary>
    /// Enables a previously disabled seat.
    /// </summary>
    public Result EnableSeat(Guid seatId)
    {
        var seat = FindSeat(seatId);
        if (seat == null)
            return Result.Failure("Seat not found in this layout");

        var result = seat.Enable();
        if (result.IsSuccess)
            MarkAsUpdated();

        return result;
    }

    /// <summary>
    /// Sets the accessibility flag on a seat for ADA compliance.
    /// </summary>
    public Result SetSeatAccessible(Guid seatId, bool isAccessible)
    {
        var seat = FindSeat(seatId);
        if (seat == null)
            return Result.Failure("Seat not found in this layout");

        var result = seat.SetAccessible(isAccessible);
        if (result.IsSuccess)
            MarkAsUpdated();

        return result;
    }

    // ──────────────────────────────────────
    // Event Assignment
    // ──────────────────────────────────────

    /// <summary>
    /// Assigns this layout to a specific event.
    /// </summary>
    public Result AssignToEvent(Guid eventId)
    {
        if (eventId == Guid.Empty)
            return Result.Failure("Event ID is required");

        if (IsTemplate)
            return Result.Failure("Cannot assign a template directly to an event. Clone it first.");

        if (EventId.HasValue && EventId.Value != eventId)
            return Result.Failure("This layout is already assigned to a different event");

        EventId = eventId;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Detaches this layout from its event.
    /// </summary>
    public Result DetachFromEvent()
    {
        if (!EventId.HasValue)
            return Result.Failure("This layout is not assigned to any event");

        EventId = null;
        MarkAsUpdated();
        return Result.Success();
    }

    // ──────────────────────────────────────
    // Validation
    // ──────────────────────────────────────

    /// <summary>
    /// Validates that this layout is suitable for an event with the given tiers.
    /// Every zone must map to an active tier, and zone seat count must not exceed tier capacity.
    /// </summary>
    public Result ValidateForEvent(IReadOnlyList<TicketTier> eventTiers)
    {
        if (!_zones.Any())
            return Result.Failure("Layout must have at least one zone");

        var activeTierIds = eventTiers.Where(t => t.IsActive).Select(t => t.Id).ToHashSet();

        foreach (var zone in _zones)
        {
            if (!zone.TicketTierId.HasValue)
                return Result.Failure($"Zone '{zone.Name}' must be mapped to a ticket tier");

            if (!activeTierIds.Contains(zone.TicketTierId.Value))
                return Result.Failure($"Zone '{zone.Name}' is mapped to a tier that does not exist or is inactive");

            // Revision #6: Zone seat count ≤ tier capacity (not strict equality)
            var tier = eventTiers.First(t => t.Id == zone.TicketTierId.Value);
            if (zone.EnabledSeatCount > tier.Capacity)
                return Result.Failure(
                    $"Zone '{zone.Name}' has {zone.EnabledSeatCount} enabled seats but the linked tier '{tier.Name}' only has capacity for {tier.Capacity}");
        }

        return Result.Success();
    }

    /// <summary>
    /// Updates the layout name.
    /// </summary>
    public Result UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Layout name is required");

        if (name.Trim().Length > 200)
            return Result.Failure("Layout name cannot exceed 200 characters");

        Name = name.Trim();
        MarkAsUpdated();
        return Result.Success();
    }

    // ──────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────

    private Seat? FindSeat(Guid seatId)
    {
        foreach (var zone in _zones)
        {
            var seat = zone.Seats.FirstOrDefault(s => s.Id == seatId);
            if (seat != null)
                return seat;
        }
        return null;
    }
}
