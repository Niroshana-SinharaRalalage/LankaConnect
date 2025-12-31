using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.PublishEvent;

public class PublishEventCommandHandler : ICommandHandler<PublishEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PublishEventCommandHandler> _logger;

    public PublishEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<PublishEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DIAG-1] PublishEventCommandHandler.Handle START - EventId: {EventId}",
            request.EventId);

        // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
        // This is required for EF Core to detect changes when we modify the entity
        var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
        if (@event == null)
        {
            _logger.LogWarning("[DIAG-2] Event not found: {EventId}", request.EventId);
            return Result.Failure("Event not found");
        }

        _logger.LogInformation(
            "[DIAG-3] Event loaded - Id: {EventId}, Status: {Status}, DomainEvents BEFORE Publish: {DomainEventCount}",
            @event.Id, @event.Status, @event.DomainEvents.Count);

        // Use domain method to publish
        var publishResult = @event.Publish();

        _logger.LogInformation(
            "[DIAG-4] After Publish() - Success: {IsSuccess}, Status: {Status}, DomainEvents AFTER Publish: {DomainEventCount}, EventTypes: [{EventTypes}]",
            publishResult.IsSuccess,
            @event.Status,
            @event.DomainEvents.Count,
            string.Join(", ", @event.DomainEvents.Select(e => e.GetType().Name)));

        if (publishResult.IsFailure)
        {
            _logger.LogWarning("[DIAG-5] Publish failed: {Error}", publishResult.Error);
            return publishResult;
        }

        _logger.LogInformation("[DIAG-6] Calling _unitOfWork.CommitAsync...");

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "[DIAG-7] PublishEventCommandHandler.Handle COMPLETE - EventId: {EventId}",
            request.EventId);

        return Result.Success();
    }
}
