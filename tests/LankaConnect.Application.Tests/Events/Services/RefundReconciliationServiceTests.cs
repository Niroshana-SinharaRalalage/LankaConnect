using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Services;

/// <summary>
/// Phase 7G — tests for the durable refund-reconciliation safety net.
///
/// The service queries the existing <c>IRegistrationRepository.GetStuckRefundsAsync</c>
/// for rows in <c>RefundRequested</c> beyond the grace window, looks up each one
/// against Stripe via <c>IStripePaymentService.GetRefundStatusAsync</c>, and on
/// "succeeded" calls <c>Registration.CompleteRefund</c> — the same domain transition
/// the webhook handler uses, so all downstream effects (email, WhatsApp, ticket
/// PDF state) fire identically. Behaviour matrix below maps each Stripe status
/// outcome to a counter on <see cref="RefundReconciliationResult"/>.
/// </summary>
public class RefundReconciliationServiceTests
{
    private readonly Mock<IRegistrationRepository> _registrationRepo = new();
    private readonly Mock<IStripePaymentService> _stripeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    // Phase 6A.148.W5.D6: deps required for stuck-Approved reconciliation. Existing tests
    // only exercise the stuck-RefundRequested code path, but the constructor now demands
    // these so DI catches misconfiguration at boot.
    private readonly Mock<LankaConnect.Domain.Events.Repositories.IRefundRequestRepository> _refundRequestRepo = new();
    private readonly Mock<IRefundExecutionService> _refundExecutionService = new();

    private RefundReconciliationService Build()
    {
        return new RefundReconciliationService(
            _registrationRepo.Object,
            _stripeService.Object,
            _unitOfWork.Object,
            Mock.Of<ILogger<RefundReconciliationService>>(),
            _refundRequestRepo.Object,
            _refundExecutionService.Object);
    }

    private static Registration BuildStuckRegistration(
        Guid? id = null,
        string? refundId = "re_stuck_001",
        DateTime? refundRequestedAt = null)
    {
        // Build a Registration aggregate already in RefundRequested state.
        // The simplest path: instantiate via reflection-light setup since the
        // public domain transitions require valid Event/User context. The
        // properties we need to drive the service are RefundRequestedAt,
        // StripeRefundId, and the public CompleteRefund state machine.
        var registration = (Registration)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(Registration));

        // Force private setters via reflection — same pattern used by other
        // application tests in this suite (see GetLayoutPublishReadinessQueryHandlerTests).
        SetProp(registration, nameof(Registration.Id), id ?? Guid.NewGuid());
        SetProp(registration, nameof(Registration.EventId), Guid.NewGuid());
        SetProp(registration, nameof(Registration.UserId), Guid.NewGuid());
        SetProp(registration, nameof(Registration.Status), RegistrationStatus.RefundRequested);
        SetProp(registration, nameof(Registration.PaymentStatus), PaymentStatus.Completed);
        SetProp(registration, nameof(Registration.StripePaymentIntentId), "pi_test_001");
        SetProp(registration, nameof(Registration.StripeRefundId), refundId);
        SetProp(registration, nameof(Registration.RefundRequestedAt),
            refundRequestedAt ?? DateTime.UtcNow.AddHours(-1));
        SetProp(registration, nameof(Registration.TotalPrice),
            Money.Create(100m, Currency.USD).Value);
        // Domain events list backing field is NOT readable via property, so we
        // leave it as default (empty). This is fine because CompleteRefund
        // raises into the entity's protected backing list.
        InitDomainEventsBackingField(registration);
        return registration;
    }

    private static void SetProp(object target, string name, object? value)
    {
        var prop = target.GetType().GetProperty(name,
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);
        prop!.SetValue(target, value);
    }

    private static void InitDomainEventsBackingField(Registration registration)
    {
        // BaseEntity._domainEvents is the typical backing field name. Use
        // reflection so the test doesn't break if the field is renamed —
        // we just default to an empty list when found.
        var fieldNames = new[] { "_domainEvents", "domainEvents" };
        foreach (var name in fieldNames)
        {
            var field = typeof(LankaConnect.Domain.Common.BaseEntity)
                .GetField(name, System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);
            if (field != null && field.GetValue(registration) == null)
            {
                var listType = typeof(List<>).MakeGenericType(
                    field.FieldType.GetGenericArguments());
                field.SetValue(registration, Activator.CreateInstance(listType));
                return;
            }
        }
    }

    [Fact]
    public async Task ReconcileStuckRefundsAsync_NoStuckRows_Should_Return_AllZeroes_NoCommit()
    {
        _registrationRepo.Setup(r => r.GetStuckRefundsAsync(
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Registration>());

        var sut = Build();
        var result = await sut.ReconcileStuckRefundsAsync(batchSize: 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScannedCount.Should().Be(0);
        result.Value.ReconciledCount.Should().Be(0);
        _stripeService.Verify(s => s.GetRefundStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReconcileStuckRefundsAsync_StripeSaysSucceeded_Should_TransitionToRefunded_AndCommit()
    {
        var stuck = BuildStuckRegistration();
        _registrationRepo.Setup(r => r.GetStuckRefundsAsync(
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { stuck });
        _stripeService.Setup(s => s.GetRefundStatusAsync("re_stuck_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = "re_stuck_001",
                Status = "succeeded",
                AmountRefunded = 10000,
            }));

        var sut = Build();
        var result = await sut.ReconcileStuckRefundsAsync(batchSize: 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScannedCount.Should().Be(1);
        result.Value.ReconciledCount.Should().Be(1);
        stuck.Status.Should().Be(RegistrationStatus.Refunded);
        stuck.PaymentStatus.Should().Be(PaymentStatus.Refunded);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReconcileStuckRefundsAsync_StripeSaysPending_Should_LeaveRowAlone_NoCommit()
    {
        var stuck = BuildStuckRegistration();
        _registrationRepo.Setup(r => r.GetStuckRefundsAsync(
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { stuck });
        _stripeService.Setup(s => s.GetRefundStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = "re_stuck_001",
                Status = "pending",
                AmountRefunded = 10000,
            }));

        var sut = Build();
        var result = await sut.ReconcileStuckRefundsAsync(batchSize: 10);

        result.Value.StillPendingCount.Should().Be(1);
        result.Value.ReconciledCount.Should().Be(0);
        stuck.Status.Should().Be(RegistrationStatus.RefundRequested);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReconcileStuckRefundsAsync_StripeSaysFailed_Should_Count_NoTransition()
    {
        var stuck = BuildStuckRegistration();
        _registrationRepo.Setup(r => r.GetStuckRefundsAsync(
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { stuck });
        _stripeService.Setup(s => s.GetRefundStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = "re_stuck_001",
                Status = "failed",
                AmountRefunded = 0,
            }));

        var sut = Build();
        var result = await sut.ReconcileStuckRefundsAsync(batchSize: 10);

        result.Value.FailedAtStripeCount.Should().Be(1);
        result.Value.ReconciledCount.Should().Be(0);
        stuck.Status.Should().Be(RegistrationStatus.RefundRequested);
        result.Value.Warnings.Should().Contain(w => w.Contains("failed"));
    }

    [Fact]
    public async Task ReconcileStuckRefundsAsync_RowMissingStripeRefundId_Should_Skip_AndCount()
    {
        var stuck = BuildStuckRegistration(refundId: null);
        _registrationRepo.Setup(r => r.GetStuckRefundsAsync(
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { stuck });

        var sut = Build();
        var result = await sut.ReconcileStuckRefundsAsync(batchSize: 10);

        result.Value.MissingRefundIdCount.Should().Be(1);
        result.Value.ReconciledCount.Should().Be(0);
        _stripeService.Verify(s => s.GetRefundStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReconcileStuckRefundsAsync_StripeLookupFails_Should_Continue_OtherRows()
    {
        var ok = BuildStuckRegistration(refundId: "re_ok");
        var bad = BuildStuckRegistration(refundId: "re_bad");

        _registrationRepo.Setup(r => r.GetStuckRefundsAsync(
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { bad, ok });

        _stripeService.Setup(s => s.GetRefundStatusAsync("re_bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StripeRefundResult>.Failure("stripe api 502"));
        _stripeService.Setup(s => s.GetRefundStatusAsync("re_ok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = "re_ok",
                Status = "succeeded",
                AmountRefunded = 10000,
            }));

        var sut = Build();
        var result = await sut.ReconcileStuckRefundsAsync(batchSize: 10);

        result.Value.StripeLookupFailedCount.Should().Be(1);
        result.Value.ReconciledCount.Should().Be(1);
        ok.Status.Should().Be(RegistrationStatus.Refunded);
        bad.Status.Should().Be(RegistrationStatus.RefundRequested);
    }

    [Fact]
    public async Task ReconcileStuckRefundsAsync_BatchSizeOverride_Should_Be_PassedToRepo()
    {
        _registrationRepo.Setup(r => r.GetStuckRefundsAsync(
                It.IsAny<DateTime>(), It.Is<int>(n => n == 7), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Registration>());

        var sut = Build();
        var result = await sut.ReconcileStuckRefundsAsync(batchSize: 7);

        result.IsSuccess.Should().BeTrue();
        _registrationRepo.Verify(r => r.GetStuckRefundsAsync(
            It.IsAny<DateTime>(), 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReconcileStuckRefundsAsync_AgeThresholdOverride_Should_RelaxRepoFilter()
    {
        // Default threshold is 10 min; explicit 0 should let a freshly-cancelled
        // row be reconciled. The repo gets called with `requestedBefore` set to
        // (approximately) "now" — verify the filter window expanded.
        DateTime? captured = null;
        _registrationRepo.Setup(r => r.GetStuckRefundsAsync(
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, int, CancellationToken>((before, _, _) => captured = before)
            .ReturnsAsync(Array.Empty<Registration>());

        var beforeCall = DateTime.UtcNow;
        var sut = Build();
        var result = await sut.ReconcileStuckRefundsAsync(batchSize: 10, ageThresholdMinutes: 0);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        // requestedBefore should be ~now (within a few seconds), proving the
        // 10-minute default was bypassed.
        (DateTime.UtcNow - captured!.Value).Should().BeLessThan(TimeSpan.FromSeconds(5));
        captured.Value.Should().BeOnOrAfter(beforeCall.AddSeconds(-1));
    }

    [Fact]
    public async Task ReconcileStuckRefundsAsync_NegativeAgeThreshold_Should_ClampToZero()
    {
        DateTime? captured = null;
        _registrationRepo.Setup(r => r.GetStuckRefundsAsync(
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, int, CancellationToken>((before, _, _) => captured = before)
            .ReturnsAsync(Array.Empty<Registration>());

        var sut = Build();
        var result = await sut.ReconcileStuckRefundsAsync(batchSize: 10, ageThresholdMinutes: -100);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        (DateTime.UtcNow - captured!.Value).Should().BeLessThan(TimeSpan.FromSeconds(5));
    }
}
