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
    Task<Result<AddOnRefundResult>> RefundUserPurchasesAsync(
        Guid userId,
        Guid eventId,
        string reason,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an add-on refund operation.
/// </summary>
public record AddOnRefundResult(
    int PurchasesRefunded,
    decimal TotalAmountRefunded,
    int PurchasesFailed);
