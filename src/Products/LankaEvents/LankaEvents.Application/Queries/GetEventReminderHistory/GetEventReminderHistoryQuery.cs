using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.BuildingBlocks.Domain;
using MediatR;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventReminderHistory;

/// <summary>
/// Phase 6A.76: Query to get reminder history for an event
/// </summary>
public record GetEventReminderHistoryQuery(Guid EventId) : IRequest<Result<List<EventReminderHistoryDto>>>;
