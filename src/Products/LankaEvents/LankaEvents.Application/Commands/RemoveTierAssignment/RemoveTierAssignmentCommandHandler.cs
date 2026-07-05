using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Commands.RemoveTierAssignment;

/// <summary>
/// Slice 5 Chunk 8: tier-assignment delete handler. Symmetric to
/// <see cref="AssignTier.AssignTierCommandHandler"/>, but calls
/// <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.TicketTier.RemoveAssignment"/>.
/// Returns <c>NotFound</c> when no matching assignment exists.
/// </summary>
public class RemoveTierAssignmentCommandHandler : ICommandHandler<RemoveTierAssignmentCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveTierAssignmentCommandHandler> _logger;

    public RemoveTierAssignmentCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IVenueLayoutRepository layoutRepository,
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveTierAssignmentCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _layoutRepository = layoutRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveTierAssignmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "RemoveTierAssignment: LayoutId={LayoutId}, TierId={TierId}, Kind={Kind}, AssignableId={AssignableId}, ExpectedRowVersion={RowVersion}",
            request.LayoutId, request.TierId, request.Kind, request.AssignableId, request.ExpectedRowVersion);

        var authResult = await _authorizationService.AuthorizeAsync(request.LayoutId, cancellationToken);
        if (authResult.IsFailure)
            return Result.Failure(authResult.Error, authResult.ErrorKind);

        var layout = await _layoutRepository.GetWithZonesAndSeatsAsync(request.LayoutId, cancellationToken);
        if (layout is null)
            return Result.NotFound("Venue layout not found");

        if (layout.RowVersion != request.ExpectedRowVersion)
        {
            _logger.LogWarning(
                "RemoveTierAssignment: concurrency conflict. LayoutId={LayoutId}, ExpectedRowVersion={Expected}, ActualRowVersion={Actual}",
                request.LayoutId, request.ExpectedRowVersion, layout.RowVersion);
            return Result.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }

        var tier = await _eventRepository.GetTicketTierWithAssignmentsAsync(request.TierId, cancellationToken);
        if (tier is null)
            return Result.NotFound("Ticket tier not found");

        var removeResult = tier.RemoveAssignment(request.Kind, request.AssignableId);
        if (removeResult.IsFailure)
        {
            // TicketTier.RemoveAssignment returns "Assignment not found" when the tuple doesn't exist.
            // Surface that as 404 so the UI can distinguish from other failures.
            return Result.NotFound(removeResult.Error);
        }

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "RemoveTierAssignment: db concurrency conflict on commit. LayoutId={LayoutId}, TierId={TierId}",
                request.LayoutId, request.TierId);
            return Result.Conflict(
                "Tier was modified concurrently. Reload and retry.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "RemoveTierAssignment: persistence failed. LayoutId={LayoutId}, TierId={TierId}",
                request.LayoutId, request.TierId);
            throw;
        }

        _logger.LogInformation(
            "RemoveTierAssignment: succeeded. TierId={TierId}, Kind={Kind}, AssignableId={AssignableId}",
            request.TierId, request.Kind, request.AssignableId);

        return Result.Success();
    }
}
