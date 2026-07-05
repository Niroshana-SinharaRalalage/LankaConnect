using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Queries.GetWaitingList;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetWaitingList;

/// <summary>
/// Query to get the waiting list for an event with user details and positions
/// </summary>
public record GetWaitingListQuery(Guid EventId) : IQuery<IReadOnlyList<WaitingListEntryDto>>;
