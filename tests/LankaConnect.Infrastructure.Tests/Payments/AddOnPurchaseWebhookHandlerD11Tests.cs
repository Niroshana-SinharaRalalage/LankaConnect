using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2
using FluentAssertions;
using Xunit;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Infrastructure.Payments.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LankaConnect.Infrastructure.Tests.Payments;

/// <summary>
/// Phase 6A.148.W4.D11 (G1 fix): pins the new AddOnPurchaseWebhookHandler.HandleChargeRefundedAsync
/// behaviour — the missing piece that left AddOnPurchase rows stuck in Completed after a
/// workflow refund (operator UAT defect F2).
///
/// Load-bearing assertions: (a) targeted AddOnPurchase transitions to Refunded; (b)
/// cart-aware narrowing via workflow line ReferenceId; (c) legacy fallback refunds all
/// purchases sharing the PaymentIntent; (d) idempotent on duplicate webhooks; (e)
/// fail-OPEN on workflow lookup exception.
/// </summary>
public class AddOnPurchaseWebhookHandlerD11Tests
{
    private readonly Mock<IAddOnPurchaseRepository> _purchaseRepo = new();
    private readonly Mock<IAddOnDefinitionRepository> _definitionRepo = new();
    private readonly Mock<IRefundRequestRepository> _refundRequestRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AddOnPurchaseWebhookHandler BuildHandler() =>
        new AddOnPurchaseWebhookHandler(
            _purchaseRepo.Object,
            _definitionRepo.Object,
            _refundRequestRepo.Object,
            _uow.Object,
            Mock.Of<ILogger<AddOnPurchaseWebhookHandler>>());

    [Fact]
    public async Task WorkflowOwnedRefund_OnlyMatchingPurchaseMarkedRefunded()
    {
        // Cart of 3 AddOnPurchases share one PaymentIntent. Workflow refund targets ONE.
        var purchase1 = CompletedPurchase("pi_cart");
        var purchase2 = CompletedPurchase("pi_cart");
        var purchase3 = CompletedPurchase("pi_cart");
        _purchaseRepo.Setup(r => r.GetAllByStripePaymentIntentIdAsync("pi_cart", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { purchase1, purchase2, purchase3 });
        _refundRequestRepo.Setup(r => r.GetWorkflowLineReferenceIdAsync(
                RefundLineItemType.AddOn, "re_workflow_aaa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchase2.Id);

        await BuildHandler().HandleChargeRefundedAsync("pi_cart", "re_workflow_aaa", Guid.NewGuid());

        purchase1.Status.Should().Be(AddOnPurchaseStatus.Completed);
        purchase2.Status.Should().Be(AddOnPurchaseStatus.Refunded);
        purchase3.Status.Should().Be(AddOnPurchaseStatus.Completed);
        _purchaseRepo.Verify(r => r.Update(purchase2), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LegacyRefund_NoWorkflowLine_RefundsAllSharingPI()
    {
        // No workflow line found → legacy cart semantics: mark ALL purchases sharing the PI.
        var purchase1 = CompletedPurchase("pi_legacy");
        var purchase2 = CompletedPurchase("pi_legacy");
        _purchaseRepo.Setup(r => r.GetAllByStripePaymentIntentIdAsync("pi_legacy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { purchase1, purchase2 });
        _refundRequestRepo.Setup(r => r.GetWorkflowLineReferenceIdAsync(
                RefundLineItemType.AddOn, "re_legacy", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        await BuildHandler().HandleChargeRefundedAsync("pi_legacy", "re_legacy", Guid.NewGuid());

        purchase1.Status.Should().Be(AddOnPurchaseStatus.Refunded);
        purchase2.Status.Should().Be(AddOnPurchaseStatus.Refunded);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IdempotentSkip_AlreadyRefunded()
    {
        var purchase = CompletedPurchase("pi_dup");
        purchase.MarkAsRefunded(); // simulate prior webhook already processed
        _purchaseRepo.Setup(r => r.GetAllByStripePaymentIntentIdAsync("pi_dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { purchase });
        _refundRequestRepo.Setup(r => r.GetWorkflowLineReferenceIdAsync(
                RefundLineItemType.AddOn, "re_dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchase.Id);

        await BuildHandler().HandleChargeRefundedAsync("pi_dup", "re_dup", Guid.NewGuid());

        purchase.Status.Should().Be(AddOnPurchaseStatus.Refunded);
        _purchaseRepo.Verify(r => r.Update(It.IsAny<AddOnPurchase>()), Times.Never,
            "already-Refunded purchase should be skipped idempotently — no Update call");
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoCandidatesForPaymentIntent_LogsWarning_NoCommit()
    {
        // Orphan webhook — Stripe fires for a PI that doesn't match any AddOnPurchase row.
        _purchaseRepo.Setup(r => r.GetAllByStripePaymentIntentIdAsync("pi_orphan", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AddOnPurchase>());

        await BuildHandler().HandleChargeRefundedAsync("pi_orphan", "re_orphan", Guid.NewGuid());

        _purchaseRepo.Verify(r => r.Update(It.IsAny<AddOnPurchase>()), Times.Never);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        // Workflow lookup never happens because we short-circuit on empty candidates.
        _refundRequestRepo.Verify(r => r.GetWorkflowLineReferenceIdAsync(
            It.IsAny<RefundLineItemType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WorkflowLookupThrows_FailsOpen_RefundsAllOnPI()
    {
        // Fail-OPEN: lookup exception falls back to legacy cart-refund semantics.
        var purchase1 = CompletedPurchase("pi_lookup_fail");
        var purchase2 = CompletedPurchase("pi_lookup_fail");
        _purchaseRepo.Setup(r => r.GetAllByStripePaymentIntentIdAsync("pi_lookup_fail", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { purchase1, purchase2 });
        _refundRequestRepo.Setup(r => r.GetWorkflowLineReferenceIdAsync(
                It.IsAny<RefundLineItemType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated transient DB error"));

        var act = async () => await BuildHandler().HandleChargeRefundedAsync(
            "pi_lookup_fail", "re_lookup_fail", Guid.NewGuid());
        await act.Should().NotThrowAsync();

        purchase1.Status.Should().Be(AddOnPurchaseStatus.Refunded);
        purchase2.Status.Should().Be(AddOnPurchaseStatus.Refunded);
    }

    private static AddOnPurchase CompletedPurchase(string paymentIntentId)
    {
        var purchase = AddOnPurchase.Create(
            eventId: Guid.NewGuid(),
            addOnDefinitionId: Guid.NewGuid(),
            buyerUserId: Guid.NewGuid(),
            buyerName: "Test Buyer",
            buyerEmail: "buyer@example.com",
            buyerPhone: "+1-555-1234",
            quantity: 1,
            unitPrice: Money.Create(15m, Currency.USD).Value).Value;
        purchase.SetStripeCheckoutSession("cs_" + Guid.NewGuid().ToString("N"), DateTime.UtcNow.AddHours(1));
        purchase.CompletePayment(paymentIntentId);
        return purchase;
    }
}
