namespace LankaConnect.Domain.Events.Services;

/// <summary>
/// Phase 6A.97: Service to lookup IANA timezone from US state location.
/// Used to determine the correct timezone for event date/time display.
/// All events in LankaConnect are in the USA, serving Sri Lankans living in America.
/// </summary>
public interface ITimeZoneLookupService
{
    /// <summary>
    /// Gets IANA timezone ID from US state code or name.
    /// Examples:
    /// - "OH" or "Ohio" → "America/New_York" (Eastern)
    /// - "CA" or "California" → "America/Los_Angeles" (Pacific)
    /// - "IL" or "Illinois" → "America/Chicago" (Central)
    /// </summary>
    /// <param name="state">US state code (e.g., "OH") or full name (e.g., "Ohio")</param>
    /// <returns>IANA timezone ID (e.g., "America/New_York"). Returns default Eastern timezone for unknown states.</returns>
    string GetTimeZoneFromState(string? state);

    /// <summary>
    /// Gets the timezone abbreviation for display (e.g., "EST", "PST", "CDT").
    /// Accounts for Daylight Saving Time.
    /// </summary>
    /// <param name="timeZoneId">IANA timezone ID (e.g., "America/New_York")</param>
    /// <param name="dateTime">The datetime to check for DST</param>
    /// <returns>Timezone abbreviation (e.g., "EST" or "EDT" depending on DST)</returns>
    string GetTimezoneAbbreviation(string timeZoneId, DateTime dateTime);

    /// <summary>
    /// Validates if a timezone ID is valid and can be used.
    /// </summary>
    /// <param name="timeZoneId">IANA timezone ID to validate</param>
    /// <returns>True if the timezone ID is valid</returns>
    bool IsValidTimeZone(string timeZoneId);

    /// <summary>
    /// Gets the default timezone ID used when state is unknown.
    /// Returns "America/New_York" (Eastern Time) as most Sri Lankan communities
    /// in the USA are concentrated on the East Coast.
    /// </summary>
    string DefaultTimeZoneId { get; }
}
