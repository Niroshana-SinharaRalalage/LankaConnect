using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Commands.DeleteDecoration;

/// <summary>
/// Slice 5 Chunk 7: DELETE decoration handler.
/// Flow: authorize → load full aggregate → <c>VenueLayout.RemoveDecoration</c>
/// → commit. No structural guard — decorations have no seats.
/// </summary>
public class DeleteDecorationCommandHandler : ICommandHandler<DeleteDecorationCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteDecorationCommandHandler> _logger;

    public DeleteDecorationCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IVenueLayoutRepository layoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteDecorationCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _layoutRepository = layoutRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteDecorationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteDecoration: LayoutId={LayoutId}, DecorationId={DecorationId}, ExpectedRowVersion={RowVersion}",
            request.LayoutId, request.DecorationId, request.ExpectedRowVersion);

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
                "DeleteDecoration: failed to load layout aggregate. LayoutId={LayoutId}",
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

        var removeResult = layout.RemoveDecoration(request.DecorationId);
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
                "DeleteDecoration: concurrency conflict. LayoutId={LayoutId}, DecorationId={DecorationId}, ExpectedRowVersion={RowVersion}",
                request.LayoutId, request.DecorationId, request.ExpectedRowVersion);
            return Result.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "DeleteDecoration: persistence failed. LayoutId={LayoutId}, DecorationId={DecorationId}",
                request.LayoutId, request.DecorationId);
            throw;
        }

        _logger.LogInformation(
            "DeleteDecoration: succeeded. LayoutId={LayoutId}, DecorationId={DecorationId}",
            request.LayoutId, request.DecorationId);

        return Result.Success();
    }
}
