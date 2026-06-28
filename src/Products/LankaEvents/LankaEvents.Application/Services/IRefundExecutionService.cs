using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Application.Services;

/// <summary>
/// Phase 6A.148: Dispatches Stripe refund calls for an approved <c>RefundRequest</c>.
///
/// MUST be invoked AFTER the approve transaction has committed (architect F10 — never
/// hold a DB transaction across a Stripe HTTP call). Typical use: subscribe via a
/// domain-event handler for <c>RefundRequestApprovedEvent</c> / <c>OrganizerInitiatedRefundCreatedEvent</c>,
/// which fires after <c>SaveChangesAsync</c>.
///
/// Idempotency: dispatch is idempotent at the line-item level (skips lines already
/// in Processing or terminal). Safe to retry on stuck rows via
/// <c>RefundReconciliationService</c> (architect F11).
/// </summary>
public interface IRefundExecutionService
{
    Task<Result> DispatchAsync(Guid refundRequestId, CancellationToken cancellationToken = default);
}
