using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.RemovePassFromEvent;

/// <summary>
/// Handler for removing event passes
/// Removes a specific ticket tier from an event
/// </summary>
public class RemovePassFromEventCommandHandler : ICommandHandler<RemovePassFromEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemovePassFromEventCommandHandler> _logger;

    public RemovePassFromEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemovePassFromEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RemovePassFromEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RemovePassFromEvent"))
        using (LogContext.PushProperty("EntityType", "EventPass"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("PassId", request.PassId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RemovePassFromEvent START: EventId={EventId}, PassId={PassId}",
                request.EventId, request.PassId);

            try
            {
                // Get the event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemovePassFromEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "RemovePassFromEvent: Event loaded - EventId={EventId}, Title={Title}, TotalPasses={TotalPasses}",
                    @event.Id, @event.Title.Value, @event.Passes.Count);

                // Get the pass to log details before removal
                var passToRemove = @event.Passes.FirstOrDefault(p => p.Id == request.PassId);
                if (passToRemove != null)
                {
                    _logger.LogInformation(
                        "RemovePassFromEvent: Pass found - PassId={PassId}, PassName={PassName}, Price={Price}",
                        passToRemove.Id, passToRemove.Name.Value, passToRemove.Price.Amount);
                }
                else
                {
                    _logger.LogWarning(
                        "RemovePassFromEvent: Pass not found in event - EventId={EventId}, PassId={PassId}",
                        request.EventId, request.PassId);
                }

                // Remove pass from event
                var removeResult = @event.RemovePass(request.PassId);
                if (removeResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemovePassFromEvent FAILED: Domain RemovePass failed - EventId={EventId}, PassId={PassId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.PassId, removeResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(removeResult.Error);
                }

                _logger.LogInformation(
                    "RemovePassFromEvent: Pass removed from event - EventId={EventId}, PassId={PassId}, RemainingPasses={RemainingPasses}",
                    @event.Id, request.PassId, @event.Passes.Count);

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RemovePassFromEvent COMPLETE: EventId={EventId}, PassId={PassId}, Duration={ElapsedMs}ms",
                    request.EventId, request.PassId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RemovePassFromEvent FAILED: Exception occurred - EventId={EventId}, PassId={PassId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.PassId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
