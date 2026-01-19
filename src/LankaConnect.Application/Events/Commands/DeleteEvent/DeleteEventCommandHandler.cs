using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.DeleteEvent;

public class DeleteEventCommandHandler : ICommandHandler<DeleteEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteEventCommandHandler> _logger;

    public DeleteEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "DeleteEvent START: EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);

            try
            {
                // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
                // This is required for EF Core to detect changes when we modify the entity
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "DeleteEvent: Event loaded - EventId={EventId}, Title={Title}, Status={Status}, OrganizerId={OrganizerId}, Registrations={Registrations}",
                    @event.Id, @event.Title.Value, @event.Status, @event.OrganizerId, @event.CurrentRegistrations);

                // Phase 6A.59: Security fix - verify event owner
                if (@event.OrganizerId != request.UserId)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteEvent FAILED: Permission denied - EventId={EventId}, UserId={UserId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, @event.OrganizerId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Only the event organizer can delete this event");
                }

                // Business rule: Only draft or cancelled events can be deleted
                if (@event.Status != EventStatus.Draft && @event.Status != EventStatus.Cancelled)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteEvent FAILED: Invalid status - EventId={EventId}, Status={Status}, Duration={ElapsedMs}ms",
                        request.EventId, @event.Status, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Only draft or cancelled events can be deleted");
                }

                // Business rule: Cannot delete events with registrations
                if (@event.CurrentRegistrations > 0)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteEvent FAILED: Has registrations - EventId={EventId}, Registrations={Registrations}, Duration={ElapsedMs}ms",
                        request.EventId, @event.CurrentRegistrations, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Cannot delete events with existing registrations");
                }

                // Delete the event using repository method
                await _eventRepository.DeleteAsync(request.EventId, cancellationToken);

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "DeleteEvent COMPLETE: EventId={EventId}, UserId={UserId}, Title={Title}, Duration={ElapsedMs}ms",
                    request.EventId, request.UserId, @event.Title.Value, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "DeleteEvent FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
