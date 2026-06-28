using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.RemoveTicketTier;

public class RemoveTicketTierCommandHandler : ICommandHandler<RemoveTicketTierCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveTicketTierCommandHandler> _logger;

    public RemoveTicketTierCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveTicketTierCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveTicketTierCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RemoveTicketTier"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("TierId", request.TierId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RemoveTicketTier START: EventId={EventId}, TierId={TierId}",
                request.EventId, request.TierId);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning("RemoveTicketTier FAILED: Event not found - EventId={EventId}", request.EventId);
                    return Result.Failure("Event not found");
                }

                var result = @event.RemoveTicketTier(request.TierId);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "RemoveTicketTier FAILED: Domain validation - EventId={EventId}, TierId={TierId}, Error={Error}",
                        request.EventId, request.TierId, result.Error);
                    return result;
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "RemoveTicketTier COMPLETE: EventId={EventId}, TierId={TierId}, Duration={ElapsedMs}ms",
                    request.EventId, request.TierId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "RemoveTicketTier FAILED: Exception - EventId={EventId}, TierId={TierId}, Duration={ElapsedMs}ms",
                    request.EventId, request.TierId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
