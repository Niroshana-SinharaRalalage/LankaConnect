namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 5: Template-specific typed parameters for password reset email.
/// Template: template-password-reset
///
/// This replaces Dictionary&lt;string, object&gt; in SendPasswordResetCommandHandler with
/// compile-time type-safe parameters.
///
/// Parameters match exactly what the template expects:
/// - UserName, UserEmail, ResetToken, ResetLink, ExpiresAt, CompanyName, SupportEmail
/// </summary>
public class PasswordResetEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for password reset.
    /// </summary>
    public string TemplateName => "template-password-reset";

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail => UserEmail;

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
    /// User's email address.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Password reset token.
    /// </summary>
    public string ResetToken { get; set; } = string.Empty;

    /// <summary>
    /// Full password reset link with token.
    /// </summary>
    public string ResetLink { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration date/time formatted.
    /// </summary>
    public string ExpiresAt { get; set; } = string.Empty;

    /// <summary>
    /// Company name for branding.
    /// </summary>
    public string CompanyName { get; set; } = "LankaConnect";

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "support@lankaconnect.com";

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "UserName", UserName },
            { "UserEmail", UserEmail },
            { "ResetToken", ResetToken },
            { "ResetLink", ResetLink },
            { "ExpiresAt", ExpiresAt },
            { "CompanyName", CompanyName },
            { "SupportEmail", SupportEmail }
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

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(UserEmail))
            errors.Add("UserEmail is required");

        if (string.IsNullOrWhiteSpace(ResetToken))
            errors.Add("ResetToken is required");

        if (string.IsNullOrWhiteSpace(ResetLink))
            errors.Add("ResetLink is required");

        if (string.IsNullOrWhiteSpace(ExpiresAt))
            errors.Add("ExpiresAt is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new PasswordResetEmailParams with required fields.
    /// </summary>
    public static PasswordResetEmailParams Create(
        Guid userId,
        string userName,
        string userEmail,
        string resetToken,
        string resetLink,
        DateTime expiresAt)
    {
        return new PasswordResetEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            ResetToken = resetToken,
            ResetLink = resetLink,
            ExpiresAt = expiresAt.ToString("yyyy-MM-dd HH:mm:ss UTC")
        };
    }

    #endregion
}
