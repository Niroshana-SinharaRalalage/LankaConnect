using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.BatchUpdateLayout;

/// <summary>
/// Slice 5 Chunk 10: PUT /api/venue-layouts/{id}/batch — atomic full-layout replacement
/// used by the Slice 8 canvas editor. The payload is a complete snapshot of the desired
/// zones/tables/decorations lists plus the layout-level Name and CanvasConfig.
/// Within each child list:
/// <list type="bullet">
///   <item>Item with <c>Id == null</c> → created</item>
///   <item>Item with existing <c>Id</c> → updated in place</item>
///   <item>Existing child missing from the list → removed (guarded on held/reserved seats)</item>
/// </list>
/// <c>ExpectedRowVersion</c> sourced from the <c>If-Match</c> header for optimistic
/// concurrency; stale values return HTTP 409. Structural removals (zone/table with seats)
/// are guarded by <c>IStructuralEditGuard</c> and return HTTP 422 when seats are held or
/// reserved. Seat-level edits remain through the existing per-seat endpoints; this batch
/// endpoint only mutates the zone/table/decoration shell (existing seats are preserved on
/// updates, discarded on removals).
/// </summary>
public record BatchUpdateLayoutCommand(
    Guid LayoutId,
    uint ExpectedRowVersion,
    BatchLayoutPayload Payload
) : ICommand;

public record BatchLayoutPayload(
    string? Name,
    BatchCanvasConfig? Canvas,
    List<BatchZone>? Zones,
    List<BatchTable>? Tables,
    List<BatchDecoration>? Decorations
);

public record BatchCanvasConfig(
    int Width,
    int Height,
    double Scale,
    string BackgroundColor
);

public record BatchZone(
    Guid? Id,
    string Name,
    string Color,
    int SortOrder,
    ZoneShape Shape,
    string? Geometry
);

public record BatchTable(
    Guid? Id,
    string Label,
    TableShape Shape,
    int Capacity,
    int SortOrder,
    Guid? ZoneId,
    string? Geometry
);

public record BatchDecoration(
    Guid? Id,
    DecorationKind Kind,
    string? Label,
    int SortOrder,
    string? Geometry,
    string? Properties
);
