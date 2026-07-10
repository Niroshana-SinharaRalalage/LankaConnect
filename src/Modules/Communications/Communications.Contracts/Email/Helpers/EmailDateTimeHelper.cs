namespace LankaConnect.Modules.Communications.Contracts.Email.Helpers;

/// <summary>
/// Phase 6A.97: Helper for consistent date/time formatting in emails.
/// Converts UTC dates to the event's local timezone for display.
///
/// This app serves Sri Lankans living in the USA, so all events are in US timezones.
/// The timezone is derived from the event's location (US state).
///
/// Usage:
/// - EmailDateTimeHelper.FormatEventDate(startDate, "America/New_York") => "January 15, 2026"
/// - EmailDateTimeHelper.FormatEventTime(startDate, "America/New_York") => "6:00 PM EST"
/// - EmailDateTimeHelper.FormatDateTimeWithTz(startDate, "America/New_York") => "January 15, 2026 at 6:00 PM EST"
/// </summary>
public static class EmailDateTimeHelper
{
    /// <summary>
    /// Default timezone when not specified (Eastern Time - most Sri Lankan communities in USA are on East Coast)
    /// </summary>
    public const string DefaultTimeZoneId = "America/New_York";

    /// <summary>
    /// Standard timezone abbreviations mapping.
    /// </summary>
    private static readonly Dictionary<string, (string Standard, string Daylight)> TimezoneAbbreviations = new()
    {
        { "America/New_York", ("EST", "EDT") },
        { "America/Chicago", ("CST", "CDT") },
        { "America/Denver", ("MST", "MDT") },
        { "America/Phoenix", ("MST", "MST") },  // Arizona doesn't observe DST
        { "America/Los_Angeles", ("PST", "PDT") },
        { "America/Anchorage", ("AKST", "AKDT") },
        { "Pacific/Honolulu", ("HST", "HST") },  // Hawaii doesn't observe DST
    };

    #region Phase 6A.97: New timezone-aware methods

    /// <summary>
    /// Phase 6A.97: Converts a UTC DateTime to the specified timezone.
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to convert</param>
    /// <param name="timeZoneId">IANA timezone ID (e.g., "America/New_York")</param>
    /// <returns>DateTime in the specified timezone</returns>
    public static DateTime ToLocalTime(DateTime utcDateTime, string? timeZoneId)
    {
        var tzId = string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId;

        try
        {
            var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall back to default timezone
            var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            var defaultTz = TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, defaultTz);
        }
    }

    /// <summary>
    /// Phase 6A.97: Gets the timezone abbreviation (e.g., "EST", "PDT") for display.
    /// Accounts for Daylight Saving Time.
    /// </summary>
    /// <param name="timeZoneId">IANA timezone ID</param>
    /// <param name="utcDateTime">The datetime to check for DST</param>
    /// <returns>Timezone abbreviation (e.g., "EST" or "EDT")</returns>
    public static string GetTimezoneAbbreviation(string? timeZoneId, DateTime utcDateTime)
    {
        var tzId = string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId;

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
            var isDst = timeZone.IsDaylightSavingTime(localTime);

            if (TimezoneAbbreviations.TryGetValue(tzId, out var abbreviations))
            {
                return isDst ? abbreviations.Daylight : abbreviations.Standard;
            }

            // Fallback: use standard/daylight name
            return isDst ? ExtractAbbreviation(timeZone.DaylightName) : ExtractAbbreviation(timeZone.StandardName);
        }
        catch
        {
            return "EST"; // Safe fallback
        }
    }

    /// <summary>
    /// Phase 6A.97: Formats a UTC date for display in emails (date only).
    /// Example: "January 15, 2026"
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to format</param>
    /// <param name="timeZoneId">IANA timezone ID (e.g., "America/New_York")</param>
    /// <returns>Formatted date string in the specified timezone</returns>
    public static string FormatEventDate(DateTime utcDateTime, string? timeZoneId)
    {
        var localTime = ToLocalTime(utcDateTime, timeZoneId);
        return localTime.ToString("MMMM dd, yyyy");
    }

    /// <summary>
    /// Phase 6A.97: Formats a UTC time for display in emails (time only, with timezone).
    /// Example: "6:00 PM EST"
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to format</param>
    /// <param name="timeZoneId">IANA timezone ID (e.g., "America/New_York")</param>
    /// <returns>Formatted time string with timezone abbreviation</returns>
    public static string FormatEventTime(DateTime utcDateTime, string? timeZoneId)
    {
        var localTime = ToLocalTime(utcDateTime, timeZoneId);
        var tzAbbr = GetTimezoneAbbreviation(timeZoneId, utcDateTime);
        return $"{localTime:h:mm tt} {tzAbbr}";
    }

    /// <summary>
    /// Phase 6A.97: Formats time without timezone abbreviation.
    /// Example: "6:00 PM"
    /// </summary>
    public static string FormatEventTimeOnly(DateTime utcDateTime, string? timeZoneId)
    {
        var localTime = ToLocalTime(utcDateTime, timeZoneId);
        return localTime.ToString("h:mm tt");
    }

    /// <summary>
    /// Phase 6A.97: Formats a UTC datetime for display in emails (date and time with timezone).
    /// Example: "January 15, 2026 at 6:00 PM EST"
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to format</param>
    /// <param name="timeZoneId">IANA timezone ID (e.g., "America/New_York")</param>
    /// <returns>Formatted datetime string with timezone abbreviation</returns>
    public static string FormatDateTimeWithTz(DateTime utcDateTime, string? timeZoneId)
    {
        var localTime = ToLocalTime(utcDateTime, timeZoneId);
        var tzAbbr = GetTimezoneAbbreviation(timeZoneId, utcDateTime);
        return $"{localTime:MMMM dd, yyyy} at {localTime:h:mm tt} {tzAbbr}";
    }

    /// <summary>
    /// Phase 6A.97: Formats a UTC datetime using a custom format pattern.
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to format</param>
    /// <param name="timeZoneId">IANA timezone ID</param>
    /// <param name="format">Custom format pattern</param>
    /// <returns>Formatted datetime string in specified timezone</returns>
    public static string Format(DateTime utcDateTime, string? timeZoneId, string format)
    {
        var localTime = ToLocalTime(utcDateTime, timeZoneId);
        return localTime.ToString(format);
    }

    #endregion

    #region Phase 8YA.1: TBD-dates support (nullable overloads)

    /// <summary>
    /// Phase 8YA.1: nullable-aware date formatter for TBD events.
    /// Returns "Date TBD" when <paramref name="utcDateTime"/> is null; otherwise
    /// delegates to <see cref="FormatEventDate(DateTime, string?)"/>.
    /// </summary>
    public static string FormatEventDate(DateTime? utcDateTime, string? timeZoneId)
    {
        return utcDateTime.HasValue
            ? FormatEventDate(utcDateTime.Value, timeZoneId)
            : "Date TBD";
    }

    /// <summary>
    /// Phase 8YA.1: nullable-aware time formatter for TBD events.
    /// Returns "Time TBD" when <paramref name="utcDateTime"/> is null; otherwise
    /// delegates to <see cref="FormatEventTime(DateTime, string?)"/>.
    /// </summary>
    public static string FormatEventTime(DateTime? utcDateTime, string? timeZoneId)
    {
        return utcDateTime.HasValue
            ? FormatEventTime(utcDateTime.Value, timeZoneId)
            : "Time TBD";
    }

    /// <summary>
    /// Phase 8YA.1: nullable-aware time-only formatter for TBD events.
    /// </summary>
    public static string FormatEventTimeOnly(DateTime? utcDateTime, string? timeZoneId)
    {
        return utcDateTime.HasValue
            ? FormatEventTimeOnly(utcDateTime.Value, timeZoneId)
            : "Time TBD";
    }

    /// <summary>
    /// Phase 8YA.1: nullable-aware date+time formatter for TBD events.
    /// </summary>
    public static string FormatDateTimeWithTz(DateTime? utcDateTime, string? timeZoneId)
    {
        return utcDateTime.HasValue
            ? FormatDateTimeWithTz(utcDateTime.Value, timeZoneId)
            : "Date and time TBD";
    }

    /// <summary>
    /// Phase 8YA.1: nullable-aware custom-format formatter for TBD events.
    /// </summary>
    public static string Format(DateTime? utcDateTime, string? timeZoneId, string format)
    {
        return utcDateTime.HasValue
            ? Format(utcDateTime.Value, timeZoneId, format)
            : "TBD";
    }

    #endregion

    #region Legacy methods (backward compatibility - will be updated to use timezone in Phase 6A.97)

    /// <summary>
    /// Phase 6A.97: Legacy method that uses default Eastern timezone.
    /// Will be updated to accept timezone parameter once all callers are updated.
    /// </summary>
    public static string FormatEventDate(DateTime utcDateTime)
    {
        return FormatEventDate(utcDateTime, DefaultTimeZoneId);
    }

    /// <summary>
    /// Phase 6A.97: Legacy method that uses default Eastern timezone.
    /// Will be updated to accept timezone parameter once all callers are updated.
    /// </summary>
    public static string FormatEventTime(DateTime utcDateTime)
    {
        return FormatEventTimeOnly(utcDateTime, DefaultTimeZoneId);
    }

    /// <summary>
    /// Phase 6A.97: Legacy method that uses default Eastern timezone.
    /// Will be updated to accept timezone parameter once all callers are updated.
    /// </summary>
    public static string FormatDateTime(DateTime utcDateTime)
    {
        var localTime = ToLocalTime(utcDateTime, DefaultTimeZoneId);
        return localTime.ToString("MMMM dd, yyyy h:mm tt");
    }

    /// <summary>
    /// Phase 6A.97: Legacy method that uses default Eastern timezone.
    /// Will be updated to accept timezone parameter once all callers are updated.
    /// </summary>
    public static string Format(DateTime utcDateTime, string format)
    {
        return Format(utcDateTime, DefaultTimeZoneId, format);
    }

    /// <summary>
    /// Phase 6A.97: Legacy method - now converts to Eastern timezone (default for US events).
    /// Previously converted to Sri Lanka time, but app serves Sri Lankans in USA.
    /// </summary>
    public static DateTime ToSriLankaTime(DateTime utcDateTime)
    {
        // Now converts to Eastern time as default (app serves Sri Lankans in USA)
        return ToLocalTime(utcDateTime, DefaultTimeZoneId);
    }

    #endregion

    #region Helper methods

    /// <summary>
    /// Extracts abbreviation from timezone name.
    /// E.g., "Eastern Standard Time" → "EST"
    /// </summary>
    private static string ExtractAbbreviation(string timezoneName)
    {
        if (string.IsNullOrWhiteSpace(timezoneName))
            return "???";

        // Take first letter of each word
        var words = timezoneName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w => char.ToUpper(w[0])));
    }

    #endregion
}
