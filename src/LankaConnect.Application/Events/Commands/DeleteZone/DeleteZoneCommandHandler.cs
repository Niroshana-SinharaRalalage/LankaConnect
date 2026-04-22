using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.DeleteZone;

/// <summary>
/// Slice 5 Chunk 5: DELETE zone handler. Always structural — guard always runs.
/// </summary>
public class DeleteZoneCommandHandler : ICommandHandler<DeleteZoneCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IStructuralEditGuard _structuralGuard;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteZoneCommandHandler> _logger;

    public DeleteZoneCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IStructuralEditGuard structuralGuard,
        IVenueLayoutRepository layoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteZoneCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _structuralGuard = structuralGuard;
        _layoutRepository = layoutRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteZoneCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteZone: LayoutId={LayoutId}, ZoneId={ZoneId}, ExpectedRowVersion={RowVersion}",
            request.LayoutId, request.ZoneId, request.ExpectedRowVersion);

        var authResult = await _authorizationService.AuthorizeAsync(request.LayoutId, cancellationToken);
        if (authResult.IsFailure)
        {
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
                "DeleteZone: failed to load layout aggregate. LayoutId={LayoutId}",
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

        // Structural guard must consider every seat that becomes unreachable when
        // this zone goes away — zone seats (XOR partition) AND seats under tables
        // scoped to this zone. Without the table-seat leg, a banquet layout with a
        // held table seat would silently pass the guard and the DELETE would
        // orphan the hold against a deleted zone.
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

        var removeResult = layout.RemoveZone(request.ZoneId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        _layoutRepository.SetOriginalRowVersion(layout, request.ExpectedRowVersion);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "DeleteZone: concurrency conflict. LayoutId={LayoutId}, ZoneId={ZoneId}, ExpectedRowVersion={RowVersion}",
                request.LayoutId, request.ZoneId, request.ExpectedRowVersion);
            return Result.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "DeleteZone: persistence failed. LayoutId={LayoutId}, ZoneId={ZoneId}",
                request.LayoutId, request.ZoneId);
            throw;
        }

        _logger.LogInformation(
            "DeleteZone: succeeded. LayoutId={LayoutId}, ZoneId={ZoneId}",
            request.LayoutId, request.ZoneId);

        return Result.Success();
    }
}
