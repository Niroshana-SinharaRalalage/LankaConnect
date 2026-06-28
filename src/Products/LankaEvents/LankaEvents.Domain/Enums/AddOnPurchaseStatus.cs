namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Status of an add-on purchase payment lifecycle.
///
/// Lifecycle:
/// 1. Buyer initiates purchase -> Status = Pending, Stripe checkout created
/// 2. Buyer completes payment -> Status = Completed (via webhook)
///
/// Alternative paths:
/// - Payment fails -> Status = Failed
/// - Checkout expires (24h) -> Status = Abandoned (stock restored)
/// - Payment refunded -> Status = Refunded (stock restored)
/// </summary>
public enum AddOnPurchaseStatus
{
    /// <summary>
    /// Purchase created, awaiting payment completion.
    /// Stripe checkout session has been created. Stock has been reserved.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment received via Stripe webhook. Final successful state.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Payment failed or was declined by Stripe.
    /// Reserved stock should be restored.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Stripe checkout session expired (24 hours) without payment.
    /// Reserved stock should be restored.
    /// </summary>
    Abandoned = 3,

    /// <summary>
    /// Payment was refunded after completion.
    /// Stock should be restored.
    /// </summary>
    Refunded = 4
}
