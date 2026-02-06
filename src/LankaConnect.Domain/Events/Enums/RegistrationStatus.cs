namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Phase 6A.81: Updated to support Three-State Registration Lifecycle for payment security
/// </summary>
public enum RegistrationStatus
{
    /// <summary>
    /// Phase 6A.81: NEW - Temporary state while waiting for payment confirmation
    /// - Does NOT consume event capacity
    /// - Does NOT block email from re-registering
    /// - Auto-expires after 25 hours (Stripe checkout expires at 24h)
    /// - Used for paid events only
    /// </summary>
    Preliminary = 0,

    /// <summary>
    /// DEPRECATED: Use Preliminary instead for backward compatibility
    /// </summary>
    [Obsolete("Use Preliminary instead. Will be removed in future version.")]
    Pending = 1,

    /// <summary>
    /// Payment completed (for paid events) OR registration completed (for free events)
    /// - Consumes event capacity
    /// - Blocks email from re-registering
    /// - Triggers confirmation email
    /// </summary>
    Confirmed = 2,

    Waitlisted = 3,
    CheckedIn = 4,

    /// <summary>
    /// Event attendance completed - user attended the event
    /// </summary>
    Attended = 5,

    /// <summary>
    /// DEPRECATED: Use Attended instead for clarity
    /// </summary>
    [Obsolete("Use Attended instead. Will be removed in future version.")]
    Completed = 5,  // Same value as Attended for backward compatibility

    Cancelled = 6,

    /// <summary>
    /// Phase 6A.81: Kept for backward compatibility with existing refunded registrations
    /// </summary>
    Refunded = 7,

    /// <summary>
    /// Phase 6A.81: NEW - Stripe checkout session expired or user never completed payment
    /// - Does NOT consume event capacity
    /// - Does NOT block email from re-registering
    /// - Auto soft-deleted after 30 days for audit trail
    /// </summary>
    Abandoned = 8,

    /// <summary>
    /// Phase 6A.91: NEW - User has requested a refund, waiting for Stripe to process
    /// - Consumes event capacity until refund completes
    /// - User can withdraw request to return to Confirmed status
    /// - Transitions to Refunded once Stripe webhook confirms completion
    /// - Cannot be cancelled after event start time
    /// </summary>
    RefundRequested = 9
}