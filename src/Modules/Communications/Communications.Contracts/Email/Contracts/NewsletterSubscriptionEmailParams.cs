namespace LankaConnect.Modules.Communications.Contracts.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for newsletter subscription confirmation emails.
/// Template: template-newsletter-subscription-confirmation
///
/// This replaces Dictionary&lt;string, object&gt; in SubscribeToNewsletterCommandHandler with
/// compile-time type-safe parameters.
///
/// Sent to user when they subscribe to the newsletter to confirm their subscription.
/// </summary>
public class NewsletterSubscriptionEmailParams : IEmailParameters
{
    #region Template Selection

    /// <summary>
    /// The template name for newsletter subscription confirmation.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.NewsletterSubscriptionConfirmation;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Recipient name (uses email for newsletter subscriptions).
    /// </summary>
    public string RecipientName => RecipientEmail;

    #endregion

    #region Subscription Properties

    /// <summary>
    /// Subscriber's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation token for verification.
    /// </summary>
    public string ConfirmationToken { get; set; } = string.Empty;

    /// <summary>
    /// Full URL to confirm the subscription.
    /// </summary>
    public string ConfirmationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Full URL to unsubscribe.
    /// </summary>
    public string UnsubscribeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Description of selected metro areas (e.g., "All Locations" or "3 Location(s)").
    /// </summary>
    public string MetroAreasText { get; set; } = "All Locations";

    #endregion

    #region Common Properties

    /// <summary>
    /// Company name for branding.
    /// </summary>
    public string CompanyName { get; set; } = "LankaConnect";

    /// <summary>
    /// Current date (formatted).
    /// </summary>
    public string Date { get; set; } = DateTime.UtcNow.ToString("MMMM dd, yyyy");

    /// <summary>
    /// Current year for footer copyright.
    /// </summary>
    public int Year { get; set; } = DateTime.UtcNow.Year;

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Uses EmailTemplateContract constants for all parameter names.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "Email", Email },
            { "ConfirmationToken", ConfirmationToken },
            { "ConfirmationUrl", ConfirmationUrl },
            { "UnsubscribeUrl", UnsubscribeUrl },
            { "MetroAreasText", MetroAreasText },
            { EmailTemplateContract.Common.CompanyName, CompanyName },
            { "Date", Date },
            { EmailTemplateContract.Common.Year, Year }
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

        if (string.IsNullOrWhiteSpace(Email))
            errors.Add("Email is required");

        if (string.IsNullOrWhiteSpace(ConfirmationToken))
            errors.Add("ConfirmationToken is required");

        if (string.IsNullOrWhiteSpace(ConfirmationUrl))
            errors.Add("ConfirmationUrl is required");

        if (string.IsNullOrWhiteSpace(UnsubscribeUrl))
            errors.Add("UnsubscribeUrl is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new NewsletterSubscriptionEmailParams with required fields.
    /// </summary>
    public static NewsletterSubscriptionEmailParams Create(
        string email,
        string confirmationToken,
        string confirmationUrl,
        string unsubscribeUrl,
        string metroAreasText)
    {
        return new NewsletterSubscriptionEmailParams
        {
            RecipientEmail = email,
            Email = email,
            ConfirmationToken = confirmationToken,
            ConfirmationUrl = confirmationUrl,
            UnsubscribeUrl = unsubscribeUrl,
            MetroAreasText = metroAreasText,
            CompanyName = "LankaConnect",
            Date = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
            Year = DateTime.UtcNow.Year
        };
    }

    #endregion
}
