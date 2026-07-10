using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.SetTicketingMode;

public class SetTicketingModeCommandHandler : ICommandHandler<SetTicketingModeCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetTicketingModeCommandHandler> _logger;

    public SetTicketingModeCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<SetTicketingModeCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(SetTicketingModeCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SetTicketingMode"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SetTicketingMode START: EventId={EventId}, Mode={Mode}",
                request.EventId, request.TicketingMode);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning("SetTicketingMode FAILED: Event not found - EventId={EventId}", request.EventId);
                    return Result.Failure("Event not found");
                }

                var result = @event.SetTicketingMode(request.TicketingMode);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "SetTicketingMode FAILED: Domain validation - EventId={EventId}, Error={Error}",
                        request.EventId, result.Error);
                    return result;
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "SetTicketingMode COMPLETE: EventId={EventId}, Mode={Mode}, Duration={ElapsedMs}ms",
                    request.EventId, request.TicketingMode, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SetTicketingMode FAILED: Exception - EventId={EventId}, Duration={ElapsedMs}ms",
                    request.EventId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
