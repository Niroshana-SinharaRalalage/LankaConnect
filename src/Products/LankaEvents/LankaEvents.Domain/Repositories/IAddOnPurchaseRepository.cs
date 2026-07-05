using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Domain.Repositories;

/// <summary>
/// Repository interface for AddOnPurchase operations.
/// </summary>
public interface IAddOnPurchaseRepository : IRepository<AddOnPurchase>
{
    Task<AddOnPurchase?> GetByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ALL purchases sharing the same checkout session (cart purchases).
    /// Used by webhook handler to complete/expire all purchases in a cart.
    /// </summary>
    Task<IReadOnlyList<AddOnPurchase>> GetAllByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnPurchase>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnPurchase>> GetByDefinitionIdAsync(
        Guid definitionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnPurchase>> GetCompletedByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalPurchasesForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnPurchase>> GetByUserIdAndEventIdAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnPurchase>> GetExpiredPendingPurchasesAsync(
        DateTime cutoffTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all purchases for a specific buyer email and event (public lookup).
    /// Returns completed and pending purchases so buyers can see their order status.
    /// </summary>
    Task<IReadOnlyList<AddOnPurchase>> GetByBuyerEmailAndEventIdAsync(
        string buyerEmail,
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.148.W4.D11: returns ALL purchases sharing the given Stripe PaymentIntent.
    /// For cart purchases, N AddOnPurchase rows share the same PI; the refund webhook
    /// uses this lookup to mark every row Refunded (legacy semantics) or to narrow down
    /// the correct row via the workflow line-item's ReferenceId.
    /// </summary>
    Task<IReadOnlyList<AddOnPurchase>> GetAllByStripePaymentIntentIdAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);
}
