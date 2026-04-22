using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.UpdateZone;

/// <summary>
/// Slice 5 Chunk 5: PATCH zone handler.
/// Flow: authorize → load layout (with zones + seats) → if shape/geometry change,
/// run the StructuralEditGuard on the zone's seats → apply changes → commit.
/// </summary>
public class UpdateZoneCommandHandler : ICommandHandler<UpdateZoneCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IStructuralEditGuard _structuralGuard;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateZoneCommandHandler> _logger;

    public UpdateZoneCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IStructuralEditGuard structuralGuard,
        IVenueLayoutRepository layoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateZoneCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _structuralGuard = structuralGuard;
        _layoutRepository = layoutRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateZoneCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateZone: LayoutId={LayoutId}, ZoneId={ZoneId}, ExpectedRowVersion={RowVersion}, HasName={HasName}, HasColor={HasColor}, HasSortOrder={HasSortOrder}, HasShape={HasShape}, HasGeometry={HasGeometry}",
            request.LayoutId, request.ZoneId, request.ExpectedRowVersion,
            request.Name != null, request.Color != null, request.SortOrder.HasValue,
            request.Shape.HasValue, request.Geometry != null);

        if (request.Name is null && request.Color is null && request.SortOrder is null
            && request.Shape is null && request.Geometry is null)
        {
            return Result.Failure("At least one field must be provided for update");
        }

        // Authorize on the layout; the service returns the tracked aggregate (without zones).
        var authResult = await _authorizationService.AuthorizeAsync(request.LayoutId, cancellationToken);
        if (authResult.IsFailure)
        {
            return Result.Failure(authResult.Error, authResult.ErrorKind);
        }

        // Load the full aggregate with zones + seats (need seats for the structural guard).
        VenueLayout? layout;
        try
        {
            layout = await _layoutRepository.GetWithZonesAndSeatsAsync(request.LayoutId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UpdateZone: failed to load layout aggregate. LayoutId={LayoutId}",
                request.LayoutId);
            throw;
        }

        if (layout is null)
        {
            return Result.NotFound("Venue layout not found");
        }

        var zone = layout.GetZone(request.ZoneId);
        if (zone is null)
        {
            return Result.NotFound("Zone not found in this layout");
        }

        var isStructural = request.Shape.HasValue || request.Geometry is not null;
        if (isStructural)
        {
            // Mirror DeleteZone: a structural edit (shape/geometry) also invalidates
            // layout positions of tables scoped to this zone, so the guard must see
            // their seats too. Without this, a held table seat inside the zone
            // would be silently approved for a geometry change.
            var seatIds = zone.Seats.Select(s => s.Id)
                .Concat(layout.Tables
                    .Where(t => t.VenueZoneId == request.ZoneId)
                    .SelectMany(t => t.Seats.Select(s => s.Id)))
                .ToList();
            var guardResult = await _structuralGuard.CheckSeatsAsync(seatIds, cancellationToken);
            if (guardResult.IsFailure)
            {
                return guardResult;
            }
        }

        // Fall back to current values for any omitted field; the domain Update method
        // revalidates the full zone so a complete tuple is required either way.
        var newName = request.Name ?? zone.Name;
        var newColor = request.Color ?? zone.Color;
        var newSortOrder = request.SortOrder ?? zone.SortOrder;

        Result applyResult;
        if (isStructural)
        {
            var newShape = request.Shape ?? zone.Shape;
            var newGeometry = request.Geometry ?? zone.Geometry;
            applyResult = layout.UpdateZone(request.ZoneId, newName, newColor, newSortOrder, newShape, newGeometry);
        }
        else
        {
            applyResult = layout.UpdateZone(request.ZoneId, newName, newColor, newSortOrder);
        }

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
                "UpdateZone: concurrency conflict. LayoutId={LayoutId}, ZoneId={ZoneId}, ExpectedRowVersion={RowVersion}",
                request.LayoutId, request.ZoneId, request.ExpectedRowVersion);
            return Result.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UpdateZone: persistence failed. LayoutId={LayoutId}, ZoneId={ZoneId}",
                request.LayoutId, request.ZoneId);
            throw;
        }

        _logger.LogInformation(
            "UpdateZone: succeeded. LayoutId={LayoutId}, ZoneId={ZoneId}, Structural={Structural}",
            request.LayoutId, request.ZoneId, isStructural);

        return Result.Success();
    }
}
