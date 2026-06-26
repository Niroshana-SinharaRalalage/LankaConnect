using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetTicketTiers;

public class GetTicketTiersQueryHandler : IQueryHandler<GetTicketTiersQuery, IReadOnlyList<TicketTierDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<GetTicketTiersQueryHandler> _logger;

    public GetTicketTiersQueryHandler(
        IEventRepository eventRepository,
        ILogger<GetTicketTiersQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<TicketTierDto>>> Handle(
        GetTicketTiersQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetTicketTiers"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("GetTicketTiers START: EventId={EventId}", request.EventId);

            try
            {
                if (request.EventId == Guid.Empty)
                {
                    _logger.LogWarning("GetTicketTiers FAILED: Invalid EventId");
                    return Result<IReadOnlyList<TicketTierDto>>.Failure("Event ID is required");
                }

                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning("GetTicketTiers FAILED: Event not found - EventId={EventId}", request.EventId);
                    return Result<IReadOnlyList<TicketTierDto>>.Failure("Event not found");
                }

                if (@event.TicketingMode != TicketingMode.Tiered)
                {
                    _logger.LogInformation(
                        "GetTicketTiers: Event is not in Tiered mode - EventId={EventId}, Mode={Mode}",
                        request.EventId, @event.TicketingMode);
                    return Result<IReadOnlyList<TicketTierDto>>.Success(Array.Empty<TicketTierDto>());
                }

                var tierDtos = @event.GetActiveTiers()
                    .Select(t => new TicketTierDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        AdultPriceAmount = t.AdultPrice.Amount,
                        AdultPriceCurrency = t.AdultPrice.Currency,
                        ChildPriceAmount = t.ChildPrice?.Amount,
                        ChildPriceCurrency = t.ChildPrice?.Currency,
                        ChildAgeLimit = t.ChildAgeLimit,
                        HasChildPricing = t.HasChildPricing,
                        Capacity = t.Capacity,
                        ReservedCount = t.ReservedCount,
                        AvailableQuantity = t.AvailableQuantity,
                        MaxPerUser = t.MaxPerUser,
                        SortOrder = t.SortOrder,
                        IsActive = t.IsActive,
                        IsFree = t.IsFree
                    })
                    .ToList();

                stopwatch.Stop();
                _logger.LogInformation(
                    "GetTicketTiers COMPLETE: EventId={EventId}, TierCount={TierCount}, Duration={ElapsedMs}ms",
                    request.EventId, tierDtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<TicketTierDto>>.Success(tierDtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetTicketTiers FAILED: Exception - EventId={EventId}, Duration={ElapsedMs}ms",
                    request.EventId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
