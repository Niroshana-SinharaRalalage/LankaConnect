namespace LankaConnect.Domain.Users.Enums;

/// <summary>
/// Subscription status for Event Organizer accounts
/// Phase 6A.1: Subscription management for role-based pricing
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// No subscription (General User)
    /// </summary>
    None = 0,

    /// <summary>
    /// Free trial period active (6 months for Event Organizer)
    /// </summary>
    Trialing = 1,

    /// <summary>
    /// Active paid subscription
    /// </summary>
    Active = 2,

    /// <summary>
    /// Payment past due - grace period
    /// </summary>
    PastDue = 3,

    /// <summary>
    /// Subscription canceled by user
    /// </summary>
    Canceled = 4,

    /// <summary>
    /// Free trial or subscription expired
    /// </summary>
    Expired = 5
}

public static class SubscriptionStatusExtensions
{
    public static string ToDisplayName(this SubscriptionStatus status)
    {
        return status switch
        {
            SubscriptionStatus.None => "No Subscription",
            SubscriptionStatus.Trialing => "Free Trial",
            SubscriptionStatus.Active => "Active",
            SubscriptionStatus.PastDue => "Past Due",
            SubscriptionStatus.Canceled => "Canceled",
            SubscriptionStatus.Expired => "Expired",
            _ => status.ToString()
        };
    }

    public static bool CanCreateEvents(this SubscriptionStatus status)
    {
        return status == SubscriptionStatus.Trialing || status == SubscriptionStatus.Active;
    }

    public static bool RequiresPayment(this SubscriptionStatus status)
    {
        return status == SubscriptionStatus.PastDue || status == SubscriptionStatus.Expired;
    }

    public static bool IsActive(this SubscriptionStatus status)
    {
        return status == SubscriptionStatus.Trialing || status == SubscriptionStatus.Active;
    }
}
