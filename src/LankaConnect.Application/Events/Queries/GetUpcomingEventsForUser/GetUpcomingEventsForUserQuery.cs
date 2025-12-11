using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetUpcomingEventsForUser;

public record GetUpcomingEventsForUserQuery(Guid UserId) : IQuery<IReadOnlyList<EventDto>>;
