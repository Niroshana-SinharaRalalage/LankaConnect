namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Types of business notifications.
/// Mirrors BusinessNotificationType in Application layer for Shared project use.
/// </summary>
public enum BusinessNotificationType
{
    BusinessCreated = 1,
    BusinessUpdated = 2,
    BusinessApproved = 3,
    BusinessRejected = 4,
    BusinessSuspended = 5,
    BusinessReactivated = 6,
    NewReview = 7,
    ReviewResponse = 8,
    ServiceAdded = 9,
    ServiceUpdated = 10,
    PaymentReceived = 11,
    PaymentFailed = 12,
    SubscriptionExpiring = 13,
    SubscriptionRenewed = 14,
    PerformanceReport = 15
}

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for business notification emails.
/// Supports 15+ notification types with different templates and parameters.
///
/// Templates: business-created, business-approved, business-rejected, business-suspended,
///            business-new-review, business-payment-received, business-performance-report, etc.
///
/// This replaces Dictionary&lt;string, object&gt; in SendBusinessNotificationCommandHandler with
/// compile-time type-safe parameters.
/// </summary>
public class BusinessNotificationEmailParams : IEmailParameters
{
    private string _templateName = "business-notification-default";

    /// <summary>
    /// The template name for business notification email.
    /// Dynamically set based on notification type.
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
    /// User identifier (business owner).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's display name.
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
    /// Business identifier.
    /// </summary>
    public Guid BusinessId { get; set; }

    /// <summary>
    /// Business name.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Business category.
    /// </summary>
    public string BusinessCategory { get; set; } = string.Empty;

    /// <summary>
    /// Business status.
    /// </summary>
    public string BusinessStatus { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification.
    /// </summary>
    public BusinessNotificationType NotificationType { get; set; }

    /// <summary>
    /// Email subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    #endregion

    #region Standard Properties

    /// <summary>
    /// Company name for branding.
    /// </summary>
    public string CompanyName { get; set; } = "LankaConnect";

    /// <summary>
    /// Notification date/time.
    /// </summary>
    public DateTime NotificationDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Dashboard URL.
    /// </summary>
    public string DashboardUrl { get; set; } = "https://lankaconnect.com/dashboard";

    /// <summary>
    /// Business URL.
    /// </summary>
    public string BusinessUrl { get; set; } = string.Empty;

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "support@lankaconnect.com";

    #endregion

    #region Notification-Specific Flags

    /// <summary>
    /// Whether this is an approval notification.
    /// </summary>
    public bool IsApproval { get; set; }

    /// <summary>
    /// Whether this is a rejection notification.
    /// </summary>
    public bool IsRejection { get; set; }

    /// <summary>
    /// Whether this is a suspension notification.
    /// </summary>
    public bool IsSuspension { get; set; }

    /// <summary>
    /// Whether this is a critical notification (high priority).
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// Whether this is a report notification.
    /// </summary>
    public bool IsReport { get; set; }

    /// <summary>
    /// Whether this notification requires action.
    /// </summary>
    public bool RequiresAction { get; set; }

    /// <summary>
    /// Next steps instructions.
    /// </summary>
    public string? NextSteps { get; set; }

    /// <summary>
    /// Action URL (e.g., review page).
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Report URL (e.g., analytics page).
    /// </summary>
    public string? ReportUrl { get; set; }

    #endregion

    #region Dynamic Data

    /// <summary>
    /// Additional dynamic data for the template.
    /// Used for notification-specific parameters passed via request.Data.
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }

    #endregion

    #region Template Type Methods

    /// <summary>
    /// Sets the appropriate template based on notification type.
    /// </summary>
    public BusinessNotificationEmailParams ForNotificationType(BusinessNotificationType notificationType)
    {
        NotificationType = notificationType;
        _templateName = GetTemplateNameForNotificationType(notificationType);
        ApplyNotificationSpecificParameters(notificationType);
        return this;
    }

    /// <summary>
    /// Gets the template name for a notification type.
    /// </summary>
    private static string GetTemplateNameForNotificationType(BusinessNotificationType notificationType)
    {
        return notificationType switch
        {
            BusinessNotificationType.BusinessCreated => "business-created",
            BusinessNotificationType.BusinessUpdated => "business-updated",
            BusinessNotificationType.BusinessApproved => "business-approved",
            BusinessNotificationType.BusinessRejected => "business-rejected",
            BusinessNotificationType.BusinessSuspended => "business-suspended",
            BusinessNotificationType.BusinessReactivated => "business-reactivated",
            BusinessNotificationType.NewReview => "business-new-review",
            BusinessNotificationType.ReviewResponse => "business-review-response",
            BusinessNotificationType.ServiceAdded => "business-service-added",
            BusinessNotificationType.ServiceUpdated => "business-service-updated",
            BusinessNotificationType.PaymentReceived => "business-payment-received",
            BusinessNotificationType.PaymentFailed => "business-payment-failed",
            BusinessNotificationType.SubscriptionExpiring => "business-subscription-expiring",
            BusinessNotificationType.SubscriptionRenewed => "business-subscription-renewed",
            BusinessNotificationType.PerformanceReport => "business-performance-report",
            _ => "business-notification-default"
        };
    }

    /// <summary>
    /// Applies notification-specific parameters based on type.
    /// </summary>
    private void ApplyNotificationSpecificParameters(BusinessNotificationType notificationType)
    {
        switch (notificationType)
        {
            case BusinessNotificationType.BusinessApproved:
                IsApproval = true;
                NextSteps = "Your business is now live on LankaConnect!";
                break;
            case BusinessNotificationType.BusinessRejected:
                IsRejection = true;
                NextSteps = "Please review and update your business information.";
                break;
            case BusinessNotificationType.BusinessSuspended:
                IsSuspension = true;
                IsCritical = true;
                break;
            case BusinessNotificationType.NewReview:
                RequiresAction = true;
                ActionUrl = $"https://lankaconnect.com/business/{BusinessId}/reviews";
                break;
            case BusinessNotificationType.PerformanceReport:
                IsReport = true;
                ReportUrl = $"https://lankaconnect.com/business/{BusinessId}/analytics";
                break;
            case BusinessNotificationType.PaymentFailed:
            case BusinessNotificationType.SubscriptionExpiring:
                IsCritical = true;
                RequiresAction = true;
                break;
        }
    }

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            // Core properties
            { EmailTemplateContract.Common.UserName, UserName },
            { "FirstName", FirstName },
            { "UserEmail", UserEmail },
            { EmailTemplateContract.Business.BusinessName, BusinessName },
            { EmailTemplateContract.Business.BusinessId, BusinessId.ToString() },
            { EmailTemplateContract.Business.BusinessCategory, BusinessCategory },
            { EmailTemplateContract.Business.BusinessStatus, BusinessStatus },
            { EmailTemplateContract.Business.NotificationType, NotificationType.ToString() },
            { "Subject", Subject },

            // Standard properties
            { EmailTemplateContract.Common.CompanyName, CompanyName },
            { EmailTemplateContract.Business.NotificationDate, NotificationDate.ToString("yyyy-MM-dd HH:mm:ss UTC") },
            { "DashboardUrl", DashboardUrl },
            { EmailTemplateContract.Business.BusinessUrl, BusinessUrl },
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year },

            // Notification-specific flags
            { EmailTemplateContract.Business.IsApproval, IsApproval },
            { EmailTemplateContract.Business.IsRejection, IsRejection },
            { EmailTemplateContract.Business.IsSuspension, IsSuspension },
            { EmailTemplateContract.Business.IsCritical, IsCritical },
            { EmailTemplateContract.Business.IsReport, IsReport },
            { EmailTemplateContract.Business.RequiresAction, RequiresAction }
        };

        // Add optional properties
        if (!string.IsNullOrEmpty(NextSteps))
        {
            dict[EmailTemplateContract.Business.NextSteps] = NextSteps;
        }

        if (!string.IsNullOrEmpty(ActionUrl))
        {
            dict[EmailTemplateContract.Business.ActionUrl] = ActionUrl;
        }

        if (!string.IsNullOrEmpty(ReportUrl))
        {
            dict[EmailTemplateContract.Business.ReportUrl] = ReportUrl;
        }

        // Add any additional dynamic data
        if (AdditionalData != null)
        {
            foreach (var kvp in AdditionalData)
            {
                dict[kvp.Key] = kvp.Value;
            }
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

        if (BusinessId == Guid.Empty)
            errors.Add("BusinessId is required");

        if (string.IsNullOrWhiteSpace(BusinessName))
            errors.Add("BusinessName is required");

        if (string.IsNullOrWhiteSpace(Subject))
            errors.Add("Subject is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new BusinessNotificationEmailParams for the specified notification type.
    /// </summary>
    public static BusinessNotificationEmailParams Create(
        BusinessNotificationType notificationType,
        Guid userId,
        string userName,
        string userEmail,
        string firstName,
        Guid businessId,
        string businessName,
        string businessCategory,
        string businessStatus,
        string subject,
        Dictionary<string, object>? additionalData = null)
    {
        var emailParams = new BusinessNotificationEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            FirstName = firstName,
            BusinessId = businessId,
            BusinessName = businessName,
            BusinessCategory = businessCategory,
            BusinessStatus = businessStatus,
            Subject = subject,
            BusinessUrl = $"https://lankaconnect.com/business/{businessId}",
            AdditionalData = additionalData
        };

        return emailParams.ForNotificationType(notificationType);
    }

    /// <summary>
    /// Creates a BusinessNotificationEmailParams for business approval.
    /// </summary>
    public static BusinessNotificationEmailParams CreateForApproval(
        Guid userId,
        string userName,
        string userEmail,
        string firstName,
        Guid businessId,
        string businessName,
        string businessCategory)
    {
        return Create(
            BusinessNotificationType.BusinessApproved,
            userId, userName, userEmail, firstName,
            businessId, businessName, businessCategory, "Approved",
            "Your business has been approved!");
    }

    /// <summary>
    /// Creates a BusinessNotificationEmailParams for business rejection.
    /// </summary>
    public static BusinessNotificationEmailParams CreateForRejection(
        Guid userId,
        string userName,
        string userEmail,
        string firstName,
        Guid businessId,
        string businessName,
        string businessCategory,
        string rejectionReason)
    {
        return Create(
            BusinessNotificationType.BusinessRejected,
            userId, userName, userEmail, firstName,
            businessId, businessName, businessCategory, "Rejected",
            "Your business application requires attention",
            new Dictionary<string, object> { { "RejectionReason", rejectionReason } });
    }

    /// <summary>
    /// Creates a BusinessNotificationEmailParams for business suspension.
    /// </summary>
    public static BusinessNotificationEmailParams CreateForSuspension(
        Guid userId,
        string userName,
        string userEmail,
        string firstName,
        Guid businessId,
        string businessName,
        string businessCategory,
        string suspensionReason)
    {
        return Create(
            BusinessNotificationType.BusinessSuspended,
            userId, userName, userEmail, firstName,
            businessId, businessName, businessCategory, "Suspended",
            "Important: Your business has been suspended",
            new Dictionary<string, object> { { "SuspensionReason", suspensionReason } });
    }

    /// <summary>
    /// Creates a BusinessNotificationEmailParams for new review notification.
    /// </summary>
    public static BusinessNotificationEmailParams CreateForNewReview(
        Guid userId,
        string userName,
        string userEmail,
        string firstName,
        Guid businessId,
        string businessName,
        string businessCategory,
        string reviewerName,
        int rating)
    {
        return Create(
            BusinessNotificationType.NewReview,
            userId, userName, userEmail, firstName,
            businessId, businessName, businessCategory, "Active",
            "You have a new review!",
            new Dictionary<string, object>
            {
                { "ReviewerName", reviewerName },
                { "Rating", rating }
            });
    }

    /// <summary>
    /// Creates a BusinessNotificationEmailParams for performance report.
    /// </summary>
    public static BusinessNotificationEmailParams CreateForPerformanceReport(
        Guid userId,
        string userName,
        string userEmail,
        string firstName,
        Guid businessId,
        string businessName,
        string businessCategory,
        string reportPeriod,
        Dictionary<string, object>? reportData = null)
    {
        var data = reportData ?? new Dictionary<string, object>();
        data["ReportPeriod"] = reportPeriod;

        return Create(
            BusinessNotificationType.PerformanceReport,
            userId, userName, userEmail, firstName,
            businessId, businessName, businessCategory, "Active",
            $"Your {reportPeriod} Performance Report",
            data);
    }

    #endregion
}