using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Queries.GetEventSignUpLists;

/// <summary>
/// Phase 7D.1: Optional Kind filter — omit (null) to return every sign-up list,
/// pass Kind=Volunteers to return only the volunteer roster, Kind=Items to
/// return only the classic items lists.
/// </summary>
public record GetEventSignUpListsQuery(Guid EventId, SignUpKind? Kind = null) : IQuery<List<SignUpListDto>>;
