using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetEventById;

public record GetEventByIdQuery(Guid Id) : IQuery<EventDto?>;
