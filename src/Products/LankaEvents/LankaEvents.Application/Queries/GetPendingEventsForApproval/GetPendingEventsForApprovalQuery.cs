using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetPendingEventsForApproval;

public record GetPendingEventsForApprovalQuery : IQuery<IReadOnlyList<EventDto>>;
