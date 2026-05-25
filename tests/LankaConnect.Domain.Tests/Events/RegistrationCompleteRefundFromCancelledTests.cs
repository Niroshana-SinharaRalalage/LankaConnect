using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 6A.148.W5.D4 — <see cref="Registration.CompleteRefundFromCancelled"/> domain transition.
///
/// Architect-mandated new transition (W5 plan Issue #6). In the decoupled cancellation/
/// refund model, an attendee may Cancel WITHOUT triggering a refund (no money moves);
/// the refund flows separately through the 6A.148 approval workflow. When the workflow's
/// final Stripe webhook (charge.refunded for the ticket portion) lands and routes through
/// the W5.D5 RegistrationWebhookHandler workflow-aware branch, the registration may
/// already be in <c>Cancelled</c> state — the existing <see cref="Registration.CompleteRefund"/>
/// method refuses this (requires RefundRequested). This method bridges the gap.
///
/// Pinned invariants:
/// - Allowed from {RefundRequested, Cancelled}
/// - Refused from {Pending, Confirmed, Abandoned, PendingRefundApproval, Refunded-with-different-srid}
/// - Idempotent — second call with same stripeRefundId on an already-Refunded row is a no-op Success
/// - Phase 6A.148.W5.6.B G1 — RefundCompletedEvent is raised ONLY when fromState is
///   RefundRequested (legacy direct-Stripe CancelRsvp path with no RefundRequest). The
///   Cancelled-from path (new workflow) MUST NOT raise it — the workflow's
///   RefundRequest.MarkCompletedIfAllSettled raises RefundRequestCompletedEvent instead,
///   at the EXACT moment Status flips to Completed. Raising both would produce the
///   $94-vs-$204 duplicate-email regression we are closing.
/// - Raises SeatReservationsReleasedEvent in both paths.
/// </summary>
public class RegistrationCompleteRefundFromCancelledTests
{
    private const string Sri = "re_w5d4_test_001";

    private static Registration BuildCancelledRegistration()
    {
        var alice = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female).Value;
        var contact = RegistrationContact.Create("alice@example.com", "8609780124", null, null, false).Value;
        var price = Money.Create(50m, Currency.USD).Value;
        var reg = Registration.CreateWithAttendees(
            Guid.NewGuid(), Guid.NewGuid(), new[] { alice }, contact, price, isPaidEvent: true).Value;
        reg.CompletePayment("pi_w5d4_cancelled").IsSuccess.Should().BeTrue();
        reg.Cancel();
        reg.ClearDomainEvents();
        return reg;
    }

    private static Registration BuildRefundRequestedRegistration()
    {
        var alice = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female).Value;
        var contact = RegistrationContact.Create("alice@example.com", "8609780124", null, null, false).Value;
        var price = Money.Create(50m, Currency.USD).Value;
        var reg = Registration.CreateWithAttendees(
            Guid.NewGuid(), Guid.NewGuid(), new[] { alice }, contact, price, isPaidEvent: true).Value;
        reg.CompletePayment("pi_w5d4_refundrequested").IsSuccess.Should().BeTrue();
        reg.RequestRefund().IsSuccess.Should().BeTrue();
        reg.ClearDomainEvents();
        return reg;
    }

    [Fact]
    public void CompleteRefundFromCancelled_FromCancelled_TransitionsToRefunded()
    {
        var reg = BuildCancelledRegistration();

        var result = reg.CompleteRefundFromCancelled(Sri);

        result.IsSuccess.Should().BeTrue();
        reg.Status.Should().Be(RegistrationStatus.Refunded);
        reg.PaymentStatus.Should().Be(PaymentStatus.Refunded);
        reg.StripeRefundId.Should().Be(Sri);
        reg.RefundCompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void CompleteRefundFromCancelled_FromCancelled_DoesNotRaiseLegacyRefundCompletedEvent()
    {
        // Phase 6A.148.W5.6.B G1 — the workflow path MUST NOT raise the legacy event.
        // RefundRequest.MarkCompletedIfAllSettled is the sole email driver for workflow
        // refunds; raising the legacy event from here too would re-introduce the mid-race
        // $94 email-undercount regression.
        var reg = BuildCancelledRegistration();

        reg.CompleteRefundFromCancelled(Sri).IsSuccess.Should().BeTrue();

        reg.DomainEvents.OfType<RefundCompletedEvent>().Should().BeEmpty(
            "workflow-path completion is driven by RefundRequestCompletedEvent (raised " +
            "from MarkCompletedIfAllSettled with the precomputed final total); raising " +
            "the legacy event here too would duplicate the email with a mid-race snapshot");
    }

    [Fact]
    public void CompleteRefundFromCancelled_FromRefundRequested_StillRaisesLegacyRefundCompletedEvent()
    {
        // Defensive — the same domain method also works for the legacy RefundRequested
        // path (no RefundRequest aggregate exists for pre-148 direct-Stripe refunds, so
        // RefundRequestCompletedEvent will never fire there). The legacy
        // RefundCompletedEvent is the only thing that drives the completion email for
        // that path — it MUST still raise here.
        var reg = BuildRefundRequestedRegistration();

        var result = reg.CompleteRefundFromCancelled(Sri);

        result.IsSuccess.Should().BeTrue();
        reg.Status.Should().Be(RegistrationStatus.Refunded);
        reg.DomainEvents.OfType<RefundCompletedEvent>().Should().ContainSingle(
            "legacy direct-Stripe path has no RefundRequest aggregate, so the legacy " +
            "event is the only completion-email driver — must not be suppressed");
    }

    [Fact]
    public void CompleteRefundFromCancelled_AlreadyRefundedWithSameSri_IsNoOpSuccess()
    {
        // Idempotency guard — Stripe webhooks can fire multiple times.
        var reg = BuildCancelledRegistration();
        reg.CompleteRefundFromCancelled(Sri).IsSuccess.Should().BeTrue();
        reg.ClearDomainEvents();

        var second = reg.CompleteRefundFromCancelled(Sri);

        second.IsSuccess.Should().BeTrue();
        reg.DomainEvents.OfType<RefundCompletedEvent>().Should().BeEmpty(
            "second call must not re-raise the completion event");
    }

    [Fact]
    public void CompleteRefundFromCancelled_AlreadyRefundedWithDifferentSri_Fails()
    {
        // Different Stripe refund ID arriving on an already-completed registration is a
        // real signal (e.g. two separate refunds against the same registration). Refuse
        // and surface for ops triage.
        var reg = BuildCancelledRegistration();
        reg.CompleteRefundFromCancelled(Sri).IsSuccess.Should().BeTrue();

        var result = reg.CompleteRefundFromCancelled("re_different_001");

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(RegistrationStatus.Confirmed)]
    [InlineData(RegistrationStatus.Preliminary)]
    [InlineData(RegistrationStatus.Abandoned)]
    public void CompleteRefundFromCancelled_FromUnsupportedStatuses_Fails(RegistrationStatus invalid)
    {
        var reg = BuildCancelledRegistration();
        // Force the registration into the unsupported state via reflection so we test
        // the guard rather than the build-path validity.
        var statusProp = typeof(Registration).GetProperty(nameof(Registration.Status),
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);
        statusProp!.SetValue(reg, invalid);

        var result = reg.CompleteRefundFromCancelled(Sri);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CompleteRefundFromCancelled_NullOrEmptySri_Fails()
    {
        var reg = BuildCancelledRegistration();

        reg.CompleteRefundFromCancelled(null!).IsFailure.Should().BeTrue();
        reg.CompleteRefundFromCancelled("").IsFailure.Should().BeTrue();
        reg.CompleteRefundFromCancelled("  ").IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CompleteRefundFromCancelled_RaisesSeatReservationsReleasedEvent()
    {
        var reg = BuildCancelledRegistration();

        reg.CompleteRefundFromCancelled(Sri).IsSuccess.Should().BeTrue();

        reg.DomainEvents.OfType<LankaConnect.Domain.Events.DomainEvents.SeatReservationsReleasedEvent>()
            .Should().ContainSingle()
            .Which.Reason.Should().Be("refund_completed_from_cancelled");
    }
}
