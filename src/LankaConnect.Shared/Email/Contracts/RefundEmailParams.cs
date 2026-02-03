using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 5: Template-specific typed parameters for refund-related emails.
/// Templates: template-refund-request-created, template-refund-completed
///
/// This replaces Dictionary&lt;string, object&gt; in RefundRequestedEventHandler and
/// RefundCompletedEventHandler with compile-time type-safe parameters.
/// </summary>
public class RefundEmailParams : IEmailParameters
{
    private string _templateName = "template-refund-request-created";

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
    public string SupportEmail { get; set; } = "support@lankaconnect.com";

    /// <summary>
    /// URL to view refund details.
    /// </summary>
    public string RefundDetailsUrl { get; set; } = string.Empty;

    #endregion

    #region Template Type

    /// <summary>
    /// Sets the template to use for refund request emails.
    /// </summary>
    public RefundEmailParams AsRequest()
    {
        _templateName = "template-refund-request-created";
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
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            { "UserName", UserName },
            { "EventTitle", EventTitle },
            { "EventStartDate", EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId) },
            { "RefundAmount", RefundAmount.ToString("C") },
            { "OriginalAmount", OriginalAmount.ToString("C") },
            { "Currency", Currency },
            { "RefundReason", RefundReason },
            { "RefundStatus", RefundStatus },
            { "RequestedAt", RequestedAt.ToString("MMMM dd, yyyy h:mm tt") },
            { "ProcessingMethod", ProcessingMethod },
            { "SupportEmail", SupportEmail },
            { "RefundDetailsUrl", RefundDetailsUrl }
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

    #region Factory Methods

    /// <summary>
    /// Creates a new RefundEmailParams for a refund request email.
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
        DateTime requestedAt)
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
            RequestedAt = requestedAt
        }.AsRequest();
    }

    /// <summary>
    /// Creates a new RefundEmailParams for a refund completed email.
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
            ProcessingMethod = processingMethod
        }.AsCompleted();
    }

    #endregion
}
