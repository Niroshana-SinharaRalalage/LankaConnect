using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Queries.GetEventPasses;

public class GetEventPassesQueryHandler : IQueryHandler<GetEventPassesQuery, IReadOnlyList<EventPassDto>>
{
    private readonly IEventRepository _eventRepository;

    public GetEventPassesQueryHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<Result<IReadOnlyList<EventPassDto>>> Handle(
        GetEventPassesQuery request,
        CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<IReadOnlyList<EventPassDto>>.Failure("Event not found");

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

        return Result<IReadOnlyList<EventPassDto>>.Success(passDtos);
    }
}
