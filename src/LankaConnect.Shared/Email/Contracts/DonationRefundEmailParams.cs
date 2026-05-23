using System.Globalization;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.137B2: Template-specific typed parameters for donation refund email.
/// Template: template-donation-refund
/// Sent when a donation payment is refunded via Stripe webhook (charge.refunded).
/// </summary>
public class DonationRefundEmailParams : IEmailParameters, IDispatchLoggable
{
    public string TemplateName => EmailTemplateContract.TemplateNames.DonationRefund;

    public string RecipientEmail => DonorEmail;
    public string RecipientName => DonorName;

    // Phase 6A.148.W5.6.B.OBS2 — dispatch-log threading. Donations are NOT part of the
    // approval workflow today (out-of-scope per plan §13), so RefundRequestId is null,
    // but logging the entity_id still gives operators a queryable trail.
    public Guid? DispatchDonationId { get; set; }
    Guid? IDispatchLoggable.DispatchRefundRequestId => null;
    string? IDispatchLoggable.DispatchEntityType => DispatchDonationId.HasValue ? "Donation" : null;
    Guid? IDispatchLoggable.DispatchEntityId => DispatchDonationId;

    #region Core Properties

    public string DonorName { get; set; } = string.Empty;
    public string DonorEmail { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public decimal DonationAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime RefundedAt { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public string EventDetailsUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    #endregion

    #region IEmailParameters Implementation

    public Dictionary<string, object> ToDictionary()
    {
        var amountFormatted = DonationAmount.ToString("F2", CultureInfo.InvariantCulture);

        return new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName, DonorName },
            { EmailTemplateContract.DonationRefund.DonorName, DonorName },
            { EmailTemplateContract.DonationRefund.DonorEmail, DonorEmail },
            { EmailTemplateContract.DonationRefund.EventTitle, EventTitle },
            { EmailTemplateContract.DonationRefund.DonationAmount, amountFormatted },
            { EmailTemplateContract.DonationRefund.Currency, Currency },
            { EmailTemplateContract.DonationRefund.RefundedAt, RefundedAt.ToString("MMMM dd, yyyy h:mm tt", CultureInfo.InvariantCulture) },
            { EmailTemplateContract.DonationRefund.PaymentIntentId, PaymentIntentId },
            { EmailTemplateContract.DonationRefund.EventDetailsUrl, EventDetailsUrl },
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year }
        };
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(DonorName))
            errors.Add("DonorName is required");
        if (string.IsNullOrWhiteSpace(DonorEmail))
            errors.Add("DonorEmail is required");
        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");
        if (DonationAmount <= 0)
            errors.Add("DonationAmount must be greater than 0");

        return errors.Count == 0;
    }

    #endregion

    #region Factory

    public static DonationRefundEmailParams Create(
        string donorName,
        string donorEmail,
        string eventTitle,
        decimal donationAmount,
        string currency,
        DateTime refundedAt,
        string paymentIntentId,
        string eventDetailsUrl)
    {
        return new DonationRefundEmailParams
        {
            DonorName = donorName,
            DonorEmail = donorEmail,
            EventTitle = eventTitle,
            DonationAmount = donationAmount,
            Currency = currency,
            RefundedAt = refundedAt,
            PaymentIntentId = paymentIntentId,
            EventDetailsUrl = eventDetailsUrl
        };
    }

    #endregion
}
