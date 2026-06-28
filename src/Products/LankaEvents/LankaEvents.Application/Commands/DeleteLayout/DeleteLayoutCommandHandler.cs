using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Commands.DeleteLayout;

/// <summary>
/// Slice 5 Chunk 9: DELETE /api/venue-layouts/{id} handler. Enforces the four
/// safety gates documented on <see cref="DeleteLayoutCommand"/>. On success,
/// cascades zone/table/decoration/seat removal via FK and detaches the owning
/// event (flipping it back to General Admission).
/// </summary>
public class DeleteLayoutCommandHandler : ICommandHandler<DeleteLayoutCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IStructuralEditGuard _structuralGuard;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILayoutMetrics _metrics;
    private readonly ILogger<DeleteLayoutCommandHandler> _logger;

    public DeleteLayoutCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IStructuralEditGuard structuralGuard,
        IVenueLayoutRepository layoutRepository,
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILayoutMetrics metrics,
        ILogger<DeleteLayoutCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _structuralGuard = structuralGuard;
        _layoutRepository = layoutRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteLayoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteLayout: LayoutId={LayoutId}, ExpectedRowVersion={RowVersion}",
            request.LayoutId, request.ExpectedRowVersion);

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
                "DeleteLayout: failed to load layout aggregate. LayoutId={LayoutId}",
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
                "DeleteLayout: concurrency conflict. LayoutId={LayoutId}, ExpectedRowVersion={Expected}, ActualRowVersion={Actual}",
                request.LayoutId, request.ExpectedRowVersion, layout.RowVersion);
            _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.ConcurrencyConflict);
            return Result.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }

        // Gather every seat in the aggregate (zones + tables). The seat XOR invariant
        // guarantees no double-counting. Structural guard short-circuits on empty set.
        var seatIds = layout.Zones.SelectMany(z => z.Seats.Select(s => s.Id))
            .Concat(layout.Tables.SelectMany(t => t.Seats.Select(s => s.Id)))
            .ToList();

        var guardResult = await _structuralGuard.CheckSeatsAsync(seatIds, cancellationToken);
        if (guardResult.IsFailure)
        {
            _logger.LogWarning(
                "DeleteLayout: structural guard rejected delete. LayoutId={LayoutId}, AffectedSeatCount={Count}",
                request.LayoutId, seatIds.Count);
            _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.SeatsReserved);
            return guardResult;
        }

        // Detach the owning event (if any) — this also rejects when preliminary/confirmed
        // registrations exist, preventing orphaned events stuck in AssignedSeating mode
        // without a layout.
        if (layout.EventId.HasValue)
        {
            Event? @event;
            try
            {
                @event = await _eventRepository.GetByIdAsync(layout.EventId.Value, trackChanges: true, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "DeleteLayout: failed to load owning event. LayoutId={LayoutId}, EventId={EventId}",
                    request.LayoutId, layout.EventId.Value);
                throw;
            }

            if (@event is null)
            {
                // Layout references a non-existent event — unusual but not a blocker for delete.
                _logger.LogWarning(
                    "DeleteLayout: owning event not found; proceeding with orphaned-layout delete. LayoutId={LayoutId}, EventId={EventId}",
                    request.LayoutId, layout.EventId.Value);
            }
            else
            {
                var disableResult = @event.DisableAssignedSeating();
                if (disableResult.IsFailure)
                {
                    _logger.LogWarning(
                        "DeleteLayout: cannot detach layout from event — {Reason}. LayoutId={LayoutId}, EventId={EventId}",
                        disableResult.Error, request.LayoutId, @event.Id);
                    return Result.StructuralEditRejected(disableResult.Error);
                }
            }
        }

        _layoutRepository.SetOriginalRowVersion(layout, request.ExpectedRowVersion);
        _layoutRepository.Remove(layout);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "DeleteLayout: db concurrency conflict on commit. LayoutId={LayoutId}",
                request.LayoutId);
            _metrics.StructuralEditRejected(request.LayoutId, StructuralEditRejectionReason.ConcurrencyConflict);
            return Result.Conflict(
                "Layout was modified concurrently. Reload and retry.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "DeleteLayout: persistence failed. LayoutId={LayoutId}",
                request.LayoutId);
            throw;
        }

        _logger.LogInformation(
            "DeleteLayout: succeeded. LayoutId={LayoutId}, SeatCount={Count}",
            request.LayoutId, seatIds.Count);

        return Result.Success();
    }
}
