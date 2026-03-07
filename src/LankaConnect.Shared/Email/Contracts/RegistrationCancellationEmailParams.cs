using System.Globalization;
using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 5: Template-specific typed parameters for registration cancellation email.
/// Template: template-event-registration-cancellation
///
/// This replaces Dictionary&lt;string, object&gt; in RegistrationCancelledEventHandler with
/// compile-time type-safe parameters.
///
/// Parameters match exactly what the template expects:
/// - UserName, EventTitle, EventStartDate, CancellationReason, RefundStatus, etc.
///
/// Phase 6A.87 Fix: Corrected template name to include "event-" prefix.
/// </summary>
public class RegistrationCancellationEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for registration cancellation.
    /// Phase 6A.87 Fix: Corrected from "template-registration-cancellation" to include "event-" prefix.
    /// Phase 6A.113: Using contract constant instead of hardcoded string.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.EventRegistrationCancellation;

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
    /// User identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's display name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Registration identifier.
    /// </summary>
    public Guid RegistrationId { get; set; }

    /// <summary>
    /// Event identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Event title.
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Event start date/time.
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
    /// Reason for cancellation.
    /// </summary>
    public string CancellationReason { get; set; } = string.Empty;

    /// <summary>
    /// Date/time of cancellation.
    /// </summary>
    public DateTime CancelledAt { get; set; }

    /// <summary>
    /// Refund status (e.g., "Refund Pending", "No Refund Required", "Refund Processed").
    /// </summary>
    public string RefundStatus { get; set; } = string.Empty;

    /// <summary>
    /// Refund amount if applicable.
    /// </summary>
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// URL to view event details.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    #endregion

    #region Organizer Contact Properties (Phase 6A.97+)

    /// <summary>
    /// Whether organizer contact info is available (controls {{#HasOrganizerContact}} conditional).
    /// Phase 6A.97+: Added to fix raw placeholder display in emails.
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

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Phase 6A.87+ Fix: Added EventDateTime combined field and EventStartTime for standardized templates.
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
            { "EventStartTime", formattedTime },  // Phase 6A.87+ Fix: Added for templates that need separate time
            { "EventDateTime", $"{formattedDate} at {formattedTime}" },  // Phase 6A.87+ Fix: Combined for standardized templates
            { "EventLocation", EventLocation },
            { "CancellationReason", CancellationReason },
            { "CancelledAt", CancelledAt.ToString("MMMM dd, yyyy h:mm tt") },
            { "CancellationDate", CancelledAt.ToString("MMMM dd, yyyy h:mm tt") },  // Issue #56.1: Alias for template compatibility
            { "RefundStatus", RefundStatus },
            { "EventDetailsUrl", EventDetailsUrl },
            { "SupportEmail", SupportEmail },
            { "Year", DateTime.UtcNow.Year },

            // Phase 6A.97+ Fix: Organizer contact params (for {{#HasOrganizerContact}} conditional)
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
        };

        if (RefundAmount.HasValue)
        {
            // Phase 6A.87 Fix: Use explicit US culture to avoid currency symbol issues
            dict["RefundAmount"] = RefundAmount.Value.ToString("C", CultureInfo.GetCultureInfo("en-US"));
            dict["Currency"] = Currency;
        }

        return dict;
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (UserId == Guid.Empty)
            errors.Add("UserId is required");

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(UserEmail))
            errors.Add("UserEmail is required");

        // Phase 6A.97 Fix: RegistrationId is now OPTIONAL because:
        // 1. RegistrationCancelledEvent domain event doesn't include RegistrationId
        // 2. Email template doesn't prominently display registration ID
        // 3. User and event information are sufficient for the recipient
        // Previously: if (RegistrationId == Guid.Empty) errors.Add("RegistrationId is required");

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

    #region Fluent Setters (Phase 6A.97+)

    /// <summary>
    /// Sets organizer contact information.
    /// Phase 6A.97+: Added to fix raw placeholder display in emails.
    /// </summary>
    public RegistrationCancellationEmailParams WithOrganizerContact(
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
    public RegistrationCancellationEmailParams WithOrganizerContacts(IReadOnlyList<OrganizerContactInfo> contacts)
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
    /// Sets signup lists URL.
    /// </summary>
    public RegistrationCancellationEmailParams WithSignUpLists(string url)
    {
        HasSignUpLists = !string.IsNullOrWhiteSpace(url);
        SignUpListsUrl = url ?? string.Empty;
        return this;
    }

    
    /// <summary>
    /// Sets signup forms URL and HasSignupForms flag together.
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public RegistrationCancellationEmailParams WithSignupForms(string url)
    {
        HasSignupForms = !string.IsNullOrWhiteSpace(url);
        SignupFormsUrl = url ?? string.Empty;
        return this;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new RegistrationCancellationEmailParams with required fields.
    /// </summary>
    public static RegistrationCancellationEmailParams Create(
        Guid userId,
        string userName,
        string userEmail,
        Guid registrationId,
        Guid eventId,
        string eventTitle,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        string cancellationReason,
        DateTime cancelledAt,
        string refundStatus,
        decimal? refundAmount = null,
        string currency = "USD")
    {
        return new RegistrationCancellationEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            RegistrationId = registrationId,
            EventId = eventId,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            CancellationReason = cancellationReason,
            CancelledAt = cancelledAt,
            RefundStatus = refundStatus,
            RefundAmount = refundAmount,
            Currency = currency
        };
    }

    #endregion
}
