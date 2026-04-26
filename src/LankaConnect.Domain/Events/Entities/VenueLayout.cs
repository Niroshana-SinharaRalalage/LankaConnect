using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Aggregate root representing a venue seating layout.
/// Owns zones (with seats), tables (banquet dining — also with seats), and
/// decorations (stage/aisle/etc., non-selectable). Structural definition only —
/// runtime seat status (Available/Held/Reserved) is derived from SeatHold /
/// SeatReservation tables. A layout can be a reusable template or assigned to a
/// specific event.
/// </summary>
public class VenueLayout : BaseEntity
{
    private readonly List<VenueZone> _zones = new();
    private readonly List<VenueTable> _tables = new();
    private readonly List<VenueDecoration> _decorations = new();

    public string Name { get; private set; } = string.Empty;
    public Guid? EventId { get; private set; }
    public LayoutType LayoutType { get; private set; }
    public bool IsTemplate { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// Canvas rendering configuration. Defaults to <see cref="CanvasConfig.Default"/>
    /// (1200×800 at scale 1) for layouts that predate the canvas editor.
    /// </summary>
    public CanvasConfig Canvas { get; private set; } = CanvasConfig.Default();

    /// <summary>Optimistic concurrency token (EF Core xmin/rowversion).</summary>
    public uint RowVersion { get; private set; }

    public IReadOnlyList<VenueZone> Zones => _zones.AsReadOnly();
    public IReadOnlyList<VenueTable> Tables => _tables.AsReadOnly();
    public IReadOnlyList<VenueDecoration> Decorations => _decorations.AsReadOnly();

    /// <summary>Total number of enabled seats across all zones AND tables.</summary>
    public int TotalCapacity =>
        _zones.Sum(z => z.EnabledSeatCount) + _tables.Sum(t => t.EnabledSeatCount);

    // EF Core parameterless constructor
    private VenueLayout() { }

    private VenueLayout(
        string name,
        LayoutType layoutType,
        Guid createdByUserId,
        Guid? eventId,
        bool isTemplate,
        CanvasConfig canvas)
    {
        Name = name;
        LayoutType = layoutType;
        CreatedByUserId = createdByUserId;
        EventId = eventId;
        IsTemplate = isTemplate;
        Canvas = canvas;
    }

    /// <summary>
    /// Creates a new venue layout with an optional custom canvas. When
    /// <paramref name="canvas"/> is null the default 1200×800 canvas is used.
    /// </summary>
    public static Result<VenueLayout> Create(
        string name,
        LayoutType layoutType,
        Guid createdByUserId,
        Guid? eventId = null,
        bool isTemplate = false,
        CanvasConfig? canvas = null)
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
            isTemplate,
            canvas ?? CanvasConfig.Default()));
    }

    /// <summary>
    /// Slice 8 S8.9b: produces a per-user template (<c>EventId == null</c>,
    /// <c>IsTemplate == true</c>, <c>CreatedByUserId == newOwnerUserId</c>) that
    /// mirrors <paramref name="source"/>'s structure with fresh server-side IDs.
    /// Per architect Option B (faithful clone): zones/tables/decorations + canvas
    /// are cloned; per-seat <c>IsEnabled</c> / <c>IsAccessible</c> flags round-trip;
    /// tier mappings are deliberately dropped (they live on the <c>TicketTier</c>
    /// aggregate, owned by the source's event — templates are tier-free by design).
    ///
    /// Holds and reservations are not cloned (they're on different aggregates and
    /// belong to the source event, not the new template).
    /// </summary>
    public static Result<VenueLayout> CloneAsTemplate(
        VenueLayout source,
        string newName,
        Guid newOwnerUserId)
    {
        if (source is null)
            return Result<VenueLayout>.Failure("Source layout is required");

        if (string.IsNullOrWhiteSpace(newName))
            return Result<VenueLayout>.Failure("Template name is required");

        if (newName.Trim().Length > 200)
            return Result<VenueLayout>.Failure("Template name cannot exceed 200 characters");

        if (newOwnerUserId == Guid.Empty)
            return Result<VenueLayout>.Failure("Owner user ID is required");

        // CanvasConfig is an EF-owned entity keyed by VenueLayoutId, so reusing
        // the source's instance would carry the source layout's FK and EF would
        // refuse the save with "The property 'CanvasConfig.VenueLayoutId' is
        // part of a key and so cannot be modified". Build a fresh value instead.
        var canvasResult = CanvasConfig.Create(
            source.Canvas.Width,
            source.Canvas.Height,
            source.Canvas.Scale,
            source.Canvas.BackgroundColor);
        if (canvasResult.IsFailure)
            return Result<VenueLayout>.Failure(canvasResult.Error);

        var layoutResult = Create(
            newName,
            source.LayoutType,
            newOwnerUserId,
            eventId: null,
            isTemplate: true,
            canvas: canvasResult.Value);
        if (layoutResult.IsFailure)
            return layoutResult;
        var clone = layoutResult.Value;

        // Decorations first — straight clone via existing public AddDecoration.
        // Order matches source.Decorations so SortOrder is preserved.
        foreach (var srcDec in source.Decorations.OrderBy(d => d.SortOrder))
        {
            var decResult = clone.AddDecoration(
                srcDec.Kind, srcDec.Label, srcDec.SortOrder, srcDec.Geometry, srcDec.Properties);
            if (decResult.IsFailure)
                return Result<VenueLayout>.Failure(decResult.Error);
        }

        // Zones — clone via AddZone overload that accepts shape/geometry, then
        // RebuildSeatsFrom(...) (S8.9b internal seat-rebuild path) to faithfully
        // copy each seat's row/number/label/sort/x/y/angle + IsEnabled +
        // IsAccessible flags. Track srcZoneId → cloneZoneId so tables can be
        // re-linked to the cloned zones below.
        var zoneIdMap = new Dictionary<Guid, Guid>();
        foreach (var srcZone in source.Zones.OrderBy(z => z.SortOrder))
        {
            var zoneResult = clone.AddZone(
                srcZone.Name, srcZone.Color, srcZone.SortOrder,
                srcZone.Shape, srcZone.Geometry);
            if (zoneResult.IsFailure)
                return Result<VenueLayout>.Failure(zoneResult.Error);
            var newZone = zoneResult.Value;
            newZone.RebuildSeatsFrom(srcZone.Seats);
            zoneIdMap[srcZone.Id] = newZone.Id;
        }

        // Tables — AddTable creates the table without auto-generating seats; then
        // RebuildSeatsFrom(...) clones the full seat set (round-table radial
        // angles + x/y included) with flag fidelity.
        foreach (var srcTable in source.Tables.OrderBy(t => t.SortOrder))
        {
            Guid? cloneZoneId = null;
            if (srcTable.VenueZoneId.HasValue
                && zoneIdMap.TryGetValue(srcTable.VenueZoneId.Value, out var mappedZoneId))
            {
                cloneZoneId = mappedZoneId;
            }

            var tableResult = clone.AddTable(
                srcTable.Label, srcTable.Shape, srcTable.Capacity,
                srcTable.SortOrder, cloneZoneId, srcTable.Geometry);
            if (tableResult.IsFailure)
                return Result<VenueLayout>.Failure(tableResult.Error);
            var newTable = tableResult.Value;
            newTable.RebuildSeatsFrom(srcTable.Seats);
        }

        return Result<VenueLayout>.Success(clone);
    }

    /// <summary>
    /// Replaces the canvas configuration (dimensions, scale, background color).
    /// </summary>
    public Result UpdateCanvas(CanvasConfig canvas)
    {
        if (canvas is null)
            return Result.Failure("Canvas configuration is required");

        Canvas = canvas;
        MarkAsUpdated();
        return Result.Success();
    }

    // ──────────────────────────────────────
    // Zone Management
    // ──────────────────────────────────────

    /// <summary>
    /// Adds a new zone to this layout. Tier mapping is set separately via
    /// <see cref="TicketTier.AssignToZone"/> (Slice 4 polymorphic tier assignments).
    /// </summary>
    public Result<VenueZone> AddZone(
        string name,
        string color,
        int sortOrder)
        => AddZone(name, color, sortOrder, ZoneShape.Rect, geometry: null);

    /// <summary>
    /// Adds a new zone with an explicit canvas shape + geometry. Used by preset
    /// factories (Slice 6) and the canvas editor (Slice 8) to stamp the zone's
    /// rendered shape at creation time rather than through a follow-up UpdateZone.
    /// </summary>
    public Result<VenueZone> AddZone(
        string name,
        string color,
        int sortOrder,
        ZoneShape shape,
        string? geometry)
    {
        // Check for duplicate zone names (case-insensitive)
        if (_zones.Any(z => z.Name.Equals(name?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
            return Result<VenueZone>.Failure($"A zone named '{name?.Trim()}' already exists in this layout");

        var zoneResult = VenueZone.Create(Id, name!, color, sortOrder, shape, geometry);
        if (zoneResult.IsFailure)
            return Result<VenueZone>.Failure(zoneResult.Error);

        _zones.Add(zoneResult.Value);
        MarkAsUpdated();
        return zoneResult;
    }

    /// <summary>
    /// Updates an existing zone's properties. Tier mapping is managed separately via
    /// <see cref="TicketTier.AssignToZone"/> / <see cref="TicketTier.RemoveAssignment"/>.
    /// </summary>
    public Result UpdateZone(
        Guid zoneId,
        string name,
        string color,
        int sortOrder)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == zoneId);
        if (zone == null)
            return Result.Failure("Zone not found in this layout");

        // Check duplicate name excluding the zone being updated
        if (_zones.Any(z => z.Id != zoneId
            && z.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Result.Failure($"A zone named '{name.Trim()}' already exists in this layout");

        var result = zone.Update(name, color, sortOrder);
        if (result.IsSuccess)
            MarkAsUpdated();

        return result;
    }

    /// <summary>
    /// Slice 5 Chunk 5: UpdateZone overload that also replaces the canvas shape + geometry.
    /// Structural (shape/geometry) changes must be guarded by <c>IStructuralEditGuard</c> at
    /// the application layer — this method does not check held/reserved seats.
    /// </summary>
    public Result UpdateZone(
        Guid zoneId,
        string name,
        string color,
        int sortOrder,
        ZoneShape shape,
        string? geometry)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == zoneId);
        if (zone == null)
            return Result.Failure("Zone not found in this layout");

        if (_zones.Any(z => z.Id != zoneId
            && z.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Result.Failure($"A zone named '{name.Trim()}' already exists in this layout");

        var result = zone.Update(name, color, sortOrder, shape, geometry);
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
    /// Every zone with seats must be mapped to an active tier via <see cref="TierAssignment"/>,
    /// and zone seat count must not exceed the mapped tier's capacity.
    /// (Slice 4 — the legacy <c>VenueZone.TicketTierId</c> read path was replaced by the
    /// polymorphic <c>tier_assignments</c> junction.)
    /// </summary>
    public Result ValidateForEvent(IReadOnlyList<TicketTier> eventTiers)
    {
        if (!_zones.Any())
            return Result.Failure("Layout must have at least one zone");

        var activeTiers = eventTiers.Where(t => t.IsActive).ToList();

        // Build a reverse lookup: zoneId → tier (polymorphic), picking the first assignment if multiple tiers map to a zone.
        var zoneToTier = new Dictionary<Guid, TicketTier>();
        foreach (var tier in activeTiers)
        {
            foreach (var assignment in tier.Assignments.Where(a => a.AssignableKind == AssignableKind.Zone))
            {
                zoneToTier.TryAdd(assignment.AssignableId, tier);
            }
        }

        foreach (var zone in _zones)
        {
            if (!zoneToTier.TryGetValue(zone.Id, out var tier))
                return Result.Failure($"Zone '{zone.Name}' must be mapped to a ticket tier");

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
    // Table Management (Slice 2+3A)
    // ──────────────────────────────────────

    /// <summary>
    /// Adds a banquet/dining table to this layout. Seats are NOT auto-generated —
    /// call <see cref="GenerateRoundTable"/> or <see cref="GenerateRectTable"/>
    /// instead to build a table + its seats in one step.
    /// </summary>
    public Result<VenueTable> AddTable(
        string label,
        TableShape shape,
        int capacity,
        int sortOrder,
        Guid? zoneId = null,
        string? geometry = null)
    {
        if (_tables.Any(t => t.Label.Equals(label?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
            return Result<VenueTable>.Failure($"A table labeled '{label?.Trim()}' already exists in this layout");

        if (zoneId.HasValue && _zones.All(z => z.Id != zoneId.Value))
            return Result<VenueTable>.Failure("Referenced zone does not exist in this layout");

        var tableResult = VenueTable.Create(Id, label!, shape, capacity, sortOrder, zoneId, geometry);
        if (tableResult.IsFailure)
            return Result<VenueTable>.Failure(tableResult.Error);

        _tables.Add(tableResult.Value);
        MarkAsUpdated();
        return tableResult;
    }

    /// <summary>
    /// Adds a round table AND radially generates its seats in one operation. The
    /// returned table is already populated with <c>capacity</c> seats.
    /// </summary>
    public Result<VenueTable> GenerateRoundTable(
        string label,
        int capacity,
        int sortOrder,
        Guid? zoneId = null,
        string? geometry = null,
        double startAngleDeg = 0)
    {
        var addResult = AddTable(label, TableShape.Round, capacity, sortOrder, zoneId, geometry);
        if (addResult.IsFailure) return addResult;

        var seatResult = addResult.Value.GenerateRoundTableSeats(startAngleDeg);
        if (seatResult.IsFailure)
        {
            _tables.Remove(addResult.Value);
            return Result<VenueTable>.Failure(seatResult.Error);
        }

        MarkAsUpdated();
        return addResult;
    }

    /// <summary>
    /// Adds a square or rectangular table AND generates its side-distributed seats.
    /// </summary>
    public Result<VenueTable> GenerateRectTable(
        string label,
        TableShape shape,
        int capacity,
        int sortOrder,
        Guid? zoneId = null,
        string? geometry = null)
    {
        if (shape is not (TableShape.Square or TableShape.Rect))
            return Result<VenueTable>.Failure("GenerateRectTable requires Square or Rect shape");

        var addResult = AddTable(label, shape, capacity, sortOrder, zoneId, geometry);
        if (addResult.IsFailure) return addResult;

        var seatResult = addResult.Value.GenerateRectTableSeats();
        if (seatResult.IsFailure)
        {
            _tables.Remove(addResult.Value);
            return Result<VenueTable>.Failure(seatResult.Error);
        }

        MarkAsUpdated();
        return addResult;
    }

    public Result UpdateTable(
        Guid tableId,
        string label,
        TableShape shape,
        int capacity,
        int sortOrder,
        Guid? zoneId,
        string? geometry)
    {
        var table = _tables.FirstOrDefault(t => t.Id == tableId);
        if (table == null)
            return Result.Failure("Table not found in this layout");

        if (_tables.Any(t => t.Id != tableId
            && t.Label.Equals(label?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure($"A table labeled '{label?.Trim()}' already exists in this layout");

        if (zoneId.HasValue && _zones.All(z => z.Id != zoneId.Value))
            return Result.Failure("Referenced zone does not exist in this layout");

        var result = table.Update(label!, shape, capacity, sortOrder, zoneId, geometry);
        if (result.IsSuccess) MarkAsUpdated();
        return result;
    }

    public Result RemoveTable(Guid tableId)
    {
        var table = _tables.FirstOrDefault(t => t.Id == tableId);
        if (table == null)
            return Result.Failure("Table not found in this layout");

        _tables.Remove(table);
        MarkAsUpdated();
        return Result.Success();
    }

    public VenueTable? GetTable(Guid tableId) => _tables.FirstOrDefault(t => t.Id == tableId);

    // ──────────────────────────────────────
    // Decoration Management (Slice 2+3A)
    // ──────────────────────────────────────

    public Result<VenueDecoration> AddDecoration(
        DecorationKind kind,
        string? label,
        int sortOrder,
        string? geometry = null,
        string? properties = null)
    {
        var decorationResult = VenueDecoration.Create(Id, kind, label, sortOrder, geometry, properties);
        if (decorationResult.IsFailure)
            return Result<VenueDecoration>.Failure(decorationResult.Error);

        _decorations.Add(decorationResult.Value);
        MarkAsUpdated();
        return decorationResult;
    }

    public Result UpdateDecoration(
        Guid decorationId,
        DecorationKind kind,
        string? label,
        int sortOrder,
        string? geometry,
        string? properties)
    {
        var decoration = _decorations.FirstOrDefault(d => d.Id == decorationId);
        if (decoration == null)
            return Result.Failure("Decoration not found in this layout");

        var result = decoration.Update(kind, label, sortOrder, geometry, properties);
        if (result.IsSuccess) MarkAsUpdated();
        return result;
    }

    public Result RemoveDecoration(Guid decorationId)
    {
        var decoration = _decorations.FirstOrDefault(d => d.Id == decorationId);
        if (decoration == null)
            return Result.Failure("Decoration not found in this layout");

        _decorations.Remove(decoration);
        MarkAsUpdated();
        return Result.Success();
    }

    public VenueDecoration? GetDecoration(Guid decorationId) =>
        _decorations.FirstOrDefault(d => d.Id == decorationId);

    // ──────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────

    private Seat? FindSeat(Guid seatId)
    {
        foreach (var zone in _zones)
        {
            var seat = zone.Seats.FirstOrDefault(s => s.Id == seatId);
            if (seat != null) return seat;
        }

        foreach (var table in _tables)
        {
            var seat = table.Seats.FirstOrDefault(s => s.Id == seatId);
            if (seat != null) return seat;
        }

        return null;
    }
}
