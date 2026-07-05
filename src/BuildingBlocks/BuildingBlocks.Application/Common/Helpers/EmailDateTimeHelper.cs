using SharedHelper = LankaConnect.Shared.Email.Helpers.EmailDateTimeHelper;
namespace LankaConnect.BuildingBlocks.Application.Common.Helpers;

/// <summary>
/// Phase 6A.97: Application-layer wrapper for email date/time formatting.
/// Delegates to the Shared project's EmailDateTimeHelper for actual implementation.
///
/// This wrapper exists for backward compatibility with existing Application layer code.
/// New code can directly reference LankaConnect.Shared.Email.Helpers.EmailDateTimeHelper.
/// </summary>
public static class EmailDateTimeHelper
{
    #region Phase 6A.97: New timezone-aware methods

    /// <summary>
    /// Phase 6A.97: Converts a UTC DateTime to the specified timezone.
    /// </summary>
    public static DateTime ToLocalTime(DateTime utcDateTime, string? timeZoneId)
        => SharedHelper.ToLocalTime(utcDateTime, timeZoneId);

    /// <summary>
    /// Phase 6A.97: Gets the timezone abbreviation (e.g., "EST", "PDT") for display.
    /// </summary>
    public static string GetTimezoneAbbreviation(string? timeZoneId, DateTime utcDateTime)
        => SharedHelper.GetTimezoneAbbreviation(timeZoneId, utcDateTime);

    /// <summary>
    /// Phase 6A.97: Formats a UTC date for display in emails (date only, with timezone).
    /// Example: "January 15, 2026"
    /// </summary>
    public static string FormatEventDate(DateTime utcDateTime, string? timeZoneId)
        => SharedHelper.FormatEventDate(utcDateTime, timeZoneId);

    /// <summary>
    /// Phase 6A.97: Formats a UTC time for display in emails (time only, with timezone).
    /// Example: "6:00 PM EST"
    /// </summary>
    public static string FormatEventTime(DateTime utcDateTime, string? timeZoneId)
        => SharedHelper.FormatEventTime(utcDateTime, timeZoneId);

    /// <summary>
    /// Phase 6A.97: Formats a UTC datetime with timezone abbreviation.
    /// Example: "January 15, 2026 at 6:00 PM EST"
    /// </summary>
    public static string FormatDateTimeWithTz(DateTime utcDateTime, string? timeZoneId)
        => SharedHelper.FormatDateTimeWithTz(utcDateTime, timeZoneId);

    /// <summary>
    /// Phase 6A.97: Formats a UTC datetime using a custom format pattern in specified timezone.
    /// </summary>
    public static string Format(DateTime utcDateTime, string? timeZoneId, string format)
        => SharedHelper.Format(utcDateTime, timeZoneId, format);

    #endregion

    #region Phase 8YA.1: TBD-dates support (nullable overloads)

    /// <summary>
    /// Phase 8YA.1: nullable-aware date formatter — returns "Date TBD" when null.
    /// </summary>
    public static string FormatEventDate(DateTime? utcDateTime, string? timeZoneId)
        => SharedHelper.FormatEventDate(utcDateTime, timeZoneId);

    /// <summary>
    /// Phase 8YA.1: nullable-aware time formatter — returns "Time TBD" when null.
    /// </summary>
    public static string FormatEventTime(DateTime? utcDateTime, string? timeZoneId)
        => SharedHelper.FormatEventTime(utcDateTime, timeZoneId);

    /// <summary>
    /// Phase 8YA.1: nullable-aware datetime+timezone formatter for TBD events.
    /// </summary>
    public static string FormatDateTimeWithTz(DateTime? utcDateTime, string? timeZoneId)
        => SharedHelper.FormatDateTimeWithTz(utcDateTime, timeZoneId);

    /// <summary>
    /// Phase 8YA.1: nullable-aware custom-format formatter for TBD events.
    /// </summary>
    public static string Format(DateTime? utcDateTime, string? timeZoneId, string format)
        => SharedHelper.Format(utcDateTime, timeZoneId, format);

    #endregion

    #region Legacy methods (backward compatibility)

    /// <summary>
    /// Converts a UTC DateTime to local time (defaults to Eastern Time).
    /// </summary>
    public static DateTime ToSriLankaTime(DateTime utcDateTime)
        => SharedHelper.ToSriLankaTime(utcDateTime);

    /// <summary>
    /// Formats a UTC date for display in emails (date only, defaults to Eastern Time).
    /// Example: "January 15, 2026"
    /// </summary>
    public static string FormatEventDate(DateTime utcDateTime)
        => SharedHelper.FormatEventDate(utcDateTime);

    /// <summary>
    /// Formats a UTC time for display in emails (time only, defaults to Eastern Time).
    /// Example: "6:00 PM"
    /// </summary>
    public static string FormatEventTime(DateTime utcDateTime)
        => SharedHelper.FormatEventTime(utcDateTime);

    /// <summary>
    /// Formats a UTC datetime for display in emails (date and time, defaults to Eastern Time).
    /// Example: "January 15, 2026 6:00 PM"
    /// </summary>
    public static string FormatDateTime(DateTime utcDateTime)
        => SharedHelper.FormatDateTime(utcDateTime);

    /// <summary>
    /// Formats a UTC datetime using a custom format pattern (defaults to Eastern Time).
    /// </summary>
    public static string Format(DateTime utcDateTime, string format)
        => SharedHelper.Format(utcDateTime, format);

    #endregion
}
