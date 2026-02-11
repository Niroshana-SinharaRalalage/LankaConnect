using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for event published notification emails.
/// Template: template-new-event-publication
///
/// This replaces Dictionary&lt;string, object&gt; in EventPublishedEventHandler with
/// compile-time type-safe parameters.
///
/// Sent to email group subscribers and newsletter subscribers when event is published.
/// </summary>
public class EventPublishedEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for event published notification email.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.NewEventPublication;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Recipient name (if known).
    /// </summary>
    public string RecipientName { get; set; } = "Subscriber";

    #region Core Event Properties

    /// <summary>
    /// Event identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Event title.
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Event description.
    /// </summary>
    public string EventDescription { get; set; } = string.Empty;

    /// <summary>
    /// Event start date/time (UTC).
    /// </summary>
    public DateTime EventStartDate { get; set; }

    /// <summary>
    /// Event timezone ID for proper date/time formatting.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Event location string.
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

    #endregion

    #region Pricing Properties

    /// <summary>
    /// Whether the event is free.
    /// </summary>
    public bool IsFree { get; set; }

    /// <summary>
    /// Ticket price text (e.g., "Free" or "$25.00").
    /// </summary>
    public string TicketPrice { get; set; } = "TBA";

    #endregion

    #region URL Properties

    /// <summary>
    /// URL to view event details.
    /// </summary>
    public string EventUrl { get; set; } = string.Empty;

    #endregion

    #region Organizer Contact Properties

    /// <summary>
    /// Whether organizer contact information is available.
    /// </summary>
    public bool HasOrganizerContact { get; set; }

    /// <summary>
    /// Organizer contact name.
    /// </summary>
    public string? OrganizerContactName { get; set; }

    /// <summary>
    /// Organizer contact email.
    /// </summary>
    public string? OrganizerContactEmail { get; set; }

    /// <summary>
    /// Organizer contact phone.
    /// </summary>
    public string? OrganizerContactPhone { get; set; }

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
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { EmailTemplateContract.Event.EventDescription, EventDescription },
            { EmailTemplateContract.Event.EventStartDate, formattedDate },
            { EmailTemplateContract.Event.EventStartTime, formattedTime },
            { EmailTemplateContract.Event.EventDateTime, $"{formattedDate} at {formattedTime}" },
            { EmailTemplateContract.Event.EventLocation, EventLocation },
            { "EventCity", EventCity },
            { "EventState", EventState },
            { "IsFree", IsFree },
            { "IsPaid", !IsFree },
            { "TicketPrice", TicketPrice },
            { EmailTemplateContract.Event.EventUrl, EventUrl },
            { EmailTemplateContract.Event.EventDetailsUrl, EventUrl },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year },
            { EmailTemplateContract.OrganizerContact.HasOrganizerContact, HasOrganizerContact },

            // Organizer contact params (always include, even if empty)
            { EmailTemplateContract.OrganizerContact.OrganizerContactName, OrganizerContactName ?? string.Empty },
            { EmailTemplateContract.OrganizerContact.OrganizerContactEmail, OrganizerContactEmail ?? string.Empty },
            { EmailTemplateContract.OrganizerContact.OrganizerContactPhone, OrganizerContactPhone ?? string.Empty },

            // Event image params (for {{#HasEventImage}} conditional)
            { EmailTemplateContract.EventImage.HasEventImage, HasEventImage },
            { EmailTemplateContract.EventImage.EventImageUrl, EventImageUrl },

            // Signup lists params (for {{#HasSignUpLists}} conditional)
            { "HasSignUpLists", HasSignUpLists },
            { "SignUpListsUrl", SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl }  // Template alias (singular form)
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

        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (EventStartDate == default)
            errors.Add("EventStartDate is required");

        if (string.IsNullOrWhiteSpace(EventUrl))
            errors.Add("EventUrl is required");

        return errors.Count == 0;
    }

    #endregion

    #region Fluent Methods

    /// <summary>
    /// Sets the event image URL. If a non-empty URL is provided, HasEventImage is set to true.
    /// </summary>
    public EventPublishedEmailParams WithEventImage(string imageUrl)
    {
        HasEventImage = !string.IsNullOrEmpty(imageUrl);
        EventImageUrl = imageUrl ?? string.Empty;
        return this;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new EventPublishedEmailParams with required fields.
    /// </summary>
    public static EventPublishedEmailParams Create(
        string recipientEmail,
        Guid eventId,
        string eventTitle,
        string eventDescription,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        string eventCity,
        string eventState,
        bool isFree,
        string ticketPrice,
        string eventUrl,
        bool hasOrganizerContact = false,
        string? organizerContactName = null,
        string? organizerContactEmail = null,
        string? organizerContactPhone = null)
    {
        return new EventPublishedEmailParams
        {
            RecipientEmail = recipientEmail,
            EventId = eventId,
            EventTitle = eventTitle,
            EventDescription = eventDescription,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            EventCity = eventCity,
            EventState = eventState,
            IsFree = isFree,
            TicketPrice = ticketPrice,
            EventUrl = eventUrl,
            HasOrganizerContact = hasOrganizerContact,
            OrganizerContactName = organizerContactName,
            OrganizerContactEmail = organizerContactEmail,
            OrganizerContactPhone = organizerContactPhone
        };
    }

    #endregion
}
