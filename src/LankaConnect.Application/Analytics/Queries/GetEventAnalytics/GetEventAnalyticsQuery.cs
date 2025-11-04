using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Analytics.Common;

namespace LankaConnect.Application.Analytics.Queries.GetEventAnalytics;

/// <summary>
/// Query to get analytics for a specific event
/// </summary>
public record GetEventAnalyticsQuery(Guid EventId) : IQuery<EventAnalyticsDto?>;
