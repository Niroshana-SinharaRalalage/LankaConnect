namespace LankaConnect.Modules.Notifications.Contracts;

/// <summary>
/// Cross-module wire-format enum identifying the kind of in-app notification
/// being published or requested. Mirrors <c>LankaConnect.Modules.Notifications.Domain.Enums.NotificationType</c>
/// 1-for-1 by ordinal value — intentionally duplicated to decouple the
/// cross-module ABI from internal domain evolution (renames or additions on
/// the Domain side must not require recompiling consumer modules).
/// </summary>
/// <remarks>
/// Adding a new value here is an additive contract change (safe). Removing a
/// value or renaming an existing one is a breaking change and requires a
/// <c>V2</c> sibling event class — see the
/// <see cref="LankaConnect.BuildingBlocks.Contracts.IntegrationEvents.IIntegrationEventV1"/>
/// versioning convention.
/// </remarks>
public enum NotificationKind
{
    /// <summary>Role upgrade request has been approved by admin.</summary>
    RoleUpgradeApproved = 1,

    /// <summary>Role upgrade request has been rejected by admin.</summary>
    RoleUpgradeRejected = 2,

    /// <summary>Free trial is expiring soon.</summary>
    FreeTrialExpiring = 3,

    /// <summary>Free trial has expired.</summary>
    FreeTrialExpired = 4,

    /// <summary>Subscription payment succeeded.</summary>
    SubscriptionPaymentSucceeded = 5,

    /// <summary>Subscription payment failed.</summary>
    SubscriptionPaymentFailed = 6,

    /// <summary>General system notification.</summary>
    System = 7,

    /// <summary>Event-related notification.</summary>
    Event = 8,
}
