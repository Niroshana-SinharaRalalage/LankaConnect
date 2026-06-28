using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;

namespace LankaConnect.Products.LankaEvents.Application.Services;

/// <summary>
/// Phase 6A.92: Shared service for processing registration refunds.
/// Uses webhook-based approach: Stripe refund is initiated, then charge.refunded webhook completes the transition.
///
/// Flow:
/// 1. ProcessRefundAsync() calls Stripe API and transitions registration to RefundRequested
/// 2. Stripe sends charge.refunded webhook
/// 3. PaymentsController.HandleChargeRefundedAsync() transitions registration to Refunded
///
/// Consolidates refund logic used by:
/// - CancelRsvpCommandHandler (user-initiated cancellation)
/// - EventCancellationEmailJob (event cancellation by organizer)
/// </summary>
public interface IRegistrationRefundService
{
    /// <summary>
    /// Processes a refund for a single registration.
    /// Calls Stripe API and transitions registration to RefundRequested.
    /// The charge.refunded webhook will complete the transition to Refunded.
    /// </summary>
    /// <param name="registration">The registration to refund (must have completed payment)</param>
    /// <param name="reason">Stripe refund reason (e.g., "requested_by_customer", "event_cancelled")</param>
    /// <param name="metadata">Additional metadata to attach to the Stripe refund</param>
    /// <param name="additionalRefundAmount">Additional refund amount (e.g., add-on purchases) to include in the refund email total</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing RefundResult on success, or error message on failure</returns>
    /// <param name="isPreApproved">
    /// Phase 6A.148 defense-in-depth (ring 2): when the
    /// <c>Refund:ApprovalWorkflow:Enabled</c> feature flag is ON, this method MUST be
    /// called with <c>isPreApproved=true</c> by callers who have already routed through
    /// the approval workflow (RefundExecutionService, EventCancellationEmailJob's
    /// ApprovedAuto path). Callers that haven't (legacy CancelRsvpCommandHandler when
    /// flag is on) will be rejected with a Failure. This guarantees the GATE cannot be
    /// bypassed by any caller — present or future — even by accident.
    /// </param>
    Task<Result<RefundResult>> ProcessRefundAsync(
        Registration registration,
        string reason,
        Dictionary<string, string> metadata,
        decimal additionalRefundAmount = 0m,
        bool isPreApproved = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a successful refund request operation.
/// Note: IsCompleted will always be false since webhook completes the refund.
/// </summary>
public record RefundResult(
    string StripeRefundId,
    decimal AmountRefunded);
