using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetEventPasses;

public record GetEventPassesQuery(Guid EventId) : IQuery<IReadOnlyList<EventPassDto>>;
