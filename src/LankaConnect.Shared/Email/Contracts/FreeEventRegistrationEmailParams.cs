using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 4: Template-specific typed parameters for free event registration confirmation.
/// Template: template-free-event-registration-confirmation
///
/// This replaces Dictionary&lt;string, object&gt; in RegistrationConfirmedEventHandler with
/// compile-time type-safe parameters.
///
/// Parameters match exactly what the template expects:
/// - Core: UserName, EventTitle, EventStartDate, EventStartTime, EventLocation, EventDetailsUrl
/// - Registration: RegistrationDate
/// - Attendees: Attendees (HTML), HasAttendeeDetails
/// - Organizer: HasOrganizerContact, OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone
/// - Contact: HasContactInfo, ContactEmail, ContactPhone
/// - Image: HasEventImage, EventImageUrl
/// - SignUp: SignUpListsUrl
/// </summary>
public class FreeEventRegistrationEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for free event registration confirmation.
    /// </summary>
    public string TemplateName => "template-free-event-registration-confirmation";

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail => UserEmail;

    /// <summary>
    /// Recipient name.
    /// </summary>
    public string RecipientName => UserName;

    #region Core Event Properties

    /// <summary>
    /// Event identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Registration identifier.
    /// </summary>
    public Guid RegistrationId { get; set; }

    /// <summary>
    /// User's display name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Event title.
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Event start date.
    /// </summary>
    public DateTime EventStartDate { get; set; }

    /// <summary>
    /// Event start time formatted (e.g., "10:00 AM").
    /// </summary>
    public string EventStartTime { get; set; } = string.Empty;

    /// <summary>
    /// Event location address.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// URL to view event details.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL to signup lists section of event (if event has signup lists).
    /// </summary>
    public string SignUpListsUrl { get; set; } = string.Empty;

    #endregion

    #region Registration Properties

    /// <summary>
    /// Date the registration was created.
    /// </summary>
    public DateTime RegistrationDate { get; set; }

    #endregion

    #region Attendee Properties

    /// <summary>
    /// Whether registration has detailed attendee information.
    /// </summary>
    public bool HasAttendeeDetails { get; set; } = false;

    /// <summary>
    /// HTML-formatted attendee list.
    /// </summary>
    public string AttendeesHtml { get; set; } = string.Empty;

    #endregion

    #region Organizer Contact Properties

    /// <summary>
    /// Whether event has organizer contact information.
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

    #region Registration Contact Properties

    /// <summary>
    /// Whether registration has contact info.
    /// </summary>
    public bool HasContactInfo { get; set; } = false;

    /// <summary>
    /// Registrant's contact email.
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Registrant's contact phone.
    /// </summary>
    public string ContactPhone { get; set; } = string.Empty;

    #endregion

    #region Event Image Properties

    /// <summary>
    /// Whether event has a primary image.
    /// </summary>
    public bool HasEventImage { get; set; } = false;

    /// <summary>
    /// URL to event's primary image.
    /// </summary>
    public string EventImageUrl { get; set; } = string.Empty;

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            // Core event parameters
            { "UserName", UserName },
            { "EventTitle", EventTitle },
            { "EventStartDate", EmailDateTimeHelper.FormatEventDate(EventStartDate) },  // Phase 6A.X Issue #40: Uses timezone helper
            { "EventStartTime", EventStartTime },
            { "EventLocation", EventLocation },
            { "EventDetailsUrl", EventDetailsUrl },
            { "SignUpListsUrl", SignUpListsUrl },

            // Registration parameters
            { "RegistrationDate", RegistrationDate.ToString("MMMM dd, yyyy h:mm tt") },

            // Attendee parameters
            { "HasAttendeeDetails", HasAttendeeDetails },
            { "Attendees", AttendeesHtml },

            // Organizer contact parameters
            { "HasOrganizerContact", HasOrganizerContact },
            { "OrganizerContactName", OrganizerContactName },
            { "OrganizerContactEmail", OrganizerContactEmail },
            { "OrganizerContactPhone", OrganizerContactPhone },

            // Registration contact parameters
            { "HasContactInfo", HasContactInfo },
            { "ContactEmail", ContactEmail },
            { "ContactPhone", ContactPhone },

            // Event image parameters
            { "HasEventImage", HasEventImage },
            { "EventImageUrl", EventImageUrl }
        };

        return dict;
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        // Required field validations
        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (RegistrationId == Guid.Empty)
            errors.Add("RegistrationId is required");

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(UserEmail))
            errors.Add("UserEmail is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (string.IsNullOrWhiteSpace(EventDetailsUrl))
            errors.Add("EventDetailsUrl is required");

        // Conditional validations
        if (HasOrganizerContact && string.IsNullOrWhiteSpace(OrganizerContactName))
            errors.Add("OrganizerContactName is required when HasOrganizerContact is true");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new FreeEventRegistrationEmailParams with required fields.
    /// </summary>
    public static FreeEventRegistrationEmailParams Create(
        Guid eventId,
        Guid registrationId,
        string userName,
        string userEmail,
        string eventTitle,
        DateTime eventStartDate,
        string eventStartTime,
        string eventLocation,
        string eventDetailsUrl,
        DateTime registrationDate)
    {
        return new FreeEventRegistrationEmailParams
        {
            EventId = eventId,
            RegistrationId = registrationId,
            UserName = userName,
            UserEmail = userEmail,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            EventStartTime = eventStartTime,
            EventLocation = eventLocation,
            EventDetailsUrl = eventDetailsUrl,
            RegistrationDate = registrationDate
        };
    }

    /// <summary>
    /// Sets organizer contact information.
    /// </summary>
    public FreeEventRegistrationEmailParams WithOrganizerContact(
        string? name,
        string? email = null,
        string? phone = null)
    {
        HasOrganizerContact = true;
        OrganizerContactName = name ?? "Event Organizer";
        OrganizerContactEmail = email ?? string.Empty;
        OrganizerContactPhone = phone ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets attendee details.
    /// </summary>
    public FreeEventRegistrationEmailParams WithAttendees(string attendeesHtml)
    {
        HasAttendeeDetails = true;
        AttendeesHtml = attendeesHtml ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets event image.
    /// </summary>
    public FreeEventRegistrationEmailParams WithEventImage(string imageUrl)
    {
        HasEventImage = !string.IsNullOrEmpty(imageUrl);
        EventImageUrl = imageUrl ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets registration contact info.
    /// </summary>
    public FreeEventRegistrationEmailParams WithContactInfo(string? email, string? phone)
    {
        HasContactInfo = true;
        ContactEmail = email ?? string.Empty;
        ContactPhone = phone ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets signup lists URL (if event has signup lists).
    /// </summary>
    public FreeEventRegistrationEmailParams WithSignUpListsUrl(string signUpListsUrl)
    {
        SignUpListsUrl = signUpListsUrl ?? string.Empty;
        return this;
    }

    #endregion
}
