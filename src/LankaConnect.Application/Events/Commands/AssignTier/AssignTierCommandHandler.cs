using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.AssignTier;

/// <summary>
/// Slice 5 Chunk 8: tier-assignment write handler. Flow:
/// <list type="number">
///   <item>Authorize the caller against the layout (two-branch: event-attached vs template).</item>
///   <item>Compare <c>If-Match</c> (ExpectedRowVersion) against the loaded layout's <c>RowVersion</c> in memory.</item>
///   <item>Validate the target zone/table exists on this layout (prevents orphan assignments).</item>
///   <item>Load the ticket tier with its current <c>Assignments</c> collection.</item>
///   <item>Call <see cref="Domain.Events.Entities.TicketTier.AssignToZone"/> or
///         <see cref="Domain.Events.Entities.TicketTier.AssignToTable"/> — idempotent.</item>
///   <item>Commit.</item>
/// </list>
/// </summary>
public class AssignTierCommandHandler : ICommandHandler<AssignTierCommand>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignTierCommandHandler> _logger;

    public AssignTierCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IVenueLayoutRepository layoutRepository,
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<AssignTierCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _layoutRepository = layoutRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignTierCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AssignTier: LayoutId={LayoutId}, TierId={TierId}, Kind={Kind}, AssignableId={AssignableId}, ExpectedRowVersion={RowVersion}",
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
                "AssignTier: concurrency conflict. LayoutId={LayoutId}, ExpectedRowVersion={Expected}, ActualRowVersion={Actual}",
                request.LayoutId, request.ExpectedRowVersion, layout.RowVersion);
            return Result.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }

        // Validate the target zone/table lives on this layout — prevents cross-layout assignments
        // and dangling polymorphic FKs.
        var targetValid = request.Kind switch
        {
            AssignableKind.Zone => layout.GetZone(request.AssignableId) is not null,
            AssignableKind.Table => layout.Tables.Any(t => t.Id == request.AssignableId),
            _ => false,
        };
        if (!targetValid)
        {
            return Result.NotFound(
                request.Kind == AssignableKind.Zone
                    ? "Zone not found on this layout"
                    : "Table not found on this layout");
        }

        var tier = await _eventRepository.GetTicketTierWithAssignmentsAsync(request.TierId, cancellationToken);
        if (tier is null)
            return Result.NotFound("Ticket tier not found");

        // Enforce that the tier belongs to the event that owns the layout (defense-in-depth;
        // the authorization service has already validated the caller can see the layout).
        if (layout.EventId.HasValue && tier.EventId != layout.EventId.Value)
            return Result.Failure("Ticket tier does not belong to this event", ErrorKind.Validation);

        var assignResult = request.Kind == AssignableKind.Zone
            ? tier.AssignToZone(request.AssignableId)
            : tier.AssignToTable(request.AssignableId);

        if (assignResult.IsFailure)
            return assignResult;

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "AssignTier: db concurrency conflict on commit. LayoutId={LayoutId}, TierId={TierId}",
                request.LayoutId, request.TierId);
            return Result.Conflict(
                "Tier was modified concurrently. Reload and retry.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AssignTier: persistence failed. LayoutId={LayoutId}, TierId={TierId}",
                request.LayoutId, request.TierId);
            throw;
        }

        _logger.LogInformation(
            "AssignTier: succeeded. TierId={TierId}, Kind={Kind}, AssignableId={AssignableId}",
            request.TierId, request.Kind, request.AssignableId);

        return Result.Success();
    }
}
