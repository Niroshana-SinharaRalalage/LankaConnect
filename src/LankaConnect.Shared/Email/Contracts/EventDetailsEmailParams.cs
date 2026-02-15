namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for event details publication emails.
/// Template: template-event-details-publication
///
/// This replaces Dictionary&lt;string, object&gt; in EventNotificationEmailJob with
/// compile-time type-safe parameters.
///
/// Sent to email groups and newsletter subscribers when an organizer manually
/// sends event details/notifications.
/// </summary>
public class EventDetailsEmailParams : IEmailParameters
{
    #region Template Selection

    /// <summary>
    /// The template name for event details publication.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.EventDetailsPublication;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Recipient name.
    /// </summary>
    public string RecipientName => UserName;

    #endregion

    #region User Properties

    /// <summary>
    /// User's display name (or "Valued Guest" for anonymous).
    /// </summary>
    public string UserName { get; set; } = "Valued Guest";

    #endregion

    #region Event Core Properties

    /// <summary>
    /// Event title.
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Event date (full format).
    /// </summary>
    public string EventDate { get; set; } = string.Empty;

    /// <summary>
    /// Event start date (formatted).
    /// </summary>
    public string EventStartDate { get; set; } = string.Empty;

    /// <summary>
    /// Event start time (formatted).
    /// </summary>
    public string EventStartTime { get; set; } = string.Empty;

    /// <summary>
    /// Combined date and time.
    /// </summary>
    public string EventDateTime { get; set; } = string.Empty;

    /// <summary>
    /// Event location.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Event city.
    /// </summary>
    public string EventCity { get; set; } = "TBA";

    /// <summary>
    /// Event state.
    /// </summary>
    public string EventState { get; set; } = "TBA";

    /// <summary>
    /// Event description.
    /// </summary>
    public string EventDescription { get; set; } = string.Empty;

    /// <summary>
    /// URL to event details page.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Alias for EventDetailsUrl.
    /// </summary>
    public string EventUrl { get; set; } = string.Empty;

    #endregion

    #region Pricing Properties

    /// <summary>
    /// Whether the event is free.
    /// </summary>
    public bool IsFreeEvent { get; set; }

    /// <summary>
    /// Alternative name for IsFreeEvent.
    /// </summary>
    public bool IsFree { get; set; }

    /// <summary>
    /// Whether the event is paid.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// Pricing details text.
    /// </summary>
    public string PricingDetails { get; set; } = "Free";

    /// <summary>
    /// Ticket price (formatted).
    /// </summary>
    public string TicketPrice { get; set; } = "Free";

    #endregion

    #region Sign-up List Properties

    /// <summary>
    /// Whether the event has sign-up lists.
    /// </summary>
    public bool HasSignUpLists { get; set; }

    /// <summary>
    /// URL to sign-up lists section.
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

    #region Organizer Contact Properties

    /// <summary>
    /// Whether organizer contact info is available.
    /// </summary>
    public bool HasOrganizerContact { get; set; }

    /// <summary>
    /// Organizer contact name.
    /// </summary>
    public string OrganizerContactName { get; set; } = string.Empty;

    /// <summary>
    /// Organizer contact email.
    /// </summary>
    public string OrganizerContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Organizer contact phone.
    /// </summary>
    public string OrganizerContactPhone { get; set; } = string.Empty;

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

    #region Common Properties

    /// <summary>
    /// Subject line prefix ("New Event:" or "Upcoming Event:").
    /// </summary>
    public string SubjectPrefix { get; set; } = "New Event:";

    /// <summary>
    /// Current year for footer.
    /// </summary>
    public int Year { get; set; } = DateTime.UtcNow.Year;

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Uses EmailTemplateContract constants for all parameter names.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName, UserName },
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { "EventDate", EventDate },
            { EmailTemplateContract.Event.EventStartDate, EventStartDate },
            { EmailTemplateContract.Event.EventStartTime, EventStartTime },
            { EmailTemplateContract.Event.EventDateTime, EventDateTime },
            { EmailTemplateContract.Event.EventLocation, EventLocation },
            { "EventCity", EventCity },
            { "EventState", EventState },
            { EmailTemplateContract.Event.EventDescription, EventDescription },
            { EmailTemplateContract.Event.EventDetailsUrl, EventDetailsUrl },
            { EmailTemplateContract.Event.EventUrl, EventUrl },
            { "IsFreeEvent", IsFreeEvent },
            { "IsFree", IsFree },
            { "IsPaid", IsPaid },
            { "PricingDetails", PricingDetails },
            { EmailTemplateContract.Registration.TicketPrice, TicketPrice },
            { "HasSignUpLists", HasSignUpLists },
            { EmailTemplateContract.Event.SignUpListsUrl, SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl },  // Alias: template uses {{SignupListUrl}} singular
            { "HasSignupForms", HasSignupForms },  // Phase 6A.112
            { "SignupFormsUrl", SignupFormsUrl },  // Phase 6A.112
            { EmailTemplateContract.OrganizerContact.HasOrganizerContact, HasOrganizerContact },
            { EmailTemplateContract.OrganizerContact.OrganizerContactName, OrganizerContactName },
            { EmailTemplateContract.OrganizerContact.OrganizerContactEmail, OrganizerContactEmail },
            { EmailTemplateContract.OrganizerContact.OrganizerContactPhone, OrganizerContactPhone },
            { "SubjectPrefix", SubjectPrefix },
            { EmailTemplateContract.Common.Year, Year },

            // Event image params (for {{#HasEventImage}} conditional)
            { EmailTemplateContract.EventImage.HasEventImage, HasEventImage },
            { EmailTemplateContract.EventImage.EventImageUrl, EventImageUrl }
        };

        return dict;
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(RecipientEmail))
            errors.Add("RecipientEmail is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (string.IsNullOrWhiteSpace(EventDetailsUrl))
            errors.Add("EventDetailsUrl is required");

        return errors.Count == 0;
    }

    #endregion

    #region Fluent Methods

    /// <summary>
    /// Sets the event image URL. If a non-empty URL is provided, HasEventImage is set to true.
    /// </summary>
    public EventDetailsEmailParams WithEventImage(string imageUrl)
    {
        HasEventImage = !string.IsNullOrEmpty(imageUrl);
        EventImageUrl = imageUrl ?? string.Empty;
        return this;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new EventDetailsEmailParams with required fields.
    /// </summary>
    public static EventDetailsEmailParams Create(
        string recipientEmail,
        string userName,
        string eventTitle,
        string eventDate,
        string eventStartDate,
        string eventStartTime,
        string eventDateTime,
        string eventLocation,
        string eventCity,
        string eventState,
        string eventDescription,
        string eventDetailsUrl,
        bool isFree,
        string pricingDetails,
        string ticketPrice,
        bool hasSignUpLists,
        string signUpListsUrl,
        bool hasOrganizerContact,
        string organizerContactName,
        string? organizerContactEmail,
        string? organizerContactPhone,
        string subjectPrefix)
    {
        return new EventDetailsEmailParams
        {
            RecipientEmail = recipientEmail,
            UserName = userName,
            EventTitle = eventTitle,
            EventDate = eventDate,
            EventStartDate = eventStartDate,
            EventStartTime = eventStartTime,
            EventDateTime = eventDateTime,
            EventLocation = eventLocation,
            EventCity = eventCity,
            EventState = eventState,
            EventDescription = eventDescription,
            EventDetailsUrl = eventDetailsUrl,
            EventUrl = eventDetailsUrl,
            IsFreeEvent = isFree,
            IsFree = isFree,
            IsPaid = !isFree,
            PricingDetails = pricingDetails,
            TicketPrice = ticketPrice,
            HasSignUpLists = hasSignUpLists,
            SignUpListsUrl = signUpListsUrl,
            HasOrganizerContact = hasOrganizerContact,
            OrganizerContactName = organizerContactName,
            OrganizerContactEmail = organizerContactEmail ?? string.Empty,
            OrganizerContactPhone = organizerContactPhone ?? string.Empty,
            SubjectPrefix = subjectPrefix,
            Year = DateTime.UtcNow.Year
        };
    }

    #endregion
}
