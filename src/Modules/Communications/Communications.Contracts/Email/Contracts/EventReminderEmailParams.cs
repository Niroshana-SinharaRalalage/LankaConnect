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
    /// Phase 6A.113: Using contract constant instead of hardcoded string.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.EventReminder;

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
    /// Event location — legacy flat-string fallback. Phase 7C.2b renamed from
    /// <c>Location</c> → <c>EventLocation</c> to align with the rest of the
    /// event-email params family on the canonical <c>EmailTemplateContract.Event.EventLocation</c>
    /// contract. Prefer <see cref="WithLocationDetails"/> to populate the 8
    /// decomposed keys via <see cref="LocationEmailDictionaryWriter"/>.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2b: Decomposed primary + secondary location projection.
    /// </summary>
    public LocationEmailProjection? LocationDetails { get; set; }

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

    /// <summary>
    /// Phase 6A.133 Email: Pre-formatted HTML for all organizer contacts.
    /// </summary>
    public string OrganizerContactsHtml { get; set; } = string.Empty;

    /// <summary>
    /// Phase 6A.133 Email: Dynamic header text ("EVENT ORGANIZER" or "EVENT ORGANIZERS").
    /// </summary>
    public string OrganizerContactHeader { get; set; } = "EVENT ORGANIZER";

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

    /// <summary>Phase 7F-E.3: pre-rendered per-tier HTML fragment with N/A placeholders.</summary>
    public string RegistrationBreakdownHtml { get; set; } = string.Empty;

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
            { "OrganizerContactsHtml", OrganizerContactsHtml },
            { "OrganizerContactHeader", OrganizerContactHeader },

            // Signup lists params (for {{#HasSignUpLists}} conditional)
            { "HasSignUpLists", HasSignUpLists },
            { "SignUpListsUrl", SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl },  // Template alias (singular form)
            { "HasSignupForms", HasSignupForms },  // Phase 6A.112
            { "SignupFormsUrl", SignupFormsUrl },  // Phase 6A.112

            // Ticket parameters (always include, even if empty)
            // HasTicket controls {{#HasTicket}} conditional in Handlebars template
            { "HasTicket", HasTicket },
            { "TicketCode", TicketCode },
            { "TicketExpiryDate", TicketExpiryDate },

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
            { EmailTemplateContract.FlexibleRegistration.RegistrationBreakdownHtml, RegistrationBreakdownHtml },
            { "LeadAttendeeName", LeadAttendeeName },

            // Footer
            { "Year", DateTime.UtcNow.Year }
        };

        // Phase 7C.2b: emit decomposed location keys + legacy EventLocation fallback.
        // Replaces the old { "Location", Location } line — templates now bind to the
        // decomposed block (which includes {{EventLocation}} as a legacy fallback).
        LocationEmailDictionaryWriter.WriteTo(
            dict,
            LocationDetails ?? LocationEmailProjection.FromLegacyScalar(EventLocation));

        return dict;
    }

    /// <summary>
    /// Phase 7C.2b: fluent setter for the decomposed location projection.
    /// </summary>
    public EventReminderEmailParams WithLocationDetails(LocationEmailProjection projection)
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
        string eventLocation,
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
            EventLocation = eventLocation,
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
    /// Phase 6A.133 Email: Sets all organizer contacts with pre-formatted HTML.
    /// </summary>
    public EventReminderEmailParams WithOrganizerContacts(IReadOnlyList<OrganizerContactInfo> contacts)
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

    
    /// <summary>
    /// Sets signup forms URL and HasSignupForms flag together.
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public EventReminderEmailParams WithSignupForms(string url)
    {
        HasSignupForms = !string.IsNullOrWhiteSpace(url);
        SignupFormsUrl = url ?? string.Empty;
        return this;
    }

    #endregion
}
