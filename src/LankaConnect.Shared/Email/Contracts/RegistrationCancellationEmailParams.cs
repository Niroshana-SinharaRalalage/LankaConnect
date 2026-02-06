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
    /// </summary>
    public string TemplateName => "template-event-registration-cancellation";

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
    public string SupportEmail { get; set; } = "support@lankaconnect.com";

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
            { "OrganizerContactPhone", OrganizerContactPhone }
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
