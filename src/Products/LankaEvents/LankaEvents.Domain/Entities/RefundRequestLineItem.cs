using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Phase 6A.148: A single bucket of a refund request — one Ticket / AddOn / Collection / Sponsor
/// charge that may or may not be approved (in part or in full) by the organizer.
///
/// One <see cref="RefundRequestLineItem"/> maps to one underlying Stripe charge. AddOnPurchases
/// each have their own StripePaymentIntentId, so multi-purchase attendees get multiple line items
/// of <see cref="RefundLineItemType.AddOn"/> (per architect F9 — never aggregated by type).
///
/// State machine: Requested → (Approved | Rejected) → Processing → (Refunded | Failed).
/// Terminal states (<see cref="RefundLineItemStatus.Refunded"/> / <see cref="RefundLineItemStatus.Failed"/>)
/// are idempotent — webhook retries from Stripe must not double-emit completion events (F4).
/// </summary>
public class RefundRequestLineItem : LegacyBaseEntity
{
    public Guid RefundRequestId { get; private set; }
    public RefundLineItemType Type { get; private set; }
    public Guid ReferenceId { get; private set; }
    public Money RequestedAmount { get; private set; } = null!;
    public Money? ApprovedAmount { get; private set; }
    public RefundLineItemStatus Status { get; private set; }
    public string? StripeRefundId { get; private set; }
    public string? StripeChargeId { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public bool IsTerminal =>
        Status == RefundLineItemStatus.Refunded || Status == RefundLineItemStatus.Failed;

    // EF Core
    private RefundRequestLineItem() { }

    private RefundRequestLineItem(
        Guid refundRequestId,
        RefundLineItemType type,
        Guid referenceId,
        Money requestedAmount)
    {
        RefundRequestId = refundRequestId;
        Type = type;
        ReferenceId = referenceId;
        RequestedAmount = requestedAmount;
        Status = RefundLineItemStatus.Requested;
    }

    public static Result<RefundRequestLineItem> Create(
        Guid refundRequestId,
        RefundLineItemType type,
        Guid referenceId,
        Money requestedAmount)
    {
        if (referenceId == Guid.Empty)
            return Result<RefundRequestLineItem>.Failure("ReferenceId is required");

        if (requestedAmount is null)
            return Result<RefundRequestLineItem>.Failure("RequestedAmount is required");

        if (requestedAmount.Amount <= 0)
            return Result<RefundRequestLineItem>.Failure("RequestedAmount must be greater than zero");

        return Result<RefundRequestLineItem>.Success(
            new RefundRequestLineItem(refundRequestId, type, referenceId, requestedAmount));
    }

    /// <summary>
    /// Organizer approves this line for the given amount. Zero means rejection of this
    /// specific line (transitions directly to <see cref="RefundLineItemStatus.Rejected"/>).
    /// </summary>
    public Result Approve(Money approvedAmount)
    {
        if (Status != RefundLineItemStatus.Requested)
            return Result.Failure(
                $"Approve only Requested line items; current status is {Status}.");

        if (approvedAmount is null)
            return Result.Failure("ApprovedAmount is required");

        // Architect F8: currency must match across request and approval.
        if (approvedAmount.Currency != RequestedAmount.Currency)
            return Result.Failure(
                $"ApprovedAmount currency must match RequestedAmount currency " +
                $"({RequestedAmount.Currency} expected, {approvedAmount.Currency} provided).");

        if (approvedAmount.Amount < 0)
            return Result.Failure("ApprovedAmount cannot be negative");

        if (approvedAmount.Amount > RequestedAmount.Amount)
            return Result.Failure(
                $"ApprovedAmount ({approvedAmount.Amount}) cannot exceed RequestedAmount " +
                $"({RequestedAmount.Amount}) for line item {Id}.");

        ApprovedAmount = approvedAmount;
        Status = approvedAmount.Amount == 0
            ? RefundLineItemStatus.Rejected
            : RefundLineItemStatus.Approved;
        return Result.Success();
    }

    /// <summary>
    /// Organizer rejects this line outright. Equivalent to <see cref="Approve"/> with zero amount.
    /// </summary>
    public Result Reject()
    {
        if (Status != RefundLineItemStatus.Requested)
            return Result.Failure(
                $"Reject only Requested line items; current status is {Status}.");

        ApprovedAmount = new Money(0m, RequestedAmount.Currency);
        Status = RefundLineItemStatus.Rejected;
        return Result.Success();
    }

    /// <summary>
    /// Marks the line as in-flight at Stripe after a successful CreateRefund call.
    /// </summary>
    public Result MarkProcessing(string stripeRefundId, string? stripeChargeId)
    {
        if (Status != RefundLineItemStatus.Approved)
            return Result.Failure(
                $"MarkProcessing requires Approved status; current status is {Status}.");

        if (string.IsNullOrWhiteSpace(stripeRefundId))
            return Result.Failure("StripeRefundId is required when marking line item as Processing");

        StripeRefundId = stripeRefundId;
        StripeChargeId = stripeChargeId;
        Status = RefundLineItemStatus.Processing;
        return Result.Success();
    }

    /// <summary>
    /// Terminal transition to Refunded. Idempotent (architect F4) — if already Refunded
    /// or Failed, returns success without mutation so duplicate Stripe webhook deliveries
    /// don't emit duplicate domain events or completion emails.
    /// </summary>
    public Result MarkRefunded(DateTime processedAt)
    {
        if (IsTerminal)
            return Result.Success();

        if (Status != RefundLineItemStatus.Processing)
            return Result.Failure(
                $"MarkRefunded requires Processing status; current status is {Status}.");

        Status = RefundLineItemStatus.Refunded;
        ProcessedAt = processedAt;
        return Result.Success();
    }

    /// <summary>
    /// Terminal transition to Failed. Idempotent — first failure reason wins.
    /// </summary>
    public Result MarkFailed(string failureReason)
    {
        if (IsTerminal)
            return Result.Success();

        if (Status != RefundLineItemStatus.Processing)
            return Result.Failure(
                $"MarkFailed requires Processing status; current status is {Status}.");

        if (string.IsNullOrWhiteSpace(failureReason))
            return Result.Failure("FailureReason is required when marking line item as Failed");

        Status = RefundLineItemStatus.Failed;
        FailureReason = failureReason;
        return Result.Success();
    }
}
