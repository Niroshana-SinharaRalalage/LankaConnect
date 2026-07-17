using System.Diagnostics;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.SharedKernel.Money;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.AddTicketTier;

public class AddTicketTierCommandHandler : ICommandHandler<AddTicketTierCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<AddTicketTierCommandHandler> _logger;

    public AddTicketTierCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        ILogger<AddTicketTierCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
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
                var adultPriceResult = MoneyBuilder.Create(request.AdultPriceAmount, request.AdultPriceCurrency);
                if (adultPriceResult.IsFailure)
                {
                    _logger.LogWarning(
                        "AddTicketTier FAILED: Adult price validation failed - Error={Error}",
                        adultPriceResult.Error);
                    return Result<Guid>.Failure(adultPriceResult.Error);
                }

                // Create child price if provided
                Money? childPrice = null;
                if (request.ChildPriceAmount.HasValue && request.ChildPriceCurrency != null)
                {
                    var childPriceResult = MoneyBuilder.Create(request.ChildPriceAmount.Value, request.ChildPriceCurrency);
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

                await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g direct-SaveChanges

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
