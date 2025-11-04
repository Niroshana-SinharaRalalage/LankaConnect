using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Queries.GetWaitingList;

namespace LankaConnect.Application.Events.Queries.GetWaitingList;

/// <summary>
/// Query to get the waiting list for an event with user details and positions
/// </summary>
public record GetWaitingListQuery(Guid EventId) : IQuery<IReadOnlyList<WaitingListEntryDto>>;
