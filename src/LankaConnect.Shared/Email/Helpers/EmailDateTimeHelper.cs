namespace LankaConnect.Shared.Email.Helpers;

/// <summary>
/// Phase 6A.X Issue #40: Helper for consistent date/time formatting in emails.
/// Converts UTC dates to Sri Lanka timezone (Asia/Colombo, UTC+5:30) for display.
///
/// Problem: Event dates are stored in UTC but displayed in local time on the website.
/// The frontend converts UTC to browser local time, but emails were showing UTC time.
/// This helper ensures emails show the same time as the website.
///
/// Usage:
/// - EmailDateTimeHelper.FormatEventDate(@event.StartDate) => "January 15, 2026"
/// - EmailDateTimeHelper.FormatEventTime(@event.StartDate) => "6:00 PM"
/// - EmailDateTimeHelper.FormatDateTime(@event.StartDate) => "January 15, 2026 6:00 PM"
/// </summary>
public static class EmailDateTimeHelper
{
    /// <summary>
    /// Sri Lanka timezone (Asia/Colombo, UTC+5:30)
    /// This is used for all LankaConnect events since the platform serves Sri Lanka.
    /// </summary>
    private static readonly TimeZoneInfo SriLankaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(GetSriLankaTimeZoneId());

    /// <summary>
    /// Converts a UTC DateTime to Sri Lanka local time.
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to convert</param>
    /// <returns>DateTime in Sri Lanka timezone</returns>
    public static DateTime ToSriLankaTime(DateTime utcDateTime)
    {
        // Ensure the input is treated as UTC
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, SriLankaTimeZone);
    }

    /// <summary>
    /// Formats a UTC date for display in emails (date only).
    /// Example: "January 15, 2026"
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to format</param>
    /// <returns>Formatted date string in Sri Lanka timezone</returns>
    public static string FormatEventDate(DateTime utcDateTime)
    {
        var localTime = ToSriLankaTime(utcDateTime);
        return localTime.ToString("MMMM dd, yyyy");
    }

    /// <summary>
    /// Formats a UTC time for display in emails (time only).
    /// Example: "6:00 PM"
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to format</param>
    /// <returns>Formatted time string in Sri Lanka timezone</returns>
    public static string FormatEventTime(DateTime utcDateTime)
    {
        var localTime = ToSriLankaTime(utcDateTime);
        return localTime.ToString("h:mm tt");
    }

    /// <summary>
    /// Formats a UTC datetime for display in emails (date and time).
    /// Example: "January 15, 2026 6:00 PM"
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to format</param>
    /// <returns>Formatted datetime string in Sri Lanka timezone</returns>
    public static string FormatDateTime(DateTime utcDateTime)
    {
        var localTime = ToSriLankaTime(utcDateTime);
        return localTime.ToString("MMMM dd, yyyy h:mm tt");
    }

    /// <summary>
    /// Formats a UTC datetime using a custom format pattern.
    /// The datetime is first converted to Sri Lanka timezone.
    /// </summary>
    /// <param name="utcDateTime">UTC datetime to format</param>
    /// <param name="format">Custom format pattern</param>
    /// <returns>Formatted datetime string in Sri Lanka timezone</returns>
    public static string Format(DateTime utcDateTime, string format)
    {
        var localTime = ToSriLankaTime(utcDateTime);
        return localTime.ToString(format);
    }

    /// <summary>
    /// Gets the platform-appropriate timezone ID for Sri Lanka.
    /// Windows uses "Sri Lanka Standard Time", Linux/Mac uses "Asia/Colombo".
    /// </summary>
    private static string GetSriLankaTimeZoneId()
    {
        // Try IANA ID first (Linux/Mac/Docker), then Windows ID
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo").Id;
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall back to Windows timezone ID
            return "Sri Lanka Standard Time";
        }
    }
}
