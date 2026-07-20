using LankaConnect.Products.LankaEvents.Contracts.Repositories;
using LankaConnect.Products.LankaEvents.Contracts.Services;
using LankaConnect.Products.LankaEvents.Contracts.DTOs;
using LankaConnect.Products.LankaEvents.Contracts.Shims; // 4C.h prereq: cycle-break
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.BuildingBlocks.Domain;
using MediatR;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventReminderHistory;

/// <summary>
/// Phase 6A.76: Query to get reminder history for an event
/// </summary>
public record GetEventReminderHistoryQuery(Guid EventId) : IRequest<Result<List<EventReminderHistoryDto>>>;
