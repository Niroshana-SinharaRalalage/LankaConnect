namespace LankaConnect.Domain.Common.Enums;

/// <summary>
/// Subscription tier classification for individual and family users
/// Defines access levels and feature availability for different subscription plans
/// </summary>
public enum SubscriptionTier
{
    /// <summary>
    /// Free tier - basic access with limited features
    /// </summary>
    Free = 1,

    /// <summary>
    /// Basic tier - essential features for individual users
    /// </summary>
    Basic = 2,

    /// <summary>
    /// Standard tier - enhanced features for regular users
    /// </summary>
    Standard = 3,

    /// <summary>
    /// Premium tier - full feature access for power users
    /// </summary>
    Premium = 4,

    /// <summary>
    /// Family tier - multi-user access for families
    /// </summary>
    Family = 5,

    /// <summary>
    /// Community tier - features for community leaders
    /// </summary>
    Community = 6,

    /// <summary>
    /// Cultural ambassador tier - special features for cultural ambassadors
    /// </summary>
    CulturalAmbassador = 7,

    /// <summary>
    /// Lifetime tier - one-time payment with permanent access
    /// </summary>
    Lifetime = 8,

    /// <summary>
    /// Student tier - discounted access for students
    /// </summary>
    Student = 9,

    /// <summary>
    /// Senior tier - specialized features for senior community members
    /// </summary>
    Senior = 10,

    /// <summary>
    /// Trial tier - temporary full access for evaluation
    /// </summary>
    Trial = 11
}