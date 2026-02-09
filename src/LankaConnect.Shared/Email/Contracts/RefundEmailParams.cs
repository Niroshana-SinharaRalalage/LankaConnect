using System.Globalization;
using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 5: Template-specific typed parameters for refund-related emails.
/// Templates: template-refund-requested, template-refund-completed
///
/// This replaces Dictionary&lt;string, object&gt; in RefundRequestedEventHandler and
/// RefundCompletedEventHandler with compile-time type-safe parameters.
///
/// Phase 6A.87 Fix: Corrected template names and added StripeRefundId parameter.
/// </summary>
public class RefundEmailParams : IEmailParameters
{
    private string _templateName = "template-refund-requested";

    /// <summary>
    /// The template name for refund email.
    /// </summary>
    public string TemplateName => _templateName;

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
    /// Refund request identifier.
    /// </summary>
    public Guid RefundId { get; set; }

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
    /// Refund amount.
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Original payment amount.
    /// </summary>
    public decimal OriginalAmount { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Reason for refund (for request).
    /// </summary>
    public string RefundReason { get; set; } = string.Empty;

    /// <summary>
    /// Status of refund (for completed).
    /// </summary>
    public string RefundStatus { get; set; } = string.Empty;

    /// <summary>
    /// Date/time of refund request.
    /// </summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>
    /// Date/time of refund completion (for completed).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Refund processing method (e.g., "Original Payment Method").
    /// </summary>
    public string ProcessingMethod { get; set; } = "Original Payment Method";

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    /// <summary>
    /// URL to view refund details.
    /// </summary>
    public string RefundDetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL to view event details page.
    /// Phase 6A.97: Added for "View Event Details" button in refund emails.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Stripe refund ID (for completed refunds - required by template).
    /// </summary>
    public string StripeRefundId { get; set; } = string.Empty;

    /// <summary>
    /// Stripe payment intent ID (for refund requests - used as reference when StripeRefundId not yet available).
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Event location.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Cancellation date formatted string (for template display).
    /// </summary>
    public string CancellationDate { get; set; } = string.Empty;

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

    #region Template Type

    /// <summary>
    /// Sets the template to use for refund request emails.
    /// </summary>
    public RefundEmailParams AsRequest()
    {
        _templateName = "template-refund-requested";
        return this;
    }

    /// <summary>
    /// Sets the template to use for refund completed emails.
    /// </summary>
    public RefundEmailParams AsCompleted()
    {
        _templateName = "template-refund-completed";
        return this;
    }

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Phase 6A.87 Fix: Added StripeRefundId and fixed currency formatting.
    /// Phase 6A.87+ Fix: Added EventDateTime alias and organizer contact params.
    /// Phase 6A.87++ Fix: Changed to F2 format (no $ symbol) since templates have $ hardcoded.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var formattedDate = EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId);
        var formattedTime = EmailDateTimeHelper.FormatEventTime(EventStartDate, TimeZoneId);

        // Phase 6A.87++ Fix: Use F2 format without $ symbol since templates have $ hardcoded
        // This prevents double dollar sign ($$480.00)
        var refundAmountFormatted = RefundAmount.ToString("F2", CultureInfo.InvariantCulture);
        var originalAmountFormatted = OriginalAmount.ToString("F2", CultureInfo.InvariantCulture);

        var dict = new Dictionary<string, object>
        {
            { "UserName", UserName },
            { "EventTitle", EventTitle },
            { "EventStartDate", formattedDate },
            { "EventStartTime", formattedTime },
            { "EventDateTime", $"{formattedDate} at {formattedTime}" },  // Combined for template
            { "RefundAmount", refundAmountFormatted },  // Phase 6A.87++ Fix: No $ symbol
            { "OriginalAmount", originalAmountFormatted },  // Phase 6A.87++ Fix: No $ symbol
            { "Currency", Currency },
            { "RefundReason", RefundReason },
            { "RefundStatus", RefundStatus },
            { "RequestedAt", RequestedAt.ToString("MMMM dd, yyyy h:mm tt") },
            { "ProcessingMethod", ProcessingMethod },
            { "SupportEmail", SupportEmail },
            { "RefundDetailsUrl", RefundDetailsUrl },
            { "EventDetailsUrl", EventDetailsUrl },  // Phase 6A.97: For "View Event Details" button
            { "StripeRefundId", StripeRefundId },
            { "ReferenceId", !string.IsNullOrEmpty(StripeRefundId) ? StripeRefundId : PaymentIntentId },  // Phase 6A.87++ Fix: Fallback to PaymentIntentId

            { "EventLocation", EventLocation },
            { "CancellationDate", CancellationDate },

            // Organizer contact params (for {{#HasOrganizerContact}} conditional)
            { "HasOrganizerContact", HasOrganizerContact },
            { "OrganizerContactName", OrganizerContactName },
            { "OrganizerContactEmail", OrganizerContactEmail },
            { "OrganizerContactPhone", OrganizerContactPhone },

            // Signup lists params (for {{#HasSignUpLists}} conditional)
            { "HasSignUpLists", HasSignUpLists },
            { "SignUpListsUrl", SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl },  // Template alias (singular form)

            // Footer params
            { "Year", DateTime.UtcNow.Year }
        };

        if (CompletedAt.HasValue)
        {
            dict["CompletedAt"] = CompletedAt.Value.ToString("MMMM dd, yyyy h:mm tt");
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

        if (RefundId == Guid.Empty)
            errors.Add("RefundId is required");

        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (RefundAmount <= 0)
            errors.Add("RefundAmount must be greater than 0");

        return errors.Count == 0;
    }

    #endregion

    #region Fluent Setters

    /// <summary>
    /// Sets organizer contact information.
    /// </summary>
    public RefundEmailParams WithOrganizerContact(
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
    public RefundEmailParams WithSignUpLists(string url)
    {
        HasSignUpLists = !string.IsNullOrWhiteSpace(url);
        SignUpListsUrl = url ?? string.Empty;
        return this;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new RefundEmailParams for a refund request email.
    /// Phase 6A.87++ Fix: Added paymentIntentId parameter for reference number in email.
    /// </summary>
    public static RefundEmailParams CreateRequest(
        Guid userId,
        string userName,
        string userEmail,
        Guid registrationId,
        Guid refundId,
        Guid eventId,
        string eventTitle,
        DateTime eventStartDate,
        string? timeZoneId,
        decimal refundAmount,
        decimal originalAmount,
        string refundReason,
        DateTime requestedAt,
        string? paymentIntentId = null)
    {
        return new RefundEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            RegistrationId = registrationId,
            RefundId = refundId,
            EventId = eventId,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            RefundAmount = refundAmount,
            OriginalAmount = originalAmount,
            RefundReason = refundReason,
            RequestedAt = requestedAt,
            PaymentIntentId = paymentIntentId ?? string.Empty
        }.AsRequest();
    }

    /// <summary>
    /// Creates a new RefundEmailParams for a refund completed email.
    /// Phase 6A.87 Fix: Added stripeRefundId parameter required by template.
    /// </summary>
    public static RefundEmailParams CreateCompleted(
        Guid userId,
        string userName,
        string userEmail,
        Guid registrationId,
        Guid refundId,
        Guid eventId,
        string eventTitle,
        DateTime eventStartDate,
        string? timeZoneId,
        decimal refundAmount,
        decimal originalAmount,
        DateTime completedAt,
        string stripeRefundId,
        string processingMethod = "Original Payment Method")
    {
        return new RefundEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            RegistrationId = registrationId,
            RefundId = refundId,
            EventId = eventId,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            RefundAmount = refundAmount,
            OriginalAmount = originalAmount,
            RefundStatus = "Completed",
            CompletedAt = completedAt,
            StripeRefundId = stripeRefundId,
            ProcessingMethod = processingMethod
        }.AsCompleted();
    }

    #endregion
}
