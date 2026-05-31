namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Phase 6A.157 — handles Stripe webhook events for packaged sponsorship
/// payments. Sibling to <see cref="ISponsorWebhookHandler"/> — the split
/// exists so completed-package webhooks call
/// <c>Sponsor.CompletePackagePayment</c> (raises
/// <c>PackageSponsorCompletedEvent</c>, drives the forked email template)
/// instead of <c>Sponsor.CompletePayment</c> (raises the generic event).
///
/// Refund handling is NOT on this interface — refund webhooks match on
/// <c>StripePaymentIntentId</c> not on <c>payment_type</c> metadata, so
/// the existing <see cref="ISponsorWebhookHandler.HandleChargeRefundedAsync"/>
/// continues to work for package sponsors (calls <c>Sponsor.MarkAsRefunded()</c>
/// which is package-agnostic).
/// </summary>
public interface IPackageSponsorWebhookHandler
{
    /// <summary>
    /// Handles <c>checkout.session.completed</c> for package sponsor payments.
    /// Loads the Sponsor by checkout session, validates it's a Pending package
    /// sponsor (idempotent skip otherwise), calls
    /// <c>Sponsor.CompletePackagePayment(paymentIntentId)</c>, commits.
    /// </summary>
    Task HandleCheckoutCompletedAsync(
        string sessionId,
        string paymentIntentId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);

    /// <summary>
    /// Handles <c>checkout.session.expired</c> for package sponsor payments.
    /// Marks the Sponsor as Abandoned AND restores the reserved package stock
    /// via <c>SponsorshipPackageRepository.TryRestoreStockAsync</c> so a future
    /// buyer can claim the slot.
    /// </summary>
    Task HandleCheckoutExpiredAsync(
        string sessionId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);
}
