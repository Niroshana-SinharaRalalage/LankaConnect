using System.Globalization;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.137B: Template-specific typed parameters for collection receipt email.
/// Template: template-collection-receipt
/// </summary>
public class CollectionReceiptEmailParams : IEmailParameters
{
    public string TemplateName => EmailTemplateContract.TemplateNames.CollectionReceipt;

    public string RecipientEmail => ContributorEmail;
    public string RecipientName => ContributorName;

    #region Core Properties

    public string ContributorName { get; set; } = string.Empty;
    public string ContributorEmail { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public decimal ContributionAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime PaymentDate { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public string EventDetailsUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    #endregion

    #region IEmailParameters Implementation

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName, ContributorName },
            { EmailTemplateContract.Collection.ContributorName, ContributorName },
            { EmailTemplateContract.Collection.ContributorEmail, ContributorEmail },
            { EmailTemplateContract.Collection.EventTitle, EventTitle },
            { EmailTemplateContract.Collection.ContributionAmount, ContributionAmount.ToString("F2", CultureInfo.InvariantCulture) },
            { EmailTemplateContract.Collection.Currency, Currency },
            { EmailTemplateContract.Collection.PaymentIntentId, PaymentIntentId },
            { EmailTemplateContract.Collection.PaymentDate, PaymentDate.ToString("MMMM dd, yyyy h:mm tt", CultureInfo.InvariantCulture) },
            { EmailTemplateContract.Collection.EventDetailsUrl, EventDetailsUrl },
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

    public static CollectionReceiptEmailParams Create(
        string contributorName,
        string contributorEmail,
        string eventTitle,
        decimal contributionAmount,
        string currency,
        DateTime paymentDate,
        string paymentIntentId,
        string eventDetailsUrl)
    {
        return new CollectionReceiptEmailParams
        {
            ContributorName = contributorName,
            ContributorEmail = contributorEmail,
            EventTitle = eventTitle,
            ContributionAmount = contributionAmount,
            Currency = currency,
            PaymentDate = paymentDate,
            PaymentIntentId = paymentIntentId,
            EventDetailsUrl = eventDetailsUrl
        };
    }

    #endregion
}
