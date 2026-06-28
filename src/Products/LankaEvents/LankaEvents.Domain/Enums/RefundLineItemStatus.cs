namespace LankaConnect.Products.LankaEvents.Domain.Enums;

/// <summary>
/// Phase 6A.148: Per-line-item refund status. Tracked independently of the parent
/// <see cref="RefundRequestStatus"/> so that a single approved request with multiple
/// line items can have mixed outcomes (e.g. ticket Refunded, add-on Failed).
/// </summary>
public enum RefundLineItemStatus
{
    /// <summary>
    /// Attendee requested this line. No organizer decision yet.
    /// </summary>
    Requested = 0,

    /// <summary>
    /// Organizer approved a non-zero <c>ApprovedAmount</c> for this line.
    /// Stripe dispatch is imminent.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Organizer set <c>ApprovedAmount</c> to zero — this line is excluded from the refund.
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Stripe refund call has been made; awaiting <c>charge.refunded</c> webhook.
    /// </summary>
    Processing = 3,

    /// <summary>
    /// Stripe confirmed the refund via webhook. Terminal state.
    /// </summary>
    Refunded = 4,

    /// <summary>
    /// Stripe call failed. Terminal state; see <c>FailureReason</c>.
    /// </summary>
    Failed = 5
}
