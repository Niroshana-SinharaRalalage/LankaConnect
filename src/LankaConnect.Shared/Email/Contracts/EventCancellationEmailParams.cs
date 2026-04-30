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
    /// Phase 6A.113: Using contract constant instead of hardcoded string.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.EventCancellation;

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
    /// Event location — legacy flat-string fallback. Phase 7C.2b: prefer
    /// <see cref="WithLocationDetails"/>.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2b: Decomposed primary + secondary location projection.
    /// </summary>
    public LocationEmailProjection? LocationDetails { get; set; }

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
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    /// <summary>
    /// URL to browse other events.
    /// </summary>
    public string BrowseEventsUrl { get; set; } = "https://lankaconnect.com/events";

    /// <summary>
    /// URL to view event details.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

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

    /// <summary>
    /// Phase 6A.133 Email: Pre-formatted HTML for all organizer contacts.
    /// </summary>
    public string OrganizerContactsHtml { get; set; } = string.Empty;

    /// <summary>
    /// Phase 6A.133 Email: Dynamic header text ("EVENT ORGANIZER" or "EVENT ORGANIZERS").
    /// </summary>
    public string OrganizerContactHeader { get; set; } = "EVENT ORGANIZER";

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

    #region Signup Lists Properties

    /// <summary>
    /// Whether signup lists are available for this event (controls {{#HasSignUpLists}} conditional).
    /// </summary>
    public bool HasSignUpLists { get; set; } = false;

    /// <summary>
    /// URL to the signup lists page.
    /// </summary>
    public string SignUpListsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether the event has signup forms (controls {{#HasSignupForms}} conditional).
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public bool HasSignupForms { get; set; } = false;

    /// <summary>
    /// URL to signup forms section of event (if event has signup forms).
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public string SignupFormsUrl { get; set; } = string.Empty;

    #endregion

    #region Phase 7F-A — Flexible Registration Mode Properties (Mode-B head-count rendering)

    /// <summary>Phase 7F-A: True for Mode A (DetailedAttendees). Toggles the per-attendee block.</summary>
    public bool HasDetailedAttendees { get; set; } = false;

    /// <summary>Phase 7F-A: True for any Mode B variant (B1-B4).</summary>
    public bool HasHeadCount { get; set; } = false;

    /// <summary>Phase 7F-A: True for B2/B3/B4 (demographic axis present); false for B1.</summary>
    public bool HasHeadCountBreakdown { get; set; } = false;

    /// <summary>Phase 7F-A: True when the registration carries TierCounts.</summary>
    public bool HasTierBreakdown { get; set; } = false;

    /// <summary>Phase 7F-A: Pre-rendered total head count (string).</summary>
    public string HeadCountTotal { get; set; } = string.Empty;

    /// <summary>Phase 7F-A: Pre-rendered demographic line, e.g. "2 adults · 1 child".</summary>
    public string HeadCountBreakdownLine { get; set; } = string.Empty;

    /// <summary>Phase 7F-A: Pre-rendered tier line, e.g. "VIP × 2, General × 3".</summary>
    public string TierBreakdownLine { get; set; } = string.Empty;

    /// <summary>Phase 7F-A: Lead attendee name for Mode B registrations.</summary>
    public string LeadAttendeeName { get; set; } = string.Empty;

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

        var dict = new Dictionary<string, object>
        {
            { "UserName", UserName },
            { "EventTitle", EventTitle },
            { "EventStartDate", formattedDate },
            { "EventStartTime", formattedTime },
            { "EventDateTime", $"{formattedDate} at {formattedTime}" },  // Phase 6A.87+ Fix: Combined for standardized templates
            { "EventDate", formattedDate },  // Template alias for EventStartDate
            { "CancellationReason", CancellationReason },
            { "CancelledAt", CancelledAt.ToString("MMMM dd, yyyy h:mm tt") },
            { "OrganizerName", OrganizerName },
            { "RefundsWillBeProcessed", RefundsWillBeProcessed },
            { "RefundMessage", RefundMessage },
            { "SupportEmail", SupportEmail },
            { "BrowseEventsUrl", BrowseEventsUrl },
            { "DashboardUrl", BrowseEventsUrl },  // Template alias for BrowseEventsUrl
            { "EventDetailsUrl", EventDetailsUrl },

            // Organizer contact params (for {{#HasOrganizerContact}} conditional)
            { "HasOrganizerContact", HasOrganizerContact },
            { "OrganizerContactName", OrganizerContactName },
            { "OrganizerContactEmail", OrganizerContactEmail },
            { "OrganizerContactPhone", OrganizerContactPhone },
            { "OrganizerContactsHtml", OrganizerContactsHtml },
            { "OrganizerContactHeader", OrganizerContactHeader },

            // Signup lists params (for {{#HasSignUpLists}} conditional)
            { "HasSignUpLists", HasSignUpLists },
            { "SignUpListsUrl", SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl },  // Template alias (singular form)
            { "HasSignupForms", HasSignupForms },  // Phase 6A.112
            { "SignupFormsUrl", SignupFormsUrl },  // Phase 6A.112

            // Event image params (for {{#HasEventImage}} conditional)
            { EmailTemplateContract.EventImage.HasEventImage, HasEventImage },
            { EmailTemplateContract.EventImage.EventImageUrl, EventImageUrl },

            // Phase 7F-A: Flexible registration mode params (always emit booleans true AND false).
            { EmailTemplateContract.FlexibleRegistration.HasDetailedAttendees, HasDetailedAttendees },
            { EmailTemplateContract.FlexibleRegistration.HasHeadCount, HasHeadCount },
            { EmailTemplateContract.FlexibleRegistration.HasHeadCountBreakdown, HasHeadCountBreakdown },
            { EmailTemplateContract.FlexibleRegistration.HasTierBreakdown, HasTierBreakdown },
            { EmailTemplateContract.FlexibleRegistration.HeadCountTotal, HeadCountTotal },
            { EmailTemplateContract.FlexibleRegistration.HeadCountBreakdownLine, HeadCountBreakdownLine },
            { EmailTemplateContract.FlexibleRegistration.TierBreakdownLine, TierBreakdownLine },
            { "LeadAttendeeName", LeadAttendeeName },

            { "Year", DateTime.UtcNow.Year }
        };

        // Phase 7C.2b: emit decomposed location keys + legacy EventLocation fallback.
        LocationEmailDictionaryWriter.WriteTo(
            dict,
            LocationDetails ?? LocationEmailProjection.FromLegacyScalar(EventLocation));

        return dict;
    }

    /// <summary>
    /// Phase 7C.2b: fluent setter for the decomposed location projection.
    /// </summary>
    public EventCancellationEmailParams WithLocationDetails(LocationEmailProjection projection)
    {
        if (projection == null)
            throw new ArgumentNullException(nameof(projection));

        LocationDetails = projection;
        EventLocation = projection.LegacyFlatString;
        return this;
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

    #region Fluent Setters

    /// <summary>
    /// Sets organizer contact information.
    /// </summary>
    public EventCancellationEmailParams WithOrganizerContact(
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
    /// Phase 6A.133 Email: Sets all organizer contacts with pre-formatted HTML.
    /// </summary>
    public EventCancellationEmailParams WithOrganizerContacts(IReadOnlyList<OrganizerContactInfo> contacts)
    {
        if (contacts.Count > 0)
        {
            HasOrganizerContact = true;
            var primary = contacts.FirstOrDefault(c => c.IsPrimary) ?? contacts[0];
            OrganizerContactName = primary.Name;
            OrganizerContactEmail = primary.Email ?? string.Empty;
            OrganizerContactPhone = primary.Phone ?? string.Empty;
        }
        OrganizerContactsHtml = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);
        OrganizerContactHeader = OrganizerContactHtmlBuilder.BuildHeaderText(contacts.Count);
        return this;
    }

    /// <summary>
    /// Sets the event image URL. If a non-empty URL is provided, HasEventImage is set to true.
    /// </summary>
    public EventCancellationEmailParams WithEventImage(string imageUrl)
    {
        HasEventImage = !string.IsNullOrEmpty(imageUrl);
        EventImageUrl = imageUrl ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets signup lists URL.
    /// </summary>
    public EventCancellationEmailParams WithSignUpLists(string url)
    {
        HasSignUpLists = !string.IsNullOrWhiteSpace(url);
        SignUpListsUrl = url ?? string.Empty;
        return this;
    }

    
    /// <summary>
    /// Sets signup forms URL and HasSignupForms flag together.
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public EventCancellationEmailParams WithSignupForms(string url)
    {
        HasSignupForms = !string.IsNullOrWhiteSpace(url);
        SignupFormsUrl = url ?? string.Empty;
        return this;
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
