using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.CancelEvent;

public class CancelEventCommandHandler : ICommandHandler<CancelEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelEventCommandHandler> _logger;

    public CancelEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<CancelEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Phase 6A.63] CancelEventCommandHandler - START cancelling event {EventId}, Reason: {Reason}",
            request.EventId, request.CancellationReason);

        try
        {
            // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
            // This is required for EF Core to detect changes when we modify the entity
            var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("[Phase 6A.63] Event {EventId} not found", request.EventId);
                return Result.Failure("Event not found");
            }

            _logger.LogInformation("[Phase 6A.63] Event {EventId} retrieved, Status: {Status}, Has DomainEvents: {HasEvents}",
                request.EventId, @event.Status, @event.GetDomainEvents().Count);

            // Use domain method to cancel
            var cancelResult = @event.Cancel(request.CancellationReason);
            if (cancelResult.IsFailure)
            {
                _logger.LogWarning("[Phase 6A.63] Event.Cancel() failed for {EventId}: {Error}",
                    request.EventId, string.Join(", ", cancelResult.Errors));
                return cancelResult;
            }

            _logger.LogInformation("[Phase 6A.63] Event.Cancel() succeeded, DomainEvents count: {Count}, Calling CommitAsync...",
                @event.GetDomainEvents().Count);

            // Save changes (EF Core tracks changes automatically)
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("[Phase 6A.63] CancelEventCommandHandler - COMPLETED for event {EventId}",
                request.EventId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.63] [ERROR] Exception in CancelEventCommandHandler for event {EventId}",
                request.EventId);
            throw;
        }
    }
}
