namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Status of a sponsorship lifecycle.
///
/// Money sponsors follow the same payment lifecycle as donations:
/// 1. Sponsor initiates money sponsorship -> Status = Pending, Stripe checkout created
/// 2. Sponsor completes payment -> Status = Completed (via webhook)
///
/// Item sponsors skip payment entirely:
/// 1. Sponsor records item sponsorship -> Status = RecordedItem (immediate, no Stripe)
///
/// Alternative paths (money sponsors only):
/// - Payment fails -> Status = Failed
/// - Checkout expires (24h) -> Status = Abandoned
/// - Payment refunded -> Status = Refunded
/// </summary>
public enum SponsorStatus
{
    /// <summary>
    /// Money sponsorship created, awaiting payment completion.
    /// Stripe checkout session has been created.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Money sponsorship payment received via Stripe webhook. Final successful state.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Payment failed or was declined by Stripe (money sponsors only).
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Stripe checkout session expired (24 hours) without payment (money sponsors only).
    /// </summary>
    Abandoned = 3,

    /// <summary>
    /// Payment was refunded after completion (money sponsors only).
    /// </summary>
    Refunded = 4,

    /// <summary>
    /// Item sponsorship recorded immediately without payment.
    /// This is the terminal successful state for item-based sponsors.
    /// </summary>
    RecordedItem = 5
}
