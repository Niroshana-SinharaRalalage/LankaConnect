namespace LankaConnect.Domain.Notifications.Enums;

/// <summary>
/// Types of notifications that can be sent to users
/// Phase 6A.6: Notification System
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Role upgrade request has been approved by admin
    /// </summary>
    RoleUpgradeApproved = 1,

    /// <summary>
    /// Role upgrade request has been rejected by admin
    /// </summary>
    RoleUpgradeRejected = 2,

    /// <summary>
    /// Free trial is expiring soon (7 days, 3 days, 1 day warnings)
    /// </summary>
    FreeTrialExpiring = 3,

    /// <summary>
    /// Free trial has expired
    /// </summary>
    FreeTrialExpired = 4,

    /// <summary>
    /// Subscription payment succeeded
    /// </summary>
    SubscriptionPaymentSucceeded = 5,

    /// <summary>
    /// Subscription payment failed
    /// </summary>
    SubscriptionPaymentFailed = 6,

    /// <summary>
    /// General system notification
    /// </summary>
    System = 7,

    /// <summary>
    /// Event-related notification (future use)
    /// </summary>
    Event = 8
}
