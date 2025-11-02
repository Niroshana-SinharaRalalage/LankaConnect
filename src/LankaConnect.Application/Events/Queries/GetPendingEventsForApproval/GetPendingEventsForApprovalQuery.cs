using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetPendingEventsForApproval;

public record GetPendingEventsForApprovalQuery : IQuery<IReadOnlyList<EventDto>>;
