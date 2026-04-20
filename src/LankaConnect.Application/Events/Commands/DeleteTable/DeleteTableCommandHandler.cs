using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.DeleteTable;

/// <summary>
/// Slice 5 Chunk 6: DELETE table handler. Always structural — guard always runs
/// against the table's seats.
/// </summary>
public class DeleteTableCommandHandler : ICommandHandler<DeleteTableCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IStructuralEditGuard _structuralGuard;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteTableCommandHandler> _logger;

    public DeleteTableCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IStructuralEditGuard structuralGuard,
        IVenueLayoutRepository layoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteTableCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _structuralGuard = structuralGuard;
        _layoutRepository = layoutRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteTableCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteTable: LayoutId={LayoutId}, TableId={TableId}, ExpectedRowVersion={RowVersion}",
            request.LayoutId, request.TableId, request.ExpectedRowVersion);

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
                "DeleteTable: failed to load layout aggregate. LayoutId={LayoutId}",
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

        var seatIds = table.Seats.Select(s => s.Id).ToList();
        var guardResult = await _structuralGuard.CheckSeatsAsync(seatIds, cancellationToken);
        if (guardResult.IsFailure)
        {
            return guardResult;
        }

        var removeResult = layout.RemoveTable(request.TableId);
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
                "DeleteTable: concurrency conflict. LayoutId={LayoutId}, TableId={TableId}, ExpectedRowVersion={RowVersion}",
                request.LayoutId, request.TableId, request.ExpectedRowVersion);
            return Result.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "DeleteTable: persistence failed. LayoutId={LayoutId}, TableId={TableId}",
                request.LayoutId, request.TableId);
            throw;
        }

        _logger.LogInformation(
            "DeleteTable: succeeded. LayoutId={LayoutId}, TableId={TableId}",
            request.LayoutId, request.TableId);

        return Result.Success();
    }
}
