using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for event approval emails.
/// Template: template-event-approval
///
/// This replaces Dictionary&lt;string, object&gt; in EventApprovedEventHandler with
/// compile-time type-safe parameters.
///
/// Sent to event organizer when their event is approved by admin.
/// </summary>
public class EventApprovalEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for event approval email.
    /// Phase 6A.113: Updated to use corrected constant name.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.EventApproval;

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
    /// Event location — legacy flat-string fallback. Phase 7C.2b: prefer
    /// <see cref="WithLocationDetails"/>.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2b: Decomposed primary + secondary location projection.
    /// </summary>
    public LocationEmailProjection? LocationDetails { get; set; }

    /// <summary>
    /// Date/time when event was approved.
    /// </summary>
    public DateTime ApprovedAt { get; set; }

    /// <summary>
    /// URL to view event details.
    /// </summary>
    public string EventUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL to manage the event.
    /// </summary>
    public string EventManageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

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

        var dict = new Dictionary<string, object>
        {
            { "OrganizerName", OrganizerName },  // Template uses OrganizerName, not UserName
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { EmailTemplateContract.Event.EventStartDate, formattedDate },
            { EmailTemplateContract.Event.EventStartTime, formattedTime },
            { "ApprovedAt", ApprovedAt.ToString("MMMM dd, yyyy h:mm tt") },  // Template-specific param
            { EmailTemplateContract.Event.EventUrl, EventUrl },
            { "EventManageUrl", EventManageUrl },  // Template-specific param for manage link
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            // Template alias: template uses EventDateTime (combined date+time)
            { "EventDateTime", $"{formattedDate} at {formattedTime}" },
            // Template alias: template uses EventDetailsUrl instead of EventUrl
            { "EventDetailsUrl", EventUrl },

            // Event image params (for {{#HasEventImage}} conditional)
            { EmailTemplateContract.EventImage.HasEventImage, HasEventImage },
            { EmailTemplateContract.EventImage.EventImageUrl, EventImageUrl },

            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year }
        };

        // Phase 7C.2b: emit decomposed location keys + legacy EventLocation fallback.
        LocationEmailDictionaryWriter.WriteTo(
            dict,
            LocationDetails ?? LocationEmailProjection.Online with { LegacyFlatString = EventLocation });

        return dict;
    }

    /// <summary>
    /// Phase 7C.2b: fluent setter for the decomposed location projection.
    /// </summary>
    public EventApprovalEmailParams WithLocationDetails(LocationEmailProjection projection)
    {
        if (projection == null)
            throw new ArgumentNullException(nameof(projection));

        LocationDetails = projection;
        EventLocation = projection.LegacyFlatString;
        return this;
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

        if (ApprovedAt == default)
            errors.Add("ApprovedAt is required");

        return errors.Count == 0;
    }

    #endregion

    #region Fluent Methods

    /// <summary>
    /// Sets the event image URL. If a non-empty URL is provided, HasEventImage is set to true.
    /// </summary>
    public EventApprovalEmailParams WithEventImage(string imageUrl)
    {
        HasEventImage = !string.IsNullOrEmpty(imageUrl);
        EventImageUrl = imageUrl ?? string.Empty;
        return this;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new EventApprovalEmailParams with required fields.
    /// </summary>
    public static EventApprovalEmailParams Create(
        Guid organizerId,
        string organizerName,
        string organizerEmail,
        Guid eventId,
        string eventTitle,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        DateTime approvedAt,
        string eventUrl,
        string eventManageUrl)
    {
        return new EventApprovalEmailParams
        {
            OrganizerId = organizerId,
            OrganizerName = organizerName,
            OrganizerEmail = organizerEmail,
            EventId = eventId,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            ApprovedAt = approvedAt,
            EventUrl = eventUrl,
            EventManageUrl = eventManageUrl
        };
    }

    #endregion
}
