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
    /// Phase 6A.X: Updated to show "New" or "Upcoming" for Published/Active events instead of status
    /// Priority order: Cancelled > Completed > Inactive > New > Upcoming > Active (Published events)
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

        // Phase 6A.X: For Published/Active events, show "Upcoming" instead of status
        // This makes it clearer for users that the event is happening in the future
        if (@event.Status == EventStatus.Published || @event.Status == EventStatus.Active)
        {
            // If event is in the future (not within 1 week of start), show "Upcoming"
            if (now < @event.StartDate)
                return "Upcoming";

            // If event is currently happening, show "Active"
            if (now >= @event.StartDate && now <= @event.EndDate)
                return "Active";
        }

        // Default: Use status as-is (Draft, Postponed, Archived, UnderReview)
        return @event.Status.ToString();
    }

    /// <summary>
    /// Phase 0 (Email System): Safely extracts event location string with defensive null handling.
    /// Eliminates duplicate GetEventLocationString methods across 4+ event handlers.
    /// Handles data inconsistency where has_location=true but address fields are null.
    /// </summary>
    /// <param name="event">The event entity</param>
    /// <returns>Formatted location string or "Online Event" if no physical location</returns>
    public static string GetLocationDisplayString(this Event @event)
    {
        // Check if Location or Address is null (defensive against data inconsistency)
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;

        // Handle case where address fields exist but are empty
        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        if (string.IsNullOrWhiteSpace(street))
            return city!;

        if (string.IsNullOrWhiteSpace(city))
            return street;

        return $"{street}, {city}";
    }
}
