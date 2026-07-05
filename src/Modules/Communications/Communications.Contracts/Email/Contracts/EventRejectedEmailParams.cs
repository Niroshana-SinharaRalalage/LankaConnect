using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for event rejection emails.
/// Template: template-event-rejected
///
/// This replaces inline HTML generation in EventRejectedEventHandler with
/// compile-time type-safe parameters and database templates.
///
/// Sent to event organizer when their event submission is rejected by admin.
/// </summary>
public class EventRejectedEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for event rejected email.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.EventRejected;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail => OrganizerEmail;

    /// <summary>
    /// Recipient name.
    /// </summary>
    public string RecipientName => OrganizerName;

    #region Core Properties

    /// <summary>
    /// Organizer user identifier.
    /// </summary>
    public Guid OrganizerId { get; set; }

    /// <summary>
    /// Organizer's display name.
    /// </summary>
    public string OrganizerName { get; set; } = string.Empty;

    /// <summary>
    /// Organizer's email address.
    /// </summary>
    public string OrganizerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Event identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Event title.
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Event start date/time.
    /// </summary>
    public DateTime EventStartDate { get; set; }

    /// <summary>
    /// Event timezone ID for proper date/time formatting.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Reason for rejection (feedback from admin).
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Date/time when event was rejected.
    /// </summary>
    public DateTime RejectedAt { get; set; }

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Uses EmailTemplateContract constants for all parameter names.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        // Format date and time using event's timezone
        var formattedDate = EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId);
        var formattedTime = EmailDateTimeHelper.FormatEventTime(EventStartDate, TimeZoneId);

        return new Dictionary<string, object>
        {
            { "OrganizerName", OrganizerName },
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { EmailTemplateContract.Event.EventStartDate, formattedDate },
            { EmailTemplateContract.Event.EventStartTime, formattedTime },
            { "Reason", Reason },
            { "RejectedAt", RejectedAt.ToString("MMMM dd, yyyy h:mm tt") },
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year }
        };
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (OrganizerId == Guid.Empty)
            errors.Add("OrganizerId is required");

        if (string.IsNullOrWhiteSpace(OrganizerName))
            errors.Add("OrganizerName is required");

        if (string.IsNullOrWhiteSpace(OrganizerEmail))
            errors.Add("OrganizerEmail is required");

        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (EventStartDate == default)
            errors.Add("EventStartDate is required");

        if (string.IsNullOrWhiteSpace(Reason))
            errors.Add("Reason is required");

        if (RejectedAt == default)
            errors.Add("RejectedAt is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new EventRejectedEmailParams with required fields.
    /// </summary>
    public static EventRejectedEmailParams Create(
        Guid organizerId,
        string organizerName,
        string organizerEmail,
        Guid eventId,
        string eventTitle,
        DateTime eventStartDate,
        string? timeZoneId,
        string reason,
        DateTime rejectedAt,
        string? supportEmail = null)
    {
        return new EventRejectedEmailParams
        {
            OrganizerId = organizerId,
            OrganizerName = organizerName,
            OrganizerEmail = organizerEmail,
            EventId = eventId,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            Reason = reason,
            RejectedAt = rejectedAt,
            SupportEmail = supportEmail ?? "lankaconnect.app@gmail.com"
        };
    }

    #endregion
}
