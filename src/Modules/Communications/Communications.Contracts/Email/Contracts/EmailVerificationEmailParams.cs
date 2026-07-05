namespace LankaConnect.Modules.Communications.Contracts.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 5: Template-specific typed parameters for member email verification.
/// Template: template-membership-email-verification
///
/// This replaces Dictionary&lt;string, object&gt; in MemberVerificationRequestedEventHandler with
/// compile-time type-safe parameters.
///
/// Parameters match exactly what the template expects:
/// - Core: UserName, Email, VerificationUrl, ExpirationHours
/// </summary>
public class EmailVerificationEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for member email verification.
    /// Phase 6A.113: Using contract constant instead of hardcoded string.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.EmailVerification;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail => Email;

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
    /// User's email address to be verified.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Full verification URL with token.
    /// </summary>
    public string VerificationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Hours until verification token expires (e.g., "24").
    /// </summary>
    public string ExpirationHours { get; set; } = "24";

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
            { "Email", Email },
            { "VerificationUrl", VerificationUrl },
            { "ExpirationHours", ExpirationHours },
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

        if (string.IsNullOrWhiteSpace(Email))
            errors.Add("Email is required");

        if (string.IsNullOrWhiteSpace(VerificationUrl))
            errors.Add("VerificationUrl is required");

        if (string.IsNullOrWhiteSpace(ExpirationHours))
            errors.Add("ExpirationHours is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new EmailVerificationEmailParams with required fields.
    /// </summary>
    public static EmailVerificationEmailParams Create(
        Guid userId,
        string userName,
        string email,
        string verificationUrl,
        string expirationHours = "24")
    {
        return new EmailVerificationEmailParams
        {
            UserId = userId,
            UserName = userName,
            Email = email,
            VerificationUrl = verificationUrl,
            ExpirationHours = expirationHours
        };
    }

    #endregion
}
