using System.Globalization;

namespace LankaConnect.Modules.Communications.Contracts.Email.Contracts;

/// <summary>
/// Phase 6A.137B: Template-specific typed parameters for add-on purchase receipt email.
/// Template: template-addon-purchase-receipt
/// </summary>
public class AddOnPurchaseReceiptEmailParams : IEmailParameters
{
    public string TemplateName => EmailTemplateContract.TemplateNames.AddOnPurchaseReceipt;

    public string RecipientEmail => BuyerEmail;
    public string RecipientName => BuyerName;

    #region Core Properties

    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string AddOnName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
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
            { EmailTemplateContract.Common.UserName, BuyerName },
            { EmailTemplateContract.AddOnPurchase.BuyerName, BuyerName },
            { EmailTemplateContract.AddOnPurchase.BuyerEmail, BuyerEmail },
            { EmailTemplateContract.AddOnPurchase.EventTitle, EventTitle },
            { EmailTemplateContract.AddOnPurchase.AddOnName, AddOnName },
            { EmailTemplateContract.AddOnPurchase.Quantity, Quantity },
            { EmailTemplateContract.AddOnPurchase.UnitPrice, UnitPrice.ToString("F2", CultureInfo.InvariantCulture) },
            { EmailTemplateContract.AddOnPurchase.TotalAmount, TotalAmount.ToString("F2", CultureInfo.InvariantCulture) },
            { EmailTemplateContract.AddOnPurchase.Currency, Currency },
            { EmailTemplateContract.AddOnPurchase.PaymentIntentId, PaymentIntentId },
            { EmailTemplateContract.AddOnPurchase.PaymentDate, PaymentDate.ToString("MMMM dd, yyyy h:mm tt", CultureInfo.InvariantCulture) },
            { EmailTemplateContract.AddOnPurchase.EventDetailsUrl, EventDetailsUrl },
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year }
        };
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(BuyerName))
            errors.Add("BuyerName is required");
        if (string.IsNullOrWhiteSpace(BuyerEmail))
            errors.Add("BuyerEmail is required");
        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");
        if (string.IsNullOrWhiteSpace(AddOnName))
            errors.Add("AddOnName is required");
        if (Quantity <= 0)
            errors.Add("Quantity must be greater than 0");
        if (TotalAmount <= 0)
            errors.Add("TotalAmount must be greater than 0");

        return errors.Count == 0;
    }

    #endregion

    #region Factory

    public static AddOnPurchaseReceiptEmailParams Create(
        string buyerName,
        string buyerEmail,
        string eventTitle,
        string addOnName,
        int quantity,
        decimal unitPrice,
        decimal totalAmount,
        string currency,
        DateTime paymentDate,
        string paymentIntentId,
        string eventDetailsUrl)
    {
        return new AddOnPurchaseReceiptEmailParams
        {
            BuyerName = buyerName,
            BuyerEmail = buyerEmail,
            EventTitle = eventTitle,
            AddOnName = addOnName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalAmount = totalAmount,
            Currency = currency,
            PaymentDate = paymentDate,
            PaymentIntentId = paymentIntentId,
            EventDetailsUrl = eventDetailsUrl
        };
    }

    #endregion
}
