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
    /// Event venue address (e.g., "123 Main St, Boston, MA").
    /// May include virtual meeting links for online events.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

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
    /// Converts to dictionary for backward compatibility with existing email system.
    /// Includes both separate date/time AND combined EventDateTime for template compatibility.
    /// </summary>
    /// <returns>Dictionary with all event parameters</returns>
    public Dictionary<string, object> ToDictionary()
    {
        var formattedDate = EventStartDate.ToString("MMMM dd, yyyy");

        return new Dictionary<string, object>
        {
            { "EventId", EventId.ToString() },
            { "EventTitle", EventTitle },
            { "EventLocation", EventLocation },
            { "EventStartDate", formattedDate },
            { "EventStartTime", EventStartTime },
            { "EventDateTime", $"{formattedDate} at {EventStartTime}" }, // Combined for templates expecting single field
            { "EventDetailsUrl", EventDetailsUrl }
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
