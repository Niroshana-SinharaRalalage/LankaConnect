using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetUpcomingEventsForUser;

public record GetUpcomingEventsForUserQuery(Guid UserId) : IQuery<IReadOnlyList<EventDto>>;
