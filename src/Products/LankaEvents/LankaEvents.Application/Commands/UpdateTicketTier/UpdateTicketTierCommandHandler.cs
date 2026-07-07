using System.Diagnostics;
using LankaConnect.SharedKernel.Money;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateTicketTier;

public class UpdateTicketTierCommandHandler : ICommandHandler<UpdateTicketTierCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTicketTierCommandHandler> _logger;

    public UpdateTicketTierCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateTicketTierCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateTicketTierCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateTicketTier"))
        using (LogContext.PushProperty("EntityType", "TicketTier"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("TierId", request.TierId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateTicketTier START: EventId={EventId}, TierId={TierId}, TierName={TierName}",
                request.EventId, request.TierId, request.Name);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning("UpdateTicketTier FAILED: Event not found - EventId={EventId}", request.EventId);
                    return Result.Failure("Event not found");
                }

                // Create adult price
                var adultPriceResult = Money.Create(request.AdultPriceAmount, request.AdultPriceCurrency);
                if (adultPriceResult.IsFailure)
                    return Result.Failure(adultPriceResult.Error);

                // Create child price if provided
                Money? childPrice = null;
                if (request.ChildPriceAmount.HasValue && request.ChildPriceCurrency.HasValue)
                {
                    var childPriceResult = Money.Create(request.ChildPriceAmount.Value, request.ChildPriceCurrency.Value);
                    if (childPriceResult.IsFailure)
                        return Result.Failure(childPriceResult.Error);
                    childPrice = childPriceResult.Value;
                }

                var result = @event.UpdateTicketTier(
                    request.TierId,
                    request.Name,
                    request.Description,
                    adultPriceResult.Value,
                    childPrice,
                    request.ChildAgeLimit,
                    request.Capacity,
                    request.MaxPerUser,
                    request.SortOrder);

                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "UpdateTicketTier FAILED: Domain validation - EventId={EventId}, TierId={TierId}, Error={Error}",
                        request.EventId, request.TierId, result.Error);
                    return result;
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "UpdateTicketTier COMPLETE: EventId={EventId}, TierId={TierId}, Duration={ElapsedMs}ms",
                    request.EventId, request.TierId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateTicketTier FAILED: Exception - EventId={EventId}, TierId={TierId}, Duration={ElapsedMs}ms",
                    request.EventId, request.TierId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
