using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Events.Queries.GetEventNotificationHistory;

/// <summary>
/// Phase 6A.61: Query to get event notification history for Communication tab display
/// </summary>
public record GetEventNotificationHistoryQuery(Guid EventId) : IRequest<Result<List<EventNotificationHistoryDto>>>;
