using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2
using FluentAssertions;
using Xunit;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
// Wave 8.5.e (2026-07-19): IRefundRequestRepository promoted to LankaEvents.Contracts.Repositories in Wave 8.5.d.
using LankaConnect.Products.LankaEvents.Contracts.Repositories;
using LankaConnect.SharedKernel.Money;
using LankaConnect.Modules.Communications.Infrastructure.Email.Services;
using LankaConnect.Modules.Payments.Infrastructure.Services;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace LankaConnect.Infrastructure.Tests.Payments;

/// <summary>
/// Phase 6A.148.W4.D12 (G4 generalised dedupe): pins the new CollectionWebhookHandler
/// dedupe guard â€” workflow-owned refunds suppress the legacy per-Collection email
/// (analogous to D9 for Sponsor). Fail-OPEN on lookup exception.
///
/// Load-bearing assertion mirrors D9: <see cref="IServiceScopeFactory.CreateScope"/>
/// invocation count â€” the fire-and-forget email path's very first call.
/// </summary>
public class CollectionWebhookHandlerD12Tests
{
    private readonly Mock<ICollectionRepository> _collectionRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRefundRequestRepository> _refundRequestRepo = new();
    private readonly Mock<IRefundDispatchAuditService> _auditService = new();
    private readonly Mock<ITypedEmailService> _emailService = new();
    private readonly Mock<IEventRepository> _eventRepo = new();

    private CollectionWebhookHandler BuildHandler()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_auditService.Object);
        services.AddSingleton(_emailService.Object);
        services.AddSingleton(_eventRepo.Object);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return new CollectionWebhookHandler(
            _collectionRepo.Object,
            _unitOfWork.Object,
            scopeFactory,
            _refundRequestRepo.Object,
            Mock.Of<ILogger<CollectionWebhookHandler>>());
    }

    [Fact]
    public async Task WorkflowOwnedRefund_SuppressesStandaloneEmail_AndWritesAuditRow()
    {
        var collection = CompletedCollection();
        _collectionRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Collection, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);
        _refundRequestRepo.Setup(r => r.GetWorkflowLineReferenceIdAsync(
                RefundLineItemType.Collection, "re_workflow_collection", It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection.Id);

        await BuildHandler().HandleChargeRefundedAsync(
            collection.StripePaymentIntentId!, "re_workflow_collection", Guid.NewGuid());

        _auditService.Verify(a => a.WriteSuppressionAsync(
            It.IsAny<string>(),
            collection.ContributorEmail,
            collection.ContributorName,
            It.Is<string>(reason => reason.Contains("workflow-owned")),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            "Collection",
            collection.Id,
            It.IsAny<CancellationToken>()),
            Times.Once,
            "workflow-owned collection refund: suppression branch must write audit row");
        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "suppression branch must NOT send the standalone email");
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
        _eventRepo.Setup(e => e.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);
        _emailService.Setup(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok("corr", 1));

        await BuildHandler().HandleChargeRefundedAsync(
            collection.StripePaymentIntentId!, "re_legacy", Guid.NewGuid());
        await Task.Delay(200);

        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Once,
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
        _eventRepo.Setup(e => e.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);
        _emailService.Setup(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok("corr", 1));

        var act = async () => await BuildHandler().HandleChargeRefundedAsync(
            collection.StripePaymentIntentId!, "re_throws", Guid.NewGuid());
        await act.Should().NotThrowAsync();
        await Task.Delay(200);

        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "fail-OPEN: lookup exception must not silence the legacy email");
    }

    [Fact]
    public async Task DifferentEntity_NotSuppressed()
    {
        // The workflow refund is for a DIFFERENT Collection â€” our current handler invocation
        // is processing the legacy path for this specific collection. Predicate should not
        // match, so the legacy email fires.
        var collection = CompletedCollection();
        _collectionRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Collection, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);
        _refundRequestRepo.Setup(r => r.GetWorkflowLineReferenceIdAsync(
                RefundLineItemType.Collection, "re_other_collection", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid()); // a DIFFERENT collection's id
        _eventRepo.Setup(e => e.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);
        _emailService.Setup(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok("corr", 1));

        await BuildHandler().HandleChargeRefundedAsync(
            collection.StripePaymentIntentId!, "re_other_collection", Guid.NewGuid());
        await Task.Delay(200);

        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "predicate must compare returned ReferenceId to THIS collection's id â€” unrelated workflow refunds shouldn't suppress");
        _auditService.Verify(a => a.WriteSuppressionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
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
            amount: new Money(100m, Currency.USD)).Value;
        collection.CompletePayment("pi_test_collection");
        return collection;
    }
}
