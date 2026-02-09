namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 5: Template-specific typed parameters for password change confirmation email.
/// Template: template-password-change-confirmation
///
/// This replaces Dictionary&lt;string, object&gt; in ResetPasswordCommandHandler with
/// compile-time type-safe parameters.
///
/// Parameters match exactly what the template expects:
/// - UserName, UserEmail, ChangedAt, CompanyName, SupportEmail, LoginUrl
/// </summary>
public class PasswordChangedEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for password change confirmation.
    /// </summary>
    public string TemplateName => "template-password-change-confirmation";

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
    /// Date/time when password was changed, formatted for display.
    /// </summary>
    public string ChangedAt { get; set; } = string.Empty;

    /// <summary>
    /// Company name for branding.
    /// </summary>
    public string CompanyName { get; set; } = "LankaConnect";

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    /// <summary>
    /// Login page URL.
    /// </summary>
    public string LoginUrl { get; set; } = "https://lankaconnect.com/login";

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Phase 6A.87+ Fix: Added Year for footer.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "UserName", UserName },
            { "UserEmail", UserEmail },
            { "ChangedAt", ChangedAt },
            { "CompanyName", CompanyName },
            { "SupportEmail", SupportEmail },
            { "LoginUrl", LoginUrl },
            { "Year", DateTime.UtcNow.Year }  // Footer param
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

        if (string.IsNullOrWhiteSpace(ChangedAt))
            errors.Add("ChangedAt is required");

        if (string.IsNullOrWhiteSpace(LoginUrl))
            errors.Add("LoginUrl is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new PasswordChangedEmailParams with required fields.
    /// </summary>
    public static PasswordChangedEmailParams Create(
        Guid userId,
        string userName,
        string userEmail,
        DateTime changedAt)
    {
        return new PasswordChangedEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            ChangedAt = changedAt.ToString("MMMM dd, yyyy h:mm tt")
        };
    }

    #endregion
}
