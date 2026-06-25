using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 7C.2: Static labels rendered in email templates next to the event's secondary location.
/// User-confirmed 2026-04-20: labels are the enum name in space-cased form with no trailing colon,
/// no "Address:" suffix — the template itself provides the colon.
/// </summary>
internal static class SecondaryLocationEmailLabels
{
    public const string ParkingLot = "Parking Lot";
    public const string SecondaryVenue = "Secondary Venue";
}

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

        // Phase 8YA.1: TBD events (StartDate/EndDate null) skip the date-driven phases
        // entirely and surface "Date TBD" instead of the lifecycle label. Q1=A allows
        // these to be Published, so the listing card still gets a useful badge.
        if (!@event.StartDate.HasValue || !@event.EndDate.HasValue)
            return "Date TBD";

        // Priority 3: Inactive (1 week after event ended)
        if (@event.EndDate.Value.AddDays(7) < now)
            return "Inactive";

        // Priority 4: New (within 1 week of publish)
        if (@event.PublishedAt.HasValue && @event.PublishedAt.Value.AddDays(7) > now)
            return "New";

        // Priority 5: Upcoming (1 week before start to start time)
        if (@event.StartDate.Value.AddDays(-7) <= now && now < @event.StartDate.Value)
            return "Upcoming";

        // Phase 6A.X: For Published/Active events, show "Upcoming" instead of status
        // This makes it clearer for users that the event is happening in the future
        if (@event.Status == EventStatus.Published || @event.Status == EventStatus.Active)
        {
            // If event is in the future (not within 1 week of start), show "Upcoming"
            if (now < @event.StartDate.Value)
                return "Upcoming";

            // If event is currently happening, show "Active"
            if (now >= @event.StartDate.Value && now <= @event.EndDate.Value)
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

    /// <summary>
    /// Phase 7C.2: Projects an event's primary + optional secondary location into a flat
    /// record consumed by email templates. Replaces ~13 duplicated GetEventLocationString()
    /// helpers across handlers/jobs so the label text, address format, and legacy fallback
    /// all live in one place.
    ///
    /// <para>Label contract (user-confirmed 2026-04-20):</para>
    /// <list type="bullet">
    /// <item><see cref="SecondaryLocationType.ParkingLot"/> → <c>"Parking Lot"</c></item>
    /// <item><see cref="SecondaryLocationType.SecondaryVenue"/> → <c>"Secondary Venue"</c></item>
    /// </list>
    ///
    /// <para>Address format: <c>"Street, City, State, ZipCode, Country"</c> (all five parts,
    /// comma-separated). <see cref="ProjectedEmailLocation.LegacyFlatString"/> keeps the old
    /// <c>"Street, City"</c> / <c>"Online Event"</c> output for un-migrated templates.</para>
    /// </summary>
    public static LocationEmailProjection ProjectEmailLocation(this Event @event)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var legacyFlatString = @event.GetLocationDisplayString();

        if (@event.Location?.Address == null)
        {
            // Custom template engine (AzureEmailService.RenderTemplateContent) has no
            // {{else}} support, so LocationAddress must always be renderable on its own.
            // For online / address-less events, reuse the legacy "Online Event" string.
            return LocationEmailProjection.Online with
            {
                LocationAddress = legacyFlatString,
                LegacyFlatString = legacyFlatString,
            };
        }

        var locationName = @event.Location.Name ?? string.Empty;
        var locationAddress = @event.Location.GetAddressDisplayString();
        var hasLocationName = @event.Location.HasName();

        var secondary = @event.SecondaryLocation;
        var secondaryInner = secondary?.Location;
        var hasSecondary = secondary != null
            && secondaryInner != null
            && secondaryInner.Address != null;

        var secondaryLabel = string.Empty;
        var secondaryName = string.Empty;
        var hasSecondaryName = false;
        var secondaryAddress = string.Empty;

        if (hasSecondary)
        {
            secondaryLabel = secondary!.Type switch
            {
                SecondaryLocationType.ParkingLot => SecondaryLocationEmailLabels.ParkingLot,
                SecondaryLocationType.SecondaryVenue => SecondaryLocationEmailLabels.SecondaryVenue,
                _ => string.Empty,
            };
            secondaryName = secondaryInner!.Name ?? string.Empty;
            hasSecondaryName = secondaryInner.HasName();
            secondaryAddress = secondaryInner.GetAddressDisplayString();
        }

        return new LocationEmailProjection(
            LocationName: locationName,
            LocationAddress: locationAddress,
            HasLocationName: hasLocationName,
            HasSecondaryLocation: hasSecondary,
            SecondaryLocationLabel: secondaryLabel,
            SecondaryLocationName: secondaryName,
            HasSecondaryLocationName: hasSecondaryName,
            SecondaryLocationAddress: secondaryAddress,
            LegacyFlatString: legacyFlatString);
    }
}
