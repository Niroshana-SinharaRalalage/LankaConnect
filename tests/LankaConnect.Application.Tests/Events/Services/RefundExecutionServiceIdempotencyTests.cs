using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Services;

/// <summary>
/// Phase 6A.148.W5.D1 — Stripe <see cref="CreateRefundRequest.IdempotencyKey"/> contract.
///
/// Root cause (W5.D7 incident on RR 624b07c5): RefundExecutionService.DispatchAsync ran
/// Stripe successfully per line, then terminal _uow.CommitAsync() threw
/// DbUpdateConcurrencyException (xmin clash with concurrent Cancel flow). All in-memory
/// line.MarkRefunded() / line.MarkProcessing(refundId) changes rolled back. Stripe kept
/// the money. Stuck-Approved lines now have NO way to re-call Stripe safely because the
/// legacy default key (PaymentIntentId + Amount + RegistrationId) is keyed on facts
/// independent of which line is being retried — re-dispatch would create duplicate
/// refunds.
///
/// Fix: every workflow-path refund line gets a STABLE per-line idempotency key
/// $"refund_line_{line.Id:N}". Stripe's 24h at-most-one-success guarantee makes
/// re-dispatch from the reconciler automatically safe — same line + same key = same
/// outcome, never a second charge.
///
/// These tests pin the key format. Changing it requires understanding that pre-existing
/// stuck rows whose keys differ would lose their idempotency-by-Stripe protection.
/// </summary>
public class RefundExecutionServiceIdempotencyTests
{
    private readonly Mock<IRefundRequestRepository> _refundRepo = new();
    private readonly Mock<IRegistrationRepository> _registrationRepo = new();
    private readonly Mock<IRegistrationPaymentRepository> _paymentRepo = new();
    private readonly Mock<IAddOnPurchaseRepository> _addOnRepo = new();
    private readonly Mock<ICollectionRepository> _collectionRepo = new();
    private readonly Mock<ISponsorRepository> _sponsorRepo = new();
    private readonly Mock<IStripePaymentService> _stripe = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private RefundExecutionService Build()
        => new(
            _refundRepo.Object,
            _registrationRepo.Object,
            _paymentRepo.Object,
            _addOnRepo.Object,
            _collectionRepo.Object,
            _sponsorRepo.Object,
            _stripe.Object,
            _uow.Object,
            Mock.Of<ILogger<RefundExecutionService>>());

    [Fact]
    public async Task DispatchAsync_SponsorLine_PassesStableIdempotencyKey_BasedOnLineId()
    {
        // Arrange — registration with one approved Sponsor line item.
        var attendeeUser = Guid.NewGuid();
        var organizerUser = Guid.NewGuid();
        var sponsorRefId = Guid.NewGuid();
        var sponsorPi = "pi_test_w5d1_sponsor";

        var reg = BuildRegistrationWithApprovedSponsorRefund(
            attendeeUser, organizerUser, sponsorRefId, out var rrId, out var lineId);

        var sponsor = Sponsor.CreateMoneySponsor(
            eventId: reg.EventId,
            sponsorUserId: attendeeUser,
            sponsorName: "Test Sponsor",
            sponsorEmail: "sponsor@example.com",
            sponsorPhone: null,
            sponsorOrganization: null,
            sponsorNotes: null,
            amount: Money.Create(50m, Currency.USD).Value).Value;
        sponsor.CompletePayment(sponsorPi).IsSuccess.Should().BeTrue();
        SetSponsorId(sponsor, sponsorRefId);

        _refundRepo.Setup(r => r.GetByIdAsync(rrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reg.RefundRequests.Single(r => r.Id == rrId));
        _registrationRepo.Setup(r => r.GetByIdAsync(reg.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reg);
        _sponsorRepo.Setup(s => s.GetByIdAsync(sponsorRefId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);

        CreateRefundRequest? capturedReq = null;
        _stripe.Setup(s => s.CreateRefundAsync(It.IsAny<CreateRefundRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateRefundRequest, CancellationToken>((req, _) => capturedReq = req)
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = "re_test_w5d1",
                Status = "succeeded",
                AmountRefunded = 5000
            }));

        // Act
        var result = await Build().DispatchAsync(rrId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedReq.Should().NotBeNull("DispatchAsync must call Stripe CreateRefundAsync exactly once");
        capturedReq!.IdempotencyKey.Should().Be(
            $"refund_line_{lineId:N}",
            "W5.D1 contract — every workflow line gets a stable, line-scoped key so reconciler re-dispatch is safe");
        capturedReq.IdempotencyKey.Should().NotBeNullOrWhiteSpace();
        capturedReq.PaymentIntentId.Should().Be(sponsorPi);
        capturedReq.Metadata.Should().ContainKey("refund_type").WhoseValue.Should().Be("sponsor");
        capturedReq.Metadata.Should().ContainKey("line_item_id").WhoseValue.Should().Be(lineId.ToString());
    }

    /// <summary>
    /// W5.D1 — Stripe IdempotencyKey is line-scoped (not amount-scoped, not PI-scoped),
    /// so calling DispatchAsync twice on the same line produces the SAME key (and Stripe
    /// returns the prior refund object on the second call rather than charging twice).
    /// This is the contract that makes W5.D6 reconciler re-dispatch safe.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_TwoCallsForSameLine_ProduceIdenticalIdempotencyKey()
    {
        var attendeeUser = Guid.NewGuid();
        var organizerUser = Guid.NewGuid();
        var sponsorRefId = Guid.NewGuid();

        var reg = BuildRegistrationWithApprovedSponsorRefund(
            attendeeUser, organizerUser, sponsorRefId, out var rrId, out var lineId);

        var sponsor = Sponsor.CreateMoneySponsor(
            reg.EventId, attendeeUser, "S", "s@e.com", null, null, null,
            Money.Create(50m, Currency.USD).Value).Value;
        sponsor.CompletePayment("pi_w5d1_b").IsSuccess.Should().BeTrue();
        SetSponsorId(sponsor, sponsorRefId);

        _refundRepo.Setup(r => r.GetByIdAsync(rrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reg.RefundRequests.Single(r => r.Id == rrId));
        _registrationRepo.Setup(r => r.GetByIdAsync(reg.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reg);
        _sponsorRepo.Setup(s => s.GetByIdAsync(sponsorRefId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);

        var captured = new List<string?>();
        _stripe.Setup(s => s.CreateRefundAsync(It.IsAny<CreateRefundRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateRefundRequest, CancellationToken>((req, _) => captured.Add(req.IdempotencyKey))
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = "re_w5d1_idempotent",
                Status = "succeeded",
                AmountRefunded = 5000
            }));

        var sut = Build();
        // Call once — line moves to Refunded
        (await sut.DispatchAsync(rrId)).IsSuccess.Should().BeTrue();

        // Reset the line AND the request to Approved so the second call also dispatches.
        // This simulates the W5.D7 root-cause scenario: prior dispatch's Stripe calls
        // succeeded, then commit rolled back — DB rehydrates the line + RR back to
        // Approved on the next reconciler load. The mocked repo returns the same
        // in-memory instance, so we reset state by reflection.
        var line = reg.RefundRequests.Single().LineItems.Single();
        ResetLineToApproved(line);
        ResetRefundRequestToApproved(reg.RefundRequests.Single());

        // Second call — same line.Id → same key
        (await sut.DispatchAsync(rrId)).IsSuccess.Should().BeTrue();

        captured.Should().HaveCount(2);
        captured[0].Should().Be(captured[1], "same line.Id must always produce the same Stripe IdempotencyKey");
        captured[0].Should().Be($"refund_line_{lineId:N}");
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private Registration BuildRegistrationWithApprovedSponsorRefund(
        Guid attendeeUser, Guid organizerUser, Guid sponsorRefId, out Guid rrId, out Guid lineId)
    {
        var eventId = Guid.NewGuid();
        var alice = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female).Value;
        var contact = RegistrationContact.Create("alice@example.com", "8609780124", null, null, false).Value;
        var price = Money.Create(50m, Currency.USD).Value;
        var reg = Registration.CreateWithAttendees(
            eventId, attendeeUser, new[] { alice }, contact, price, isPaidEvent: true).Value;
        reg.CompletePayment("pi_w5d1_reg").IsSuccess.Should().BeTrue();

        var lineItems = new[]
        {
            new RefundRequestLineItemInput(
                RefundLineItemType.Sponsor, sponsorRefId, new Money(50m, Currency.USD))
        };
        var createResult = reg.CreateRefundRequest(
            requestedByUserId: attendeeUser,
            isOrganizerInitiated: false,
            requesterReason: "Test",
            organizerNotes: null,
            overrideScanGuard: false,
            anyTicketsScanned: false,
            lineItems: lineItems);
        createResult.IsSuccess.Should().BeTrue();
        rrId = createResult.Value.Id;

        var line = createResult.Value.LineItems.Single();
        lineId = line.Id;

        var approve = reg.ApproveRefundRequest(
            refundRequestId: rrId,
            organizerUserId: organizerUser,
            organizerNotes: "ok",
            perLineApprovedAmounts: new Dictionary<Guid, Money>
            {
                [line.Id] = new Money(50m, Currency.USD)
            });
        approve.IsSuccess.Should().BeTrue();
        reg.ClearDomainEvents();
        return reg;
    }

    private static void SetSponsorId(Sponsor sponsor, Guid id)
    {
        var prop = typeof(Sponsor).GetProperty(nameof(Sponsor.Id),
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);
        prop!.SetValue(sponsor, id);
    }

    private static void ResetLineToApproved(RefundRequestLineItem line)
    {
        // Reflection — simulates the line being re-loaded from DB in an Approved state
        // after a prior dispatch's commit rolled back. The domain doesn't expose a
        // "reset" transition (correctly — production code should never need it), so
        // tests bypass it directly.
        var statusProp = typeof(RefundRequestLineItem).GetProperty(nameof(RefundRequestLineItem.Status),
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);
        statusProp!.SetValue(line, RefundLineItemStatus.Approved);

        var sriProp = typeof(RefundRequestLineItem).GetProperty(nameof(RefundRequestLineItem.StripeRefundId),
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);
        sriProp!.SetValue(line, null);
    }

    private static void ResetRefundRequestToApproved(RefundRequest req)
    {
        // Same reflection pattern as the line — simulates RR being re-loaded from
        // DB in Approved state after a prior dispatch's commit rolled back.
        var statusProp = typeof(RefundRequest).GetProperty(nameof(RefundRequest.Status),
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);
        statusProp!.SetValue(req, RefundRequestStatus.Approved);
    }
}
