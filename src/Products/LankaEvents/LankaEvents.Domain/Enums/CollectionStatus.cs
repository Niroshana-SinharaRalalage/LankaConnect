namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Status of a collection (event fund) contribution payment lifecycle.
///
/// Lifecycle:
/// 1. Contributor initiates contribution -> Status = Pending, Stripe checkout created
/// 2. Contributor completes payment -> Status = Completed (via webhook)
///
/// Alternative paths:
/// - Payment fails -> Status = Failed
/// - Checkout expires (24h) -> Status = Abandoned
/// - Payment refunded -> Status = Refunded
/// </summary>
public enum CollectionStatus
{
    /// <summary>
    /// Collection created, awaiting payment completion.
    /// Stripe checkout session has been created.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment received via Stripe webhook. Final successful state.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Payment failed or was declined by Stripe.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Stripe checkout session expired (24 hours) without payment.
    /// </summary>
    Abandoned = 3,

    /// <summary>
    /// Payment was refunded after completion.
    /// </summary>
    Refunded = 4
}
