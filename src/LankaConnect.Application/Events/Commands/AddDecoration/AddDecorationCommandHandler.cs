using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.AddDecoration;

/// <summary>
/// Slice 5 Chunk 7: POST decoration handler.
/// Flow: authorize → load full aggregate → <c>VenueLayout.AddDecoration</c> →
/// apply <c>ExpectedRowVersion</c> → commit. No structural guard needed —
/// decorations are non-seating and adding one doesn't touch any existing seat.
/// </summary>
public class AddDecorationCommandHandler : ICommandHandler<AddDecorationCommand, Guid>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddDecorationCommandHandler> _logger;

    public AddDecorationCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IVenueLayoutRepository layoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddDecorationCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _layoutRepository = layoutRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(AddDecorationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AddDecoration: LayoutId={LayoutId}, Kind={Kind}, Label={Label}, SortOrder={SortOrder}, ExpectedRowVersion={RowVersion}",
            request.LayoutId, request.Kind, request.Label, request.SortOrder, request.ExpectedRowVersion);

        var authResult = await _authorizationService.AuthorizeAsync(request.LayoutId, cancellationToken);
        if (authResult.IsFailure)
        {
            return Result<Guid>.Failure(authResult.Error, authResult.ErrorKind);
        }

        VenueLayout? layout;
        try
        {
            layout = await _layoutRepository.GetWithZonesAndSeatsAsync(request.LayoutId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AddDecoration: failed to load layout aggregate. LayoutId={LayoutId}",
                request.LayoutId);
            throw;
        }

        if (layout is null)
        {
            return Result<Guid>.NotFound("Venue layout not found");
        }

        var addResult = layout.AddDecoration(
            request.Kind,
            request.Label,
            request.SortOrder,
            request.Geometry,
            request.Properties);

        if (addResult.IsFailure)
        {
            return Result<Guid>.Failure(addResult.Error);
        }

        _layoutRepository.SetOriginalRowVersion(layout, request.ExpectedRowVersion);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "AddDecoration: concurrency conflict. LayoutId={LayoutId}, ExpectedRowVersion={RowVersion}",
                request.LayoutId, request.ExpectedRowVersion);
            return Result<Guid>.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AddDecoration: persistence failed. LayoutId={LayoutId}",
                request.LayoutId);
            throw;
        }

        _logger.LogInformation(
            "AddDecoration: succeeded. LayoutId={LayoutId}, DecorationId={DecorationId}, Kind={Kind}",
            request.LayoutId, addResult.Value.Id, addResult.Value.Kind);

        return Result<Guid>.Success(addResult.Value.Id);
    }
}
