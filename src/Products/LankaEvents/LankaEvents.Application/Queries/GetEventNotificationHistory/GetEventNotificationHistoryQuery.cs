using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventNotificationHistory;

/// <summary>
/// Phase 6A.61: Query to get event notification history for Communication tab display
/// </summary>
public record GetEventNotificationHistoryQuery(Guid EventId) : IRequest<Result<List<EventNotificationHistoryDto>>>;
