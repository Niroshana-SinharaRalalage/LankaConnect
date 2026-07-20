using LankaConnect.Products.LankaEvents.Contracts.Repositories;
using LankaConnect.Products.LankaEvents.Contracts.Services;
using LankaConnect.Products.LankaEvents.Contracts.DTOs;
using LankaConnect.Products.LankaEvents.Contracts.Shims; // 4C.h prereq: cycle-break
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.BuildingBlocks.Domain;
using MediatR;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventNotificationHistory;

/// <summary>
/// Phase 6A.61: Query to get event notification history for Communication tab display
/// </summary>
public record GetEventNotificationHistoryQuery(Guid EventId) : IRequest<Result<List<EventNotificationHistoryDto>>>;
