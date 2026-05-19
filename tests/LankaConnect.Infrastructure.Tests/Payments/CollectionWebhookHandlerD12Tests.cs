using FluentAssertions;
using Xunit;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Infrastructure.Payments.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace LankaConnect.Infrastructure.Tests.Payments;

/// <summary>
/// Phase 6A.148.W4.D12 (G4 generalised dedupe): pins the new CollectionWebhookHandler
/// dedupe guard — workflow-owned refunds suppress the legacy per-Collection email
/// (analogous to D9 for Sponsor). Fail-OPEN on lookup exception.
///
/// Load-bearing assertion mirrors D9: <see cref="IServiceScopeFactory.CreateScope"/>
/// invocation count — the fire-and-forget email path's very first call.
/// </summary>
public class CollectionWebhookHandlerD12Tests
{
    private readonly Mock<ICollectionRepository> _collectionRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<IRefundRequestRepository> _refundRequestRepo = new();

    private CollectionWebhookHandler BuildHandler() =>
        new CollectionWebhookHandler(
            _collectionRepo.Object,
            _unitOfWork.Object,
            _scopeFactory.Object,
            _refundRequestRepo.Object,
            Mock.Of<ILogger<CollectionWebhookHandler>>());

    [Fact]
    public async Task WorkflowOwnedRefund_SuppressesStandaloneEmail()
    {
        var collection = CompletedCollection();
        _collectionRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Collection, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);
        _refundRequestRepo.Setup(r => r.GetWorkflowLineReferenceIdAsync(
                RefundLineItemType.Collection, "re_workflow_collection", It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection.Id);

        await BuildHandler().HandleChargeRefundedAsync(
            collection.StripePaymentIntentId!, "re_workflow_collection", Guid.NewGuid());

        _scopeFactory.Verify(s => s.CreateScope(), Times.Never,
            "workflow-owned refund must short-circuit before the fire-and-forget email block creates a DI scope");
    }

    [Fact]
    public async Task NonWorkflowRefund_SendsStandaloneEmail_RegressionGuard()
    {
        var collection = CompletedCollection();
        _collectionRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Collection, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);
        _refundRequestRepo.Setup(r => r.GetWorkflowLineReferenceIdAsync(
                RefundLineItemType.Collection, "re_legacy", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        await BuildHandler().HandleChargeRefundedAsync(
            collection.StripePaymentIntentId!, "re_legacy", Guid.NewGuid());
        await Task.Delay(150); // let fire-and-forget Task.Run schedule

        _scopeFactory.Verify(s => s.CreateScope(), Times.Once,
            "legacy path must send the standalone email when no workflow line-item exists");
    }

    [Fact]
    public async Task WorkflowLookupThrows_DefaultsToSendingStandaloneEmail_FailOpenGuardrail()
    {
        var collection = CompletedCollection();
        _collectionRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Collection, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);
        _refundRequestRepo.Setup(r => r.GetWorkflowLineReferenceIdAsync(
                It.IsAny<RefundLineItemType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated DB error"));

        var act = async () => await BuildHandler().HandleChargeRefundedAsync(
            collection.StripePaymentIntentId!, "re_throws", Guid.NewGuid());
        await act.Should().NotThrowAsync();
        await Task.Delay(150);

        _scopeFactory.Verify(s => s.CreateScope(), Times.Once,
            "fail-OPEN: lookup exception must not silence the legacy email");
    }

    [Fact]
    public async Task DifferentEntity_NotSuppressed()
    {
        // The workflow refund is for a DIFFERENT Collection — our current handler invocation
        // is processing the legacy path for this specific collection. Predicate should not
        // match, so the legacy email fires.
        var collection = CompletedCollection();
        _collectionRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Collection, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);
        _refundRequestRepo.Setup(r => r.GetWorkflowLineReferenceIdAsync(
                RefundLineItemType.Collection, "re_other_collection", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid()); // a DIFFERENT collection's id

        await BuildHandler().HandleChargeRefundedAsync(
            collection.StripePaymentIntentId!, "re_other_collection", Guid.NewGuid());
        await Task.Delay(150);

        _scopeFactory.Verify(s => s.CreateScope(), Times.Once,
            "predicate must compare returned ReferenceId to THIS collection's id — unrelated workflow refunds shouldn't suppress");
    }

    private static Collection CompletedCollection()
    {
        var collection = Collection.Create(
            eventId: Guid.NewGuid(),
            contributorUserId: Guid.NewGuid(),
            contributorName: "Test Contributor",
            contributorEmail: "contributor@example.com",
            contributorPhone: "+1-555-1234",
            contributorNotes: null,
            amount: Money.Create(100m, Currency.USD).Value).Value;
        collection.CompletePayment("pi_test_collection");
        return collection;
    }
}
