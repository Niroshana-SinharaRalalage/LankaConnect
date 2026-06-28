using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventById;

public record GetEventByIdQuery(Guid Id) : IQuery<EventDto?>;
