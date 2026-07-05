using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Domain;

public interface IRegistrationRepository : IRepository<Registration>
{
    /// <summary>
    /// Gets all registrations for an event.
    /// Phase 6A.93: Added trackChanges parameter for write operations (e.g., auto-refund processing).
    /// When trackChanges is true, entities are tracked by EF Core and domain events are dispatched on CommitAsync.
    /// </summary>
    /// <param name="eventId">The event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="trackChanges">If true, entities are tracked by EF Core (required for write operations)</param>
    Task<IReadOnlyList<Registration>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default, bool trackChanges = false);
    Task<IReadOnlyList<Registration>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Registration?> GetByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Registration>> GetByStatusAsync(RegistrationStatus status, CancellationToken cancellationToken = default);
    Task<int> GetTotalQuantityForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.24: Gets an anonymous registration by event ID and contact email
    /// Used to fetch registration details for anonymous users' confirmation emails
    /// </summary>
    Task<Registration?> GetAnonymousByEventAndEmailAsync(Guid eventId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.X: Gets a registration by Stripe PaymentIntentId.
    /// Used by charge.refunded webhook handler as fallback when refund metadata is missing.
    /// </summary>
    Task<Registration?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 7G — returns registrations stuck in <see cref="RegistrationStatus.RefundRequested"/>
    /// whose <see cref="Registration.RefundRequestedAt"/> is older than
    /// <paramref name="requestedBefore"/>. Used by the refund-reconciliation
    /// safety net to detect rows where the <c>charge.refunded</c> webhook was
    /// missed (typically because the API container restarted mid-delivery).
    /// Entities are loaded WITH change-tracking so the caller can complete the
    /// state transition and persist via the existing <c>IUnitOfWork</c>.
    /// </summary>
    Task<IReadOnlyList<Registration>> GetStuckRefundsAsync(
        DateTime requestedBefore,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.148.W5.5.D6.5 — returns registrations stuck in
    /// <see cref="RegistrationStatus.Cancelled"/> whose workflow ticket-line refund
    /// has settled at Stripe but whose registration row was never transitioned to
    /// <see cref="RegistrationStatus.Refunded"/>. Operator-UAT pattern:
    /// <c>r.Status='Cancelled' AND r.RefundCompletedAt IS NULL AND r.StripeRefundId IS NULL</c>
    /// AND there exists a <c>RefundRequestLineItem</c> for this registration with
    /// <c>type=Ticket(0), status=Refunded(4), stripe_refund_id NOT NULL,
    /// processed_at &lt; threshold</c>.
    ///
    /// Returns tracked entities + the matching ticket-line <c>StripeRefundId</c>
    /// so the reconciler can call
    /// <see cref="Registration.CompleteRefundFromCancelled"/> per row.
    /// </summary>
    Task<IReadOnlyList<(Registration Registration, string StripeRefundId)>>
        GetStuckCancelledWithRefundedTicketAsync(
            DateTime processedBefore,
            int take,
            CancellationToken cancellationToken = default);
}