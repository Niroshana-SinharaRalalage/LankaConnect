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

    #region Optional Properties - Ticket Info

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
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            // Required parameters
            { "AttendeeName", AttendeeName },
            { "EventTitle", EventTitle },
            { "EventStartDate", EventStartDate.ToString("MMMM dd, yyyy") },
            { "EventStartTime", EventStartTime },
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

            // Ticket parameters (always include, even if empty)
            { "TicketCode", TicketCode },
            { "TicketExpiryDate", TicketExpiryDate }
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
    /// </summary>
    public EventReminderEmailParams WithTicket(
        string? ticketCode,
        string? expiryDate)
    {
        TicketCode = ticketCode ?? string.Empty;
        TicketExpiryDate = expiryDate ?? string.Empty;
        return this;
    }

    #endregion
}
