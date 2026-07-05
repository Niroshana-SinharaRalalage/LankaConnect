namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for event postponement emails.
/// Template: template-event-postponed
///
/// This replaces inline HTML generation in EventPostponedEventHandler with
/// compile-time type-safe parameters and database templates.
///
/// Sent to registered attendees when an event is postponed.
/// </summary>
public class EventPostponedEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for event postponed email.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.EventPostponed;

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
    /// User identifier.
    /// </summary>
    public Guid UserId { get; set; }

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
    /// Original event start date (before postponement).
    /// </summary>
    public DateTime OriginalStartDate { get; set; }

    /// <summary>
    /// Event timezone ID for proper date/time formatting.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Reason for postponement.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Date/time when postponement was announced.
    /// </summary>
    public DateTime PostponedAt { get; set; }

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
        return new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName, UserName },
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { "OriginalStartDate", OriginalStartDate.ToString("MMMM dd, yyyy") },
            { "OriginalStartTime", OriginalStartDate.ToString("h:mm tt") },
            { "Reason", Reason },
            { "PostponedAt", PostponedAt.ToString("MMMM dd, yyyy h:mm tt") },
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

        if (UserId == Guid.Empty)
            errors.Add("UserId is required");

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(UserEmail))
            errors.Add("UserEmail is required");

        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (OriginalStartDate == default)
            errors.Add("OriginalStartDate is required");

        if (string.IsNullOrWhiteSpace(Reason))
            errors.Add("Reason is required");

        if (PostponedAt == default)
            errors.Add("PostponedAt is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new EventPostponedEmailParams with required fields.
    /// </summary>
    public static EventPostponedEmailParams Create(
        Guid userId,
        string userName,
        string userEmail,
        Guid eventId,
        string eventTitle,
        DateTime originalStartDate,
        string? timeZoneId,
        string reason,
        DateTime postponedAt,
        string? supportEmail = null)
    {
        return new EventPostponedEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            EventId = eventId,
            EventTitle = eventTitle,
            OriginalStartDate = originalStartDate,
            TimeZoneId = timeZoneId,
            Reason = reason,
            PostponedAt = postponedAt,
            SupportEmail = supportEmail ?? "lankaconnect.app@gmail.com"
        };
    }

    #endregion
}
