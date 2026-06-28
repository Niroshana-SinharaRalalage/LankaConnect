using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetPendingEventsForApproval;

public record GetPendingEventsForApprovalQuery : IQuery<IReadOnlyList<EventDto>>;
