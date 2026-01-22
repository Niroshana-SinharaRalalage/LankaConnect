using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Events.Queries.GetEventReminderHistory;

/// <summary>
/// Phase 6A.76: Query to get reminder history for an event
/// </summary>
public record GetEventReminderHistoryQuery(Guid EventId) : IRequest<Result<List<EventReminderHistoryDto>>>;
