using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.UnpublishEvent;

/// <summary>
/// Phase 6A.41: Handler for unpublishing events (returning to Draft status).
/// Follows the same pattern as PublishEventCommandHandler.
/// </summary>
public class UnpublishEventCommandHandler : ICommandHandler<UnpublishEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnpublishEventCommandHandler> _logger;

    public UnpublishEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UnpublishEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UnpublishEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Phase 6A.41] UnpublishEventCommandHandler.Handle START - EventId: {EventId}",
            request.EventId);

        // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
        // This is required for EF Core to detect changes when we modify the entity
        var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
        if (@event == null)
        {
            _logger.LogWarning("Event not found: {EventId}", request.EventId);
            return Result.Failure("Event not found");
        }

        _logger.LogInformation(
            "Event loaded - Id: {EventId}, Status: {Status}, CurrentRegistrations: {Registrations}",
            @event.Id, @event.Status, @event.CurrentRegistrations);

        // Use domain method to unpublish
        var unpublishResult = @event.Unpublish();

        if (unpublishResult.IsFailure)
        {
            _logger.LogWarning("Unpublish failed: {Error}", unpublishResult.Error);
            return unpublishResult;
        }

        // Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "[Phase 6A.41] Event {EventId} unpublished successfully - Status changed to Draft",
            request.EventId);

        return Result.Success();
    }
}
