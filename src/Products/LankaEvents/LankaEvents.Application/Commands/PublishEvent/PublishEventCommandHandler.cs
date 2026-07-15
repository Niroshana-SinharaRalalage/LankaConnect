using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.PublishEvent;

public class PublishEventCommandHandler : ICommandHandler<PublishEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    // Wave 8.5.g (2026-07-15): direct-SaveChanges pattern per Consult #25 Q6 blanket
    // + Wave 8.5.f interceptor now dispatches domain events for us. IUnitOfWork.CommitAsync
    // fires on AppDbContext (0 changes — Event lives on LankaEventsDbContext) so publish
    // returned 200 but Status stayed 0. Same split-brain as CreateEventCommandHandler
    // resolved at sprint 17th deploy.
    private readonly LankaEventsDbContext _dbContext;
    private readonly ILogger<PublishEventCommandHandler> _logger;

    public PublishEventCommandHandler(
        IEventRepository eventRepository,
        IVenueLayoutRepository venueLayoutRepository,
        LankaEventsDbContext dbContext,
        ILogger<PublishEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _venueLayoutRepository = venueLayoutRepository;
        _dbContext = dbContext;
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

                // Wave 8.5.g direct-SaveChanges: _unitOfWork.CommitAsync fires on AppDbContext
                // which has 0 changes (Event is tracked by LankaEventsDbContext post-Consult #20
                // sweep). Route directly so status transition actually persists. Wave 8.5.f
                // interceptor on LankaEventsDbContext dispatches EventPublishedIntegrationEvent
                // + related domain events raised inside event.Publish().
                await _dbContext.SaveChangesAsync(cancellationToken);

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
