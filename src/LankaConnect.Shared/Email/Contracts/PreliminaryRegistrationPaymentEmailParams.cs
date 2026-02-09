using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for preliminary registration payment emails.
/// Template: template-preliminary-registration-payment-pending
///
/// This replaces Dictionary&lt;string, object&gt; in RegistrationPendingPaymentEventHandler with
/// compile-time type-safe parameters.
///
/// Sent to user when preliminary registration is created, containing payment link and expiration notice.
/// </summary>
public class PreliminaryRegistrationPaymentEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for preliminary registration payment email.
    /// </summary>
    public string TemplateName => "template-preliminary-registration-payment-pending";

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Recipient name.
    /// </summary>
    public string RecipientName => UserName;

    #region User Properties

    /// <summary>
    /// User's display name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    #endregion

    #region Event Properties

    /// <summary>
    /// Event identifier.
    /// </summary>
    public Guid EventId { get; set; }

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
    /// Event location string.
    /// </summary>
    public string EventLocation { get; set; } = "TBD";

    #endregion

    #region Registration Properties

    /// <summary>
    /// Registration identifier.
    /// </summary>
    public Guid RegistrationId { get; set; }

    /// <summary>
    /// Number of attendees.
    /// </summary>
    public int AttendeeCount { get; set; }

    /// <summary>
    /// Total amount to pay.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code (e.g., "USD").
    /// </summary>
    public string Currency { get; set; } = "USD";

    #endregion

    #region Payment Properties

    /// <summary>
    /// Stripe checkout session URL for payment.
    /// </summary>
    public string PaymentLink { get; set; } = string.Empty;

    /// <summary>
    /// Checkout session expiration date/time.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

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

        // Calculate hours remaining
        var now = DateTime.UtcNow;
        var expiresIn = ExpiresAt - now;
        var hoursRemaining = Math.Max(0, (int)expiresIn.TotalHours);

        return new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName, UserName },
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { EmailTemplateContract.Event.EventStartDate, formattedDate },
            { EmailTemplateContract.Event.EventStartTime, formattedTime },
            { EmailTemplateContract.Event.EventLocation, EventLocation },
            { "AttendeeCount", AttendeeCount },
            { EmailTemplateContract.Payment.TotalAmount, $"${TotalAmount:F2}" },
            { EmailTemplateContract.Refund.Currency, Currency.ToUpper() },
            { "PaymentLink", PaymentLink },
            { "ExpiresAt", ExpiresAt.ToString("MMMM dd, yyyy h:mm tt UTC") },
            { "HoursRemaining", hoursRemaining },
            { "RegistrationId", RegistrationId.ToString() },
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year }
        };
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(RecipientEmail))
            errors.Add("RecipientEmail is required");

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (EventStartDate == default)
            errors.Add("EventStartDate is required");

        if (RegistrationId == Guid.Empty)
            errors.Add("RegistrationId is required");

        if (AttendeeCount <= 0)
            errors.Add("AttendeeCount must be greater than 0");

        if (TotalAmount <= 0)
            errors.Add("TotalAmount must be greater than 0");

        if (string.IsNullOrWhiteSpace(PaymentLink))
            errors.Add("PaymentLink is required");

        if (ExpiresAt == default)
            errors.Add("ExpiresAt is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new PreliminaryRegistrationPaymentEmailParams with required fields.
    /// </summary>
    public static PreliminaryRegistrationPaymentEmailParams Create(
        string recipientEmail,
        string userName,
        Guid eventId,
        string eventTitle,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        Guid registrationId,
        int attendeeCount,
        decimal totalAmount,
        string currency,
        string paymentLink,
        DateTime expiresAt)
    {
        return new PreliminaryRegistrationPaymentEmailParams
        {
            RecipientEmail = recipientEmail,
            UserName = userName,
            EventId = eventId,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            RegistrationId = registrationId,
            AttendeeCount = attendeeCount,
            TotalAmount = totalAmount,
            Currency = currency,
            PaymentLink = paymentLink,
            ExpiresAt = expiresAt
        };
    }

    #endregion
}
