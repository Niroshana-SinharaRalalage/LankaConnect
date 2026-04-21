using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87: Base parameter contract for event-related email fields.
///
/// Used by all email templates that need event information.
/// Provides strongly-typed access to event data with validation.
///
/// Common parameters:
/// - EventId: Unique event identifier
/// - EventTitle: Event name/title
/// - EventLocation: Venue address
/// - EventStartDate: Date of the event
/// - EventStartTime: Time of the event
/// - EventDateTime: Combined date + time (for templates expecting single field)
/// - EventDetailsUrl: Link to event details page
///
/// Templates using these parameters:
/// - Event registration confirmation
/// - Event reminder
/// - Event cancellation
/// - Event published notification
/// - Payment completed confirmation
/// </summary>
public class EventEmailParams
{
    /// <summary>
    /// Unique identifier for the event.
    /// Used for tracking, logging, and database correlation.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Event title/name (e.g., "Community Meetup 2026").
    /// Used in subject lines and email body.
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2 (legacy): Single-line venue address (e.g., "123 Main St, Boston, MA").
    /// Preserved for backward compatibility with un-migrated templates that still reference
    /// <c>{{EventLocation}}</c>. New templates MUST use the decomposed fields below.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2: Bold first line of the primary location block — the venue name
    /// (e.g., "Aurora Clubhouse"). Empty string when the event has no venue name
    /// or no physical location.
    /// </summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2: Second line of the primary location block — the full comma-separated
    /// address "Street, City, State, ZipCode, Country". Empty string for online events.
    /// </summary>
    public string LocationAddress { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2: True when a venue name is present on the primary location.
    /// Drives {{#if HasLocationName}} in the template.
    /// </summary>
    public bool HasLocationName { get; set; }

    /// <summary>
    /// Phase 7C.2: True when the event has a secondary (parking lot / secondary venue)
    /// address configured. Drives {{#if HasSecondaryLocation}} in the template.
    /// </summary>
    public bool HasSecondaryLocation { get; set; }

    /// <summary>
    /// Phase 7C.2: Visible label for the secondary location block — "Parking Lot" or
    /// "Secondary Venue" based on the configured <c>SecondaryLocationType</c>. No
    /// trailing colon; the template itself supplies the colon.
    /// </summary>
    public string SecondaryLocationLabel { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2: Bold first line of the secondary location block — the venue name
    /// (e.g., "Geoga Lake Parking"). Empty string when the secondary location is unnamed.
    /// </summary>
    public string SecondaryLocationName { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2: True when the secondary location has a venue name. Drives
    /// {{#if HasSecondaryLocationName}} in the template.
    /// </summary>
    public bool HasSecondaryLocationName { get; set; }

    /// <summary>
    /// Phase 7C.2: Second line of the secondary location block — the full comma-separated
    /// address. Empty string when no secondary location.
    /// </summary>
    public string SecondaryLocationAddress { get; set; } = string.Empty;

    /// <summary>
    /// Date when the event starts.
    /// Formatted as "MMMM dd, yyyy" (e.g., "February 15, 2026") in ToDictionary().
    /// </summary>
    public DateTime EventStartDate { get; set; }

    /// <summary>
    /// Time when the event starts (e.g., "10:00 AM").
    /// Stored as string to preserve exact formatting.
    /// </summary>
    public string EventStartTime { get; set; } = string.Empty;

    /// <summary>
    /// URL to the event details page.
    /// Used for "View Event" links in emails.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Phase 6A.97: IANA timezone ID for the event (e.g., "America/New_York").
    /// Used for consistent date/time formatting in emails.
    /// If null, defaults to Eastern Time.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Converts to dictionary for backward compatibility with existing email system.
    /// Includes both separate date/time AND combined EventDateTime for template compatibility.
    /// </summary>
    /// <returns>Dictionary with all event parameters</returns>
    public Dictionary<string, object> ToDictionary()
    {
        // Phase 6A.97: Use timezone-aware helper for consistent formatting
        var formattedDate = EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId);
        var formattedTime = EmailDateTimeHelper.FormatEventTime(EventStartDate, TimeZoneId); // Includes timezone abbr (e.g., "6:00 PM EST")
        var tzAbbreviation = EmailDateTimeHelper.GetTimezoneAbbreviation(TimeZoneId, EventStartDate);

        return new Dictionary<string, object>
        {
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { "EventId", EventId.ToString() },
            // Phase 7C.2 (legacy): keep EventLocation for un-migrated templates.
            { EmailTemplateContract.Event.EventLocation, EventLocation },
            { EmailTemplateContract.Event.EventStartDate, formattedDate },
            { EmailTemplateContract.Event.EventStartTime, !string.IsNullOrEmpty(EventStartTime) ? EventStartTime : formattedTime },
            { EmailTemplateContract.Event.EventDateTime, $"{formattedDate} at {formattedTime}" },
            { EmailTemplateContract.Event.EventDetailsUrl, EventDetailsUrl },
            { "TimeZoneAbbreviation", tzAbbreviation },

            // Phase 7C.2: Decomposed primary + secondary location (rendered by new templates).
            { EmailTemplateContract.Event.LocationName, LocationName },
            { EmailTemplateContract.Event.LocationAddress, LocationAddress },
            { EmailTemplateContract.Event.HasLocationName, HasLocationName },
            { EmailTemplateContract.Event.HasSecondaryLocation, HasSecondaryLocation },
            { EmailTemplateContract.Event.SecondaryLocationLabel, SecondaryLocationLabel },
            { EmailTemplateContract.Event.SecondaryLocationName, SecondaryLocationName },
            { EmailTemplateContract.Event.HasSecondaryLocationName, HasSecondaryLocationName },
            { EmailTemplateContract.Event.SecondaryLocationAddress, SecondaryLocationAddress },
        };
    }

    /// <summary>
    /// Validates that all required event parameters are provided.
    /// </summary>
    /// <param name="errors">List of validation errors if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        // EventLocation can be empty for virtual events (optional)
        // EventStartDate defaults to DateTime.MinValue if not set - could add validation
        // EventStartTime could be empty for all-day events (optional)
        // EventDetailsUrl could be empty (optional)

        return errors.Count == 0;
    }
}
