namespace LankaConnect.Modules.Payments.Contracts;

/// <summary>
/// Cross-module read-only API for the Payments module. Consumers outside Payments
/// (e.g. <c>EventCancellationEmailJob</c> reading refund state for cancellation
/// emails, <c>CancelRsvpCommandHandler</c> probing refund history, account-settings
/// surface reading Stripe customer mapping, the webhook controller's idempotency
/// probe) inject this interface instead of reaching into
/// <c>LankaConnect.Domain.Payments</c> or the legacy
/// <c>IRefundRequestRepository</c>, preserving the ArchTest module boundary
/// <c>LegacyApplication_DoesNotDependOnPaymentsDomain</c> (Wave 4.4.d.3).
/// </summary>
/// <remarks>
/// Wave 4.4.a (2026-06-23). Implementation lives in Payments.Application as
/// <c>PaymentQueries</c> (added in Wave 4.4.b); DI wiring is in
/// <c>Payments.Api.PaymentsModule</c>.
/// <para>
/// Per architect ruling 2026-06-23 (Risk #1 Option A), <c>RefundRequest</c> +
/// <c>RefundRequestLineItem</c> + <c>RegistrationPayment</c> remain Registration
/// aggregate children in <c>LankaConnect.Domain.Events.Entities</c>. The
/// transitional <c>Payments.Domain -&gt; LankaConnect.Domain</c> ProjectReference
/// is a permanent structural compromise for this wave. The DTOs on this surface
/// are projections of those legacy types - safe for cross-module consumption
/// because they carry primitives only (decimal+string for Money, byte enums).
/// </para>
/// </remarks>
public interface IPaymentQueries
{
    // -------- Refund request reads --------

    /// <summary>
    /// Returns a single refund request by primary key WITHOUT line items.
    /// Returns <c>null</c> when no row exists.
    /// </summary>
    Task<RefundRequestSummaryDto?> GetRefundRequestByIdAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a single refund request by primary key WITH its line items
    /// hydrated. Returns <c>null</c> when no row exists.
    /// </summary>
    Task<RefundRequestDetailDto?> GetRefundRequestWithLineItemsAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all refund requests filed for a given registration, newest first.
    /// </summary>
    Task<IReadOnlyList<RefundRequestSummaryDto>> GetByRegistrationAsync(
        Guid registrationId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the most recent refund request the given attendee filed for the
    /// given event, or <c>null</c> if the user has never filed one. Drives the
    /// attendee-side "your refund request" detail page.
    /// </summary>
    Task<RefundRequestSummaryDto?> GetMyMostRecentForEventAsync(
        Guid eventId,
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Organizer-queue list query: refund requests filed against any registration
    /// in the given event, optionally filtered by status. Sort order is newest
    /// first at the Contracts boundary; callers must not assume any specific
    /// secondary sort.
    /// </summary>
    Task<IReadOnlyList<RefundRequestSummaryDto>> ListByEventAsync(
        Guid eventId,
        RefundRequestStatusDto? statusFilter,
        CancellationToken ct = default);

    /// <summary>
    /// Safety-net query (architect F11): refund requests that have been in
    /// <see cref="RefundRequestStatusDto.Approved"/> longer than the supplied
    /// cutoff without dispatching to Stripe. Used by the stuck-refund sweep
    /// job that pages an operator.
    /// </summary>
    Task<IReadOnlyList<RefundRequestSummaryDto>> ListStuckApprovedAsync(
        DateTime olderThanUtc,
        CancellationToken ct = default);

    // -------- Stripe customer reads --------

    /// <summary>
    /// Returns the Stripe customer id mapped to the given LankaConnect user, or
    /// <c>null</c> if no Stripe customer has been minted yet. Single-field
    /// shortcut for the common checkout path - callers needing full detail use
    /// <see cref="GetStripeCustomerAsync"/>.
    /// </summary>
    Task<string?> GetStripeCustomerIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the full Stripe-customer projection for the given LankaConnect
    /// user, or <c>null</c> if no mapping exists.
    /// </summary>
    Task<StripeCustomerSummaryDto?> GetStripeCustomerAsync(
        Guid userId,
        CancellationToken ct = default);

    // -------- Webhook idempotency probe --------

    /// <summary>
    /// Returns <c>true</c> when the given Stripe event id has already been
    /// processed by the webhook handler (idempotency guard). Used by
    /// <c>StripeWebhookHandler</c> before invoking any state mutation.
    /// </summary>
    /// <remarks>
    /// Architect Additional Finding #2 (2026-06-23): at 4.4.d.2 the webhook
    /// handler's call site must resolve <see cref="IPaymentQueries"/> from the
    /// SAME DI scope as the webhook controller, or events silently re-process.
    /// </remarks>
    Task<bool> IsWebhookEventProcessedAsync(
        string stripeEventId,
        CancellationToken ct = default);
}
