namespace LankaConnect.Modules.Communications.Contracts.Email.Helpers;

/// <summary>
/// Phase 7C.2: Email-ready projection of an event's primary + optional secondary location.
///
/// Shared DTO carrying the 8 decomposed fields every event email template renders plus
/// a flat legacy fallback for un-migrated templates. Populated in the Application layer
/// by <c>EventExtensions.ProjectEmailLocation</c> and copied into any template-specific
/// <c>*EmailParams</c> via its <c>WithLocationDetails(LocationEmailProjection)</c> method.
///
/// <para>Label contract (user-confirmed 2026-04-20):</para>
/// <list type="bullet">
/// <item>ParkingLot      → <c>"Parking Lot"</c></item>
/// <item>SecondaryVenue  → <c>"Secondary Venue"</c></item>
/// </list>
///
/// <para>Address format: <c>"Street, City, State, ZipCode, Country"</c> — all five parts
/// comma-separated. <see cref="LegacyFlatString"/> keeps the old <c>"Street, City"</c> /
/// <c>"Online Event"</c> output.</para>
/// </summary>
/// <param name="LocationName">Bold first line. Empty string for online / name-less events.</param>
/// <param name="LocationAddress">Full comma-separated address. Empty string for online events.</param>
/// <param name="HasLocationName">True when <paramref name="LocationName"/> is non-empty.</param>
/// <param name="HasSecondaryLocation">True when the event has a secondary location.</param>
/// <param name="SecondaryLocationLabel">"Parking Lot" or "Secondary Venue" — drives the visible label.</param>
/// <param name="SecondaryLocationName">Bold first line of the secondary block. Empty when unnamed.</param>
/// <param name="HasSecondaryLocationName">True when <paramref name="SecondaryLocationName"/> is set.</param>
/// <param name="SecondaryLocationAddress">Second-line address of the secondary block.</param>
/// <param name="LegacyFlatString">Single-line fallback ("Street, City" or "Online Event") kept for
/// backward compatibility with un-migrated templates that still reference <c>{{EventLocation}}</c>.</param>
public sealed record LocationEmailProjection(
    string LocationName,
    string LocationAddress,
    bool HasLocationName,
    bool HasSecondaryLocation,
    string SecondaryLocationLabel,
    string SecondaryLocationName,
    bool HasSecondaryLocationName,
    string SecondaryLocationAddress,
    string LegacyFlatString)
{
    /// <summary>
    /// Phase 7C.2: Empty projection representing an online event (no primary location).
    /// Convenient sentinel for callers that want a consistent "everything empty" value.
    /// </summary>
    public static LocationEmailProjection Online { get; } = new(
        LocationName: string.Empty,
        LocationAddress: string.Empty,
        HasLocationName: false,
        HasSecondaryLocation: false,
        SecondaryLocationLabel: string.Empty,
        SecondaryLocationName: string.Empty,
        HasSecondaryLocationName: false,
        SecondaryLocationAddress: string.Empty,
        LegacyFlatString: "Online Event");

    /// <summary>
    /// Phase 7C.2b Chunk 2c: defence-in-depth fallback for every
    /// <c>*EmailParams.ToDictionary()</c>. When a handler constructs a params
    /// object but forgets to call <c>WithLocationDetails(projection)</c>, this
    /// factory keeps the decomposed template rendering the scalar
    /// <c>EventLocation</c> value in the <c>{{LocationAddress}}</c> slot instead
    /// of an empty span — avoiding the 2026-04-23 "LOCATION header with empty
    /// value" regression on the paid-ticket confirmation. Matches the
    /// <c>Location?.Address == null</c> branch of
    /// <c>EventExtensions.ProjectEmailLocation</c>, which already projects the
    /// flat string into <see cref="LocationAddress"/> and
    /// <see cref="LegacyFlatString"/>.
    /// </summary>
    /// <param name="eventLocation">The flat scalar — street+city, "Online
    /// Event", or whatever the handler's legacy helper returned. Null is
    /// normalized to empty string.</param>
    public static LocationEmailProjection FromLegacyScalar(string eventLocation)
    {
        var s = eventLocation ?? string.Empty;
        return Online with
        {
            LocationAddress = s,
            LegacyFlatString = s,
        };
    }
}
