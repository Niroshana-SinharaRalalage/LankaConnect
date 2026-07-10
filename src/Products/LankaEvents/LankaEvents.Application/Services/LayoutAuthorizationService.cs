using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Services;

/// <inheritdoc />
public class LayoutAuthorizationService : ILayoutAuthorizationService
{
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LayoutAuthorizationService> _logger;

    public LayoutAuthorizationService(
        IVenueLayoutRepository layoutRepository,
        IEventRepository eventRepository,
        ICurrentUserService currentUserService,
        ILogger<LayoutAuthorizationService> logger)
    {
        _layoutRepository = layoutRepository;
        _eventRepository = eventRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<VenueLayout>> AuthorizeAsync(Guid layoutId, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId == Guid.Empty)
        {
            _logger.LogWarning(
                "Layout authorization denied: caller not authenticated. LayoutId={LayoutId}",
                layoutId);
            return Result<VenueLayout>.Forbidden("Authentication required");
        }

        VenueLayout? layout;
        try
        {
            layout = await _layoutRepository.GetByIdAsync(layoutId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to load layout for authorization. LayoutId={LayoutId}, UserId={UserId}",
                layoutId, _currentUserService.UserId);
            throw;
        }

        if (layout == null)
        {
            _logger.LogInformation(
                "Layout authorization NotFound: layout does not exist. LayoutId={LayoutId}, UserId={UserId}",
                layoutId, _currentUserService.UserId);
            return Result<VenueLayout>.NotFound("Venue layout not found");
        }

        var inner = await AuthorizeLayoutAsync(layout, cancellationToken);
        return inner.IsSuccess
            ? Result<VenueLayout>.Success(layout)
            : Result<VenueLayout>.Failure(inner.Error, inner.ErrorKind);
    }

    public async Task<Result> AuthorizeLayoutAsync(VenueLayout layout, CancellationToken cancellationToken)
    {
        if (layout == null)
        {
            return Result.NotFound("Venue layout not found");
        }

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId == Guid.Empty)
        {
            _logger.LogWarning(
                "Layout authorization denied: caller not authenticated. LayoutId={LayoutId}",
                layout.Id);
            return Result.Forbidden("Authentication required");
        }

        var userId = _currentUserService.UserId;

        // Admin short-circuit: bypass both branches without loading the event.
        if (_currentUserService.IsAdmin)
        {
            _logger.LogInformation(
                "Layout authorization: admin bypass. LayoutId={LayoutId}, UserId={UserId}",
                layout.Id, userId);
            return Result.Success();
        }

        // Branch 1: Template layout — check CreatedByUserId.
        if (layout.EventId == null)
        {
            if (layout.CreatedByUserId == userId)
            {
                return Result.Success();
            }

            _logger.LogWarning(
                "Layout authorization denied (template branch): caller is not the template owner. " +
                "LayoutId={LayoutId}, UserId={UserId}, OwnerId={OwnerId}",
                layout.Id, userId, layout.CreatedByUserId);
            return Result.Forbidden("You do not have permission to modify this template layout");
        }

        // Branch 2: Event-attached layout — check Event.IsOrganizer (covers primary + co-organizers).
        LankaConnect.Products.LankaEvents.Domain.Event? @event;
        try
        {
            @event = await _eventRepository.GetByIdAsync(layout.EventId.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to load owning event for layout authorization. " +
                "LayoutId={LayoutId}, EventId={EventId}, UserId={UserId}",
                layout.Id, layout.EventId, userId);
            throw;
        }

        if (@event == null)
        {
            _logger.LogWarning(
                "Layout authorization NotFound: owning event does not exist. " +
                "LayoutId={LayoutId}, EventId={EventId}, UserId={UserId}",
                layout.Id, layout.EventId, userId);
            return Result.NotFound("The event associated with this layout was not found");
        }

        if (@event.IsOrganizer(userId))
        {
            return Result.Success();
        }

        _logger.LogWarning(
            "Layout authorization denied (event branch): caller is not an organizer. " +
            "LayoutId={LayoutId}, EventId={EventId}, UserId={UserId}, OrganizerId={OrganizerId}",
            layout.Id, @event.Id, userId, @event.OrganizerId);
        return Result.Forbidden("You do not have permission to modify this layout");
    }
}
