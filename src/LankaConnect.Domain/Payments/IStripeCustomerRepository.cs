namespace LankaConnect.Domain.Payments;

/// <summary>
/// Repository interface for Stripe customer sync data
/// Phase 6A.4: Stripe Payment Integration - MVP
/// Note: This is an Application layer concern, moved to Application.Common.Interfaces
/// Domain layer should not define repository interfaces for infrastructure entities
/// </summary>
public interface IStripeCustomerRepository
{
    /// <summary>
    /// Get Stripe customer ID by UserId
    /// </summary>
    Task<string?> GetStripeCustomerIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if Stripe customer exists for user
    /// </summary>
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update Stripe customer record
    /// </summary>
    Task SaveStripeCustomerAsync(
        Guid userId,
        string stripeCustomerId,
        string email,
        string name,
        DateTime stripeCreatedAt,
        CancellationToken cancellationToken = default);
}
