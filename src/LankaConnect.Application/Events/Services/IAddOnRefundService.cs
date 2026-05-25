using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Service for processing add-on purchase refunds during event cancellation.
/// Handles Stripe refund calls, domain status transitions, and stock restoration.
/// Non-blocking: partial failures are logged but do not prevent registration cancellation.
/// </summary>
public interface IAddOnRefundService
{
    /// <summary>
    /// Refunds all completed add-on purchases for a user in a specific event.
    /// For each purchase: calls Stripe refund → marks as refunded → restores stock.
    /// Continues processing remaining purchases if one fails (partial failure tolerant).
    /// </summary>
    /// <param name="userId">The user whose purchases to refund</param>
    /// <param name="eventId">The event containing the purchases</param>
    /// <param name="reason">Stripe refund reason (e.g., "requested_by_customer")</param>
    /// <param name="metadata">Additional metadata for Stripe refund</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing AddOnRefundResult on success</returns>
    /// <param name="isPreApproved">
    /// Phase 6A.148 defense-in-depth (ring 2): when the
    /// <c>Refund:ApprovalWorkflow:Enabled</c> feature flag is ON, this method MUST be
    /// called with <c>isPreApproved=true</c> by callers that have already routed through
    /// the approval workflow. Otherwise it returns a Failure. This guarantees the GATE
    /// cannot be bypassed by any caller — present or future — even by accident.
    /// </param>
    Task<Result<AddOnRefundResult>> RefundUserPurchasesAsync(
        Guid userId,
        Guid eventId,
        string reason,
        Dictionary<string, string> metadata,
        Guid? registrationId = null,
        bool isPreApproved = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an add-on refund operation.
/// </summary>
public record AddOnRefundResult(
    int PurchasesRefunded,
    decimal TotalAmountRefunded,
    int PurchasesFailed);
