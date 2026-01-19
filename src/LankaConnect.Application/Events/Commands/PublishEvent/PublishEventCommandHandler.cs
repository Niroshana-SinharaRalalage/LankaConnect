using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
        using (LogContext.PushProperty("Operation", "PublishEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("PublishEvent START: EventId={EventId}", request.EventId);

            try
            {
                // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
                // This is required for EF Core to detect changes when we modify the entity
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "PublishEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "PublishEvent: Event loaded - EventId={EventId}, CurrentStatus={Status}, DomainEventsCount={DomainEventCount}",
                    @event.Id, @event.Status, @event.DomainEvents.Count);

                // Use domain method to publish
                var publishResult = @event.Publish();

                _logger.LogInformation(
                    "PublishEvent: Domain method called - EventId={EventId}, Success={IsSuccess}, NewStatus={Status}, DomainEventsCount={DomainEventCount}",
                    @event.Id, publishResult.IsSuccess, @event.Status, @event.DomainEvents.Count);

                if (publishResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "PublishEvent FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, publishResult.Error, stopwatch.ElapsedMilliseconds);

                    return publishResult;
                }

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "PublishEvent COMPLETE: EventId={EventId}, Status={Status}, DomainEventsCount={DomainEventCount}, Duration={ElapsedMs}ms",
                    request.EventId, @event.Status, @event.DomainEvents.Count, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "PublishEvent FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
