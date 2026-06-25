using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateMaxAttendeesPerRegistration;

/// <summary>
/// Issue #51: Handler for updating max attendees per registration
/// Validates the new value and persists the change
/// </summary>
public class UpdateMaxAttendeesPerRegistrationCommandHandler : ICommandHandler<UpdateMaxAttendeesPerRegistrationCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateMaxAttendeesPerRegistrationCommandHandler> _logger;

    public UpdateMaxAttendeesPerRegistrationCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateMaxAttendeesPerRegistrationCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateMaxAttendeesPerRegistrationCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateMaxAttendeesPerRegistration"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[Issue #51] UpdateMaxAttendeesPerRegistration START: EventId={EventId}, NewMax={NewMax}",
                request.EventId, request.MaxAttendeesPerRegistration);

            try
            {
                // Retrieve event with change tracking enabled
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "[Issue #51] UpdateMaxAttendeesPerRegistration FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "[Issue #51] UpdateMaxAttendeesPerRegistration: Event loaded - EventId={EventId}, Title={Title}, CurrentMax={CurrentMax}, Capacity={Capacity}",
                    @event.Id, @event.Title.Value, @event.MaxAttendeesPerRegistration, @event.Capacity);

                // Use domain method to update max attendees per registration
                var updateResult = @event.UpdateMaxAttendeesPerRegistration(request.MaxAttendeesPerRegistration);
                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "[Issue #51] UpdateMaxAttendeesPerRegistration FAILED: Domain validation failed - EventId={EventId}, NewMax={NewMax}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.MaxAttendeesPerRegistration, updateResult.Error, stopwatch.ElapsedMilliseconds);

                    return updateResult;
                }

                _logger.LogInformation(
                    "[Issue #51] UpdateMaxAttendeesPerRegistration: Domain method succeeded - EventId={EventId}, NewMax={NewMax}",
                    @event.Id, request.MaxAttendeesPerRegistration);

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "[Issue #51] UpdateMaxAttendeesPerRegistration COMPLETE: EventId={EventId}, NewMax={NewMax}, Duration={ElapsedMs}ms",
                    request.EventId, request.MaxAttendeesPerRegistration, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "[Issue #51] UpdateMaxAttendeesPerRegistration FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
