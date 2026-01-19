using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
        using (LogContext.PushProperty("Operation", "UnpublishEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("UnpublishEvent START: EventId={EventId}", request.EventId);

            try
            {
                // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
                // This is required for EF Core to detect changes when we modify the entity
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UnpublishEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "UnpublishEvent: Event loaded - EventId={EventId}, Title={Title}, CurrentStatus={Status}, CurrentRegistrations={Registrations}",
                    @event.Id, @event.Title.Value, @event.Status, @event.CurrentRegistrations);

                // Use domain method to unpublish
                var unpublishResult = @event.Unpublish();

                if (unpublishResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UnpublishEvent FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, unpublishResult.Error, stopwatch.ElapsedMilliseconds);

                    return unpublishResult;
                }

                _logger.LogInformation(
                    "UnpublishEvent: Domain method succeeded - EventId={EventId}, NewStatus={Status}",
                    @event.Id, @event.Status);

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UnpublishEvent COMPLETE: EventId={EventId}, Status={Status}, Duration={ElapsedMs}ms",
                    request.EventId, @event.Status, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UnpublishEvent FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
