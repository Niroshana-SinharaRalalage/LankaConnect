using System.Globalization;

namespace LankaConnect.Modules.Communications.Contracts.Email.Contracts;

/// <summary>
/// Phase 6A.137B: Template-specific typed parameters for sponsor confirmation email.
/// Covers both monetary sponsors and item sponsors via conditional Handlebars blocks.
/// Template: template-sponsor-confirmation
/// </summary>
public class SponsorConfirmationEmailParams : IEmailParameters
{
    public string TemplateName => EmailTemplateContract.TemplateNames.SponsorConfirmation;

    public string RecipientEmail => SponsorEmail;
    public string RecipientName => SponsorName;

    #region Core Properties

    public string SponsorName { get; set; } = string.Empty;
    public string SponsorEmail { get; set; } = string.Empty;
    public string? SponsorOrganization { get; set; }
    public string EventTitle { get; set; } = string.Empty;

    // Sponsor type discrimination
    public bool IsMonetarySponsor { get; set; }
    public bool IsItemSponsor { get; set; }

    // Monetary sponsor fields
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentIntentId { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }

    // Item sponsor fields
    public string ItemName { get; set; } = string.Empty;
    public string? ItemDescription { get; set; }
    public decimal? EstimatedValue { get; set; }
    public DateTime RecordedAt { get; set; }

    public string EventDetailsUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    #endregion

    #region IEmailParameters Implementation

    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName, SponsorName },
            { EmailTemplateContract.Sponsor.SponsorName, SponsorName },
            { EmailTemplateContract.Sponsor.SponsorEmail, SponsorEmail },
            { EmailTemplateContract.Sponsor.HasOrganization, !string.IsNullOrWhiteSpace(SponsorOrganization) },
            { EmailTemplateContract.Sponsor.SponsorOrganization, SponsorOrganization ?? string.Empty },
            { EmailTemplateContract.Sponsor.EventTitle, EventTitle },
            { EmailTemplateContract.Sponsor.SponsorType, IsMonetarySponsor ? "Monetary Sponsor" : "Item Sponsor" },
            { EmailTemplateContract.Sponsor.IsMonetarySponsor, IsMonetarySponsor },
            { EmailTemplateContract.Sponsor.IsItemSponsor, IsItemSponsor },
            { EmailTemplateContract.Sponsor.EventDetailsUrl, EventDetailsUrl },
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year }
        };

        if (IsMonetarySponsor)
        {
            dict[EmailTemplateContract.Sponsor.Amount] = Amount.ToString("F2", CultureInfo.InvariantCulture);
            dict[EmailTemplateContract.Sponsor.Currency] = Currency;
            dict[EmailTemplateContract.Sponsor.PaymentIntentId] = PaymentIntentId;
            dict[EmailTemplateContract.Sponsor.PaymentDate] = PaymentDate.ToString("MMMM dd, yyyy h:mm tt", CultureInfo.InvariantCulture);
        }

        if (IsItemSponsor)
        {
            dict[EmailTemplateContract.Sponsor.ItemName] = ItemName;
            dict[EmailTemplateContract.Sponsor.HasItemDescription] = !string.IsNullOrWhiteSpace(ItemDescription);
            dict[EmailTemplateContract.Sponsor.ItemDescription] = ItemDescription ?? string.Empty;
            dict[EmailTemplateContract.Sponsor.HasEstimatedValue] = EstimatedValue.HasValue && EstimatedValue > 0;
            dict[EmailTemplateContract.Sponsor.EstimatedValue] = EstimatedValue?.ToString("F2", CultureInfo.InvariantCulture) ?? "0.00";
            dict[EmailTemplateContract.Sponsor.RecordedAt] = RecordedAt.ToString("MMMM dd, yyyy h:mm tt", CultureInfo.InvariantCulture);
        }

        return dict;
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
        if (!IsMonetarySponsor && !IsItemSponsor)
            errors.Add("Must be either monetary or item sponsor");
        if (IsMonetarySponsor && Amount <= 0)
            errors.Add("Amount must be greater than 0 for monetary sponsors");
        if (IsItemSponsor && string.IsNullOrWhiteSpace(ItemName))
            errors.Add("ItemName is required for item sponsors");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    public static SponsorConfirmationEmailParams CreateForMoneySponsor(
        string sponsorName,
        string sponsorEmail,
        string? sponsorOrganization,
        string eventTitle,
        decimal amount,
        string currency,
        DateTime paymentDate,
        string paymentIntentId,
        string eventDetailsUrl)
    {
        return new SponsorConfirmationEmailParams
        {
            SponsorName = sponsorName,
            SponsorEmail = sponsorEmail,
            SponsorOrganization = sponsorOrganization,
            EventTitle = eventTitle,
            IsMonetarySponsor = true,
            IsItemSponsor = false,
            Amount = amount,
            Currency = currency,
            PaymentDate = paymentDate,
            PaymentIntentId = paymentIntentId,
            EventDetailsUrl = eventDetailsUrl
        };
    }

    public static SponsorConfirmationEmailParams CreateForItemSponsor(
        string sponsorName,
        string sponsorEmail,
        string? sponsorOrganization,
        string eventTitle,
        string itemName,
        string? itemDescription,
        decimal? estimatedValue,
        DateTime recordedAt,
        string eventDetailsUrl)
    {
        return new SponsorConfirmationEmailParams
        {
            SponsorName = sponsorName,
            SponsorEmail = sponsorEmail,
            SponsorOrganization = sponsorOrganization,
            EventTitle = eventTitle,
            IsMonetarySponsor = false,
            IsItemSponsor = true,
            ItemName = itemName,
            ItemDescription = itemDescription,
            EstimatedValue = estimatedValue,
            RecordedAt = recordedAt,
            EventDetailsUrl = eventDetailsUrl
        };
    }

    #endregion
}
