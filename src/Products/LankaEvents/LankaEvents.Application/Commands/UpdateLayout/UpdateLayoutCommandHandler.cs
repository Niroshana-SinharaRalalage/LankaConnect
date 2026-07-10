using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateLayout;

/// <summary>
/// Slice 5 Chunk 4: PUT /api/venue-layouts/{id} handler.
/// Flow: authorize → load tracked aggregate → apply name/canvas changes → set
/// expected RowVersion OriginalValue → commit. On <see cref="DbUpdateConcurrencyException"/>
/// maps to <see cref="ErrorKind.Conflict"/> → HTTP 409 (stale <c>If-Match</c>).
/// </summary>
public class UpdateLayoutCommandHandler : ICommandHandler<UpdateLayoutCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateLayoutCommandHandler> _logger;

    public UpdateLayoutCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IVenueLayoutRepository layoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateLayoutCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _layoutRepository = layoutRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateLayoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateLayout: LayoutId={LayoutId}, ExpectedRowVersion={ExpectedRowVersion}, HasName={HasName}, HasCanvas={HasCanvas}",
            request.LayoutId, request.ExpectedRowVersion, request.Name != null, request.Canvas != null);

        if (request.Name is null && request.Canvas is null)
        {
            return Result.Failure("At least one of 'name' or 'canvas' must be provided");
        }

        var authResult = await _authorizationService.AuthorizeAsync(request.LayoutId, cancellationToken);
        if (authResult.IsFailure)
        {
            return Result.Failure(authResult.Error, authResult.ErrorKind);
        }

        var layout = authResult.Value;

        if (request.Name is not null)
        {
            var nameResult = layout.UpdateName(request.Name);
            if (nameResult.IsFailure)
            {
                return nameResult;
            }
        }

        if (request.Canvas is not null)
        {
            var canvasResult = CanvasConfig.Create(
                request.Canvas.Width,
                request.Canvas.Height,
                request.Canvas.Scale,
                request.Canvas.BackgroundColor);

            if (canvasResult.IsFailure)
            {
                return Result.Failure(canvasResult.Error);
            }

            var updateResult = layout.UpdateCanvas(canvasResult.Value);
            if (updateResult.IsFailure)
            {
                return updateResult;
            }
        }

        _layoutRepository.SetOriginalRowVersion(layout, request.ExpectedRowVersion);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "UpdateLayout: concurrency conflict. LayoutId={LayoutId}, ExpectedRowVersion={ExpectedRowVersion}",
                request.LayoutId, request.ExpectedRowVersion);
            return Result.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UpdateLayout: persistence failed. LayoutId={LayoutId}",
                request.LayoutId);
            throw;
        }

        _logger.LogInformation(
            "UpdateLayout: succeeded. LayoutId={LayoutId}",
            request.LayoutId);

        return Result.Success();
    }
}
