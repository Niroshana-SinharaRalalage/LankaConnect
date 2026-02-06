using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateRsvp;

public class UpdateRsvpCommandHandler : ICommandHandler<UpdateRsvpCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateRsvpCommandHandler> _logger;

    public UpdateRsvpCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateRsvpCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateRsvpCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateRsvp"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateRsvp START: EventId={EventId}, UserId={UserId}, NewQuantity={NewQuantity}",
                request.EventId, request.UserId, request.NewQuantity);

            try
            {
                // Retrieve event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateRsvp FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "UpdateRsvp: Event loaded - EventId={EventId}, Title={Title}, CurrentRegistrations={Registrations}",
                    @event.Id, @event.Title.Value, @event.CurrentRegistrations);

                // Use domain method to update registration quantity
                var updateResult = @event.UpdateRegistration(request.UserId, request.NewQuantity);
                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateRsvp FAILED: Domain validation failed - EventId={EventId}, UserId={UserId}, NewQuantity={NewQuantity}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, request.NewQuantity, updateResult.Error, stopwatch.ElapsedMilliseconds);

                    return updateResult;
                }

                _logger.LogInformation(
                    "UpdateRsvp: Domain method succeeded - EventId={EventId}, UserId={UserId}, NewQuantity={NewQuantity}",
                    @event.Id, request.UserId, request.NewQuantity);

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateRsvp COMPLETE: EventId={EventId}, UserId={UserId}, NewQuantity={NewQuantity}, Duration={ElapsedMs}ms",
                    request.EventId, request.UserId, request.NewQuantity, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateRsvp FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
