using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetEventSignUpLists;

public record GetEventSignUpListsQuery(Guid EventId) : IQuery<List<SignUpListDto>>;
