using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // Wave 6.5.g Day 5 (2026-07-10): promoted per Consult #15 PASS C — interfaces belong in Contracts. Interfaces are Payments-domain concepts (final home is Payments.Contracts in Phase B).

/// <summary>
/// Phase 6A.148.W5.D2 — per-line Stripe dispatcher that runs each refund line in its
/// own isolated DI scope.
///
/// Architect-mandated design (W5 plan §1, Issue #1-3): the W5.D7 root cause was that
/// <c>RefundExecutionService.DispatchAsync</c> loaded the parent Registration aggregate,
/// mutated line items in memory through it, then committed the whole aggregate under
/// xmin concurrency — when a concurrent Cancel flow flipped Registration.Status, the
/// terminal CommitAsync threw DbUpdateConcurrencyException and rolled back the
/// in-memory MarkProcessing(refundId) / MarkRefunded() changes for lines whose Stripe
/// refunds had already succeeded. The stuck-Approved RR had Stripe's money but DB
/// state saying "not yet refunded" — and the reconciler couldn't safely re-dispatch
/// without W5.D1 idempotency keys.
///
/// This interface decouples the per-line Stripe + DB save from the parent aggregate.
/// Each call to <see cref="DispatchAsync"/>:
/// <list type="number">
///   <item>Opens a FRESH <c>IServiceScopeFactory</c> scope</item>
///   <item>Resolves the line via <see cref="LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions.IRefundRequestRepository.GetLineItemByIdAsync"/></item>
///   <item>Calls Stripe with the W5.D1 stable idempotency key (<c>refund_line_{lineId:N}</c>)</item>
///   <item>Mutates line state in-place (MarkProcessing / MarkRefunded / MarkFailed)</item>
///   <item>Commits the SCOPED <c>IUnitOfWork</c> — touches only the
///     <c>refund_request_line_items</c> row, no Registration / RefundRequest write,
///     no xmin clash surface</item>
/// </list>
///
/// Idempotency: relies on W5.D1 Stripe key + entity-level state-machine guards.
/// Re-dispatch of a line whose Stripe refund already succeeded returns the prior
/// refund object from Stripe (no duplicate charge) and the entity transition is a
/// no-op (state machine refuses Refunded → Refunded).
/// </summary>
public interface IRefundLineDispatcher
{
    /// <summary>
    /// Dispatch the Stripe refund for one line item.
    /// </summary>
    /// <param name="lineItemId">The <c>RefundRequestLineItem.Id</c> to dispatch.</param>
    /// <param name="registrationId">Parent Registration ID — used only for Stripe
    /// metadata (audit trail in Stripe dashboard), not for DB writes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Result.Success</c> when the line has reached Processing or Refunded.
    /// <c>Result.Failure</c> when Stripe returned a hard error AND the line could be
    /// marked Failed in DB. Exceptions during the scope (e.g. transient DB outage)
    /// propagate to the caller; the line stays Approved for the reconciler to retry.
    /// </returns>
    Task<Result> DispatchAsync(
        Guid lineItemId,
        Guid registrationId,
        CancellationToken cancellationToken = default);
}
