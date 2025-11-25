using LankaConnect.Domain.Common;

namespace LankaConnect.Infrastructure.Payments.Entities;

/// <summary>
/// Infrastructure entity to track Stripe customer data
/// This is an infrastructure concern, not part of the core domain
/// </summary>
public class StripeCustomer : BaseEntity
{
    /// <summary>
    /// Reference to the User entity
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Stripe customer ID (cus_xxx)
    /// </summary>
    public string StripeCustomerId { get; private set; } = string.Empty;

    /// <summary>
    /// Customer email in Stripe
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Customer name in Stripe
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// When the customer was created in Stripe
    /// </summary>
    public DateTime StripeCreatedAt { get; private set; }

    // EF Core constructor
    private StripeCustomer()
    {
    }

    private StripeCustomer(
        Guid userId,
        string stripeCustomerId,
        string email,
        string name,
        DateTime stripeCreatedAt)
    {
        UserId = userId;
        StripeCustomerId = stripeCustomerId;
        Email = email;
        Name = name;
        StripeCreatedAt = stripeCreatedAt;
    }

    public static StripeCustomer Create(
        Guid userId,
        string stripeCustomerId,
        string email,
        string name,
        DateTime stripeCreatedAt)
    {
        return new StripeCustomer(userId, stripeCustomerId, email, name, stripeCreatedAt);
    }
}
