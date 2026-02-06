using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetEventPasses;

public class GetEventPassesQueryHandler : IQueryHandler<GetEventPassesQuery, IReadOnlyList<EventPassDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<GetEventPassesQueryHandler> _logger;

    public GetEventPassesQueryHandler(
        IEventRepository eventRepository,
        ILogger<GetEventPassesQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<EventPassDto>>> Handle(
        GetEventPassesQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventPasses"))
        using (LogContext.PushProperty("EntityType", "EventPass"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventPasses START: EventId={EventId}",
                request.EventId);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventPasses FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventPassDto>>.Failure("Event ID is required");
                }

                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventPasses FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventPassDto>>.Failure("Event not found");
                }

                _logger.LogInformation(
                    "GetEventPasses: Event loaded - EventId={EventId}, Title={Title}, PassCount={PassCount}",
                    @event.Id, @event.Title.Value, @event.Passes.Count);

                var passDtos = @event.Passes
                    .Select(p => new EventPassDto
                    {
                        Id = p.Id,
                        Name = p.Name.Value,
                        Description = p.Description.Value,
                        PriceAmount = p.Price.Amount,
                        PriceCurrency = p.Price.Currency.ToString(),
                        TotalQuantity = p.TotalQuantity,
                        AvailableQuantity = p.AvailableQuantity,
                        ReservedQuantity = p.ReservedQuantity
                    })
                    .ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventPasses COMPLETE: EventId={EventId}, PassCount={PassCount}, TotalAvailable={TotalAvailable}, Duration={ElapsedMs}ms",
                    request.EventId, passDtos.Count, passDtos.Sum(p => p.AvailableQuantity), stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<EventPassDto>>.Success(passDtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventPasses FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
