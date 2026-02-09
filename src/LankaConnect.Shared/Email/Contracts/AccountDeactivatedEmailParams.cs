namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for account deactivated by admin emails.
/// Template: template-admin-user-deactivation
///
/// This replaces Dictionary&lt;string, object&gt; in AdminDeactivateUserCommandHandler with
/// compile-time type-safe parameters.
///
/// Sent to user when their account is deactivated by an admin.
/// </summary>
public class AccountDeactivatedEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for account deactivation email.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.AdminUserDeactivation;

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

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new AccountDeactivatedEmailParams with required fields.
    /// </summary>
    public static AccountDeactivatedEmailParams Create(
        Guid userId,
        string recipientEmail,
        string userName)
    {
        return new AccountDeactivatedEmailParams
        {
            UserId = userId,
            RecipientEmail = recipientEmail,
            UserName = userName
        };
    }

    #endregion
}
