using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.AddTicketTier;

public class AddTicketTierCommandHandler : ICommandHandler<AddTicketTierCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddTicketTierCommandHandler> _logger;

    public AddTicketTierCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddTicketTierCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(AddTicketTierCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AddTicketTier"))
        using (LogContext.PushProperty("EntityType", "TicketTier"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AddTicketTier START: EventId={EventId}, TierName={TierName}, AdultPrice={AdultPrice} {Currency}, Capacity={Capacity}",
                request.EventId, request.Name, request.AdultPriceAmount, request.AdultPriceCurrency, request.Capacity);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning(
                        "AddTicketTier FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure("Event not found");
                }

                // Create adult price
                var adultPriceResult = Money.Create(request.AdultPriceAmount, request.AdultPriceCurrency);
                if (adultPriceResult.IsFailure)
                {
                    _logger.LogWarning(
                        "AddTicketTier FAILED: Adult price validation failed - Error={Error}",
                        adultPriceResult.Error);
                    return Result<Guid>.Failure(adultPriceResult.Error);
                }

                // Create child price if provided
                Money? childPrice = null;
                if (request.ChildPriceAmount.HasValue && request.ChildPriceCurrency.HasValue)
                {
                    var childPriceResult = Money.Create(request.ChildPriceAmount.Value, request.ChildPriceCurrency.Value);
                    if (childPriceResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "AddTicketTier FAILED: Child price validation failed - Error={Error}",
                            childPriceResult.Error);
                        return Result<Guid>.Failure(childPriceResult.Error);
                    }
                    childPrice = childPriceResult.Value;
                }

                // Add tier to event (domain validates tiered mode, duplicate names, capacity)
                var tierResult = @event.AddTicketTier(
                    request.Name,
                    request.Description,
                    adultPriceResult.Value,
                    childPrice,
                    request.ChildAgeLimit,
                    request.Capacity,
                    request.MaxPerUser,
                    request.SortOrder);

                if (tierResult.IsFailure)
                {
                    _logger.LogWarning(
                        "AddTicketTier FAILED: Domain validation - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, tierResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure(tierResult.Error);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "AddTicketTier COMPLETE: EventId={EventId}, TierId={TierId}, TierName={TierName}, Duration={ElapsedMs}ms",
                    request.EventId, tierResult.Value.Id, tierResult.Value.Name, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(tierResult.Value.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "AddTicketTier FAILED: Exception - EventId={EventId}, TierName={TierName}, Duration={ElapsedMs}ms",
                    request.EventId, request.Name, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
