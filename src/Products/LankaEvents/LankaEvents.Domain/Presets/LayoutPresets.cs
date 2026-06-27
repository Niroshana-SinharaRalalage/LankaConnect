using System.Globalization;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Domain.Presets;

/// <summary>
/// Slice 6 Chunk S6.1: industry-standard layout presets. Produces a fully-populated
/// <see cref="VenueLayout"/> (zones, tables, decorations, generated seats) that the
/// organizer can use as-is or later customize in the canvas editor (Slice 8).
///
/// Preset IDs are exposed as public constants so the API, frontend, and metrics
/// can reference them without typos. <see cref="All"/> is the source-of-truth
/// metadata list used by the preset-library modal; each entry's
/// <see cref="PresetMetadata.ThumbnailUrl"/> points to a pre-generated PNG in
/// <c>/public/layouts/presets/</c> — the modal does NOT render react-konva for
/// thumbnails, keeping the react-konva bundle lazy-loaded until the canvas
/// editor or SeatPicker actually need it (architect soft-issue B1).
/// </summary>
public static class LayoutPresets
{
    // ─────────────────────── Preset IDs ───────────────────────
    public const string TheaterClassicId      = "theater-classic";
    public const string TheaterWithBalconyId  = "theater-with-balcony";
    public const string TheaterWithAislesId   = "theater-with-aisles";
    public const string TheaterCurvedId       = "theater-curved";
    public const string BanquetRound8Id       = "banquet-round-8";
    public const string BanquetRound10Id      = "banquet-round-10";
    public const string BanquetMixedId        = "banquet-mixed";
    public const string ConferenceRoomId      = "conference-room";

    // ─────────────────────── Metadata ───────────────────────

    /// <summary>
    /// Metadata exposed to the preset-library modal. Thumbnails are static PNGs,
    /// not react-konva renders — the modal stays cheap to load.
    /// </summary>
    public record PresetMetadata(
        string Id,
        string Name,
        string Description,
        LayoutType LayoutType,
        int TotalCapacity,
        string ThumbnailUrl);

    private const string ThumbnailRoot = "/layouts/presets/";

    /// <summary>
    /// All 8 presets in the order they should appear in the library modal.
    /// Capacity numbers are enforced by a unit test that rebuilds every preset
    /// and asserts <see cref="VenueLayout.TotalCapacity"/> matches this metadata.
    /// </summary>
    public static IReadOnlyList<PresetMetadata> All { get; } = new List<PresetMetadata>
    {
        new(TheaterClassicId,     "Theater Classic",
            "10 rows × 20 seats facing a central stage.",
            LayoutType.Theater, TotalCapacity: 200, ThumbnailRoot + "theater-classic.svg"),

        new(TheaterWithBalconyId, "Theater with Balcony",
            "Orchestra + Mezzanine + Balcony sections.",
            LayoutType.Theater, TotalCapacity: 420, ThumbnailRoot + "theater-with-balcony.svg"),

        new(TheaterWithAislesId,  "Theater with Aisles",
            "Wide center section flanked by two side sections with aisles.",
            LayoutType.Theater, TotalCapacity: 240, ThumbnailRoot + "theater-with-aisles.svg"),

        new(TheaterCurvedId,      "Theater Curved",
            "Curved front rows following the stage edge, straight rear section.",
            LayoutType.Theater, TotalCapacity: 160, ThumbnailRoot + "theater-curved.svg"),

        new(BanquetRound8Id,      "Banquet · 15 Round Tables × 8",
            "15 round tables, 8 seats each. Ideal for a seated dinner.",
            LayoutType.Banquet, TotalCapacity: 120, ThumbnailRoot + "banquet-round-8.svg"),

        new(BanquetRound10Id,     "Banquet · 15 Round Tables × 10",
            "15 round tables, 10 seats each. Larger banquet capacity.",
            LayoutType.Banquet, TotalCapacity: 150, ThumbnailRoot + "banquet-round-10.svg"),

        new(BanquetMixedId,       "Banquet Mixed",
            "10 round tables plus 5 rectangular head tables, dance floor at center.",
            LayoutType.Banquet, TotalCapacity: 120, ThumbnailRoot + "banquet-mixed.svg"),

        new(ConferenceRoomId,     "Conference Room",
            "U-shape head tables with classroom-style rows behind.",
            LayoutType.Mixed,   TotalCapacity:  68, ThumbnailRoot + "conference-room.svg"),
    }.AsReadOnly();

    /// <summary>
    /// Resolves metadata by preset ID. Returns <c>null</c> for unknown IDs.
    /// </summary>
    public static PresetMetadata? FindMetadata(string presetId) =>
        All.FirstOrDefault(p => string.Equals(p.Id, presetId, StringComparison.Ordinal));

    // ─────────────────────── Factory ───────────────────────

    /// <summary>
    /// Builds a fully-populated <see cref="VenueLayout"/> from the given preset ID.
    /// When <paramref name="eventId"/> is null, the layout is created as a template
    /// (<see cref="VenueLayout.IsTemplate"/> = true); otherwise it is attached to
    /// the event. Returns <see cref="ErrorKind.NotFound"/> for unknown IDs.
    /// </summary>
    public static Result<VenueLayout> Create(
        string presetId,
        Guid createdByUserId,
        Guid? eventId = null)
    {
        var meta = FindMetadata(presetId);
        if (meta is null)
            return Result<VenueLayout>.NotFound($"Unknown preset ID '{presetId}'");

        return presetId switch
        {
            TheaterClassicId      => BuildTheaterClassic(meta, createdByUserId, eventId),
            TheaterWithBalconyId  => BuildTheaterWithBalcony(meta, createdByUserId, eventId),
            TheaterWithAislesId   => BuildTheaterWithAisles(meta, createdByUserId, eventId),
            TheaterCurvedId       => BuildTheaterCurved(meta, createdByUserId, eventId),
            BanquetRound8Id       => BuildBanquetRound(meta, createdByUserId, eventId, seatsPerTable: 8),
            BanquetRound10Id      => BuildBanquetRound(meta, createdByUserId, eventId, seatsPerTable: 10),
            BanquetMixedId        => BuildBanquetMixed(meta, createdByUserId, eventId),
            ConferenceRoomId      => BuildConferenceRoom(meta, createdByUserId, eventId),
            _                     => Result<VenueLayout>.NotFound($"Unknown preset ID '{presetId}'"),
        };
    }

    // ─────────────────────── Geometry helpers ───────────────────────

    /// <summary>Rect geometry JSON in canvas pixel coordinates.</summary>
    private static string RectGeometry(int x, int y, int width, int height, double rotation = 0) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{{\"x\":{x},\"y\":{y},\"width\":{width},\"height\":{height},\"rotation\":{rotation}}}");

    /// <summary>Curve geometry JSON. Angles in degrees, 0° = 3 o'clock.</summary>
    private static string CurveGeometry(
        int centerX, int centerY, int radius,
        double startAngleDeg, double sweepAngleDeg, int rowCount) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{{\"centerX\":{centerX},\"centerY\":{centerY},\"radius\":{radius}," +
            $"\"startAngleDeg\":{startAngleDeg},\"sweepAngleDeg\":{sweepAngleDeg},\"rowCount\":{rowCount}}}");

    /// <summary>Round-table geometry (center + radius).</summary>
    private static string RoundTableGeometry(int centerX, int centerY, int radius) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{{\"centerX\":{centerX},\"centerY\":{centerY},\"radius\":{radius}}}");

    /// <summary>Rect-table geometry (center + width/height).</summary>
    private static string RectTableGeometry(int centerX, int centerY, int width, int height, double rotation = 0) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{{\"centerX\":{centerX},\"centerY\":{centerY},\"width\":{width},\"height\":{height},\"rotation\":{rotation}}}");

    // Zone colors chosen to be distinguishable on a light canvas.
    private const string ColorOrchestra = "#3b82f6"; // blue
    private const string ColorMezzanine = "#8b5cf6"; // purple
    private const string ColorBalcony   = "#ec4899"; // pink
    private const string ColorLeft      = "#10b981"; // emerald
    private const string ColorCenter    = "#3b82f6"; // blue
    private const string ColorRight     = "#f59e0b"; // amber
    private const string ColorCurved    = "#3b82f6";
    private const string ColorRear      = "#6b7280"; // gray
    private const string ColorHead      = "#ef4444"; // red
    private const string ColorClassroom = "#3b82f6";

    // ─────────────────────── Builders ───────────────────────

    private static Result<VenueLayout> BuildTheaterClassic(
        PresetMetadata meta, Guid userId, Guid? eventId)
    {
        var layoutResult = VenueLayout.Create(
            meta.Name, LayoutType.Theater, userId, eventId, isTemplate: !eventId.HasValue);
        if (layoutResult.IsFailure) return layoutResult;
        var layout = layoutResult.Value;

        var stage = layout.AddDecoration(
            DecorationKind.Stage, "Stage", sortOrder: 0,
            geometry: RectGeometry(200, 20, 800, 80));
        if (stage.IsFailure) return Result<VenueLayout>.Failure(stage.Error);

        var zone = layout.AddZone(
            "Main Floor", ColorOrchestra, sortOrder: 0,
            ZoneShape.Rect, RectGeometry(100, 140, 1000, 600));
        if (zone.IsFailure) return Result<VenueLayout>.Failure(zone.Error);

        var seats = layout.GenerateTheaterSeats(zone.Value.Id, rows: 10, seatsPerRow: 20);
        if (seats.IsFailure) return Result<VenueLayout>.Failure(seats.Error);

        return Result<VenueLayout>.Success(layout);
    }

    private static Result<VenueLayout> BuildTheaterWithBalcony(
        PresetMetadata meta, Guid userId, Guid? eventId)
    {
        var layoutResult = VenueLayout.Create(
            meta.Name, LayoutType.Theater, userId, eventId, isTemplate: !eventId.HasValue);
        if (layoutResult.IsFailure) return layoutResult;
        var layout = layoutResult.Value;

        var stage = layout.AddDecoration(
            DecorationKind.Stage, "Stage", sortOrder: 0,
            geometry: RectGeometry(300, 20, 600, 80));
        if (stage.IsFailure) return Result<VenueLayout>.Failure(stage.Error);

        var orchestra = layout.AddZone(
            "Orchestra", ColorOrchestra, 0, ZoneShape.Rect,
            RectGeometry(100, 140, 1000, 320));
        if (orchestra.IsFailure) return Result<VenueLayout>.Failure(orchestra.Error);
        var orch = layout.GenerateTheaterSeats(orchestra.Value.Id, rows: 10, seatsPerRow: 20);
        if (orch.IsFailure) return Result<VenueLayout>.Failure(orch.Error);

        var mezzanine = layout.AddZone(
            "Mezzanine", ColorMezzanine, 1, ZoneShape.Rect,
            RectGeometry(100, 480, 1000, 200));
        if (mezzanine.IsFailure) return Result<VenueLayout>.Failure(mezzanine.Error);
        var mezz = layout.GenerateTheaterSeats(mezzanine.Value.Id, rows: 6, seatsPerRow: 20, startRowLabel: "K");
        if (mezz.IsFailure) return Result<VenueLayout>.Failure(mezz.Error);

        var balcony = layout.AddZone(
            "Balcony", ColorBalcony, 2, ZoneShape.Rect,
            RectGeometry(100, 700, 1000, 80));
        if (balcony.IsFailure) return Result<VenueLayout>.Failure(balcony.Error);
        var balc = layout.GenerateTheaterSeats(balcony.Value.Id, rows: 5, seatsPerRow: 20, startRowLabel: "Q");
        if (balc.IsFailure) return Result<VenueLayout>.Failure(balc.Error);

        return Result<VenueLayout>.Success(layout);
    }

    private static Result<VenueLayout> BuildTheaterWithAisles(
        PresetMetadata meta, Guid userId, Guid? eventId)
    {
        var layoutResult = VenueLayout.Create(
            meta.Name, LayoutType.Theater, userId, eventId, isTemplate: !eventId.HasValue);
        if (layoutResult.IsFailure) return layoutResult;
        var layout = layoutResult.Value;

        var stage = layout.AddDecoration(
            DecorationKind.Stage, "Stage", 0, RectGeometry(300, 20, 600, 80));
        if (stage.IsFailure) return Result<VenueLayout>.Failure(stage.Error);

        var left = layout.AddZone("Left Section", ColorLeft, 0, ZoneShape.Rect,
            RectGeometry(60, 140, 300, 600));
        if (left.IsFailure) return Result<VenueLayout>.Failure(left.Error);
        var leftSeats = layout.GenerateTheaterSeats(left.Value.Id, rows: 8, seatsPerRow: 7);
        if (leftSeats.IsFailure) return Result<VenueLayout>.Failure(leftSeats.Error);

        var aisle1 = layout.AddDecoration(
            DecorationKind.Aisle, "Aisle Left", 1, RectGeometry(370, 140, 20, 600));
        if (aisle1.IsFailure) return Result<VenueLayout>.Failure(aisle1.Error);

        var center = layout.AddZone("Center Section", ColorCenter, 1, ZoneShape.Rect,
            RectGeometry(400, 140, 400, 600));
        if (center.IsFailure) return Result<VenueLayout>.Failure(center.Error);
        var centerSeats = layout.GenerateTheaterSeats(center.Value.Id, rows: 8, seatsPerRow: 16);
        if (centerSeats.IsFailure) return Result<VenueLayout>.Failure(centerSeats.Error);

        var aisle2 = layout.AddDecoration(
            DecorationKind.Aisle, "Aisle Right", 2, RectGeometry(810, 140, 20, 600));
        if (aisle2.IsFailure) return Result<VenueLayout>.Failure(aisle2.Error);

        var right = layout.AddZone("Right Section", ColorRight, 2, ZoneShape.Rect,
            RectGeometry(840, 140, 300, 600));
        if (right.IsFailure) return Result<VenueLayout>.Failure(right.Error);
        var rightSeats = layout.GenerateTheaterSeats(right.Value.Id, rows: 8, seatsPerRow: 7);
        if (rightSeats.IsFailure) return Result<VenueLayout>.Failure(rightSeats.Error);

        return Result<VenueLayout>.Success(layout);
    }

    private static Result<VenueLayout> BuildTheaterCurved(
        PresetMetadata meta, Guid userId, Guid? eventId)
    {
        var layoutResult = VenueLayout.Create(
            meta.Name, LayoutType.Theater, userId, eventId, isTemplate: !eventId.HasValue);
        if (layoutResult.IsFailure) return layoutResult;
        var layout = layoutResult.Value;

        var stage = layout.AddDecoration(
            DecorationKind.Stage, "Stage", 0, RectGeometry(300, 20, 600, 80));
        if (stage.IsFailure) return Result<VenueLayout>.Failure(stage.Error);

        // Front curved zone — 4 rows × 20 seats. Geometry describes the arc
        // the editor will render; the seat generator produces flat rows that
        // the canvas editor can reposition along the arc.
        var front = layout.AddZone("Front (Curved)", ColorCurved, 0, ZoneShape.Curve,
            CurveGeometry(centerX: 600, centerY: 100, radius: 380,
                          startAngleDeg: 20, sweepAngleDeg: 140, rowCount: 4));
        if (front.IsFailure) return Result<VenueLayout>.Failure(front.Error);
        var frontSeats = layout.GenerateTheaterSeats(front.Value.Id, rows: 4, seatsPerRow: 20);
        if (frontSeats.IsFailure) return Result<VenueLayout>.Failure(frontSeats.Error);

        var rear = layout.AddZone("Rear", ColorRear, 1, ZoneShape.Rect,
            RectGeometry(100, 550, 1000, 250));
        if (rear.IsFailure) return Result<VenueLayout>.Failure(rear.Error);
        var rearSeats = layout.GenerateTheaterSeats(rear.Value.Id, rows: 4, seatsPerRow: 20, startRowLabel: "E");
        if (rearSeats.IsFailure) return Result<VenueLayout>.Failure(rearSeats.Error);

        return Result<VenueLayout>.Success(layout);
    }

    private static Result<VenueLayout> BuildBanquetRound(
        PresetMetadata meta, Guid userId, Guid? eventId, int seatsPerTable)
    {
        var layoutResult = VenueLayout.Create(
            meta.Name, LayoutType.Banquet, userId, eventId, isTemplate: !eventId.HasValue);
        if (layoutResult.IsFailure) return layoutResult;
        var layout = layoutResult.Value;

        var stage = layout.AddDecoration(
            DecorationKind.Stage, "Stage", 0, RectGeometry(400, 20, 400, 60));
        if (stage.IsFailure) return Result<VenueLayout>.Failure(stage.Error);

        // 3 rows × 5 cols = 15 tables, centered on 1200×800 canvas.
        const int cols = 5;
        const int rows = 3;
        const int radius = 55;
        const int stepX = 220;
        const int stepY = 210;
        const int originX = 140;
        const int originY = 160;

        var sortOrder = 0;
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var label = $"T{sortOrder + 1}";
                var cx = originX + c * stepX;
                var cy = originY + r * stepY;
                var table = layout.GenerateRoundTable(
                    label, seatsPerTable, sortOrder,
                    zoneId: null,
                    geometry: RoundTableGeometry(cx, cy, radius));
                if (table.IsFailure) return Result<VenueLayout>.Failure(table.Error);
                sortOrder++;
            }
        }

        return Result<VenueLayout>.Success(layout);
    }

    private static Result<VenueLayout> BuildBanquetMixed(
        PresetMetadata meta, Guid userId, Guid? eventId)
    {
        var layoutResult = VenueLayout.Create(
            meta.Name, LayoutType.Banquet, userId, eventId, isTemplate: !eventId.HasValue);
        if (layoutResult.IsFailure) return layoutResult;
        var layout = layoutResult.Value;

        var stage = layout.AddDecoration(
            DecorationKind.Stage, "Stage", 0, RectGeometry(400, 20, 400, 60));
        if (stage.IsFailure) return Result<VenueLayout>.Failure(stage.Error);

        // 5 rect head tables across the top, under the stage.
        const int headY = 120;
        for (var i = 0; i < 5; i++)
        {
            var table = layout.GenerateRectTable(
                $"H{i + 1}", TableShape.Rect, capacity: 8, sortOrder: i,
                zoneId: null,
                geometry: RectTableGeometry(centerX: 180 + i * 220, centerY: headY, width: 160, height: 60));
            if (table.IsFailure) return Result<VenueLayout>.Failure(table.Error);
        }

        // Dance floor in the middle.
        var danceFloor = layout.AddDecoration(
            DecorationKind.DanceFloor, "Dance Floor", 10,
            RectGeometry(350, 340, 500, 160));
        if (danceFloor.IsFailure) return Result<VenueLayout>.Failure(danceFloor.Error);

        // 10 round tables in 2 rows × 5 cols below the dance floor.
        const int tableRadius = 55;
        const int rndOriginX = 140;
        const int rndOriginY = 560;
        const int rndStepX = 220;
        const int rndStepY = 170;
        var rndOrder = 5;
        for (var r = 0; r < 2; r++)
        {
            for (var c = 0; c < 5; c++)
            {
                var cx = rndOriginX + c * rndStepX;
                var cy = rndOriginY + r * rndStepY;
                var table = layout.GenerateRoundTable(
                    $"T{rndOrder - 4}", capacity: 8, sortOrder: rndOrder,
                    zoneId: null,
                    geometry: RoundTableGeometry(cx, cy, tableRadius));
                if (table.IsFailure) return Result<VenueLayout>.Failure(table.Error);
                rndOrder++;
            }
        }

        return Result<VenueLayout>.Success(layout);
    }

    private static Result<VenueLayout> BuildConferenceRoom(
        PresetMetadata meta, Guid userId, Guid? eventId)
    {
        var layoutResult = VenueLayout.Create(
            meta.Name, LayoutType.Mixed, userId, eventId, isTemplate: !eventId.HasValue);
        if (layoutResult.IsFailure) return layoutResult;
        var layout = layoutResult.Value;

        // U-shape: one head table on top, two side tables running down.
        var top = layout.GenerateRectTable(
            "Head", TableShape.Rect, capacity: 8, sortOrder: 0,
            zoneId: null,
            geometry: RectTableGeometry(600, 120, 500, 60));
        if (top.IsFailure) return Result<VenueLayout>.Failure(top.Error);

        var leftSide = layout.GenerateRectTable(
            "Left", TableShape.Rect, capacity: 8, sortOrder: 1,
            zoneId: null,
            geometry: RectTableGeometry(340, 280, 60, 260, rotation: 90));
        if (leftSide.IsFailure) return Result<VenueLayout>.Failure(leftSide.Error);

        var rightSide = layout.GenerateRectTable(
            "Right", TableShape.Rect, capacity: 8, sortOrder: 2,
            zoneId: null,
            geometry: RectTableGeometry(860, 280, 60, 260, rotation: 90));
        if (rightSide.IsFailure) return Result<VenueLayout>.Failure(rightSide.Error);

        // Classroom rows behind the U.
        var rows = layout.AddZone("Classroom Rows", ColorClassroom, 0, ZoneShape.Rect,
            RectGeometry(150, 500, 900, 280));
        if (rows.IsFailure) return Result<VenueLayout>.Failure(rows.Error);
        var rowSeats = layout.GenerateTheaterSeats(rows.Value.Id, rows: 4, seatsPerRow: 11);
        if (rowSeats.IsFailure) return Result<VenueLayout>.Failure(rowSeats.Error);

        return Result<VenueLayout>.Success(layout);
    }
}
