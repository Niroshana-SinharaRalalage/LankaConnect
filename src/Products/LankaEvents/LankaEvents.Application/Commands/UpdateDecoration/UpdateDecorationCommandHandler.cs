using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateDecoration;

/// <summary>
/// Slice 5 Chunk 7: PATCH decoration handler.
/// Flow: authorize → load full aggregate → resolve decoration → merge patch over
/// current values → call domain <c>UpdateDecoration</c> → commit.
/// No structural guard — decorations have no seats.
/// </summary>
public class UpdateDecorationCommandHandler : ICommandHandler<UpdateDecorationCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateDecorationCommandHandler> _logger;

    public UpdateDecorationCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IVenueLayoutRepository layoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateDecorationCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _layoutRepository = layoutRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateDecorationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateDecoration: LayoutId={LayoutId}, DecorationId={DecorationId}, ExpectedRowVersion={RowVersion}, HasKind={HasKind}, HasLabel={HasLabel}, ClearLabel={ClearLabel}, HasSortOrder={HasSortOrder}, HasGeometry={HasGeometry}, HasProperties={HasProperties}",
            request.LayoutId, request.DecorationId, request.ExpectedRowVersion,
            request.Kind.HasValue, request.Label != null, request.ClearLabel,
            request.SortOrder.HasValue, request.Geometry != null, request.Properties != null);

        if (request.Kind is null && request.Label is null && !request.ClearLabel
            && request.SortOrder is null && request.Geometry is null && request.Properties is null)
        {
            return Result.Failure("At least one field must be provided for update");
        }

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
                "UpdateDecoration: failed to load layout aggregate. LayoutId={LayoutId}",
                request.LayoutId);
            throw;
        }

        if (layout is null)
        {
            return Result.NotFound("Venue layout not found");
        }

        var decoration = layout.GetDecoration(request.DecorationId);
        if (decoration is null)
        {
            return Result.NotFound("Decoration not found in this layout");
        }

        var newKind = request.Kind ?? decoration.Kind;
        // Label semantics: non-null value wins; null + ClearLabel=true detaches;
        // null + ClearLabel=false keeps current.
        var newLabel = request.Label ?? (request.ClearLabel ? null : decoration.Label);
        var newSortOrder = request.SortOrder ?? decoration.SortOrder;
        var newGeometry = request.Geometry ?? decoration.Geometry;
        var newProperties = request.Properties ?? decoration.Properties;

        var applyResult = layout.UpdateDecoration(
            request.DecorationId,
            newKind,
            newLabel,
            newSortOrder,
            newGeometry,
            newProperties);

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
                "UpdateDecoration: concurrency conflict. LayoutId={LayoutId}, DecorationId={DecorationId}, ExpectedRowVersion={RowVersion}",
                request.LayoutId, request.DecorationId, request.ExpectedRowVersion);
            return Result.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UpdateDecoration: persistence failed. LayoutId={LayoutId}, DecorationId={DecorationId}",
                request.LayoutId, request.DecorationId);
            throw;
        }

        _logger.LogInformation(
            "UpdateDecoration: succeeded. LayoutId={LayoutId}, DecorationId={DecorationId}",
            request.LayoutId, request.DecorationId);

        return Result.Success();
    }
}
