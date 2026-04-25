using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.BatchUpdateLayout;

/// <summary>
/// Slice 5 Chunk 10: atomic batch replacement of a venue layout's zones, tables,
/// and decorations. See <see cref="BatchUpdateLayoutCommand"/> for semantics.
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
///   6. <see cref="IVenueLayoutRepository.SetOriginalRowVersion"/> + commit.
///      <see cref="DbUpdateConcurrencyException"/> → 409.
/// </summary>
public class BatchUpdateLayoutCommandHandler : ICommandHandler<BatchUpdateLayoutCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IStructuralEditGuard _structuralGuard;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILayoutMetrics _metrics;
    private readonly ILogger<BatchUpdateLayoutCommandHandler> _logger;

    public BatchUpdateLayoutCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IStructuralEditGuard structuralGuard,
        IVenueLayoutRepository layoutRepository,
        IUnitOfWork unitOfWork,
        ILayoutMetrics metrics,
        ILogger<BatchUpdateLayoutCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _structuralGuard = structuralGuard;
        _layoutRepository = layoutRepository;
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
            return Result.Failure("Batch payload is required");
        }

        var authResult = await _authorizationService.AuthorizeAsync(request.LayoutId, cancellationToken);
        if (authResult.IsFailure)
        {
            _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.AuthFailed);
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
            return Result.NotFound("Venue layout not found");
        }

        if (layout.RowVersion != request.ExpectedRowVersion)
        {
            _logger.LogWarning(
                "BatchUpdateLayout: early concurrency conflict. LayoutId={LayoutId}, Expected={Expected}, Actual={Actual}",
                request.LayoutId, request.ExpectedRowVersion, layout.RowVersion);
            _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.ConcurrencyConflict);
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
                return guardResult;
            }
        }

        // ----- Apply changes (removals → updates → additions) -----

        // Decorations — cheapest, no seat impact.
        var payloadDecorationIds = decorations.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToHashSet();
        foreach (var existing in layout.Decorations.Where(d => !payloadDecorationIds.Contains(d.Id)).ToList())
        {
            var removeResult = layout.RemoveDecoration(existing.Id);
            if (removeResult.IsFailure) return removeResult;
        }

        // Zones — remove missing, update existing, add new.
        foreach (var zoneToRemove in zonesToRemove)
        {
            var removeResult = layout.RemoveZone(zoneToRemove.Id);
            if (removeResult.IsFailure) return removeResult;
        }

        // Tables — remove missing, update existing, add new.
        foreach (var tableToRemove in tablesToRemove)
        {
            var removeResult = layout.RemoveTable(tableToRemove.Id);
            if (removeResult.IsFailure) return removeResult;
        }

        // Zone upserts (update must run before table updates in case a table's ZoneId
        // refers to a newly-added zone — additions are batched at the end).
        foreach (var zoneDto in zones.Where(z => z.Id.HasValue))
        {
            var updateResult = layout.UpdateZone(
                zoneDto.Id!.Value, zoneDto.Name, zoneDto.Color, zoneDto.SortOrder,
                zoneDto.Shape, zoneDto.Geometry);
            if (updateResult.IsFailure) return updateResult;
        }

        // Track newly-created zones so we can resolve table.ZoneId references below.
        // Key: position in the payload zones list (stable for this request only).
        var newZoneIdsByIndex = new Dictionary<int, Guid>();
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
        }

        // Table updates.
        foreach (var tableDto in tables.Where(t => t.Id.HasValue))
        {
            var updateResult = layout.UpdateTable(
                tableDto.Id!.Value, tableDto.Label, tableDto.Shape, tableDto.Capacity,
                tableDto.SortOrder, tableDto.ZoneId, tableDto.Geometry);
            if (updateResult.IsFailure) return updateResult;
        }

        // Table additions — auto-generate seats for parity with AddTableCommandHandler.
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
        }

        // Decoration updates.
        foreach (var decorationDto in decorations.Where(d => d.Id.HasValue))
        {
            var updateResult = layout.UpdateDecoration(
                decorationDto.Id!.Value, decorationDto.Kind, decorationDto.Label,
                decorationDto.SortOrder, decorationDto.Geometry, decorationDto.Properties);
            if (updateResult.IsFailure) return updateResult;
        }

        // Decoration additions.
        foreach (var decorationDto in decorations.Where(d => !d.Id.HasValue))
        {
            var addResult = layout.AddDecoration(
                decorationDto.Kind, decorationDto.Label, decorationDto.SortOrder,
                decorationDto.Geometry, decorationDto.Properties);
            if (addResult.IsFailure) return Result.Failure(addResult.Error);
        }

        // Layout-level updates last (so Canvas validation errors surface last, after
        // the more likely zone/table/decoration failures).
        if (payload.Name is not null)
        {
            var nameResult = layout.UpdateName(payload.Name);
            if (nameResult.IsFailure) return nameResult;
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
            "BatchUpdateLayout: succeeded. LayoutId={LayoutId}, ZonesRemoved={ZonesRemoved}, TablesRemoved={TablesRemoved}",
            request.LayoutId, zonesToRemove.Count, tablesToRemove.Count);

        return Result.Success();
    }
}
