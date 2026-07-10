using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Phase 6A.148 — RefundRequestLineItem entity unit tests.
///
/// Pins down: factory validation, per-line state machine (Requested → Approved/Rejected →
/// Processing → Refunded/Failed), currency-match invariant, ApprovedAmount ≤ RequestedAmount,
/// and webhook idempotency (MarkRefunded/MarkFailed is a no-op when already terminal).
/// </summary>
public class RefundRequestLineItemTests
{
    private static Money Usd(decimal amount) => new(amount, Currency.USD);
    private static Money Lkr(decimal amount) => new(amount, Currency.LKR);

    private readonly Guid _refundRequestId = Guid.NewGuid();
    private readonly Guid _referenceId = Guid.NewGuid();

    // ============================================================
    // Create factory
    // ============================================================

    [Fact]
    public void Create_HappyPath_ReturnsRequestedLineItem()
    {
        var result = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m));

        result.IsSuccess.Should().BeTrue();
        var item = result.Value;
        item.Id.Should().NotBe(Guid.Empty);
        item.RefundRequestId.Should().Be(_refundRequestId);
        item.Type.Should().Be(RefundLineItemType.Ticket);
        item.ReferenceId.Should().Be(_referenceId);
        item.RequestedAmount.Amount.Should().Be(50m);
        item.RequestedAmount.Currency.Should().Be(Currency.USD);
        item.ApprovedAmount.Should().BeNull();
        item.Status.Should().Be(RefundLineItemStatus.Requested);
        item.StripeRefundId.Should().BeNull();
        item.StripeChargeId.Should().BeNull();
        item.ProcessedAt.Should().BeNull();
        item.FailureReason.Should().BeNull();
    }

    [Fact]
    public void Create_WithZeroAmount_Fails()
    {
        var result = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.AddOn, _referenceId, Usd(0m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("RequestedAmount must be greater than zero");
    }

    [Fact]
    public void Create_WithNegativeAmount_Fails()
    {
        var result = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Collection, _referenceId, Usd(-1m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("RequestedAmount must be greater than zero");
    }

    [Fact]
    public void Create_WithEmptyReferenceId_Fails()
    {
        var result = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Sponsor, Guid.Empty, Usd(50m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("ReferenceId");
    }

    // ============================================================
    // Approve
    // ============================================================

    [Fact]
    public void Approve_WithFullAmount_TransitionsToApproved()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;

        var result = item.Approve(Usd(50m));

        result.IsSuccess.Should().BeTrue();
        item.Status.Should().Be(RefundLineItemStatus.Approved);
        item.ApprovedAmount!.Amount.Should().Be(50m);
    }

    [Fact]
    public void Approve_WithPartialAmount_TransitionsToApproved()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.AddOn, _referenceId, Usd(50m)).Value;

        var result = item.Approve(Usd(25m));

        result.IsSuccess.Should().BeTrue();
        item.Status.Should().Be(RefundLineItemStatus.Approved);
        item.ApprovedAmount!.Amount.Should().Be(25m);
    }

    [Fact]
    public void Approve_WithZeroAmount_TransitionsToRejected()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;

        var result = item.Approve(Usd(0m));

        result.IsSuccess.Should().BeTrue();
        item.Status.Should().Be(RefundLineItemStatus.Rejected);
        item.ApprovedAmount!.Amount.Should().Be(0m);
    }

    [Fact]
    public void Approve_AmountExceedsRequested_Fails()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;

        var result = item.Approve(Usd(60m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot exceed RequestedAmount");
        item.Status.Should().Be(RefundLineItemStatus.Requested, "failed approve must not mutate");
        item.ApprovedAmount.Should().BeNull();
    }

    [Fact]
    public void Approve_CurrencyMismatch_Fails()
    {
        // Architect must-fix F8 — invariant must hold.
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;

        var result = item.Approve(Lkr(50m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("currency must match");
        item.Status.Should().Be(RefundLineItemStatus.Requested);
    }

    [Fact]
    public void Approve_NotInRequestedState_Fails()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;
        item.Approve(Usd(50m));

        var second = item.Approve(Usd(25m));

        second.IsFailure.Should().BeTrue();
        second.Error.Should().Contain("only Requested line items");
    }

    // ============================================================
    // MarkProcessing
    // ============================================================

    [Fact]
    public void MarkProcessing_FromApproved_TransitionsAndCapturesStripeIds()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;
        item.Approve(Usd(50m));

        var result = item.MarkProcessing("re_abc123", "ch_xyz789");

        result.IsSuccess.Should().BeTrue();
        item.Status.Should().Be(RefundLineItemStatus.Processing);
        item.StripeRefundId.Should().Be("re_abc123");
        item.StripeChargeId.Should().Be("ch_xyz789");
    }

    [Fact]
    public void MarkProcessing_FromRequested_Fails()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;

        var result = item.MarkProcessing("re_abc", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Approved");
    }

    [Fact]
    public void MarkProcessing_BlankRefundId_Fails()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;
        item.Approve(Usd(50m));

        var result = item.MarkProcessing("", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("StripeRefundId");
    }

    // ============================================================
    // MarkRefunded — terminal, idempotent
    // ============================================================

    [Fact]
    public void MarkRefunded_FromProcessing_TransitionsToRefunded()
    {
        var item = NewProcessingItem();
        var when = DateTime.UtcNow;

        var result = item.MarkRefunded(when);

        result.IsSuccess.Should().BeTrue();
        item.Status.Should().Be(RefundLineItemStatus.Refunded);
        item.ProcessedAt.Should().Be(when);
    }

    [Fact]
    public void MarkRefunded_AlreadyRefunded_IsNoopAndStaysSuccess()
    {
        // Architect must-fix F4 — webhook idempotency. Stripe retries charge.refunded.
        var item = NewProcessingItem();
        var firstTime = DateTime.UtcNow.AddMinutes(-5);
        item.MarkRefunded(firstTime);

        var secondTry = item.MarkRefunded(DateTime.UtcNow);

        secondTry.IsSuccess.Should().BeTrue("idempotent — must not throw or fail");
        item.ProcessedAt.Should().Be(firstTime, "first refund timestamp must be preserved");
        item.Status.Should().Be(RefundLineItemStatus.Refunded);
    }

    [Fact]
    public void MarkRefunded_FromFailed_IsNoopAndStaysSuccess()
    {
        var item = NewProcessingItem();
        item.MarkFailed("card_declined");

        var result = item.MarkRefunded(DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue("once terminal, further webhook events are ignored");
        item.Status.Should().Be(RefundLineItemStatus.Failed);
    }

    [Fact]
    public void MarkRefunded_FromRequested_Fails()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;

        var result = item.MarkRefunded(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Processing");
    }

    // ============================================================
    // MarkFailed — terminal, idempotent
    // ============================================================

    [Fact]
    public void MarkFailed_FromProcessing_TransitionsToFailed()
    {
        var item = NewProcessingItem();

        var result = item.MarkFailed("card_declined");

        result.IsSuccess.Should().BeTrue();
        item.Status.Should().Be(RefundLineItemStatus.Failed);
        item.FailureReason.Should().Be("card_declined");
    }

    [Fact]
    public void MarkFailed_AlreadyFailed_IsNoopAndStaysSuccess()
    {
        var item = NewProcessingItem();
        item.MarkFailed("first_reason");

        var second = item.MarkFailed("second_reason");

        second.IsSuccess.Should().BeTrue();
        item.FailureReason.Should().Be("first_reason", "first failure reason wins");
    }

    [Fact]
    public void MarkFailed_BlankReason_Fails()
    {
        var item = NewProcessingItem();

        var result = item.MarkFailed("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("FailureReason");
    }

    // ============================================================
    // Reject
    // ============================================================

    [Fact]
    public void Reject_FromRequested_TransitionsToRejectedWithZeroAmount()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;

        var result = item.Reject();

        result.IsSuccess.Should().BeTrue();
        item.Status.Should().Be(RefundLineItemStatus.Rejected);
        item.ApprovedAmount!.Amount.Should().Be(0m);
    }

    [Fact]
    public void Reject_FromApproved_Fails()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;
        item.Approve(Usd(50m));

        var result = item.Reject();

        result.IsFailure.Should().BeTrue();
    }

    // ============================================================
    // Terminal-state helpers
    // ============================================================

    [Fact]
    public void IsTerminal_RecognizesRefundedAndFailed()
    {
        var refunded = NewProcessingItem();
        refunded.MarkRefunded(DateTime.UtcNow);

        var failed = NewProcessingItem();
        failed.MarkFailed("test");

        var inFlight = NewProcessingItem();

        refunded.IsTerminal.Should().BeTrue();
        failed.IsTerminal.Should().BeTrue();
        inFlight.IsTerminal.Should().BeFalse();
    }

    // ============================================================
    // Helpers
    // ============================================================

    private RefundRequestLineItem NewProcessingItem()
    {
        var item = RefundRequestLineItem.Create(
            _refundRequestId, RefundLineItemType.Ticket, _referenceId, Usd(50m)).Value;
        item.Approve(Usd(50m));
        item.MarkProcessing("re_test_" + Guid.NewGuid().ToString("N")[..8], "ch_test");
        return item;
    }
}
