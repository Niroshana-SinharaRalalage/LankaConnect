using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventSignUpLists;

/// <summary>
/// Phase 7D.1: Optional Kind filter — omit (null) to return every sign-up list,
/// pass Kind=Volunteers to return only the volunteer roster, Kind=Items to
/// return only the classic items lists.
/// </summary>
public record GetEventSignUpListsQuery(Guid EventId, SignUpKind? Kind = null) : IQuery<List<SignUpListDto>>;
