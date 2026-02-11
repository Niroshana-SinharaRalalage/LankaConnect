using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 2: Template-specific typed parameters for template-event-reminder.
///
/// This is the first pilot implementation of strongly-typed email parameters,
/// replacing Dictionary&lt;string, object&gt; in EventReminderJob with compile-time
/// type-safe parameters.
///
/// Parameters match exactly what template-event-reminder expects:
/// - AttendeeName, EventTitle, EventStartDate, EventStartTime, Location
/// - Quantity, HoursUntilEvent, ReminderTimeframe, ReminderMessage
/// - EventDetailsUrl
/// - Organizer contact (conditional)
/// - Ticket info (conditional)
/// </summary>
public class EventReminderEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name used for event reminders.
    /// </summary>
    public string TemplateName => "template-event-reminder";

    /// <summary>
    /// Recipient email address (attendee's email).
    /// </summary>
    public string RecipientEmail => AttendeeEmail;

    /// <summary>
    /// Recipient name (attendee's name).
    /// </summary>
    public string RecipientName => AttendeeName;

    #region Required Properties

    /// <summary>
    /// Event identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Registration identifier.
    /// </summary>
    public Guid RegistrationId { get; set; }

    /// <summary>
    /// Attendee's name.
    /// </summary>
    public string AttendeeName { get; set; } = string.Empty;

    /// <summary>
    /// Attendee's email address.
    /// </summary>
    public string AttendeeEmail { get; set; } = string.Empty;

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
    /// Event start time (e.g., "10:00 AM").
    /// </summary>
    public string EventStartTime { get; set; } = string.Empty;

    /// <summary>
    /// Event location address.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Number of tickets/seats for this registration.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Hours until the event starts.
    /// </summary>
    public double HoursUntilEvent { get; set; }

    /// <summary>
    /// Human-readable reminder timeframe (e.g., "tomorrow", "in 2 days").
    /// </summary>
    public string ReminderTimeframe { get; set; } = string.Empty;

    /// <summary>
    /// Reminder message displayed in the email.
    /// </summary>
    public string ReminderMessage { get; set; } = string.Empty;

    /// <summary>
    /// URL to view event details.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

    #endregion

    #region Optional Properties - Organizer Contact

    /// <summary>
    /// Whether the event has organizer contact information.
    /// </summary>
    public bool HasOrganizerContact { get; set; } = false;

    /// <summary>
    /// Organizer's name.
    /// </summary>
    public string OrganizerContactName { get; set; } = string.Empty;

    /// <summary>
    /// Organizer's email address.
    /// </summary>
    public string OrganizerContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Organizer's phone number.
    /// </summary>
    public string OrganizerContactPhone { get; set; } = string.Empty;

    #endregion

    #region Optional Properties - Signup Lists

    /// <summary>
    /// Whether signup lists are available for this event (controls {{#HasSignUpLists}} conditional).
    /// </summary>
    public bool HasSignUpLists { get; set; } = false;

    /// <summary>
    /// URL to the signup lists page.
    /// </summary>
    public string SignUpListsUrl { get; set; } = string.Empty;

    #endregion

    #region Optional Properties - Event Image

    /// <summary>
    /// Whether the event has an image (controls {{#HasEventImage}} conditional).
    /// </summary>
    public bool HasEventImage { get; set; } = false;

    /// <summary>
    /// URL to the event's primary image.
    /// </summary>
    public string EventImageUrl { get; set; } = string.Empty;

    #endregion

    #region Optional Properties - Ticket Info

    /// <summary>
    /// Whether the registration has a ticket (for paid events).
    /// This controls the {{#HasTicket}} conditional in the email template.
    /// </summary>
    public bool HasTicket { get; set; } = false;

    /// <summary>
    /// Ticket code for paid events.
    /// </summary>
    public string TicketCode { get; set; } = string.Empty;

    /// <summary>
    /// Ticket expiry date formatted string.
    /// </summary>
    public string TicketExpiryDate { get; set; } = string.Empty;

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
        var formattedTime = !string.IsNullOrEmpty(EventStartTime)
            ? EventStartTime
            : EmailDateTimeHelper.FormatEventTime(EventStartDate, TimeZoneId);

        var dict = new Dictionary<string, object>
        {
            // Required parameters
            { "AttendeeName", AttendeeName },
            { "EventTitle", EventTitle },
            { "EventStartDate", formattedDate },  // Phase 6A.97: Uses event's timezone
            { "EventStartTime", formattedTime },
            { "EventDateTime", $"{formattedDate} at {formattedTime}" },  // Phase 6A.87+ Fix: Combined for standardized templates
            { "Location", Location },
            { "Quantity", Quantity },
            { "HoursUntilEvent", HoursUntilEvent },
            { "ReminderTimeframe", ReminderTimeframe },
            { "ReminderMessage", ReminderMessage },
            { "EventDetailsUrl", EventDetailsUrl },

            // Organizer contact parameters (always include, even if empty)
            { "HasOrganizerContact", HasOrganizerContact },
            { "OrganizerContactName", OrganizerContactName },
            { "OrganizerContactEmail", OrganizerContactEmail },
            { "OrganizerContactPhone", OrganizerContactPhone },

            // Signup lists params (for {{#HasSignUpLists}} conditional)
            { "HasSignUpLists", HasSignUpLists },
            { "SignUpListsUrl", SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl },  // Template alias (singular form)

            // Ticket parameters (always include, even if empty)
            // HasTicket controls {{#HasTicket}} conditional in Handlebars template
            { "HasTicket", HasTicket },
            { "TicketCode", TicketCode },
            { "TicketExpiryDate", TicketExpiryDate },

            // Event image params (for {{#HasEventImage}} conditional)
            { EmailTemplateContract.EventImage.HasEventImage, HasEventImage },
            { EmailTemplateContract.EventImage.EventImageUrl, EventImageUrl },

            // Footer
            { "Year", DateTime.UtcNow.Year }
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

        if (string.IsNullOrWhiteSpace(AttendeeName))
            errors.Add("AttendeeName is required");

        if (string.IsNullOrWhiteSpace(AttendeeEmail))
            errors.Add("AttendeeEmail is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (string.IsNullOrWhiteSpace(EventDetailsUrl))
            errors.Add("EventDetailsUrl is required");

        if (string.IsNullOrWhiteSpace(ReminderTimeframe))
            errors.Add("ReminderTimeframe is required");

        if (string.IsNullOrWhiteSpace(ReminderMessage))
            errors.Add("ReminderMessage is required");

        // Conditional validation: If HasOrganizerContact, name is required
        if (HasOrganizerContact && string.IsNullOrWhiteSpace(OrganizerContactName))
            errors.Add("OrganizerContactName is required when HasOrganizerContact is true");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new EventReminderEmailParams with required fields.
    /// </summary>
    public static EventReminderEmailParams Create(
        Guid eventId,
        Guid registrationId,
        string attendeeName,
        string attendeeEmail,
        string eventTitle,
        DateTime eventStartDate,
        string eventStartTime,
        string location,
        int quantity,
        double hoursUntilEvent,
        string reminderTimeframe,
        string reminderMessage,
        string eventDetailsUrl)
    {
        return new EventReminderEmailParams
        {
            EventId = eventId,
            RegistrationId = registrationId,
            AttendeeName = attendeeName,
            AttendeeEmail = attendeeEmail,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            EventStartTime = eventStartTime,
            Location = location,
            Quantity = quantity,
            HoursUntilEvent = hoursUntilEvent,
            ReminderTimeframe = reminderTimeframe,
            ReminderMessage = reminderMessage,
            EventDetailsUrl = eventDetailsUrl
        };
    }

    /// <summary>
    /// Returns a new instance with organizer contact information set.
    /// </summary>
    public EventReminderEmailParams WithOrganizerContact(
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
    /// Returns a new instance with ticket information set.
    /// Sets HasTicket = true to enable {{#HasTicket}} conditional in email template.
    /// </summary>
    public EventReminderEmailParams WithTicket(
        string? ticketCode,
        string? expiryDate)
    {
        HasTicket = true;
        TicketCode = ticketCode ?? string.Empty;
        TicketExpiryDate = expiryDate ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets the event image URL. If a non-empty URL is provided, HasEventImage is set to true.
    /// </summary>
    public EventReminderEmailParams WithEventImage(string imageUrl)
    {
        HasEventImage = !string.IsNullOrEmpty(imageUrl);
        EventImageUrl = imageUrl ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets signup lists URL.
    /// </summary>
    public EventReminderEmailParams WithSignUpLists(string url)
    {
        HasSignUpLists = !string.IsNullOrWhiteSpace(url);
        SignUpListsUrl = url ?? string.Empty;
        return this;
    }

    #endregion
}
