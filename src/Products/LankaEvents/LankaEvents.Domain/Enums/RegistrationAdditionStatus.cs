namespace LankaConnect.Products.LankaEvents.Domain.Enums;

/// <summary>
/// Status of a pending attendee addition to an existing registration.
/// Part of the Add-Only Attendees with Delta Payment feature.
///
/// Lifecycle:
/// 1. User initiates addition -> Status = Pending, Stripe checkout created
/// 2. User completes payment -> Status = PaymentCompleted (via webhook)
/// 3. System merges into registration -> Status = Merged
///
/// Alternative paths:
/// - Payment fails -> Status = Failed
/// - Checkout expires (24h) -> Status = Abandoned
/// - User cancels before payment -> Record deleted
/// </summary>
public enum RegistrationAdditionStatus
{
    /// <summary>
    /// Addition created, awaiting payment completion.
    /// Stripe checkout session has been created.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment received via Stripe webhook, not yet merged into registration.
    /// This is a transient state during the merge operation.
    /// </summary>
    PaymentCompleted = 1,

    /// <summary>
    /// New attendees have been successfully merged into the registration.
    /// This is the final successful state.
    /// </summary>
    Merged = 2,

    /// <summary>
    /// Payment failed or was declined by Stripe.
    /// User can retry with a new addition request.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Stripe checkout session expired (24 hours) without payment.
    /// User can retry with a new addition request.
    /// </summary>
    Abandoned = 4
}
