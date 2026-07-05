using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Commands.BatchUpdateLayout;

/// <summary>
/// Slice 5 Chunk 10 + Slice 8 S8.8c: atomic batch replacement of a venue layout's
/// zones, tables, decorations, and (S8.8c) polymorphic tier assignments. See
/// <see cref="BatchUpdateLayoutCommand"/> for semantics.
///
/// Flow:
///   1. Authorize (two-branch via <see cref="ILayoutAuthorizationService"/>).
///   2. Load full aggregate (zones + tables + decorations + seats).
///   3. Early concurrency check vs. <c>ExpectedRowVersion</c>.
///   4. Compute desired removals (zones/tables not present in payload).
///      Collect seats owned by those children → <see cref="IStructuralEditGuard"/>.
///      A failure here short-circuits with 422 before mutating anything.
///   5. Apply in order: removals → updates → additions for zones, then tables, then
///      decorations, then layout-level Name + Canvas. Each mutation goes through a
///      domain method so invariants stay in the domain layer.
///   6. (S8.8c) Reconcile <c>TierAssignments</c> against the loaded
///      <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.TicketTier"/>s for the event. Resolves
///      newly-added zone/table client-Guids → server-Guids via the
///      <c>ClientId</c> fields on <see cref="BatchZone"/> / <see cref="BatchTable"/>.
///      Diffs current vs desired and calls <c>AssignToZone</c> / <c>AssignToTable</c> /
///      <c>RemoveAssignment</c> on the affected tiers. Skipped when
///      <c>payload.TierAssignments == null</c> (backward-compat for callers that
///      don't manage tiers).
///   7. <see cref="IVenueLayoutRepository.SetOriginalRowVersion"/> + commit.
///      <see cref="DbUpdateConcurrencyException"/> → 409. Domain mutations on the
///      <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.TicketTier"/> aggregates flush in the
///      same <c>SaveChanges</c>.
/// </summary>
public class BatchUpdateLayoutCommandHandler : ICommandHandler<BatchUpdateLayoutCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IStructuralEditGuard _structuralGuard;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILayoutMetrics _metrics;
    private readonly ILogger<BatchUpdateLayoutCommandHandler> _logger;

    public BatchUpdateLayoutCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IStructuralEditGuard structuralGuard,
        IVenueLayoutRepository layoutRepository,
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILayoutMetrics metrics,
        ILogger<BatchUpdateLayoutCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _structuralGuard = structuralGuard;
        _layoutRepository = layoutRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Result> Handle(BatchUpdateLayoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "BatchUpdateLayout: LayoutId={LayoutId}, ExpectedRowVersion={RowVersion}, " +
            "ZonesInPayload={ZoneCount}, TablesInPayload={TableCount}, DecorationsInPayload={DecorationCount}",
            request.LayoutId, request.ExpectedRowVersion,
            request.Payload.Zones?.Count ?? 0,
            request.Payload.Tables?.Count ?? 0,
            request.Payload.Decorations?.Count ?? 0);

        if (request.Payload is null)
        {
            EmitSaveFailed(request.LayoutId, "validation_failed");
            return Result.Failure("Batch payload is required");
        }

        var authResult = await _authorizationService.AuthorizeAsync(request.LayoutId, cancellationToken);
        if (authResult.IsFailure)
        {
            _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.AuthFailed);
            EmitSaveFailed(request.LayoutId, "auth_failed");
            return Result.Failure(authResult.Error, authResult.ErrorKind);
        }

        VenueLayout? layout;
        try
        {
            layout = await _layoutRepository.GetWithZonesAndSeatsAsync(request.LayoutId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "BatchUpdateLayout: failed to load layout aggregate. LayoutId={LayoutId}",
                request.LayoutId);
            throw;
        }

        if (layout is null)
        {
            EmitSaveFailed(request.LayoutId, "not_found");
            return Result.NotFound("Venue layout not found");
        }

        if (layout.RowVersion != request.ExpectedRowVersion)
        {
            _logger.LogWarning(
                "BatchUpdateLayout: early concurrency conflict. LayoutId={LayoutId}, Expected={Expected}, Actual={Actual}",
                request.LayoutId, request.ExpectedRowVersion, layout.RowVersion);
            _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.ConcurrencyConflict);
            EmitSaveFailed(request.LayoutId, "concurrency_conflict");
            return Result.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }

        var payload = request.Payload;
        var zones = payload.Zones ?? new List<BatchZone>();
        var tables = payload.Tables ?? new List<BatchTable>();
        var decorations = payload.Decorations ?? new List<BatchDecoration>();

        // Structural guard: gather seats owned by zones/tables slated for removal.
        // Updates are NOT treated as structural here (zone/table updates keep their
        // existing seats). The guard short-circuits on an empty set.
        var payloadZoneIds = zones.Where(z => z.Id.HasValue).Select(z => z.Id!.Value).ToHashSet();
        var payloadTableIds = tables.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToHashSet();

        var zonesToRemove = layout.Zones.Where(z => !payloadZoneIds.Contains(z.Id)).ToList();
        var tablesToRemove = layout.Tables.Where(t => !payloadTableIds.Contains(t.Id)).ToList();

        // Slice S2 (Architect Rev 4 §A.3) — destructive-PUT protection. Any item
        // that's missing from the payload AND not explicitly listed in deletedXIds
        // is a CLIENT-side bug, not an intentional deletion. Return 409 Conflict
        // so the bug surfaces instead of silently destroying data. Pre-S2, this
        // check did not exist — empty zones / tables / decorations were nuked with
        // no warning whenever a client payload accidentally dropped them.
        var deletedZoneIds = (payload.DeletedZoneIds ?? new List<Guid>()).ToHashSet();
        var deletedTableIds = (payload.DeletedTableIds ?? new List<Guid>()).ToHashSet();
        var deletedDecorationIds = (payload.DeletedDecorationIds ?? new List<Guid>()).ToHashSet();

        var unintendedZoneRemovals = zonesToRemove
            .Where(z => !deletedZoneIds.Contains(z.Id))
            .Select(z => z.Id)
            .ToList();
        var unintendedTableRemovals = tablesToRemove
            .Where(t => !deletedTableIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToList();
        var payloadDecorationIdsForGuard = decorations.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToHashSet();
        var unintendedDecorationRemovals = layout.Decorations
            .Where(d => !payloadDecorationIdsForGuard.Contains(d.Id) && !deletedDecorationIds.Contains(d.Id))
            .Select(d => d.Id)
            .ToList();

        if (unintendedZoneRemovals.Count > 0
            || unintendedTableRemovals.Count > 0
            || unintendedDecorationRemovals.Count > 0)
        {
            _logger.LogWarning(
                "BatchUpdateLayout: ambiguous payload — unintended removals. LayoutId={LayoutId}, " +
                "OmittedZones={OmittedZones}, OmittedTables={OmittedTables}, OmittedDecorations={OmittedDecorations}",
                request.LayoutId,
                unintendedZoneRemovals.Count, unintendedTableRemovals.Count, unintendedDecorationRemovals.Count);

            // Build a precise message naming the omitted ids so the client can either
            // include them in the payload (to keep them) or list them in deletedXIds
            // (to delete them).
            var parts = new List<string>();
            if (unintendedZoneRemovals.Count > 0)
                parts.Add($"{unintendedZoneRemovals.Count} zone(s): [{string.Join(", ", unintendedZoneRemovals)}]");
            if (unintendedTableRemovals.Count > 0)
                parts.Add($"{unintendedTableRemovals.Count} table(s): [{string.Join(", ", unintendedTableRemovals)}]");
            if (unintendedDecorationRemovals.Count > 0)
                parts.Add($"{unintendedDecorationRemovals.Count} decoration(s): [{string.Join(", ", unintendedDecorationRemovals)}]");

            var message =
                $"Layout has shapes the payload neither lists nor explicitly deletes: {string.Join("; ", parts)}. " +
                $"To keep them, include them in the corresponding zones/tables/decorations array. " +
                $"To delete them, list their IDs in deletedZoneIds / deletedTableIds / deletedDecorationIds.";

            _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.ConcurrencyConflict);
            EmitSaveFailed(request.LayoutId, "structural_edit_rejected");
            return Result.Conflict(message);
        }

        var seatsAtRisk = zonesToRemove.SelectMany(z => z.Seats.Select(s => s.Id))
            .Concat(tablesToRemove.SelectMany(t => t.Seats.Select(s => s.Id)))
            .ToList();

        if (seatsAtRisk.Count > 0)
        {
            var guardResult = await _structuralGuard.CheckSeatsAsync(seatsAtRisk, cancellationToken);
            if (guardResult.IsFailure)
            {
                _logger.LogWarning(
                    "BatchUpdateLayout: structural guard rejected removals. LayoutId={LayoutId}, SeatsAtRisk={Count}",
                    request.LayoutId, seatsAtRisk.Count);
                _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.SeatsReserved);
                EmitSaveFailed(request.LayoutId, "structural_edit_rejected");
                return guardResult;
            }
        }

        // ----- Apply changes (removals → updates → additions) -----
        //
        // Slice 8 S8.8a: track each mutation so the post-commit `layout.canvas_editor_saved`
        // metric can carry an honest `changesCount` tag. Counts every domain mutation the
        // handler issues — server-side perspective on "what was applied" rather than the
        // client-side "what was sent", which keeps the dashboard immune to clients that
        // include unchanged items in the payload.
        var changesCount = 0;

        // Decorations — cheapest, no seat impact.
        var payloadDecorationIds = decorations.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToHashSet();
        foreach (var existing in layout.Decorations.Where(d => !payloadDecorationIds.Contains(d.Id)).ToList())
        {
            var removeResult = layout.RemoveDecoration(existing.Id);
            if (removeResult.IsFailure) return removeResult;
            changesCount++;
        }

        // Zones — remove missing, update existing, add new.
        foreach (var zoneToRemove in zonesToRemove)
        {
            var removeResult = layout.RemoveZone(zoneToRemove.Id);
            if (removeResult.IsFailure) return removeResult;
            changesCount++;
        }

        // Tables — remove missing, update existing, add new.
        foreach (var tableToRemove in tablesToRemove)
        {
            var removeResult = layout.RemoveTable(tableToRemove.Id);
            if (removeResult.IsFailure) return removeResult;
            changesCount++;
        }

        // Zone upserts (update must run before table updates in case a table's ZoneId
        // refers to a newly-added zone — additions are batched at the end).
        foreach (var zoneDto in zones.Where(z => z.Id.HasValue))
        {
            var updateResult = layout.UpdateZone(
                zoneDto.Id!.Value, zoneDto.Name, zoneDto.Color, zoneDto.SortOrder,
                zoneDto.Shape, zoneDto.Geometry);
            if (updateResult.IsFailure) return updateResult;
            changesCount++;

            // Slice 9.5 — optional seat regeneration for the existing zone. The
            // domain method ClearSeats() then GenerateTheaterSeats(rows × cols).
            // Frontend currently only sends these for empty zones (UI gate),
            // but the domain handles either case correctly.
            if (zoneDto.RowCount is { } rowCount && rowCount > 0
                && zoneDto.SeatsPerRow is { } seatsPerRow && seatsPerRow > 0)
            {
                var seatGenResult = layout.GenerateTheaterSeats(
                    zoneDto.Id!.Value, rowCount, seatsPerRow);
                if (seatGenResult.IsFailure) return seatGenResult;
                _logger.LogInformation(
                    "BatchUpdateLayout: regenerated {SeatCount} seats in zone {ZoneId} ({Rows}×{Cols})",
                    rowCount * seatsPerRow, zoneDto.Id!.Value, rowCount, seatsPerRow);
                changesCount++;
            }
        }

        // Track newly-created zones so we can resolve table.ZoneId references below
        // and (S8.8c) translate <c>BatchTierAssignment.AssignableId</c> client-Guids
        // for new zones into the freshly-assigned server-Guids.
        var newZoneIdsByIndex = new Dictionary<int, Guid>();
        var clientIdToZoneServerGuid = new Dictionary<Guid, Guid>();
        for (var i = 0; i < zones.Count; i++)
        {
            var zoneDto = zones[i];
            if (zoneDto.Id.HasValue) continue;

            var addResult = layout.AddZone(zoneDto.Name, zoneDto.Color, zoneDto.SortOrder);
            if (addResult.IsFailure) return Result.Failure(addResult.Error);

            // Apply shape/geometry via the overload that accepts them.
            var shapeUpdate = layout.UpdateZone(
                addResult.Value.Id, zoneDto.Name, zoneDto.Color, zoneDto.SortOrder,
                zoneDto.Shape, zoneDto.Geometry);
            if (shapeUpdate.IsFailure) return shapeUpdate;

            newZoneIdsByIndex[i] = addResult.Value.Id;
            if (zoneDto.ClientId is { } clientGuid)
            {
                clientIdToZoneServerGuid[clientGuid] = addResult.Value.Id;
            }
            changesCount++;

            // Slice 9.5 — optional seat generation for the newly-added zone.
            if (zoneDto.RowCount is { } rowCount && rowCount > 0
                && zoneDto.SeatsPerRow is { } seatsPerRow && seatsPerRow > 0)
            {
                var seatGenResult = layout.GenerateTheaterSeats(
                    addResult.Value.Id, rowCount, seatsPerRow);
                if (seatGenResult.IsFailure) return seatGenResult;
                _logger.LogInformation(
                    "BatchUpdateLayout: generated {SeatCount} seats in new zone {ZoneId} ({Rows}×{Cols})",
                    rowCount * seatsPerRow, addResult.Value.Id, rowCount, seatsPerRow);
                changesCount++;
            }
        }

        // Table updates.
        foreach (var tableDto in tables.Where(t => t.Id.HasValue))
        {
            var updateResult = layout.UpdateTable(
                tableDto.Id!.Value, tableDto.Label, tableDto.Shape, tableDto.Capacity,
                tableDto.SortOrder, tableDto.ZoneId, tableDto.Geometry);
            if (updateResult.IsFailure) return updateResult;
            changesCount++;
        }

        // Table additions — auto-generate seats for parity with AddTableCommandHandler.
        // S8.8c: track client → server Guid mapping for tier-assignment reconciliation
        // below.
        var clientIdToTableServerGuid = new Dictionary<Guid, Guid>();
        foreach (var tableDto in tables.Where(t => !t.Id.HasValue))
        {
            Result<VenueTable> addResult = tableDto.Shape == TableShape.Round
                ? layout.GenerateRoundTable(
                    tableDto.Label, tableDto.Capacity, tableDto.SortOrder,
                    tableDto.ZoneId, tableDto.Geometry)
                : layout.GenerateRectTable(
                    tableDto.Label, tableDto.Shape, tableDto.Capacity, tableDto.SortOrder,
                    tableDto.ZoneId, tableDto.Geometry);

            if (addResult.IsFailure) return Result.Failure(addResult.Error);

            if (tableDto.ClientId is { } clientGuid)
            {
                clientIdToTableServerGuid[clientGuid] = addResult.Value.Id;
            }
            changesCount++;
        }

        // Decoration updates.
        foreach (var decorationDto in decorations.Where(d => d.Id.HasValue))
        {
            var updateResult = layout.UpdateDecoration(
                decorationDto.Id!.Value, decorationDto.Kind, decorationDto.Label,
                decorationDto.SortOrder, decorationDto.Geometry, decorationDto.Properties);
            if (updateResult.IsFailure) return updateResult;
            changesCount++;
        }

        // Decoration additions.
        foreach (var decorationDto in decorations.Where(d => !d.Id.HasValue))
        {
            var addResult = layout.AddDecoration(
                decorationDto.Kind, decorationDto.Label, decorationDto.SortOrder,
                decorationDto.Geometry, decorationDto.Properties);
            if (addResult.IsFailure) return Result.Failure(addResult.Error);
            changesCount++;
        }

        // Layout-level updates last (so Canvas validation errors surface last, after
        // the more likely zone/table/decoration failures).
        if (payload.Name is not null)
        {
            var nameResult = layout.UpdateName(payload.Name);
            if (nameResult.IsFailure) return nameResult;
            changesCount++;
        }

        if (payload.Canvas is not null)
        {
            var canvasResult = CanvasConfig.Create(
                payload.Canvas.Width,
                payload.Canvas.Height,
                payload.Canvas.Scale,
                payload.Canvas.BackgroundColor);

            if (canvasResult.IsFailure)
            {
                return Result.Failure(canvasResult.Error);
            }

            var updateResult = layout.UpdateCanvas(canvasResult.Value);
            if (updateResult.IsFailure) return updateResult;
            changesCount++;
        }

        // ───────── Slice 8 S8.8c: tier-assignment reconciliation ─────────
        // Runs after all geometry mutations so newly-added zones/tables exist
        // on the layout (with server Guids) when we validate AssignableIds.
        // null payload.TierAssignments → skip (backward-compat for callers that
        // don't manage tiers); empty list → reconcile to "no assignments".
        if (payload.TierAssignments is not null)
        {
            var reconcileResult = await ReconcileTierAssignmentsAsync(
                layout,
                payload.TierAssignments,
                clientIdToZoneServerGuid,
                clientIdToTableServerGuid,
                cancellationToken);

            if (reconcileResult.IsFailure)
            {
                EmitSaveFailed(request.LayoutId, "validation_failed");
                return Result.Failure(reconcileResult.Error, reconcileResult.ErrorKind);
            }
            changesCount += reconcileResult.Value;
        }

        _layoutRepository.SetOriginalRowVersion(layout, request.ExpectedRowVersion);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "BatchUpdateLayout: db concurrency conflict on commit. LayoutId={LayoutId}",
                request.LayoutId);
            _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.ConcurrencyConflict);
            EmitSaveFailed(request.LayoutId, "concurrency_conflict");
            return Result.Conflict(
                "Layout was modified concurrently. Reload and retry.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "BatchUpdateLayout: persistence failed. LayoutId={LayoutId}",
                request.LayoutId);
            throw;
        }

        _logger.LogInformation(
            "BatchUpdateLayout: succeeded. LayoutId={LayoutId}, ZonesRemoved={ZonesRemoved}, TablesRemoved={TablesRemoved}, ChangesCount={ChangesCount}",
            request.LayoutId, zonesToRemove.Count, tablesToRemove.Count, changesCount);

        try
        {
            _metrics.LayoutCanvasEditorSaved(request.LayoutId, changesCount);
        }
        catch (Exception ex)
        {
            // Metric emission must never fail the save — the commit has already
            // happened and the client will see 204. Log + swallow to keep the
            // observability path strictly side-effecting.
            _logger.LogWarning(ex,
                "BatchUpdateLayout: metric emission failed (commit already succeeded). LayoutId={LayoutId}, ChangesCount={ChangesCount}",
                request.LayoutId, changesCount);
        }

        return Result.Success();
    }

    /// <summary>
    /// Phase 7H: emits the <c>canvas_editor.save_failed</c> dashboard metric with
    /// a low-cardinality reason tag (one of <c>not_found</c>, <c>auth_failed</c>,
    /// <c>concurrency_conflict</c>, <c>structural_edit_rejected</c>,
    /// <c>validation_failed</c>, <c>unknown</c>). Wrapped in try-catch so
    /// observability never blocks the user-facing failure response.
    /// </summary>
    private void EmitSaveFailed(Guid layoutId, string reason)
    {
        try
        {
            _metrics.LayoutCanvasEditorSaveFailed(layoutId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Phase 7H] Failed to emit canvas_editor.save_failed metric (non-fatal) - LayoutId={LayoutId}, Reason={Reason}",
                layoutId, reason);
        }
    }

    /// <summary>
    /// Slice 8 S8.8c: declarative tier-assignment reconciliation. Loads every
    /// <see cref="TicketTier"/> for the layout's event with its <c>Assignments</c>
    /// collection, resolves any client-Guids in <paramref name="desired"/> to the
    /// freshly-assigned server-Guids of zones/tables created earlier in this
    /// batch, validates every <c>(Kind, AssignableId)</c> exists on the
    /// post-mutation layout, and applies the minimum set of
    /// <see cref="TicketTier.AssignToZone"/> / <see cref="TicketTier.AssignToTable"/> /
    /// <see cref="TicketTier.RemoveAssignment"/> calls to bring current state in
    /// line with desired state. Returns the number of mutations applied for the
    /// canvas-editor-saved metric tag.
    /// </summary>
    private async Task<Result<int>> ReconcileTierAssignmentsAsync(
        VenueLayout layout,
        List<BatchTierAssignment> desired,
        IReadOnlyDictionary<Guid, Guid> clientIdToZoneServerGuid,
        IReadOnlyDictionary<Guid, Guid> clientIdToTableServerGuid,
        CancellationToken cancellationToken)
    {
        if (!layout.EventId.HasValue)
        {
            _logger.LogWarning(
                "BatchUpdateLayout: tier assignments rejected on template layout. LayoutId={LayoutId}",
                layout.Id);
            return Result<int>.Failure(
                "Tier assignments require an event-attached layout, not a template",
                ErrorKind.Validation);
        }

        // Resolve every desired assignableId from client-Guid → server-Guid.
        // Untranslated Ids are assumed to already reference server entities;
        // the validation step below catches any miss.
        var resolved = new List<(AssignableKind Kind, Guid AssignableId, IReadOnlyList<Guid> TierIds)>(desired.Count);
        foreach (var item in desired)
        {
            var resolvedId = item.Kind switch
            {
                AssignableKind.Zone =>
                    clientIdToZoneServerGuid.TryGetValue(item.AssignableId, out var z) ? z : item.AssignableId,
                AssignableKind.Table =>
                    clientIdToTableServerGuid.TryGetValue(item.AssignableId, out var t) ? t : item.AssignableId,
                _ => item.AssignableId,
            };
            resolved.Add((item.Kind, resolvedId, item.TierIds ?? new List<Guid>()));
        }

        // Validate every (kind, id) tuple exists on the *current* (post-mutation)
        // layout. Items being deleted in this batch are already gone from the
        // aggregate at this point, so an attempt to assign tiers to a deleted
        // zone fails fast with NotFound.
        var validZoneIds = layout.Zones.Select(z => z.Id).ToHashSet();
        var validTableIds = layout.Tables.Select(t => t.Id).ToHashSet();
        foreach (var (kind, id, _) in resolved)
        {
            var exists = kind == AssignableKind.Zone
                ? validZoneIds.Contains(id)
                : validTableIds.Contains(id);
            if (!exists)
            {
                _logger.LogWarning(
                    "BatchUpdateLayout: tier assignment references unknown {Kind} {Id}. LayoutId={LayoutId}",
                    kind, id, layout.Id);
                return Result<int>.NotFound(
                    kind == AssignableKind.Zone
                        ? $"Zone {id} not found on this layout"
                        : $"Table {id} not found on this layout");
            }
        }

        // Load all tiers for the event (with assignments) so the diff can read
        // current state and the domain mutations track on the same aggregates.
        var tiers = await _eventRepository.GetTicketTiersWithAssignmentsForEventAsync(
            layout.EventId.Value, cancellationToken);

        // Sanity: every tier referenced in the desired state must belong to this
        // event (defense-in-depth — frontend should never send cross-event tiers).
        var validTierIds = tiers.Select(t => t.Id).ToHashSet();
        foreach (var (_, _, tierIds) in resolved)
        {
            foreach (var tierId in tierIds)
            {
                if (!validTierIds.Contains(tierId))
                {
                    _logger.LogWarning(
                        "BatchUpdateLayout: tier {TierId} not in event {EventId}. LayoutId={LayoutId}",
                        tierId, layout.EventId.Value, layout.Id);
                    return Result<int>.Failure(
                        $"Ticket tier {tierId} does not belong to this event",
                        ErrorKind.Validation);
                }
            }
        }

        // Build current-state map: per-tier set of (kind, id) tuples currently assigned.
        var current = new Dictionary<Guid, HashSet<(AssignableKind, Guid)>>();
        foreach (var tier in tiers)
        {
            current[tier.Id] = tier.Assignments
                .Select(a => (a.AssignableKind, a.AssignableId))
                .ToHashSet();
        }

        // Build desired-state map: same shape, derived from the resolved list.
        var desiredByTier = new Dictionary<Guid, HashSet<(AssignableKind, Guid)>>();
        foreach (var (kind, id, tierIds) in resolved)
        {
            foreach (var tierId in tierIds)
            {
                if (!desiredByTier.TryGetValue(tierId, out var set))
                {
                    set = new HashSet<(AssignableKind, Guid)>();
                    desiredByTier[tierId] = set;
                }
                set.Add((kind, id));
            }
        }

        // Diff and apply per tier. Goes through domain methods so invariants
        // (idempotence on assign, "Assignment not found" on bad remove) stay in
        // the domain layer. Both add + remove path bump the tier's RowVersion
        // via MarkAsUpdated() — exactly the behavior the architect called out.
        var tierMutations = 0;
        foreach (var tier in tiers)
        {
            var currentSet = current[tier.Id];
            desiredByTier.TryGetValue(tier.Id, out var desiredSet);
            desiredSet ??= new HashSet<(AssignableKind, Guid)>();

            // Removals: in current but not in desired.
            foreach (var (kind, id) in currentSet.Except(desiredSet).ToList())
            {
                var removeResult = tier.RemoveAssignment(kind, id);
                if (removeResult.IsFailure)
                {
                    return Result<int>.Failure(removeResult.Error);
                }
                tierMutations++;
            }

            // Additions: in desired but not in current.
            foreach (var (kind, id) in desiredSet.Except(currentSet).ToList())
            {
                var addResult = kind == AssignableKind.Zone
                    ? tier.AssignToZone(id)
                    : tier.AssignToTable(id);
                if (addResult.IsFailure)
                {
                    return Result<int>.Failure(addResult.Error);
                }
                tierMutations++;
            }
        }

        _logger.LogInformation(
            "BatchUpdateLayout: tier reconciliation applied. LayoutId={LayoutId}, EventId={EventId}, TierMutations={TierMutations}",
            layout.Id, layout.EventId.Value, tierMutations);

        return Result<int>.Success(tierMutations);
    }
}
