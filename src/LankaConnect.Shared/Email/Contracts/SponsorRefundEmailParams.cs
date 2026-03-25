using System.Globalization;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.137B2: Template-specific typed parameters for sponsor refund email.
/// Template: template-sponsor-refund
/// Sent when a money sponsor payment is refunded via Stripe webhook (charge.refunded).
/// Only applies to money sponsors (item sponsors cannot be refunded).
/// </summary>
public class SponsorRefundEmailParams : IEmailParameters
{
    public string TemplateName => EmailTemplateContract.TemplateNames.SponsorRefund;

    public string RecipientEmail => SponsorEmail;
    public string RecipientName => SponsorName;

    #region Core Properties

    public string SponsorName { get; set; } = string.Empty;
    public string SponsorEmail { get; set; } = string.Empty;
    public string? SponsorOrganization { get; set; }
    public bool HasOrganization => !string.IsNullOrWhiteSpace(SponsorOrganization);
    public string EventTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime RefundedAt { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public string EventDetailsUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    #endregion

    #region IEmailParameters Implementation

    public Dictionary<string, object> ToDictionary()
    {
        var amountFormatted = Amount.ToString("F2", CultureInfo.InvariantCulture);

        return new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName, SponsorName },
            { EmailTemplateContract.SponsorRefund.SponsorName, SponsorName },
            { EmailTemplateContract.SponsorRefund.SponsorEmail, SponsorEmail },
            { EmailTemplateContract.SponsorRefund.SponsorOrganization, SponsorOrganization ?? "" },
            { EmailTemplateContract.SponsorRefund.HasOrganization, HasOrganization },
            { EmailTemplateContract.SponsorRefund.EventTitle, EventTitle },
            { EmailTemplateContract.SponsorRefund.Amount, amountFormatted },
            { EmailTemplateContract.SponsorRefund.Currency, Currency },
            { EmailTemplateContract.SponsorRefund.RefundedAt, RefundedAt.ToString("MMMM dd, yyyy h:mm tt", CultureInfo.InvariantCulture) },
            { EmailTemplateContract.SponsorRefund.PaymentIntentId, PaymentIntentId },
            { EmailTemplateContract.SponsorRefund.EventDetailsUrl, EventDetailsUrl },
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year }
        };
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SponsorName))
            errors.Add("SponsorName is required");
        if (string.IsNullOrWhiteSpace(SponsorEmail))
            errors.Add("SponsorEmail is required");
        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");
        if (Amount <= 0)
            errors.Add("Amount must be greater than 0");

        return errors.Count == 0;
    }

    #endregion

    #region Factory

    public static SponsorRefundEmailParams Create(
        string sponsorName,
        string sponsorEmail,
        string? sponsorOrganization,
        string eventTitle,
        decimal amount,
        string currency,
        DateTime refundedAt,
        string paymentIntentId,
        string eventDetailsUrl)
    {
        return new SponsorRefundEmailParams
        {
            SponsorName = sponsorName,
            SponsorEmail = sponsorEmail,
            SponsorOrganization = sponsorOrganization,
            EventTitle = eventTitle,
            Amount = amount,
            Currency = currency,
            RefundedAt = refundedAt,
            PaymentIntentId = paymentIntentId,
            EventDetailsUrl = eventDetailsUrl
        };
    }

    #endregion
}
