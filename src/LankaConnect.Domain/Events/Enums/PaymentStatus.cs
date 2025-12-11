namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Payment status for event registrations (Session 23 - Stripe payment integration)
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending (Stripe Checkout session created, awaiting completion)
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment completed successfully
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Payment failed or was declined
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Payment was refunded (registration cancelled after payment)
    /// </summary>
    Refunded = 3,

    /// <summary>
    /// No payment required (free event)
    /// </summary>
    NotRequired = 4
}
