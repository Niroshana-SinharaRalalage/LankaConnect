using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PostponeEvent;

public class PostponeEventCommandHandler : ICommandHandler<PostponeEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostponeEventCommandHandler> _logger;

    public PostponeEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<PostponeEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(PostponeEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "PostponeEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("PostponementReason", request.PostponementReason ?? "Not specified"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "PostponeEvent START: EventId={EventId}, Reason={Reason}",
                request.EventId, request.PostponementReason ?? "Not specified");

            try
            {
                // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
                // This is required for EF Core to detect changes when we modify the entity
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "PostponeEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "PostponeEvent: Event loaded - EventId={EventId}, Title={Title}, CurrentStatus={Status}",
                    @event.Id, @event.Title.Value, @event.Status);

                // Use domain method to postpone
                var postponeResult = @event.Postpone(request.PostponementReason ?? "No reason provided");
                if (postponeResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "PostponeEvent FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, postponeResult.Error, stopwatch.ElapsedMilliseconds);

                    return postponeResult;
                }

                _logger.LogInformation(
                    "PostponeEvent: Domain method succeeded - EventId={EventId}, NewStatus={Status}",
                    @event.Id, @event.Status);

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "PostponeEvent COMPLETE: EventId={EventId}, Reason={Reason}, Status={Status}, Duration={ElapsedMs}ms",
                    request.EventId, request.PostponementReason ?? "Not specified", @event.Status, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "PostponeEvent FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
