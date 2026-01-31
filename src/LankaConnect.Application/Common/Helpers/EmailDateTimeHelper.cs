using SharedHelper = LankaConnect.Shared.Email.Helpers.EmailDateTimeHelper;

namespace LankaConnect.Application.Common.Helpers;

/// <summary>
/// Phase 6A.X Issue #40: Application-layer wrapper for email date/time formatting.
/// Delegates to the Shared project's EmailDateTimeHelper for actual implementation.
///
/// This wrapper exists for backward compatibility with existing Application layer code.
/// New code can directly reference LankaConnect.Shared.Email.Helpers.EmailDateTimeHelper.
/// </summary>
public static class EmailDateTimeHelper
{
    /// <summary>
    /// Converts a UTC DateTime to Sri Lanka local time.
    /// </summary>
    public static DateTime ToSriLankaTime(DateTime utcDateTime)
        => SharedHelper.ToSriLankaTime(utcDateTime);

    /// <summary>
    /// Formats a UTC date for display in emails (date only).
    /// Example: "January 15, 2026"
    /// </summary>
    public static string FormatEventDate(DateTime utcDateTime)
        => SharedHelper.FormatEventDate(utcDateTime);

    /// <summary>
    /// Formats a UTC time for display in emails (time only).
    /// Example: "6:00 PM"
    /// </summary>
    public static string FormatEventTime(DateTime utcDateTime)
        => SharedHelper.FormatEventTime(utcDateTime);

    /// <summary>
    /// Formats a UTC datetime for display in emails (date and time).
    /// Example: "January 15, 2026 6:00 PM"
    /// </summary>
    public static string FormatDateTime(DateTime utcDateTime)
        => SharedHelper.FormatDateTime(utcDateTime);

    /// <summary>
    /// Formats a UTC datetime using a custom format pattern.
    /// The datetime is first converted to Sri Lanka timezone.
    /// </summary>
    public static string Format(DateTime utcDateTime, string format)
        => SharedHelper.Format(utcDateTime, format);
}
