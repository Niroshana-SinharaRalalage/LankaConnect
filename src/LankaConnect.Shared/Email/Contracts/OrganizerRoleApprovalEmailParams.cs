namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for organizer role approval emails.
/// Template: template-organizer-role-approval
///
/// This replaces Dictionary&lt;string, object&gt; in ApproveRoleUpgradeCommandHandler with
/// compile-time type-safe parameters.
///
/// Sent to user when their Event Organizer role upgrade request is approved.
/// </summary>
public class OrganizerRoleApprovalEmailParams : IEmailParameters
{
    #region Template Selection

    /// <summary>
    /// The template name for organizer role approval.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.OrganizerRoleApproval;

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

    #endregion

    #region Approval Properties

    /// <summary>
    /// Timestamp when the role was approved (formatted).
    /// </summary>
    public string ApprovedAt { get; set; } = string.Empty;

    /// <summary>
    /// URL to the user's dashboard.
    /// </summary>
    public string DashboardUrl { get; set; } = string.Empty;

    #endregion

    #region Common Properties

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
            { EmailTemplateContract.Common.UserName, UserName },
            { "ApprovedAt", ApprovedAt },
            { "DashboardUrl", DashboardUrl },
            { EmailTemplateContract.Common.Year, Year }
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

        if (string.IsNullOrWhiteSpace(ApprovedAt))
            errors.Add("ApprovedAt is required");

        if (string.IsNullOrWhiteSpace(DashboardUrl))
            errors.Add("DashboardUrl is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new OrganizerRoleApprovalEmailParams with required fields.
    /// </summary>
    public static OrganizerRoleApprovalEmailParams Create(
        Guid userId,
        string recipientEmail,
        string userName,
        string dashboardUrl)
    {
        return new OrganizerRoleApprovalEmailParams
        {
            UserId = userId,
            RecipientEmail = recipientEmail,
            UserName = userName,
            ApprovedAt = DateTime.UtcNow.ToString("MMMM dd, yyyy h:mm tt"),
            DashboardUrl = dashboardUrl,
            Year = DateTime.UtcNow.Year
        };
    }

    #endregion
}
