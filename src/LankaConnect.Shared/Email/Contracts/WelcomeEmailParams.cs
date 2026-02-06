namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for welcome emails.
/// Template: welcome-{triggerType} (e.g., welcome-registration, welcome-verification)
///
/// This replaces Dictionary&lt;string, object&gt; in SendWelcomeEmailCommandHandler with
/// compile-time type-safe parameters.
///
/// Sent to user on various triggers: registration, email verification, account activation, manual.
/// </summary>
public class WelcomeEmailParams : IEmailParameters
{
    #region Template Selection

    /// <summary>
    /// Welcome email trigger type.
    /// </summary>
    public WelcomeEmailTriggerType TriggerType { get; set; } = WelcomeEmailTriggerType.Default;

    /// <summary>
    /// The template name based on trigger type.
    /// </summary>
    public string TemplateName => TriggerType switch
    {
        WelcomeEmailTriggerType.Registration => "welcome-registration",
        WelcomeEmailTriggerType.EmailVerification => "welcome-verification",
        WelcomeEmailTriggerType.AccountActivation => "welcome-activation",
        WelcomeEmailTriggerType.Manual => "welcome-manual",
        _ => "welcome-default"
    };

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Recipient name.
    /// </summary>
    public string RecipientName => UserName;

    #endregion

    #region User Properties

    /// <summary>
    /// User identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's full display name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// User's role.
    /// </summary>
    public string UserRole { get; set; } = "Member";

    /// <summary>
    /// Whether user is an event organizer.
    /// </summary>
    public bool IsEventOrganizer { get; set; }

    /// <summary>
    /// Whether user is an admin.
    /// </summary>
    public bool IsAdmin { get; set; }

    #endregion

    #region URL Properties

    /// <summary>
    /// URL to the login page.
    /// </summary>
    public string LoginUrl { get; set; } = "https://lankaconnect.com/login";

    /// <summary>
    /// URL to the dashboard.
    /// </summary>
    public string DashboardUrl { get; set; } = "https://lankaconnect.com/dashboard";

    #endregion

    #region Company Properties

    /// <summary>
    /// Company name for branding.
    /// </summary>
    public string CompanyName { get; set; } = "LankaConnect";

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "support@lankaconnect.com";

    #endregion

    #region Optional Custom Message

    /// <summary>
    /// Whether a custom message is included.
    /// </summary>
    public bool HasCustomMessage { get; set; }

    /// <summary>
    /// Optional custom message content.
    /// </summary>
    public string? CustomMessage { get; set; }

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
            { "FirstName", FirstName },
            { EmailTemplateContract.Auth.UserEmail, UserEmail },
            { EmailTemplateContract.Common.CompanyName, CompanyName },
            { EmailTemplateContract.Auth.LoginUrl, LoginUrl },
            { "DashboardUrl", DashboardUrl },
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            { "TriggerType", TriggerType.ToString() },
            { "WelcomeDate", DateTime.UtcNow.ToString("yyyy-MM-dd") },
            { "HasCustomMessage", HasCustomMessage },
            { "CustomMessage", CustomMessage ?? string.Empty },
            { "UserRole", UserRole },
            { "IsEventOrganizer", IsEventOrganizer },
            { "IsAdmin", IsAdmin },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year }
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

        if (string.IsNullOrWhiteSpace(UserEmail))
            errors.Add("UserEmail is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new WelcomeEmailParams with required fields.
    /// </summary>
    public static WelcomeEmailParams Create(
        Guid userId,
        string recipientEmail,
        string userName,
        string firstName,
        string userEmail,
        WelcomeEmailTriggerType triggerType,
        string userRole = "Member",
        bool isEventOrganizer = false,
        bool isAdmin = false,
        string? customMessage = null)
    {
        return new WelcomeEmailParams
        {
            UserId = userId,
            RecipientEmail = recipientEmail,
            UserName = userName,
            FirstName = firstName,
            UserEmail = userEmail,
            TriggerType = triggerType,
            UserRole = userRole,
            IsEventOrganizer = isEventOrganizer,
            IsAdmin = isAdmin,
            HasCustomMessage = !string.IsNullOrWhiteSpace(customMessage),
            CustomMessage = customMessage
        };
    }

    #endregion
}

/// <summary>
/// Trigger types for welcome emails.
/// </summary>
public enum WelcomeEmailTriggerType
{
    /// <summary>
    /// Default welcome email.
    /// </summary>
    Default,

    /// <summary>
    /// Triggered on user registration.
    /// </summary>
    Registration,

    /// <summary>
    /// Triggered on email verification.
    /// </summary>
    EmailVerification,

    /// <summary>
    /// Triggered on account activation.
    /// </summary>
    AccountActivation,

    /// <summary>
    /// Manually triggered by admin.
    /// </summary>
    Manual
}
