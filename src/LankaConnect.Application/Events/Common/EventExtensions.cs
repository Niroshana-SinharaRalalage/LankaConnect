using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 6A.46: Extension methods for Event entity
/// </summary>
public static class EventExtensions
{
    /// <summary>
    /// Phase 6A.46: Calculate user-facing display label based on event lifecycle
    /// Priority order: Cancelled > Completed > Inactive > New > Upcoming > Status
    /// </summary>
    /// <param name="event">The event entity</param>
    /// <returns>Display label string</returns>
    public static string GetDisplayLabel(this Event @event)
    {
        var now = DateTime.UtcNow;

        // Priority 1: Cancelled (highest priority)
        if (@event.Status == EventStatus.Cancelled)
            return "Cancelled";

        // Priority 2: Completed
        if (@event.Status == EventStatus.Completed)
            return "Completed";

        // Priority 3: Inactive (1 week after event ended)
        if (@event.EndDate.AddDays(7) < now)
            return "Inactive";

        // Priority 4: New (within 1 week of publish)
        if (@event.PublishedAt.HasValue && @event.PublishedAt.Value.AddDays(7) > now)
            return "New";

        // Priority 5: Upcoming (1 week before start to start time)
        if (@event.StartDate.AddDays(-7) <= now && now < @event.StartDate)
            return "Upcoming";

        // Default: Use status as-is (Draft, Published, Active, Postponed, Archived, UnderReview)
        return @event.Status.ToString();
    }
}
