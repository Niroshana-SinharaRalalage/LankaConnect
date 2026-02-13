using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.107: Template-specific typed parameters for form response emails.
/// Templates: template-form-response-confirmation, template-form-response-update, template-form-response-cancellation
///
/// This provides compile-time type-safe parameters for form response notification emails,
/// following the pattern established by SignupCommitmentEmailParams.
///
/// Parameters match what the templates expect:
/// - UserName, EventTitle, FormTitle, ResponseSummary, EditFormUrl, EventDateTime, EventLocation, etc.
///
/// Architect Review: Approved - mirrors signup commitment pattern with response summary length limits.
/// </summary>
public class FormResponseEmailParams : IEmailParameters
{
    private string _templateName = EmailTemplateContract.TemplateNames.FormResponseConfirmation;

    /// <summary>
    /// The template name for form response emails.
    /// </summary>
    public string TemplateName => _templateName;

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
    /// Form title/name.
    /// </summary>
    public string FormTitle { get; set; } = string.Empty;

    /// <summary>
    /// Form description (optional).
    /// </summary>
    public string FormDescription { get; set; } = string.Empty;

    /// <summary>
    /// Summary of user's responses (e.g., "Vegetarian preference, Arriving Friday").
    /// Architect fix: Limited to 5 questions, 100 chars per answer to prevent email bloat.
    /// </summary>
    public string ResponseSummary { get; set; } = string.Empty;

    /// <summary>
    /// URL to edit the form response (includes access token for anonymous users).
    /// </summary>
    public string EditFormUrl { get; set; } = string.Empty;

    /// <summary>
    /// Event start date/time.
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
    /// URL to view event details.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// When the response was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// When the response was last updated (for update emails).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the response was cancelled (for cancellation emails).
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Response deadline (if set by organizer).
    /// </summary>
    public DateTime? ResponseDeadline { get; set; }

    #endregion

    #region Event Image Properties

    /// <summary>
    /// Whether the event has an image (controls {{#HasEventImage}} conditional).
    /// </summary>
    public bool HasEventImage { get; set; } = false;

    /// <summary>
    /// URL to the event's primary image.
    /// </summary>
    public string EventImageUrl { get; set; } = string.Empty;

    #endregion

    #region Organizer Contact Properties

    /// <summary>
    /// Whether organizer contact info is available (controls {{#HasOrganizerContact}} conditional).
    /// </summary>
    public bool HasOrganizerContact { get; set; } = false;

    /// <summary>
    /// Organizer's name.
    /// </summary>
    public string OrganizerContactName { get; set; } = string.Empty;

    /// <summary>
    /// Organizer's email.
    /// </summary>
    public string OrganizerContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Organizer's phone.
    /// </summary>
    public string OrganizerContactPhone { get; set; } = string.Empty;

    #endregion

    #region Form Description Properties

    /// <summary>
    /// Whether the form has a description (controls {{#HasFormDescription}} conditional).
    /// </summary>
    public bool HasFormDescription { get; set; } = false;

    #endregion

    #region Response Deadline Properties

    /// <summary>
    /// Whether the form has a response deadline (controls {{#HasResponseDeadline}} conditional).
    /// </summary>
    public bool HasResponseDeadline { get; set; } = false;

    #endregion

    #region Template Type

    /// <summary>
    /// Sets the template to use for confirmation emails.
    /// </summary>
    public FormResponseEmailParams AsConfirmation()
    {
        _templateName = EmailTemplateContract.TemplateNames.FormResponseConfirmation;
        return this;
    }

    /// <summary>
    /// Sets the template to use for update emails.
    /// </summary>
    public FormResponseEmailParams AsUpdate()
    {
        _templateName = EmailTemplateContract.TemplateNames.FormResponseUpdate;
        return this;
    }

    /// <summary>
    /// Sets the template to use for cancellation emails.
    /// </summary>
    public FormResponseEmailParams AsCancellation()
    {
        _templateName = EmailTemplateContract.TemplateNames.FormResponseCancellation;
        return this;
    }

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Phase 6A.107: Uses EmailTemplateContract constants for parameter names.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            // Core params
            { EmailTemplateContract.Common.UserName, UserName },
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { EmailTemplateContract.FormResponse.FormTitle, FormTitle },
            { EmailTemplateContract.FormResponse.ResponseSummary, ResponseSummary },
            { EmailTemplateContract.FormResponse.EditFormUrl, EditFormUrl },
            { EmailTemplateContract.Event.EventDateTime, EmailDateTimeHelper.FormatDateTimeWithTz(EventStartDate, TimeZoneId) },
            { EmailTemplateContract.Event.EventLocation, EventLocation },
            { EmailTemplateContract.Event.EventDetailsUrl, EventDetailsUrl },

            // Timestamps
            { EmailTemplateContract.FormResponse.SubmittedAt, EmailDateTimeHelper.FormatDateTimeWithTz(SubmittedAt, TimeZoneId) },

            // Event image params
            { EmailTemplateContract.EventImage.HasEventImage, HasEventImage },
            { EmailTemplateContract.EventImage.EventImageUrl, EventImageUrl },

            // Organizer contact params
            { EmailTemplateContract.OrganizerContact.HasOrganizerContact, HasOrganizerContact },
            { EmailTemplateContract.OrganizerContact.OrganizerContactName, OrganizerContactName },
            { EmailTemplateContract.OrganizerContact.OrganizerContactEmail, OrganizerContactEmail },
            { EmailTemplateContract.OrganizerContact.OrganizerContactPhone, OrganizerContactPhone },

            // Form description params
            { EmailTemplateContract.FormResponse.HasFormDescription, HasFormDescription },
            { EmailTemplateContract.FormResponse.FormDescription, FormDescription },

            // Footer params
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year }
        };

        // Add optional update timestamp
        if (UpdatedAt.HasValue)
        {
            dict[EmailTemplateContract.FormResponse.UpdatedAt] = EmailDateTimeHelper.FormatDateTimeWithTz(UpdatedAt.Value, TimeZoneId);
        }

        // Add optional cancellation timestamp
        if (CancelledAt.HasValue)
        {
            dict[EmailTemplateContract.FormResponse.CancelledAt] = EmailDateTimeHelper.FormatDateTimeWithTz(CancelledAt.Value, TimeZoneId);
        }

        // Add optional response deadline
        if (ResponseDeadline.HasValue)
        {
            dict[EmailTemplateContract.FormResponse.HasResponseDeadline] = true;
            dict[EmailTemplateContract.FormResponse.ResponseDeadline] = EmailDateTimeHelper.FormatDateTimeWithTz(ResponseDeadline.Value, TimeZoneId);
        }
        else
        {
            dict[EmailTemplateContract.FormResponse.HasResponseDeadline] = false;
            dict[EmailTemplateContract.FormResponse.ResponseDeadline] = string.Empty;
        }

        return dict;
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(UserEmail))
            errors.Add("UserEmail is required");

        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (string.IsNullOrWhiteSpace(FormTitle))
            errors.Add("FormTitle is required");

        if (EventStartDate == default)
            errors.Add("EventStartDate is required");

        return errors.Count == 0;
    }

    #endregion

    #region Fluent Setters

    /// <summary>
    /// Sets the event image URL. If a non-empty URL is provided, HasEventImage is set to true.
    /// </summary>
    public FormResponseEmailParams WithEventImage(string imageUrl)
    {
        HasEventImage = !string.IsNullOrEmpty(imageUrl);
        EventImageUrl = imageUrl ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets organizer contact information.
    /// </summary>
    public FormResponseEmailParams WithOrganizerContact(
        string? name,
        string? email = null,
        string? phone = null)
    {
        HasOrganizerContact = !string.IsNullOrWhiteSpace(name);
        OrganizerContactName = name ?? string.Empty;
        OrganizerContactEmail = email ?? string.Empty;
        OrganizerContactPhone = phone ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets form description (if provided by organizer).
    /// </summary>
    public FormResponseEmailParams WithFormDescription(string description)
    {
        HasFormDescription = !string.IsNullOrWhiteSpace(description);
        FormDescription = description ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets response deadline (if set by organizer).
    /// </summary>
    public FormResponseEmailParams WithResponseDeadline(DateTime? deadline)
    {
        HasResponseDeadline = deadline.HasValue;
        ResponseDeadline = deadline;
        return this;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new FormResponseEmailParams for a confirmation email.
    /// </summary>
    public static FormResponseEmailParams CreateConfirmation(
        string userName,
        string userEmail,
        Guid eventId,
        string eventTitle,
        string formTitle,
        string responseSummary,
        string editFormUrl,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        string eventDetailsUrl,
        DateTime submittedAt)
    {
        return new FormResponseEmailParams
        {
            UserName = userName,
            UserEmail = userEmail,
            EventId = eventId,
            EventTitle = eventTitle,
            FormTitle = formTitle,
            ResponseSummary = responseSummary,
            EditFormUrl = editFormUrl,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            EventDetailsUrl = eventDetailsUrl,
            SubmittedAt = submittedAt
        }.AsConfirmation();
    }

    /// <summary>
    /// Creates a new FormResponseEmailParams for an update email.
    /// </summary>
    public static FormResponseEmailParams CreateUpdate(
        string userName,
        string userEmail,
        Guid eventId,
        string eventTitle,
        string formTitle,
        string responseSummary,
        string editFormUrl,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        string eventDetailsUrl,
        DateTime updatedAt)
    {
        return new FormResponseEmailParams
        {
            UserName = userName,
            UserEmail = userEmail,
            EventId = eventId,
            EventTitle = eventTitle,
            FormTitle = formTitle,
            ResponseSummary = responseSummary,
            EditFormUrl = editFormUrl,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            EventDetailsUrl = eventDetailsUrl,
            UpdatedAt = updatedAt
        }.AsUpdate();
    }

    /// <summary>
    /// Creates a new FormResponseEmailParams for a cancellation email.
    /// </summary>
    public static FormResponseEmailParams CreateCancellation(
        string userName,
        string userEmail,
        Guid eventId,
        string eventTitle,
        string formTitle,
        string responseSummary,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        string eventDetailsUrl,
        DateTime cancelledAt)
    {
        return new FormResponseEmailParams
        {
            UserName = userName,
            UserEmail = userEmail,
            EventId = eventId,
            EventTitle = eventTitle,
            FormTitle = formTitle,
            ResponseSummary = responseSummary,
            EditFormUrl = string.Empty,  // No edit link for cancellation
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            EventDetailsUrl = eventDetailsUrl,
            CancelledAt = cancelledAt
        }.AsCancellation();
    }

    #endregion
}
