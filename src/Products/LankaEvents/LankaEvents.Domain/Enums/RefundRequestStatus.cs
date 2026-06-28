namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Phase 6A.148: Lifecycle of a refund approval workflow.
///
/// Attendee path:    Pending → Approved → Processing → Completed | Rejected | Withdrawn
/// Organizer path:   (skips Pending) → Approved → Processing → Completed
///
/// Once a request reaches <see cref="Approved"/>, no backward transitions are allowed —
/// money is moving. Withdraw is attendee-only and Pending-only.
/// </summary>
public enum RefundRequestStatus
{
    /// <summary>
    /// Attendee submitted, awaiting organizer review. No Stripe call has been made.
    /// Registration.Status is <c>PendingRefundApproval</c>.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Organizer approved (or organizer-initiated). Stripe dispatch is imminent.
    /// Registration.Status remains <c>PendingRefundApproval</c> until dispatch.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Stripe refund call(s) made; awaiting <c>charge.refunded</c> webhook(s).
    /// Registration.Status transitions to <c>RefundRequested</c> (legacy in-flight semantic).
    /// </summary>
    Processing = 2,

    /// <summary>
    /// All approved line items reached terminal state (Refunded or Failed).
    /// Registration.Status is <c>Refunded</c>.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Organizer declined. Registration.Status returns to <c>Confirmed</c>.
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Attendee withdrew before approval. Registration.Status returns to <c>Confirmed</c>.
    /// </summary>
    Withdrawn = 5
}
