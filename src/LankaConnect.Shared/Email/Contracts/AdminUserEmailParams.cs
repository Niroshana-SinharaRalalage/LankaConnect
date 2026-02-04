namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 5: Template-specific typed parameters for admin user management emails.
/// Templates: template-account-locked-by-admin, template-account-unlocked-by-admin,
///            template-account-activated-by-admin, template-account-deactivated-by-admin,
///            template-organizer-role-approval
///
/// This replaces Dictionary&lt;string, object&gt; in admin command handlers with
/// compile-time type-safe parameters.
/// </summary>
public class AdminUserEmailParams : IEmailParameters
{
    private string _templateName = "template-account-locked-by-admin";

    /// <summary>
    /// The template name for admin user email.
    /// </summary>
    public string TemplateName => _templateName;

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
    /// Admin identifier who performed the action.
    /// </summary>
    public Guid AdminId { get; set; }

    /// <summary>
    /// Admin name.
    /// </summary>
    public string AdminName { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the action.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Date/time of action.
    /// </summary>
    public DateTime ActionDate { get; set; }

    /// <summary>
    /// Type of action performed (for logging).
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Whether user can appeal the decision.
    /// </summary>
    public bool CanAppeal { get; set; }

    /// <summary>
    /// Appeal instructions if applicable.
    /// </summary>
    public string AppealInstructions { get; set; } = string.Empty;

    /// <summary>
    /// Company name for branding.
    /// </summary>
    public string CompanyName { get; set; } = "LankaConnect";

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "support@lankaconnect.com";

    /// <summary>
    /// Login URL for the application.
    /// </summary>
    public string LoginUrl { get; set; } = "https://lankaconnect.com/login";

    /// <summary>
    /// Date/time until which account is locked (for locked accounts).
    /// </summary>
    public DateTime? LockUntil { get; set; }

    #endregion

    #region Template Type Methods

    /// <summary>
    /// Sets the template to use for account locked emails.
    /// </summary>
    public AdminUserEmailParams AsAccountLocked()
    {
        _templateName = "template-account-locked-by-admin";
        ActionType = "Account Locked";
        return this;
    }

    /// <summary>
    /// Sets the template to use for account unlocked emails.
    /// </summary>
    public AdminUserEmailParams AsAccountUnlocked()
    {
        _templateName = "template-account-unlocked-by-admin";
        ActionType = "Account Unlocked";
        return this;
    }

    /// <summary>
    /// Sets the template to use for account activated emails.
    /// </summary>
    public AdminUserEmailParams AsAccountActivated()
    {
        _templateName = "template-account-activated-by-admin";
        ActionType = "Account Activated";
        return this;
    }

    /// <summary>
    /// Sets the template to use for account deactivated emails.
    /// </summary>
    public AdminUserEmailParams AsAccountDeactivated()
    {
        _templateName = "template-account-deactivated-by-admin";
        ActionType = "Account Deactivated";
        return this;
    }

    /// <summary>
    /// Sets the template to use for organizer role approval emails.
    /// </summary>
    public AdminUserEmailParams AsOrganizerRoleApproval()
    {
        _templateName = "template-organizer-role-approval";
        ActionType = "Organizer Role Approved";
        return this;
    }

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Phase 6A.87+ Fix: Added LockUntil and Year for footer.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            { "UserName", UserName },
            { "UserEmail", UserEmail },
            { "AdminName", AdminName },
            { "Reason", Reason },
            { "ActionDate", ActionDate.ToString("MMMM dd, yyyy h:mm tt") },
            { "ActionType", ActionType },
            { "CanAppeal", CanAppeal },
            { "AppealInstructions", AppealInstructions },
            { "CompanyName", CompanyName },
            { "SupportEmail", SupportEmail },
            { "LoginUrl", LoginUrl },
            { "Year", DateTime.UtcNow.Year }  // Footer param
        };

        // Add LockUntil for locked account templates
        if (LockUntil.HasValue)
        {
            dict["LockUntil"] = LockUntil.Value.ToString("MMMM dd, yyyy h:mm tt");
            dict["HasLockExpiration"] = true;
        }
        else
        {
            dict["HasLockExpiration"] = false;
        }

        return dict;
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

        if (AdminId == Guid.Empty)
            errors.Add("AdminId is required");

        if (ActionDate == default)
            errors.Add("ActionDate is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new AdminUserEmailParams for account locked notification.
    /// </summary>
    public static AdminUserEmailParams CreateAccountLocked(
        Guid userId,
        string userName,
        string userEmail,
        Guid adminId,
        string adminName,
        string reason,
        DateTime actionDate,
        bool canAppeal = true,
        string? appealInstructions = null)
    {
        return new AdminUserEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            AdminId = adminId,
            AdminName = adminName,
            Reason = reason,
            ActionDate = actionDate,
            CanAppeal = canAppeal,
            AppealInstructions = appealInstructions ?? "To appeal this decision, please contact support@lankaconnect.com with your account details."
        }.AsAccountLocked();
    }

    /// <summary>
    /// Creates a new AdminUserEmailParams for account unlocked notification.
    /// </summary>
    public static AdminUserEmailParams CreateAccountUnlocked(
        Guid userId,
        string userName,
        string userEmail,
        Guid adminId,
        string adminName,
        DateTime actionDate)
    {
        return new AdminUserEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            AdminId = adminId,
            AdminName = adminName,
            ActionDate = actionDate
        }.AsAccountUnlocked();
    }

    /// <summary>
    /// Creates a new AdminUserEmailParams for account activated notification.
    /// </summary>
    public static AdminUserEmailParams CreateAccountActivated(
        Guid userId,
        string userName,
        string userEmail,
        Guid adminId,
        string adminName,
        DateTime actionDate)
    {
        return new AdminUserEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            AdminId = adminId,
            AdminName = adminName,
            ActionDate = actionDate
        }.AsAccountActivated();
    }

    /// <summary>
    /// Creates a new AdminUserEmailParams for account deactivated notification.
    /// </summary>
    public static AdminUserEmailParams CreateAccountDeactivated(
        Guid userId,
        string userName,
        string userEmail,
        Guid adminId,
        string adminName,
        string reason,
        DateTime actionDate,
        bool canAppeal = true)
    {
        return new AdminUserEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            AdminId = adminId,
            AdminName = adminName,
            Reason = reason,
            ActionDate = actionDate,
            CanAppeal = canAppeal
        }.AsAccountDeactivated();
    }

    /// <summary>
    /// Creates a new AdminUserEmailParams for organizer role approval notification.
    /// </summary>
    public static AdminUserEmailParams CreateOrganizerRoleApproval(
        Guid userId,
        string userName,
        string userEmail,
        Guid adminId,
        string adminName,
        DateTime actionDate)
    {
        return new AdminUserEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            AdminId = adminId,
            AdminName = adminName,
            ActionDate = actionDate
        }.AsOrganizerRoleApproval();
    }

    #endregion
}
