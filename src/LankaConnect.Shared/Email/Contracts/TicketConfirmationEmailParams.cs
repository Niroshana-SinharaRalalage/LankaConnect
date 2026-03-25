using System.Globalization;
using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 4: Template-specific typed parameters for paid event registration confirmation.
/// Template: template-paid-event-registration-confirmation-with-ticket
///
/// This replaces Dictionary&lt;string, object&gt; in PaymentCompletedEventHandler with
/// compile-time type-safe parameters.
///
/// Parameters match exactly what the template expects:
/// - Core: UserName, EventTitle, EventStartDate, EventStartTime, EventLocation, EventDetailsUrl
/// - Payment: AmountPaid, TotalAmount, PaymentIntentId, PaymentDate, OrderNumber
/// - Attendees: Attendees (HTML), HasAttendeeDetails, Quantity
/// - Ticket: HasTicket, TicketCode, TicketExpiryDate, TicketUrl
/// - Organizer: HasOrganizerContact, OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone
/// - Contact: HasContactInfo, ContactEmail, ContactPhone
/// - Image: HasEventImage, EventImageUrl
/// </summary>
public class TicketConfirmationEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for paid event registration confirmation with ticket.
    /// Phase 6A.113: Using contract constant instead of hardcoded string.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.PaidEventRegistration;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail => ContactEmail;

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
    /// Recipient's name (user or first attendee).
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Contact email for the registration.
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

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
    /// Event location address.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

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

    /// <summary>
    /// Ticket type description (e.g., "General Admission").
    /// </summary>
    public string TicketType { get; set; } = "General Admission";

    #endregion

    #region Payment Properties

    /// <summary>
    /// Amount paid for the registration.
    /// </summary>
    public decimal AmountPaid { get; set; }

    /// <summary>
    /// Stripe payment intent ID.
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Date and time payment was completed.
    /// </summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// Date the registration was created.
    /// </summary>
    public DateTime RegistrationDate { get; set; }

    /// <summary>
    /// Number of tickets/attendees.
    /// </summary>
    public int Quantity { get; set; }

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

    #region Ticket Properties

    /// <summary>
    /// Whether a ticket was generated (controls {{#HasTicket}} conditional).
    /// </summary>
    public bool HasTicket { get; set; } = false;

    /// <summary>
    /// Ticket code for the event.
    /// </summary>
    public string TicketCode { get; set; } = string.Empty;

    /// <summary>
    /// Ticket expiry date formatted.
    /// </summary>
    public string TicketExpiryDate { get; set; } = string.Empty;

    /// <summary>
    /// URL to view/download ticket.
    /// </summary>
    public string TicketUrl { get; set; } = string.Empty;

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
    /// Registrant's email (may differ from ContactEmail if registering for others).
    /// </summary>
    public string RegistrantEmail { get; set; } = string.Empty;

    /// <summary>
    /// Registrant's phone.
    /// </summary>
    public string RegistrantPhone { get; set; } = string.Empty;

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

    #region Financial Breakdown Properties (Phase 6A.137C)

    /// <summary>
    /// Whether the payment includes a financial breakdown (ticket + donation).
    /// Controls {{#if HasFinancialBreakdown}} conditional in template.
    /// </summary>
    public bool HasFinancialBreakdown { get; set; } = false;

    /// <summary>
    /// Whether the registration includes a bundled donation.
    /// Controls {{#if HasDonation}} conditional in template.
    /// </summary>
    public bool HasDonation { get; set; } = false;

    /// <summary>
    /// Ticket subtotal (AmountPaid minus donation, formatted as "140.00").
    /// </summary>
    public string TicketSubtotal { get; set; } = string.Empty;

    /// <summary>
    /// Bundled donation amount formatted as "25.00".
    /// </summary>
    public string DonationAmount { get; set; } = string.Empty;

    /// <summary>
    /// Currency code (e.g., "USD") for the financial breakdown.
    /// </summary>
    public string BreakdownCurrency { get; set; } = "USD";

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
            { "EventLocation", EventLocation },
            { "EventDetailsUrl", EventDetailsUrl },
            { "HasSignUpLists", HasSignUpLists },
            { "SignUpListsUrl", SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl },  // Alias: template uses {{SignupListUrl}} singular
            { "HasSignupForms", HasSignupForms },  // Phase 6A.112
            { "SignupFormsUrl", SignupFormsUrl },  // Phase 6A.112
            { "TicketType", TicketType },

            // Payment parameters
            { "AmountPaid", AmountPaid.ToString("C", CultureInfo.GetCultureInfo("en-US")) },
            { "TotalAmount", AmountPaid.ToString("C", CultureInfo.GetCultureInfo("en-US")) },
            { "PaymentIntentId", PaymentIntentId },
            { "OrderNumber", PaymentIntentId }, // OrderNumber is same as PaymentIntentId
            { "PaymentDate", EmailDateTimeHelper.FormatDateTimeWithTz(PaymentDate, TimeZoneId) },  // Phase 6A.97: Uses event's timezone
            { "RegistrationDate", EmailDateTimeHelper.FormatDateTimeWithTz(RegistrationDate, TimeZoneId) },  // Phase 6A.97: Uses event's timezone
            { "Quantity", Quantity },

            // Attendee parameters
            { "HasAttendeeDetails", HasAttendeeDetails },
            { "Attendees", AttendeesHtml },

            // Ticket parameters
            { "HasTicket", HasTicket },
            { "TicketCode", TicketCode },
            { "TicketExpiryDate", TicketExpiryDate },
            { "TicketUrl", TicketUrl },

            // Organizer contact parameters
            { "HasOrganizerContact", HasOrganizerContact },
            { "OrganizerContactName", OrganizerContactName },
            { "OrganizerContactEmail", OrganizerContactEmail },
            { "OrganizerContactPhone", OrganizerContactPhone },
            { "OrganizerContactsHtml", OrganizerContactsHtml },
            { "OrganizerContactHeader", OrganizerContactHeader },

            // Registration contact parameters
            { "HasContactInfo", HasContactInfo },
            { "ContactEmail", RegistrantEmail },
            { "ContactPhone", RegistrantPhone },

            // Event image parameters
            { "HasEventImage", HasEventImage },
            { "EventImageUrl", EventImageUrl },

            // Phase 6A.137C: Financial breakdown parameters
            { "HasFinancialBreakdown", HasFinancialBreakdown },
            { "HasDonation", HasDonation },
            { "TicketSubtotal", TicketSubtotal },
            { "DonationAmount", DonationAmount },
            { "BreakdownCurrency", BreakdownCurrency }
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

        if (string.IsNullOrWhiteSpace(ContactEmail))
            errors.Add("ContactEmail is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (string.IsNullOrWhiteSpace(EventDetailsUrl))
            errors.Add("EventDetailsUrl is required");

        if (string.IsNullOrWhiteSpace(PaymentIntentId))
            errors.Add("PaymentIntentId is required");

        if (AmountPaid <= 0)
            errors.Add("AmountPaid must be greater than zero");

        // Conditional validations
        if (HasTicket && string.IsNullOrWhiteSpace(TicketCode))
            errors.Add("TicketCode is required when HasTicket is true");

        if (HasOrganizerContact && string.IsNullOrWhiteSpace(OrganizerContactName))
            errors.Add("OrganizerContactName is required when HasOrganizerContact is true");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new TicketConfirmationEmailParams with required fields.
    /// </summary>
    public static TicketConfirmationEmailParams Create(
        Guid eventId,
        Guid registrationId,
        string userName,
        string contactEmail,
        string eventTitle,
        DateTime eventStartDate,
        string eventStartTime,
        string eventLocation,
        string eventDetailsUrl,
        decimal amountPaid,
        string paymentIntentId,
        DateTime paymentDate,
        int quantity)
    {
        return new TicketConfirmationEmailParams
        {
            EventId = eventId,
            RegistrationId = registrationId,
            UserName = userName,
            ContactEmail = contactEmail,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            EventStartTime = eventStartTime,
            EventLocation = eventLocation,
            EventDetailsUrl = eventDetailsUrl,
            AmountPaid = amountPaid,
            PaymentIntentId = paymentIntentId,
            PaymentDate = paymentDate,
            RegistrationDate = paymentDate, // Default to payment date
            Quantity = quantity
        };
    }

    /// <summary>
    /// Sets ticket information.
    /// </summary>
    public TicketConfirmationEmailParams WithTicket(
        string ticketCode,
        string expiryDate,
        string ticketUrl)
    {
        HasTicket = true;
        TicketCode = ticketCode ?? string.Empty;
        TicketExpiryDate = expiryDate ?? string.Empty;
        TicketUrl = ticketUrl ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets organizer contact information.
    /// </summary>
    public TicketConfirmationEmailParams WithOrganizerContact(
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
    public TicketConfirmationEmailParams WithOrganizerContacts(IReadOnlyList<OrganizerContactInfo> contacts)
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
    public TicketConfirmationEmailParams WithAttendees(string attendeesHtml)
    {
        HasAttendeeDetails = true;
        AttendeesHtml = attendeesHtml ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets event image.
    /// </summary>
    public TicketConfirmationEmailParams WithEventImage(string imageUrl)
    {
        HasEventImage = !string.IsNullOrEmpty(imageUrl);
        EventImageUrl = imageUrl ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets registration contact info.
    /// </summary>
    public TicketConfirmationEmailParams WithContactInfo(string? email, string? phone)
    {
        HasContactInfo = true;
        RegistrantEmail = email ?? string.Empty;
        RegistrantPhone = phone ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets signup lists URL and HasSignUpLists flag together.
    /// Phase 6A.128: Removed WithSignUpListsUrl() which was a trap — it set URL without the flag.
    /// </summary>
    public TicketConfirmationEmailParams WithSignUpLists(string url)
    {
        HasSignUpLists = !string.IsNullOrWhiteSpace(url);
        SignUpListsUrl = url ?? string.Empty;
        return this;
    }

    
    /// <summary>
    /// Sets signup forms URL and HasSignupForms flag together.
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public TicketConfirmationEmailParams WithSignupForms(string url)
    {
        HasSignupForms = !string.IsNullOrWhiteSpace(url);
        SignupFormsUrl = url ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Phase 6A.137C: Sets financial breakdown showing ticket + donation itemization.
    /// Only applicable when registration includes a bundled donation.
    /// </summary>
    public TicketConfirmationEmailParams WithFinancialBreakdown(
        decimal ticketSubtotal,
        decimal donationAmount,
        string currency)
    {
        HasFinancialBreakdown = true;
        HasDonation = donationAmount > 0;
        TicketSubtotal = ticketSubtotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        DonationAmount = donationAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        BreakdownCurrency = currency;
        return this;
    }

    #endregion
}
