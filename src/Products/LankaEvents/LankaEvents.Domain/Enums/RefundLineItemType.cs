namespace LankaConnect.Products.LankaEvents.Domain.Enums;

/// <summary>
/// Phase 6A.148: The kind of payment a single refund line item targets.
///
/// One line item per underlying Stripe charge — for AddOn this means one line per
/// <c>AddOnPurchase</c> (Add-Only Attendees can create multiple purchases per attendee,
/// each with its own <c>StripePaymentIntentId</c>).
///
/// Donations are intentionally excluded for v1 — non-refundable per product decision.
/// </summary>
public enum RefundLineItemType
{
    /// <summary>
    /// Registration ticket payment (RegistrationPayment.Id).
    /// </summary>
    Ticket = 0,

    /// <summary>
    /// Add-on purchase (AddOnPurchase.Id). One line per purchase, not aggregated.
    /// </summary>
    AddOn = 1,

    /// <summary>
    /// Collection / contribution (Collection.Id).
    /// </summary>
    Collection = 2,

    /// <summary>
    /// Event sponsorship (Sponsor.Id).
    /// </summary>
    Sponsor = 3
}
