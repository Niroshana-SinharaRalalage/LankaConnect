using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Application.Commands.BatchUpdateLayout;

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
    List<BatchDecoration>? Decorations,
    /// <summary>
    /// Slice 8 S8.8c: declarative reconciliation of the polymorphic
    /// <c>tier_assignments</c> junction. The list is the *complete desired
    /// state* per <c>(Kind, AssignableId)</c> tuple — handler diffs against
    /// the layout's current assignments, removes obsolete tuples, and adds
    /// new ones inside the same <c>UnitOfWork</c> commit. <c>null</c> means
    /// "do not reconcile" (preserves backward compatibility for callers that
    /// don't manage tiers); an empty list means "remove every assignment".
    /// For newly-added zones/tables the <c>AssignableId</c> may be a client-side
    /// draft Guid — the handler resolves it to the server-assigned Guid via
    /// each <see cref="BatchZone.ClientId"/> / <see cref="BatchTable.ClientId"/>.
    /// </summary>
    List<BatchTierAssignment>? TierAssignments = null,
    /// <summary>
    /// Slice S2 (Architect Rev 4 §A.3): explicit deletion opt-in. When the
    /// payload's <c>Zones</c> list omits an existing zone, the handler now
    /// requires the omitted zone's id to appear in <c>DeletedZoneIds</c> —
    /// otherwise the call returns <b>HTTP 409 Conflict</b>. Closes the
    /// destructive-PUT bug class: pre-S2, any client bug that dropped a zone
    /// from the payload silently deleted it (only protected by the structural
    /// guard for held/reserved seats — empty zones got nuked with no warning).
    /// <c>null</c> AND <c>empty list</c> both mean "no explicit deletions" —
    /// any omission is therefore unintentional → 409.
    /// </summary>
    List<Guid>? DeletedZoneIds = null,
    List<Guid>? DeletedTableIds = null,
    List<Guid>? DeletedDecorationIds = null
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
    string? Geometry,
    /// <summary>
    /// Slice 8 S8.8c: optional client-side draft Guid for newly-added zones
    /// (<see cref="Id"/> == null). Lets <c>BatchTierAssignment.AssignableId</c>
    /// reference the new zone before the server has assigned its real Guid.
    /// Ignored when <see cref="Id"/> is set.
    /// </summary>
    Guid? ClientId = null,
    /// <summary>
    /// Slice 9.5 — theater-style seat-generation parameters. When BOTH are
    /// supplied (non-null AND positive), the handler invokes
    /// <c>VenueLayout.GenerateTheaterSeats</c> on the zone after add/update.
    /// The domain method clears any existing seats before regenerating, so
    /// passing these for a zone that already has seats is destructive — the
    /// frontend property panel currently only surfaces these inputs for
    /// empty zones. Both omitted = no seat-gen (preserves existing seats).
    /// </summary>
    int? RowCount = null,
    int? SeatsPerRow = null
);

public record BatchTable(
    Guid? Id,
    string Label,
    TableShape Shape,
    int Capacity,
    int SortOrder,
    Guid? ZoneId,
    string? Geometry,
    /// <summary>
    /// Slice 8 S8.8c: optional client-side draft Guid for newly-added tables.
    /// See <see cref="BatchZone.ClientId"/> for the same semantics.
    /// </summary>
    Guid? ClientId = null
);

public record BatchDecoration(
    Guid? Id,
    DecorationKind Kind,
    string? Label,
    int SortOrder,
    string? Geometry,
    string? Properties
);

/// <summary>
/// Slice 8 S8.8c: desired tier-assignment state for a single zone or table
/// in the canvas-editor batch save. <c>TierIds</c> is the complete set of
/// tiers the organizer wants assigned to <c>(Kind, AssignableId)</c> after
/// the save lands — the handler diffs against the current assignments and
/// applies the minimum set of <c>AssignToZone</c> / <c>AssignToTable</c> /
/// <c>RemoveAssignment</c> domain calls in the same UoW commit.
/// </summary>
public record BatchTierAssignment(
    AssignableKind Kind,
    Guid AssignableId,
    List<Guid> TierIds
);
