namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for account activated by admin emails.
/// Template: template-admin-user-activation
///
/// This replaces Dictionary&lt;string, object&gt; in AdminActivateUserCommandHandler with
/// compile-time type-safe parameters.
///
/// Sent to user when their account is activated by an admin.
/// </summary>
public class AccountActivatedEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for account activation email.
    /// Phase 6A.113: Updated to use corrected constant name.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.AccountActivatedByAdmin;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

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
    /// URL to the login page.
    /// </summary>
    public string LoginUrl { get; set; } = string.Empty;

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

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
            { EmailTemplateContract.Common.UserName, UserName },
            { EmailTemplateContract.Auth.LoginUrl, LoginUrl },
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year.ToString() }
        };
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (UserId == Guid.Empty)
            errors.Add("UserId is required");

        if (string.IsNullOrWhiteSpace(RecipientEmail))
            errors.Add("RecipientEmail is required");

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(LoginUrl))
            errors.Add("LoginUrl is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new AccountActivatedEmailParams with required fields.
    /// </summary>
    public static AccountActivatedEmailParams Create(
        Guid userId,
        string recipientEmail,
        string userName,
        string loginUrl)
    {
        return new AccountActivatedEmailParams
        {
            UserId = userId,
            RecipientEmail = recipientEmail,
            UserName = userName,
            LoginUrl = loginUrl
        };
    }

    #endregion
}
