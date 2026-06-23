using LankaConnect.Modules.Payments.Application.Services; // W4.4.c.4: service impls moved here (interfaces stay in legacy)
using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Modules.Payments.Application.Tests.Services;

/// <summary>
/// Phase 6A.148.W5.D1 — Stripe <see cref="CreateRefundRequest.IdempotencyKey"/> contract
/// pinned at the boundary between <see cref="RefundLineDispatcher"/> and
/// <see cref="IStripePaymentService"/>.
///
/// After the W5.D2 restructure, the per-line dispatcher is the only place that builds the
/// <c>CreateRefundRequest</c>. These tests assert the key format directly through
/// <c>RefundLineDispatcher</c> using a mocked Stripe service.
///
/// Why this contract matters: the W5.D7 root-cause scenario was that the prior
/// <c>RefundExecutionService.DispatchAsync</c> ran Stripe successfully, then
/// <c>_uow.CommitAsync()</c> threw a DbUpdateConcurrencyException (xmin clash with a
/// concurrent Cancel flow), rolling back all in-memory MarkRefunded/MarkProcessing
/// changes. Stuck-Approved lines now have NO way to re-call Stripe safely UNLESS Stripe
/// itself recognises the retry via an idempotency key. W5.D2 plus this contract make
/// W5.D6 reconciler re-dispatch automatically safe.
/// </summary>
public class RefundExecutionServiceIdempotencyTests
{
    [Fact]
    public async Task RefundLineDispatcher_PassesStableIdempotencyKey_BasedOnLineId()
    {
        // Arrange — build a tracked line item snapshot the dispatcher will resolve.
        var lineId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var sponsorRefId = Guid.NewGuid();
        var sponsorPi = "pi_test_w5d1_sponsor";

        var line = BuildApprovedSponsorLine(lineId, sponsorRefId, amount: 50m);

        var sponsor = BuildCompletedSponsor(sponsorRefId, sponsorPi);

        // Mocks scoped to a per-call IServiceProvider that the dispatcher resolves from.
        var refundRepo = new Mock<IRefundRequestRepository>();
        refundRepo.Setup(r => r.GetLineItemByIdAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(line);

        var sponsorRepo = new Mock<ISponsorRepository>();
        sponsorRepo.Setup(s => s.GetByIdAsync(sponsorRefId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);

        var paymentRepo = new Mock<IRegistrationPaymentRepository>();
        var addOnRepo = new Mock<IAddOnPurchaseRepository>();
        var collectionRepo = new Mock<ICollectionRepository>();
        var registrationRepo = new Mock<IRegistrationRepository>();
        var uow = new Mock<IUnitOfWork>();

        CreateRefundRequest? capturedReq = null;
        var stripe = new Mock<IStripePaymentService>();
        stripe.Setup(s => s.CreateRefundAsync(It.IsAny<CreateRefundRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateRefundRequest, CancellationToken>((req, _) => capturedReq = req)
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = "re_test_w5d1",
                Status = "succeeded",
                AmountRefunded = 5000
            }));

        var scopeFactory = BuildScopeFactory(sp =>
        {
            sp.AddSingleton(refundRepo.Object);
            sp.AddSingleton(sponsorRepo.Object);
            sp.AddSingleton(paymentRepo.Object);
            sp.AddSingleton(addOnRepo.Object);
            sp.AddSingleton(collectionRepo.Object);
            sp.AddSingleton(registrationRepo.Object);
            sp.AddSingleton(stripe.Object);
            sp.AddSingleton(uow.Object);
        });

        var dispatcher = new RefundLineDispatcher(scopeFactory, Mock.Of<ILogger<RefundLineDispatcher>>());

        // Act
        var result = await dispatcher.DispatchAsync(lineId, registrationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedReq.Should().NotBeNull("dispatcher must call Stripe CreateRefundAsync exactly once");
        capturedReq!.IdempotencyKey.Should().Be(
            $"refund_line_{lineId:N}",
            "W5.D1 contract — every workflow line gets a stable, line-scoped key so reconciler re-dispatch is safe");
        capturedReq.PaymentIntentId.Should().Be(sponsorPi);
        capturedReq.Metadata.Should().ContainKey("refund_type").WhoseValue.Should().Be("sponsor");
        capturedReq.Metadata.Should().ContainKey("line_item_id").WhoseValue.Should().Be(lineId.ToString());
        uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once,
            "W5.D2 — scoped UoW must commit exactly once per line dispatch");
    }

    /// <summary>
    /// Calling the dispatcher twice for the same line.Id produces the SAME Stripe
    /// idempotency key — the contract that makes W5.D6 reconciler re-dispatch safe.
    /// (Stripe deduplicates by the key for 24h; same key = at-most-one successful refund.)
    /// </summary>
    [Fact]
    public async Task RefundLineDispatcher_TwoCallsForSameLine_ProduceIdenticalIdempotencyKey()
    {
        var lineId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var sponsorRefId = Guid.NewGuid();

        var refundRepo = new Mock<IRefundRequestRepository>();
        // Each call returns a FRESHLY-built Approved line — simulates what the reconciler
        // would see after the prior dispatch's transaction rolled back.
        refundRepo.Setup(r => r.GetLineItemByIdAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => BuildApprovedSponsorLine(lineId, sponsorRefId, amount: 50m));

        var sponsorRepo = new Mock<ISponsorRepository>();
        sponsorRepo.Setup(s => s.GetByIdAsync(sponsorRefId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => BuildCompletedSponsor(sponsorRefId, "pi_w5d1_b"));

        var captured = new List<string?>();
        var stripe = new Mock<IStripePaymentService>();
        stripe.Setup(s => s.CreateRefundAsync(It.IsAny<CreateRefundRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateRefundRequest, CancellationToken>((req, _) => captured.Add(req.IdempotencyKey))
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = "re_w5d1_idempotent",
                Status = "succeeded",
                AmountRefunded = 5000
            }));

        var scopeFactory = BuildScopeFactory(sp =>
        {
            sp.AddSingleton(refundRepo.Object);
            sp.AddSingleton(sponsorRepo.Object);
            sp.AddSingleton(new Mock<IRegistrationPaymentRepository>().Object);
            sp.AddSingleton(new Mock<IAddOnPurchaseRepository>().Object);
            sp.AddSingleton(new Mock<ICollectionRepository>().Object);
            sp.AddSingleton(new Mock<IRegistrationRepository>().Object);
            sp.AddSingleton(stripe.Object);
            sp.AddSingleton(new Mock<IUnitOfWork>().Object);
        });

        var dispatcher = new RefundLineDispatcher(scopeFactory, Mock.Of<ILogger<RefundLineDispatcher>>());

        (await dispatcher.DispatchAsync(lineId, registrationId)).IsSuccess.Should().BeTrue();
        (await dispatcher.DispatchAsync(lineId, registrationId)).IsSuccess.Should().BeTrue();

        captured.Should().HaveCount(2);
        captured[0].Should().Be(captured[1], "same line.Id must always produce the same Stripe IdempotencyKey");
        captured[0].Should().Be($"refund_line_{lineId:N}");
    }

    // ========================================================================
    // Helpers — build entities via reflection so tests don't pin private setters
    // ========================================================================

    private static LankaConnect.Domain.Events.Entities.RefundRequestLineItem BuildApprovedSponsorLine(
        Guid lineId, Guid sponsorRefId, decimal amount)
    {
        var line = (LankaConnect.Domain.Events.Entities.RefundRequestLineItem)
            System.Runtime.CompilerServices.RuntimeHelpers
                .GetUninitializedObject(typeof(LankaConnect.Domain.Events.Entities.RefundRequestLineItem));
        SetProp(line, nameof(line.Id), lineId);
        SetProp(line, nameof(line.RefundRequestId), Guid.NewGuid());
        SetProp(line, nameof(line.Type), LankaConnect.Domain.Events.Enums.RefundLineItemType.Sponsor);
        SetProp(line, nameof(line.ReferenceId), sponsorRefId);
        SetProp(line, nameof(line.Status), LankaConnect.Domain.Events.Enums.RefundLineItemStatus.Approved);
        var money = new LankaConnect.Domain.Shared.ValueObjects.Money(
            amount, LankaConnect.Domain.Shared.Enums.Currency.USD);
        SetProp(line, nameof(line.RequestedAmount), money);
        SetProp(line, nameof(line.ApprovedAmount), money);
        return line;
    }

    private static LankaConnect.Domain.Events.Sponsor BuildCompletedSponsor(Guid id, string paymentIntentId)
    {
        var sponsor = LankaConnect.Domain.Events.Sponsor.CreateMoneySponsor(
            eventId: Guid.NewGuid(),
            sponsorUserId: Guid.NewGuid(),
            sponsorName: "Test Sponsor",
            sponsorEmail: "sponsor@example.com",
            sponsorPhone: null,
            sponsorOrganization: null,
            sponsorNotes: null,
            amount: LankaConnect.Domain.Shared.ValueObjects.Money.Create(
                50m, LankaConnect.Domain.Shared.Enums.Currency.USD).Value).Value;
        sponsor.CompletePayment(paymentIntentId);
        SetProp(sponsor, nameof(sponsor.Id), id);
        return sponsor;
    }

    private static void SetProp(object target, string name, object? value)
    {
        var prop = target.GetType().GetProperty(name,
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);
        prop!.SetValue(target, value);
    }

    /// <summary>
    /// Builds an IServiceScopeFactory whose CreateScope() yields an IServiceProvider
    /// configured via the supplied builder action. Each call to <c>CreateScope</c>
    /// returns a fresh container so per-line state stays isolated between dispatches.
    /// </summary>
    private static IServiceScopeFactory BuildScopeFactory(Action<IServiceCollection> configure)
    {
        // Build a parent provider that resolves IServiceScopeFactory normally; the parent
        // scope's services are the mocked singletons configured by `configure`.
        var services = new ServiceCollection();
        configure(services);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }
}
