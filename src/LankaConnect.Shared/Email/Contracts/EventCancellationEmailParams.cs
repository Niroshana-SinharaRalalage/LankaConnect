using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 5: Template-specific typed parameters for event cancellation emails.
/// Template: template-event-cancellation-notifications
///
/// This replaces Dictionary&lt;string, object&gt; in EventCancellationEmailJob with
/// compile-time type-safe parameters.
///
/// Sent to attendees when an event is cancelled by the organizer.
///
/// Phase 6A.87 Fix: Corrected template name to include "-notifications" suffix.
/// </summary>
public class EventCancellationEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for event cancellation.
    /// Phase 6A.87 Fix: Corrected from "template-event-cancellation" to include "-notifications" suffix.
    /// </summary>
    public string TemplateName => "template-event-cancellation-notifications";

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail => UserEmail;

    /// <summary>
    /// Recipient name.
    /// </summary>
    public string RecipientName => UserName;

    #region Core Properties

    /// <summary>
    /// User identifier. Optional for anonymous recipients (email groups/newsletter).
    /// Phase 6A.100: Made optional to support bulk notification emails.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// User's display name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Event identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Event title.
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Event start date/time (original scheduled date).
    /// </summary>
    public DateTime EventStartDate { get; set; }

    /// <summary>
    /// Event timezone ID for proper date/time formatting.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Event location.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Reason for cancellation.
    /// </summary>
    public string CancellationReason { get; set; } = string.Empty;

    /// <summary>
    /// Date/time when event was cancelled.
    /// </summary>
    public DateTime CancelledAt { get; set; }

    /// <summary>
    /// Organizer name.
    /// </summary>
    public string OrganizerName { get; set; } = string.Empty;

    /// <summary>
    /// Whether refunds will be processed.
    /// </summary>
    public bool RefundsWillBeProcessed { get; set; }

    /// <summary>
    /// Refund status message.
    /// </summary>
    public string RefundMessage { get; set; } = string.Empty;

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "support@lankaconnect.com";

    /// <summary>
    /// URL to browse other events.
    /// </summary>
    public string BrowseEventsUrl { get; set; } = "https://lankaconnect.com/events";

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Phase 6A.87+ Fix: Added EventDateTime combined field for standardized templates.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        // Format date and time separately for backward compatibility
        var formattedDate = EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId);
        var formattedTime = EmailDateTimeHelper.FormatEventTime(EventStartDate, TimeZoneId);

        return new Dictionary<string, object>
        {
            { "UserName", UserName },
            { "EventTitle", EventTitle },
            { "EventStartDate", formattedDate },
            { "EventStartTime", formattedTime },
            { "EventDateTime", $"{formattedDate} at {formattedTime}" },  // Phase 6A.87+ Fix: Combined for standardized templates
            { "EventLocation", EventLocation },
            { "CancellationReason", CancellationReason },
            { "CancelledAt", CancelledAt.ToString("MMMM dd, yyyy h:mm tt") },
            { "OrganizerName", OrganizerName },
            { "RefundsWillBeProcessed", RefundsWillBeProcessed },
            { "RefundMessage", RefundMessage },
            { "SupportEmail", SupportEmail },
            { "BrowseEventsUrl", BrowseEventsUrl },
            { "Year", DateTime.UtcNow.Year }
        };
    }

    /// <summary>
    /// Validates the email parameters.
    /// Phase 6A.100: UserId is now optional for bulk notification emails.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        // UserId is optional for anonymous recipients (email groups/newsletter)

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(UserEmail))
            errors.Add("UserEmail is required");

        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (EventStartDate == default)
            errors.Add("EventStartDate is required");

        if (CancelledAt == default)
            errors.Add("CancelledAt is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new EventCancellationEmailParams with required fields.
    /// Phase 6A.100: UserId is now optional to support bulk notification emails.
    /// </summary>
    public static EventCancellationEmailParams Create(
        Guid? userId,
        string userName,
        string userEmail,
        Guid eventId,
        string eventTitle,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        string cancellationReason,
        DateTime cancelledAt,
        string organizerName,
        bool refundsWillBeProcessed,
        string? refundMessage = null)
    {
        return new EventCancellationEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            EventId = eventId,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            CancellationReason = cancellationReason,
            CancelledAt = cancelledAt,
            OrganizerName = organizerName,
            RefundsWillBeProcessed = refundsWillBeProcessed,
            RefundMessage = refundMessage ?? (refundsWillBeProcessed
                ? "Refunds will be processed automatically within 5-10 business days."
                : "No refunds are required for this event.")
        };
    }

    #endregion
}
