namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 7D Fix 4: typed parameters for the "we turned WhatsApp off because you never
/// verified" notification email. Sent exclusively by the daily background job after the
/// configured grace window elapses — never user-triggered, so no form/HTTP path builds
/// this object.
/// </summary>
public class WhatsAppAutoDisabledEmailParams : IEmailParameters
{
    public string TemplateName => EmailTemplateContract.TemplateNames.WhatsAppAutoDisabled;
    public string RecipientEmail => UserEmail;
    public string RecipientName => UserName;

    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>Phone number masked to last 4 digits (e.g. "•••• 3717"). Never the full number.</summary>
    public string MaskedPhone { get; set; } = string.Empty;

    /// <summary>Formatted enable date in the user's locale-agnostic long form.</summary>
    public string EnabledAt { get; set; } = string.Empty;

    /// <summary>Grace window size in days (matches <c>WhatsAppSettings:UnverifiedGracePeriodDays</c>).</summary>
    public int GracePeriodDays { get; set; } = 30;

    /// <summary>URL to the WhatsApp preferences screen where the user can re-enable.</summary>
    public string ReEnableUrl { get; set; } = string.Empty;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName, UserName },
            { EmailTemplateContract.WhatsAppAutoDisabled.MaskedPhone, MaskedPhone },
            { EmailTemplateContract.WhatsAppAutoDisabled.EnabledAt, EnabledAt },
            { EmailTemplateContract.WhatsAppAutoDisabled.GracePeriodDays, GracePeriodDays },
            { EmailTemplateContract.WhatsAppAutoDisabled.ReEnableUrl, ReEnableUrl },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year }
        };
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        if (string.IsNullOrWhiteSpace(UserName)) errors.Add("UserName is required");
        if (string.IsNullOrWhiteSpace(UserEmail)) errors.Add("UserEmail is required");
        if (string.IsNullOrWhiteSpace(MaskedPhone)) errors.Add("MaskedPhone is required");
        if (string.IsNullOrWhiteSpace(EnabledAt)) errors.Add("EnabledAt is required");
        if (GracePeriodDays <= 0) errors.Add("GracePeriodDays must be > 0");
        if (string.IsNullOrWhiteSpace(ReEnableUrl)) errors.Add("ReEnableUrl is required");
        return errors.Count == 0;
    }
}
