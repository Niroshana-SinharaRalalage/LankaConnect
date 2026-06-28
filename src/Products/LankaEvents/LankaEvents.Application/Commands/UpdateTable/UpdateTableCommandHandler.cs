using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateTable;

/// <summary>
/// Slice 5 Chunk 6: PATCH table handler.
/// Flow: authorize → load layout → resolve table → if shape / capacity / geometry
/// change, run the <c>IStructuralEditGuard</c> on the table's seats → apply via
/// domain <c>UpdateTable</c> → commit.
/// </summary>
public class UpdateTableCommandHandler : ICommandHandler<UpdateTableCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IStructuralEditGuard _structuralGuard;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILayoutMetrics _metrics;
    private readonly ILogger<UpdateTableCommandHandler> _logger;

    public UpdateTableCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IStructuralEditGuard structuralGuard,
        IVenueLayoutRepository layoutRepository,
        IUnitOfWork unitOfWork,
        ILayoutMetrics metrics,
        ILogger<UpdateTableCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _structuralGuard = structuralGuard;
        _layoutRepository = layoutRepository;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateTableCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateTable: LayoutId={LayoutId}, TableId={TableId}, ExpectedRowVersion={RowVersion}, HasLabel={HasLabel}, HasShape={HasShape}, HasCapacity={HasCapacity}, HasSortOrder={HasSortOrder}, HasZoneId={HasZoneId}, ClearZoneId={ClearZoneId}, HasGeometry={HasGeometry}",
            request.LayoutId, request.TableId, request.ExpectedRowVersion,
            request.Label != null, request.Shape.HasValue, request.Capacity.HasValue,
            request.SortOrder.HasValue, request.ZoneId.HasValue, request.ClearZoneId,
            request.Geometry != null);

        if (request.Label is null && request.Shape is null && request.Capacity is null
            && request.SortOrder is null && request.ZoneId is null && !request.ClearZoneId
            && request.Geometry is null)
        {
            return Result.Failure("At least one field must be provided for update");
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
                "UpdateTable: failed to load layout aggregate. LayoutId={LayoutId}",
                request.LayoutId);
            throw;
        }

        if (layout is null)
        {
            return Result.NotFound("Venue layout not found");
        }

        var table = layout.GetTable(request.TableId);
        if (table is null)
        {
            return Result.NotFound("Table not found in this layout");
        }

        var isStructural = request.Shape.HasValue || request.Capacity.HasValue || request.Geometry is not null;
        if (isStructural)
        {
            var seatIds = table.Seats.Select(s => s.Id).ToList();
            var guardResult = await _structuralGuard.CheckSeatsAsync(seatIds, cancellationToken);
            if (guardResult.IsFailure)
            {
                _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.SeatsReserved);
                return guardResult;
            }
        }

        var newLabel = request.Label ?? table.Label;
        var newShape = request.Shape ?? table.Shape;
        var newCapacity = request.Capacity ?? table.Capacity;
        var newSortOrder = request.SortOrder ?? table.SortOrder;
        var newGeometry = request.Geometry ?? table.Geometry;
        // ZoneId semantics: null value + ClearZoneId=true detaches; null + false keeps
        // current; any non-null value sets.
        Guid? newZoneId = request.ZoneId ?? (request.ClearZoneId ? null : table.VenueZoneId);

        var applyResult = layout.UpdateTable(
            request.TableId,
            newLabel,
            newShape,
            newCapacity,
            newSortOrder,
            newZoneId,
            newGeometry);

        if (applyResult.IsFailure)
        {
            return applyResult;
        }

        _layoutRepository.SetOriginalRowVersion(layout, request.ExpectedRowVersion);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "UpdateTable: concurrency conflict. LayoutId={LayoutId}, TableId={TableId}, ExpectedRowVersion={RowVersion}",
                request.LayoutId, request.TableId, request.ExpectedRowVersion);
            _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.ConcurrencyConflict);
            return Result.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UpdateTable: persistence failed. LayoutId={LayoutId}, TableId={TableId}",
                request.LayoutId, request.TableId);
            throw;
        }

        _logger.LogInformation(
            "UpdateTable: succeeded. LayoutId={LayoutId}, TableId={TableId}, Structural={Structural}",
            request.LayoutId, request.TableId, isStructural);

        return Result.Success();
    }
}
