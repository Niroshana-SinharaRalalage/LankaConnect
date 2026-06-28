using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Commands.SetSeatingMode;

public class SetSeatingModeCommandHandler : ICommandHandler<SetSeatingModeCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetSeatingModeCommandHandler> _logger;

    public SetSeatingModeCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<SetSeatingModeCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(SetSeatingModeCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SetSeatingMode"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SetSeatingMode START: EventId={EventId}, Mode={Mode}",
                request.EventId, request.SeatingMode);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning(
                        "SetSeatingMode FAILED: Event not found - EventId={EventId}",
                        request.EventId);
                    return Result.Failure("Event not found");
                }

                var previousMode = @event.SeatingMode;
                var result = @event.SetSeatingMode(request.SeatingMode);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "SetSeatingMode FAILED: Domain validation - EventId={EventId}, " +
                        "PreviousMode={PreviousMode}, RequestedMode={RequestedMode}, Error={Error}",
                        request.EventId, previousMode, request.SeatingMode, result.Error);
                    return result;
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "SetSeatingMode COMPLETE: EventId={EventId}, PreviousMode={PreviousMode}, " +
                    "NewMode={NewMode}, Duration={ElapsedMs}ms",
                    request.EventId, previousMode, @event.SeatingMode, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SetSeatingMode FAILED: Exception - EventId={EventId}, Mode={Mode}, " +
                    "Duration={ElapsedMs}ms",
                    request.EventId, request.SeatingMode, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
