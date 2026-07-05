using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.PublishEvent;

public class PublishEventCommandHandler : ICommandHandler<PublishEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PublishEventCommandHandler> _logger;

    public PublishEventCommandHandler(
        IEventRepository eventRepository,
        IVenueLayoutRepository venueLayoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<PublishEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _venueLayoutRepository = venueLayoutRepository;
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

            _logger.LogInformation("PublishEvent START: EventId={EventId}", request.EventId);

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
                    "PublishEvent: Event loaded - EventId={EventId}, CurrentStatus={Status}, DomainEventsCount={DomainEventCount}, VenueLayoutId={VenueLayoutId}",
                    @event.Id, @event.Status, @event.DomainEvents.Count, @event.VenueLayoutId);

                // Slice 9.1: publish-readiness gate. For seated events (VenueLayoutId set),
                // load the assigned layout and ask the domain whether it's publish-ready —
                // strict tier-mapping + capacity invariants. GA events skip this check.
                if (@event.VenueLayoutId.HasValue)
                {
                    var layout = await _venueLayoutRepository.GetAssignedLayoutForEventAsync(
                        @event.Id, cancellationToken);

                    if (layout is null)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "PublishEvent FAILED: VenueLayoutId={VenueLayoutId} but layout could not be loaded — orphan or DB integrity issue. EventId={EventId}, Duration={ElapsedMs}ms",
                            @event.VenueLayoutId.Value, request.EventId, stopwatch.ElapsedMilliseconds);
                        return Result.Failure(
                            "Event references a venue layout but the layout could not be loaded. Please reattach the layout in the seating section.");
                    }

                    var readinessResult = @event.CheckLayoutPublishReadiness(layout);
                    if (readinessResult.IsFailure)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "PublishEvent FAILED: Layout publish-readiness check failed - EventId={EventId}, LayoutId={LayoutId}, Reason={Reason}, Duration={ElapsedMs}ms",
                            request.EventId, layout.Id, readinessResult.Error, stopwatch.ElapsedMilliseconds);
                        return readinessResult;
                    }

                    _logger.LogInformation(
                        "PublishEvent: Layout publish-readiness check passed - EventId={EventId}, LayoutId={LayoutId}",
                        request.EventId, layout.Id);
                }

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
