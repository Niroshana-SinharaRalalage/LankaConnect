using System.Globalization;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Donation Feature: Template-specific typed parameters for donation receipt email.
/// Template: template-donation-receipt
/// </summary>
public class DonationReceiptEmailParams : IEmailParameters
{
    public string TemplateName => EmailTemplateContract.TemplateNames.DonationReceipt;

    public string RecipientEmail => DonorEmail;
    public string RecipientName => DonorName;

    #region Core Properties

    public string DonorName { get; set; } = string.Empty;
    public string DonorEmail { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public decimal DonationAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime PaymentDate { get; set; }
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
            { EmailTemplateContract.Donation.DonorName, DonorName },
            { EmailTemplateContract.Donation.DonorEmail, DonorEmail },
            { EmailTemplateContract.Donation.EventTitle, EventTitle },
            { EmailTemplateContract.Donation.DonationAmount, amountFormatted },
            { EmailTemplateContract.Donation.DonationCurrency, Currency },
            { EmailTemplateContract.Donation.PaymentIntentId, PaymentIntentId },
            { EmailTemplateContract.Donation.PaymentDate, PaymentDate.ToString("MMMM dd, yyyy h:mm tt", CultureInfo.InvariantCulture) },
            { EmailTemplateContract.Donation.EventDetailsUrl, EventDetailsUrl },
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

    public static DonationReceiptEmailParams Create(
        string donorName,
        string donorEmail,
        string eventTitle,
        decimal donationAmount,
        string currency,
        DateTime paymentDate,
        string paymentIntentId,
        string eventDetailsUrl)
    {
        return new DonationReceiptEmailParams
        {
            DonorName = donorName,
            DonorEmail = donorEmail,
            EventTitle = eventTitle,
            DonationAmount = donationAmount,
            Currency = currency,
            PaymentDate = paymentDate,
            PaymentIntentId = paymentIntentId,
            EventDetailsUrl = eventDetailsUrl
        };
    }

    #endregion
}
