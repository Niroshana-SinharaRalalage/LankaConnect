using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetUserRsvps;

public record GetUserRsvpsQuery(Guid UserId) : IQuery<IReadOnlyList<RsvpDto>>;
