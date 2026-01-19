using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
        using (LogContext.PushProperty("Operation", "CancelEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("CancellationReason", request.CancellationReason ?? "Not specified"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CancelEvent START: EventId={EventId}, Reason={Reason}",
                request.EventId, request.CancellationReason ?? "Not specified");

            try
            {
                // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
                // This is required for EF Core to detect changes when we modify the entity
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "CancelEvent: Event loaded - EventId={EventId}, CurrentStatus={Status}, DomainEventsCount={DomainEventCount}",
                    request.EventId, @event.Status, @event.GetDomainEvents().Count);

                // Use domain method to cancel
                var cancelResult = @event.Cancel(request.CancellationReason ?? "No reason provided");
                if (cancelResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelEvent FAILED: Domain validation failed - EventId={EventId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        request.EventId, string.Join(", ", cancelResult.Errors), stopwatch.ElapsedMilliseconds);

                    return cancelResult;
                }

                _logger.LogInformation(
                    "CancelEvent: Domain method succeeded - EventId={EventId}, NewStatus={Status}, DomainEventsCount={DomainEventCount}",
                    request.EventId, @event.Status, @event.GetDomainEvents().Count);

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CancelEvent COMPLETE: EventId={EventId}, Status={Status}, Reason={Reason}, DomainEventsCount={DomainEventCount}, Duration={ElapsedMs}ms",
                    request.EventId, @event.Status, request.CancellationReason ?? "Not specified",
                    @event.GetDomainEvents().Count, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CancelEvent FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
