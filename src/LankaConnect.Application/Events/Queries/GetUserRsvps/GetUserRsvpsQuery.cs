using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetUserRsvps;

public record GetUserRsvpsQuery(Guid UserId) : IQuery<IReadOnlyList<RsvpDto>>;
