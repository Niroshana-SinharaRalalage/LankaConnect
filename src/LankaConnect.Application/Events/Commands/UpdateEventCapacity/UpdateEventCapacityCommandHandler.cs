using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateEventCapacity;

public class UpdateEventCapacityCommandHandler : ICommandHandler<UpdateEventCapacityCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateEventCapacityCommandHandler> _logger;

    public UpdateEventCapacityCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateEventCapacityCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateEventCapacityCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateEventCapacity"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateEventCapacity START: EventId={EventId}, NewCapacity={NewCapacity}",
                request.EventId, request.NewCapacity);

            try
            {
                // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
                // This is required for EF Core to detect changes when we modify the entity
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateEventCapacity FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "UpdateEventCapacity: Event loaded - EventId={EventId}, Title={Title}, CurrentCapacity={CurrentCapacity}, CurrentRegistrations={Registrations}",
                    @event.Id, @event.Title.Value, @event.Capacity, @event.CurrentRegistrations);

                // Use domain method to update capacity
                var updateResult = @event.UpdateCapacity(request.NewCapacity);
                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateEventCapacity FAILED: Domain validation failed - EventId={EventId}, NewCapacity={NewCapacity}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.NewCapacity, updateResult.Error, stopwatch.ElapsedMilliseconds);

                    return updateResult;
                }

                _logger.LogInformation(
                    "UpdateEventCapacity: Domain method succeeded - EventId={EventId}, OldCapacity={OldCapacity}, NewCapacity={NewCapacity}",
                    @event.Id, @event.Capacity, request.NewCapacity);

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateEventCapacity COMPLETE: EventId={EventId}, NewCapacity={NewCapacity}, Duration={ElapsedMs}ms",
                    request.EventId, request.NewCapacity, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateEventCapacity FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
