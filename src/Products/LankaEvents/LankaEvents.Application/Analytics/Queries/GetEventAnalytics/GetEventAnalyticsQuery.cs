using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Analytics.Common;
namespace LankaConnect.Products.LankaEvents.Application.Analytics.Queries.GetEventAnalytics;

/// <summary>
/// Query to get analytics for a specific event
/// </summary>
public record GetEventAnalyticsQuery(Guid EventId) : IQuery<EventAnalyticsDto?>;
