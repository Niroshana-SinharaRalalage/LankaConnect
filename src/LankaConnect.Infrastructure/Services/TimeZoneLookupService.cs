using LankaConnect.Domain.Events.Services;
using Serilog;

namespace LankaConnect.Infrastructure.Services;

/// <summary>
/// Phase 6A.97: Maps US states to IANA timezone identifiers.
/// Used to determine the correct timezone for event date/time display.
/// All events in LankaConnect are in the USA, serving Sri Lankans living in America.
/// </summary>
public class TimeZoneLookupService : ITimeZoneLookupService
{
    private readonly ILogger _logger;

    /// <summary>
    /// Default timezone when state is unknown (Eastern Time).
    /// Most Sri Lankan communities in the USA are on the East Coast.
    /// </summary>
    public string DefaultTimeZoneId => "America/New_York";

    /// <summary>
    /// Mapping of US state codes and names to IANA timezone identifiers.
    /// Uses case-insensitive comparison for flexibility.
    /// </summary>
    private static readonly Dictionary<string, string> StateToTimezone = new(StringComparer.OrdinalIgnoreCase)
    {
        // Eastern Time (UTC-5 / UTC-4 DST) - "America/New_York"
        { "OH", "America/New_York" }, { "Ohio", "America/New_York" },
        { "NY", "America/New_York" }, { "New York", "America/New_York" },
        { "PA", "America/New_York" }, { "Pennsylvania", "America/New_York" },
        { "FL", "America/New_York" }, { "Florida", "America/New_York" },
        { "GA", "America/New_York" }, { "Georgia", "America/New_York" },
        { "NC", "America/New_York" }, { "North Carolina", "America/New_York" },
        { "SC", "America/New_York" }, { "South Carolina", "America/New_York" },
        { "VA", "America/New_York" }, { "Virginia", "America/New_York" },
        { "WV", "America/New_York" }, { "West Virginia", "America/New_York" },
        { "MI", "America/New_York" }, { "Michigan", "America/New_York" },
        { "IN", "America/New_York" }, { "Indiana", "America/New_York" },
        { "KY", "America/New_York" }, { "Kentucky", "America/New_York" },
        { "TN", "America/New_York" }, { "Tennessee", "America/New_York" },
        { "MA", "America/New_York" }, { "Massachusetts", "America/New_York" },
        { "CT", "America/New_York" }, { "Connecticut", "America/New_York" },
        { "NJ", "America/New_York" }, { "New Jersey", "America/New_York" },
        { "MD", "America/New_York" }, { "Maryland", "America/New_York" },
        { "DC", "America/New_York" }, { "District of Columbia", "America/New_York" },
        { "DE", "America/New_York" }, { "Delaware", "America/New_York" },
        { "ME", "America/New_York" }, { "Maine", "America/New_York" },
        { "NH", "America/New_York" }, { "New Hampshire", "America/New_York" },
        { "VT", "America/New_York" }, { "Vermont", "America/New_York" },
        { "RI", "America/New_York" }, { "Rhode Island", "America/New_York" },

        // Central Time (UTC-6 / UTC-5 DST) - "America/Chicago"
        { "IL", "America/Chicago" }, { "Illinois", "America/Chicago" },
        { "TX", "America/Chicago" }, { "Texas", "America/Chicago" },
        { "MN", "America/Chicago" }, { "Minnesota", "America/Chicago" },
        { "WI", "America/Chicago" }, { "Wisconsin", "America/Chicago" },
        { "IA", "America/Chicago" }, { "Iowa", "America/Chicago" },
        { "MO", "America/Chicago" }, { "Missouri", "America/Chicago" },
        { "AR", "America/Chicago" }, { "Arkansas", "America/Chicago" },
        { "LA", "America/Chicago" }, { "Louisiana", "America/Chicago" },
        { "MS", "America/Chicago" }, { "Mississippi", "America/Chicago" },
        { "AL", "America/Chicago" }, { "Alabama", "America/Chicago" },
        { "OK", "America/Chicago" }, { "Oklahoma", "America/Chicago" },
        { "KS", "America/Chicago" }, { "Kansas", "America/Chicago" },
        { "NE", "America/Chicago" }, { "Nebraska", "America/Chicago" },
        { "SD", "America/Chicago" }, { "South Dakota", "America/Chicago" },
        { "ND", "America/Chicago" }, { "North Dakota", "America/Chicago" },

        // Mountain Time (UTC-7 / UTC-6 DST) - "America/Denver"
        { "CO", "America/Denver" }, { "Colorado", "America/Denver" },
        { "NM", "America/Denver" }, { "New Mexico", "America/Denver" },
        { "UT", "America/Denver" }, { "Utah", "America/Denver" },
        { "WY", "America/Denver" }, { "Wyoming", "America/Denver" },
        { "MT", "America/Denver" }, { "Montana", "America/Denver" },
        { "ID", "America/Denver" }, { "Idaho", "America/Denver" },

        // Arizona - No DST (UTC-7 year-round) - "America/Phoenix"
        { "AZ", "America/Phoenix" }, { "Arizona", "America/Phoenix" },

        // Pacific Time (UTC-8 / UTC-7 DST) - "America/Los_Angeles"
        { "CA", "America/Los_Angeles" }, { "California", "America/Los_Angeles" },
        { "WA", "America/Los_Angeles" }, { "Washington", "America/Los_Angeles" },
        { "OR", "America/Los_Angeles" }, { "Oregon", "America/Los_Angeles" },
        { "NV", "America/Los_Angeles" }, { "Nevada", "America/Los_Angeles" },

        // Alaska (UTC-9 / UTC-8 DST) - "America/Anchorage"
        { "AK", "America/Anchorage" }, { "Alaska", "America/Anchorage" },

        // Hawaii - No DST (UTC-10 year-round) - "Pacific/Honolulu"
        { "HI", "Pacific/Honolulu" }, { "Hawaii", "Pacific/Honolulu" },
    };

    /// <summary>
    /// Standard timezone abbreviations mapping.
    /// Key format: "TimeZoneId|IsDST" (e.g., "America/New_York|False" = "EST")
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

    public TimeZoneLookupService()
    {
        _logger = Log.ForContext<TimeZoneLookupService>();
    }

    public TimeZoneLookupService(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string GetTimeZoneFromState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            _logger.Debug("State is null or empty, returning default timezone {DefaultTimeZone}", DefaultTimeZoneId);
            return DefaultTimeZoneId;
        }

        var trimmedState = state.Trim();

        if (StateToTimezone.TryGetValue(trimmedState, out var timeZone))
        {
            _logger.Debug("Mapped state {State} to timezone {TimeZone}", trimmedState, timeZone);
            return timeZone;
        }

        _logger.Warning("Unknown state {State}, returning default timezone {DefaultTimeZone}", trimmedState, DefaultTimeZoneId);
        return DefaultTimeZoneId;
    }

    /// <inheritdoc />
    public string GetTimezoneAbbreviation(string timeZoneId, DateTime dateTime)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                _logger.Debug("TimeZoneId is null or empty, using default");
                timeZoneId = DefaultTimeZoneId;
            }

            // Get the timezone info
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            // Ensure datetime is UTC for DST check
            var utcDateTime = dateTime.Kind == DateTimeKind.Utc
                ? dateTime
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            // Convert to local time to check DST
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
            var isDst = timeZone.IsDaylightSavingTime(localTime);

            // Look up abbreviation
            if (TimezoneAbbreviations.TryGetValue(timeZoneId, out var abbreviations))
            {
                var abbreviation = isDst ? abbreviations.Daylight : abbreviations.Standard;
                _logger.Debug("Timezone {TimeZoneId} at {DateTime} is {IsDST} DST, abbreviation: {Abbreviation}",
                    timeZoneId, dateTime, isDst ? "in" : "not in", abbreviation);
                return abbreviation;
            }

            // Fallback: extract from DisplayName or use StandardName
            var fallbackAbbr = isDst
                ? ExtractAbbreviation(timeZone.DaylightName)
                : ExtractAbbreviation(timeZone.StandardName);

            _logger.Debug("Using fallback abbreviation {Abbreviation} for timezone {TimeZoneId}", fallbackAbbr, timeZoneId);
            return fallbackAbbr;
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.Error(ex, "Invalid timezone ID {TimeZoneId}, returning EST as fallback", timeZoneId);
            return "EST";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting timezone abbreviation for {TimeZoneId}", timeZoneId);
            return "EST";
        }
    }

    /// <inheritdoc />
    public bool IsValidTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.Debug("Timezone {TimeZoneId} is not valid", timeZoneId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error validating timezone {TimeZoneId}", timeZoneId);
            return false;
        }
    }

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
        var abbreviation = string.Concat(words.Select(w => char.ToUpper(w[0])));

        return abbreviation;
    }
}
