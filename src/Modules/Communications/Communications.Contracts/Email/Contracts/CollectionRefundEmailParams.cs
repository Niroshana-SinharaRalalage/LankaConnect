using System.Globalization;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.137B2: Template-specific typed parameters for collection refund email.
/// Template: template-collection-refund
/// Sent when a collection (event fund) payment is refunded via Stripe webhook (charge.refunded).
/// </summary>
public class CollectionRefundEmailParams : IEmailParameters, IDispatchLoggable
{
    public string TemplateName => EmailTemplateContract.TemplateNames.CollectionRefund;

    public string RecipientEmail => ContributorEmail;
    public string RecipientName => ContributorName;

    // Phase 6A.148.W5.6.B.OBS2 — dispatch-log threading.
    public Guid? DispatchRefundRequestId { get; set; }
    public Guid? DispatchCollectionId { get; set; }
    Guid? IDispatchLoggable.DispatchRefundRequestId => DispatchRefundRequestId;
    string? IDispatchLoggable.DispatchEntityType => DispatchCollectionId.HasValue ? "Collection" : null;
    Guid? IDispatchLoggable.DispatchEntityId => DispatchCollectionId;

    #region Core Properties

    public string ContributorName { get; set; } = string.Empty;
    public string ContributorEmail { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public decimal ContributionAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime RefundedAt { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public string EventDetailsUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    #endregion

    #region IEmailParameters Implementation

    public Dictionary<string, object> ToDictionary()
    {
        var amountFormatted = ContributionAmount.ToString("F2", CultureInfo.InvariantCulture);

        return new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName, ContributorName },
            { EmailTemplateContract.CollectionRefund.ContributorName, ContributorName },
            { EmailTemplateContract.CollectionRefund.ContributorEmail, ContributorEmail },
            { EmailTemplateContract.CollectionRefund.EventTitle, EventTitle },
            { EmailTemplateContract.CollectionRefund.ContributionAmount, amountFormatted },
            { EmailTemplateContract.CollectionRefund.Currency, Currency },
            { EmailTemplateContract.CollectionRefund.RefundedAt, RefundedAt.ToString("MMMM dd, yyyy h:mm tt", CultureInfo.InvariantCulture) },
            { EmailTemplateContract.CollectionRefund.PaymentIntentId, PaymentIntentId },
            { EmailTemplateContract.CollectionRefund.EventDetailsUrl, EventDetailsUrl },
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year }
        };
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ContributorName))
            errors.Add("ContributorName is required");
        if (string.IsNullOrWhiteSpace(ContributorEmail))
            errors.Add("ContributorEmail is required");
        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");
        if (ContributionAmount <= 0)
            errors.Add("ContributionAmount must be greater than 0");

        return errors.Count == 0;
    }

    #endregion

    #region Factory

    public static CollectionRefundEmailParams Create(
        string contributorName,
        string contributorEmail,
        string eventTitle,
        decimal contributionAmount,
        string currency,
        DateTime refundedAt,
        string paymentIntentId,
        string eventDetailsUrl)
    {
        return new CollectionRefundEmailParams
        {
            ContributorName = contributorName,
            ContributorEmail = contributorEmail,
            EventTitle = eventTitle,
            ContributionAmount = contributionAmount,
            Currency = currency,
            RefundedAt = refundedAt,
            PaymentIntentId = paymentIntentId,
            EventDetailsUrl = eventDetailsUrl
        };
    }

    #endregion
}
