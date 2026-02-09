using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 5: Template-specific typed parameters for attendees added confirmation email.
/// Template: template-attendees-added-confirmation
///
/// This replaces Dictionary&lt;string, object&gt; in AttendeesAddedEventHandler with
/// compile-time type-safe parameters.
///
/// Used when additional attendees are added to an existing paid registration (Add-Only Attendees feature).
/// </summary>
public class AttendeesAddedEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for attendees added confirmation.
    /// </summary>
    public string TemplateName => "template-attendees-added-confirmation";

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
    /// User identifier (may be null for anonymous registrations).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Registration identifier.
    /// </summary>
    public Guid RegistrationId { get; set; }

    /// <summary>
    /// Event identifier.
    /// </summary>
    public Guid EventId { get; set; }

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
    /// Event start date/time (UTC).
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
    /// Previous attendee count before adding.
    /// </summary>
    public int PreviousCount { get; set; }

    /// <summary>
    /// Number of attendees added.
    /// </summary>
    public int AddedCount { get; set; }

    /// <summary>
    /// New total attendee count.
    /// </summary>
    public int NewTotalCount { get; set; }

    /// <summary>
    /// Additional amount paid for new attendees.
    /// </summary>
    public decimal AdditionalAmount { get; set; }

    /// <summary>
    /// Total amount paid including this addition.
    /// </summary>
    public decimal TotalPaid { get; set; }

    /// <summary>
    /// Text list of newly added attendees.
    /// </summary>
    public string NewAttendees { get; set; } = string.Empty;

    /// <summary>
    /// HTML formatted list of newly added attendees.
    /// </summary>
    public string NewAttendeesHtml { get; set; } = string.Empty;

    /// <summary>
    /// Text list of all attendees.
    /// </summary>
    public string AllAttendees { get; set; } = string.Empty;

    /// <summary>
    /// HTML formatted list of all attendees.
    /// </summary>
    public string AllAttendeesHtml { get; set; } = string.Empty;

    /// <summary>
    /// URL to view event details.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL to view/download ticket.
    /// </summary>
    public string? TicketUrl { get; set; }

    /// <summary>
    /// Ticket code for QR/reference.
    /// </summary>
    public string? TicketCode { get; set; }

    /// <summary>
    /// Whether a ticket was generated.
    /// </summary>
    public bool HasTicket { get; set; }

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
    /// Phase 6A.87+ Fix: Added template aliases (NewAttendeesCount, TotalAttendeesCount, AmountCharged, EventDateTime).
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var formattedDate = EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId);
        var formattedTime = EmailDateTimeHelper.FormatEventTime(EventStartDate, TimeZoneId);

        var dict = new Dictionary<string, object>
        {
            { "UserName", UserName },
            { "EventTitle", EventTitle },
            { "EventStartDate", formattedDate },
            { "EventStartTime", formattedTime },
            { "EventDateTime", $"{formattedDate} at {formattedTime}" },  // Combined for template
            { "EventLocation", EventLocation },

            // Original params (keep for compatibility)
            { "PreviousCount", PreviousCount },
            { "AddedCount", AddedCount },
            { "NewTotalCount", NewTotalCount },
            { "AdditionalAmount", AdditionalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
            { "TotalPaid", TotalPaid.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },

            // Template aliases (Phase 6A.96 templates expect these names)
            { "NewAttendeesCount", AddedCount },
            { "TotalAttendeesCount", NewTotalCount },
            { "AmountCharged", AdditionalAmount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")) },

            // Attendee lists
            { "NewAttendees", NewAttendees },
            { "NewAttendeesHtml", NewAttendeesHtml },
            { "AllAttendees", AllAttendees },
            { "AllAttendeesHtml", AllAttendeesHtml },

            { "EventDetailsUrl", EventDetailsUrl },
            { "HasTicket", HasTicket },

            // Organizer contact params (for {{#HasOrganizerContact}} conditional)
            { "HasOrganizerContact", HasOrganizerContact },
            { "OrganizerContactName", OrganizerContactName },
            { "OrganizerContactEmail", OrganizerContactEmail },
            { "OrganizerContactPhone", OrganizerContactPhone },

            // Signup lists params (for {{#HasSignUpLists}} conditional)
            { "HasSignUpLists", HasSignUpLists },
            { "SignUpListsUrl", SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl },  // Template alias (singular form)

            { "Year", DateTime.UtcNow.Year }
        };

        if (HasTicket && !string.IsNullOrWhiteSpace(TicketUrl))
        {
            dict["TicketUrl"] = TicketUrl;
            dict["TicketCode"] = TicketCode ?? string.Empty;
        }

        return dict;
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (RegistrationId == Guid.Empty)
            errors.Add("RegistrationId is required");

        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(UserEmail))
            errors.Add("UserEmail is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (EventStartDate == default)
            errors.Add("EventStartDate is required");

        if (AddedCount <= 0)
            errors.Add("AddedCount must be greater than 0");

        if (NewTotalCount <= 0)
            errors.Add("NewTotalCount must be greater than 0");

        return errors.Count == 0;
    }

    #endregion

    #region Fluent Setters

    /// <summary>
    /// Sets organizer contact information.
    /// </summary>
    public AttendeesAddedEmailParams WithOrganizerContact(
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
    /// Sets signup lists URL.
    /// </summary>
    public AttendeesAddedEmailParams WithSignUpLists(string url)
    {
        HasSignUpLists = !string.IsNullOrWhiteSpace(url);
        SignUpListsUrl = url ?? string.Empty;
        return this;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new AttendeesAddedEmailParams with required fields.
    /// </summary>
    public static AttendeesAddedEmailParams Create(
        Guid? userId,
        Guid registrationId,
        Guid eventId,
        string userName,
        string userEmail,
        string eventTitle,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        int previousCount,
        int addedCount,
        int newTotalCount,
        decimal additionalAmount,
        decimal totalPaid,
        string newAttendees,
        string newAttendeesHtml,
        string allAttendees,
        string allAttendeesHtml,
        string eventDetailsUrl,
        string? ticketUrl = null,
        string? ticketCode = null)
    {
        return new AttendeesAddedEmailParams
        {
            UserId = userId,
            RegistrationId = registrationId,
            EventId = eventId,
            UserName = userName,
            UserEmail = userEmail,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            PreviousCount = previousCount,
            AddedCount = addedCount,
            NewTotalCount = newTotalCount,
            AdditionalAmount = additionalAmount,
            TotalPaid = totalPaid,
            NewAttendees = newAttendees,
            NewAttendeesHtml = newAttendeesHtml,
            AllAttendees = allAttendees,
            AllAttendeesHtml = allAttendeesHtml,
            EventDetailsUrl = eventDetailsUrl,
            TicketUrl = ticketUrl,
            TicketCode = ticketCode,
            HasTicket = !string.IsNullOrWhiteSpace(ticketUrl)
        };
    }

    #endregion
}
