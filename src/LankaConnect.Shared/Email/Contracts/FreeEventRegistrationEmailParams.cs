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
    /// Phase 6A.113: Using contract constant instead of hardcoded string.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.FreeEventRegistration;

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
    /// Phase 6A.97: IANA timezone identifier for consistent date/time display.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Event start time formatted (e.g., "10:00 AM").
    /// </summary>
    public string EventStartTime { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2 (legacy): Single-line venue address. Preserved for backward compatibility
    /// with un-migrated templates that still reference <c>{{EventLocation}}</c>. New
    /// templates MUST use the Phase 7C.2 decomposed fields below.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2: Decomposed primary + secondary location projection.
    /// When set, <see cref="ToDictionary"/> emits the 8 decomposed keys; when null,
    /// a neutral Online-event projection is written.
    /// Populated via <see cref="WithLocationDetails"/>.
    /// </summary>
    public LocationEmailProjection? LocationDetails { get; set; }

    /// <summary>
    /// URL to view event details.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether the event has signup lists (controls {{#HasSignUpLists}} conditional).
    /// </summary>
    public bool HasSignUpLists { get; set; } = false;

    /// <summary>
    /// URL to signup lists section of event (if event has signup lists).
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

    /// <summary>
    /// Phase 6A.133 Email: Pre-formatted HTML for all organizer contacts.
    /// </summary>
    public string OrganizerContactsHtml { get; set; } = string.Empty;

    /// <summary>
    /// Phase 6A.133 Email: Dynamic header text ("EVENT ORGANIZER" or "EVENT ORGANIZERS").
    /// </summary>
    public string OrganizerContactHeader { get; set; } = "EVENT ORGANIZER";

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
    /// Phase 6A.87 Fix: Added EventDateTime combined field for Phase 6A.96 standardized templates.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        // Format date and time separately for backward compatibility
        var formattedDate = EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId);
        var formattedTime = !string.IsNullOrEmpty(EventStartTime)
            ? EventStartTime
            : EmailDateTimeHelper.FormatEventTime(EventStartDate, TimeZoneId);

        var dict = new Dictionary<string, object>
        {
            // Core event parameters
            { "UserName", UserName },
            { "EventTitle", EventTitle },
            { "EventStartDate", formattedDate },  // Phase 6A.97: Uses event's timezone
            { "EventStartTime", formattedTime },
            { "EventDateTime", $"{formattedDate} at {formattedTime}" },  // Phase 6A.87 Fix: Combined for standardized templates
            // Phase 7C.2: LocationDetails (when set) overwrites EventLocation with its
            // legacy flat string AND adds the 8 decomposed keys. EventLocation below
            // is the fallback used when no projection has been supplied.
            { "EventLocation", EventLocation },
            { "EventDetailsUrl", EventDetailsUrl },
            { "HasSignUpLists", HasSignUpLists },
            { "SignUpListsUrl", SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl },  // Alias: template uses {{SignupListUrl}} singular
            { "HasSignupForms", HasSignupForms },  // Phase 6A.112
            { "SignupFormsUrl", SignupFormsUrl },  // Phase 6A.112

            // Registration parameters
            { "RegistrationDate", EmailDateTimeHelper.FormatDateTimeWithTz(RegistrationDate, TimeZoneId) },  // Phase 6A.97: Uses event's timezone

            // Attendee parameters
            { "HasAttendeeDetails", HasAttendeeDetails },
            { "Attendees", AttendeesHtml },

            // Organizer contact parameters
            { "HasOrganizerContact", HasOrganizerContact },
            { "OrganizerContactName", OrganizerContactName },
            { "OrganizerContactEmail", OrganizerContactEmail },
            { "OrganizerContactPhone", OrganizerContactPhone },
            { "OrganizerContactsHtml", OrganizerContactsHtml },
            { "OrganizerContactHeader", OrganizerContactHeader },

            // Registration contact parameters
            { "HasContactInfo", HasContactInfo },
            { "ContactEmail", ContactEmail },
            { "ContactPhone", ContactPhone },

            // Event image parameters
            { "HasEventImage", HasEventImage },
            { "EventImageUrl", EventImageUrl }
        };

        // Phase 7C.2: Emit 8 decomposed location keys (+ legacy EventLocation override).
        // When no projection was supplied we still emit the keys as empty/false so
        // templates containing {{#if HasSecondaryLocation}} don't see an "undefined".
        LocationEmailDictionaryWriter.WriteTo(
            dict,
            LocationDetails ?? LocationEmailProjection.Online with { LegacyFlatString = EventLocation });

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
    /// Phase 6A.133 Email: Sets all organizer contacts with pre-formatted HTML.
    /// </summary>
    public FreeEventRegistrationEmailParams WithOrganizerContacts(IReadOnlyList<OrganizerContactInfo> contacts)
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
    /// Sets signup lists URL and HasSignUpLists flag together.
    /// Phase 6A.128: Removed WithSignUpListsUrl() which was a trap — it set URL without the flag.
    /// </summary>
    public FreeEventRegistrationEmailParams WithSignUpLists(string url)
    {
        HasSignUpLists = !string.IsNullOrWhiteSpace(url);
        SignUpListsUrl = url ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets signup forms URL and HasSignupForms flag together.
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public FreeEventRegistrationEmailParams WithSignupForms(string url)
    {
        HasSignupForms = !string.IsNullOrWhiteSpace(url);
        SignupFormsUrl = url ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Phase 7C.2: Sets the decomposed primary + optional secondary location fields
    /// and syncs the legacy <see cref="EventLocation"/> with the projection's flat
    /// fallback so every template — new or un-migrated — renders consistent data.
    /// </summary>
    public FreeEventRegistrationEmailParams WithLocationDetails(LocationEmailProjection projection)
    {
        if (projection == null)
            throw new ArgumentNullException(nameof(projection));

        LocationDetails = projection;
        EventLocation = projection.LegacyFlatString;
        return this;
    }

    #endregion
}
