using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 6A.148.W4.D13.5 — Aggregate-level Approve / Reject / Withdraw methods on
/// <see cref="Registration"/>. These methods proxy to the child <see cref="RefundRequest"/>
/// entity AND re-raise the corresponding domain event from the Registration root with
/// EventId populated.
///
/// Root cause they fix (F1 / G3): the child entity raises its events with
/// <c>EventId=Guid.Empty</c> because it doesn't know its parent's EventId. Downstream
/// email handlers call <c>_eventRepository.GetByIdAsync(EventId)</c> — which returns
/// null for Guid.Empty — and exit silently with a WARN log. Empirically proven during
/// W4 API smoke test T7 Withdraw: the handler ran but emitted
/// <c>"event not found EventId=00000000-0000-0000-0000-000000000000"</c>.
///
/// Load-bearing assertion: after each aggregate-level method call, the Registration
/// root's <see cref="Registration.DomainEvents"/> collection contains the relevant
/// event with <c>EventId == reg.EventId</c> (not Guid.Empty).
///
/// Side-channel: the child entity still raises its own event with EventId=Empty for
/// backward compatibility with the existing direct-entity unit tests in
/// <c>RefundRequestTests.cs</c>. That event dispatches and the handler fails silently
/// (one WARN log line per fire); the root-level event with proper EventId dispatches
/// and the email actually sends. Acceptable trade-off vs touching all the child tests.
/// </summary>
public class RegistrationApproveRejectWithdrawRefundRequestTests
{
    private static readonly Guid AttendeeUser = Guid.NewGuid();
    private static readonly Guid OrganizerUser = Guid.NewGuid();
    private static readonly Guid TicketRef = Guid.NewGuid();
    private readonly Guid _eventId = Guid.NewGuid();

    private static Money Usd(decimal amount) => new(amount, Currency.USD);

    private Registration BuildRegistrationWithPendingRefund(out Guid refundRequestId)
    {
        var alice = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female).Value;
        var contact = RegistrationContact.Create(
            "alice@example.com", "8609780124", null, null, false).Value;
        var price = Money.Create(50m, Currency.USD).Value;
        var reg = Registration.CreateWithAttendees(
            _eventId, AttendeeUser, new[] { alice }, contact, price, isPaidEvent: true).Value;
        reg.CompletePayment("pi_test_d13_5").IsSuccess.Should().BeTrue();

        var lineItems = new[]
        {
            new RefundRequestLineItemInput(RefundLineItemType.Ticket, TicketRef, Usd(50m))
        };
        var createResult = reg.CreateRefundRequest(
            requestedByUserId: AttendeeUser,
            isOrganizerInitiated: false,
            requesterReason: "Test",
            organizerNotes: null,
            overrideScanGuard: false,
            anyTicketsScanned: false,
            lineItems: lineItems);
        createResult.IsSuccess.Should().BeTrue();
        refundRequestId = createResult.Value.Id;
        reg.ClearDomainEvents(); // clear the Created event so we only assert on the new one
        return reg;
    }

    // ========================================================================
    // Approve
    // ========================================================================

    [Fact]
    public void ApproveRefundRequest_ReRaisesEventOnRegistrationRoot_WithPopulatedEventId()
    {
        var reg = BuildRegistrationWithPendingRefund(out var rrId);
        var line = reg.RefundRequests.Single().LineItems.Single();
        var perLine = new Dictionary<Guid, Money> { [line.Id] = Usd(50m) };

        var result = reg.ApproveRefundRequest(
            refundRequestId: rrId,
            organizerUserId: OrganizerUser,
            organizerNotes: "looks good",
            perLineApprovedAmounts: perLine);

        result.IsSuccess.Should().BeTrue();
        var approvedEvent = reg.DomainEvents
            .OfType<RefundRequestApprovedEvent>()
            .Single();
        approvedEvent.EventId.Should().Be(_eventId, "the F1/G3 fix — event MUST carry the populated EventId so the email handler can resolve the Event entity");
        approvedEvent.EventId.Should().NotBe(Guid.Empty);
        approvedEvent.RegistrationId.Should().Be(reg.Id);
        approvedEvent.RefundRequestId.Should().Be(rrId);
        approvedEvent.OrganizerUserId.Should().Be(OrganizerUser);
    }

    [Fact]
    public void ApproveRefundRequest_WhenRefundRequestNotFound_ReturnsFailure_NoEventRaised()
    {
        var reg = BuildRegistrationWithPendingRefund(out _);
        var bogusId = Guid.NewGuid();

        var result = reg.ApproveRefundRequest(bogusId, OrganizerUser, null,
            new Dictionary<Guid, Money>());

        result.IsFailure.Should().BeTrue();
        reg.DomainEvents.OfType<RefundRequestApprovedEvent>().Should().BeEmpty();
    }

    [Fact]
    public void ApproveRefundRequest_WhenChildValidationFails_ReturnsFailure_NoRootEventRaised()
    {
        // All-zero approval is invalid per architect F2 — child returns failure.
        var reg = BuildRegistrationWithPendingRefund(out var rrId);
        var perLine = new Dictionary<Guid, Money>(); // empty = all zeroes per child contract

        var result = reg.ApproveRefundRequest(rrId, OrganizerUser, null, perLine);

        result.IsFailure.Should().BeTrue();
        reg.DomainEvents.OfType<RefundRequestApprovedEvent>().Should().BeEmpty(
            "no root re-raise should happen if the child validation rejected the approval");
    }

    // ========================================================================
    // Reject
    // ========================================================================

    [Fact]
    public void RejectRefundRequest_ReRaisesEventOnRegistrationRoot_WithPopulatedEventId()
    {
        var reg = BuildRegistrationWithPendingRefund(out var rrId);

        var result = reg.RejectRefundRequest(rrId, OrganizerUser, "outside terms");

        result.IsSuccess.Should().BeTrue();
        var rejectedEvent = reg.DomainEvents.OfType<RefundRequestRejectedEvent>().Single();
        rejectedEvent.EventId.Should().Be(_eventId);
        rejectedEvent.EventId.Should().NotBe(Guid.Empty);
        rejectedEvent.RegistrationId.Should().Be(reg.Id);
        rejectedEvent.RefundRequestId.Should().Be(rrId);
        rejectedEvent.OrganizerUserId.Should().Be(OrganizerUser);
        rejectedEvent.RejectionReason.Should().Be("outside terms");
    }

    [Fact]
    public void RejectRefundRequest_WhenRefundRequestNotFound_ReturnsFailure()
    {
        var reg = BuildRegistrationWithPendingRefund(out _);
        var result = reg.RejectRefundRequest(Guid.NewGuid(), OrganizerUser, "x");
        result.IsFailure.Should().BeTrue();
        reg.DomainEvents.OfType<RefundRequestRejectedEvent>().Should().BeEmpty();
    }

    // ========================================================================
    // Withdraw
    // ========================================================================

    [Fact]
    public void WithdrawRefundRequestV2_ReRaisesEventOnRegistrationRoot_WithPopulatedEventId()
    {
        var reg = BuildRegistrationWithPendingRefund(out var rrId);

        var result = reg.WithdrawRefundRequestV2(rrId, AttendeeUser);

        result.IsSuccess.Should().BeTrue();
        var withdrawnEvent = reg.DomainEvents.OfType<RefundRequestWithdrawnEvent>().Single();
        withdrawnEvent.EventId.Should().Be(_eventId, "the F1/G3 fix — empirically broken before D13.5 (T7 smoke showed event not found WARN)");
        withdrawnEvent.EventId.Should().NotBe(Guid.Empty);
        withdrawnEvent.RegistrationId.Should().Be(reg.Id);
        withdrawnEvent.RefundRequestId.Should().Be(rrId);
        withdrawnEvent.WithdrawnByUserId.Should().Be(AttendeeUser);
    }

    [Fact]
    public void WithdrawRefundRequestV2_WhenNotPending_ReturnsFailure()
    {
        var reg = BuildRegistrationWithPendingRefund(out var rrId);
        // First withdraw transitions to Withdrawn; second attempt should fail (child guard).
        reg.WithdrawRefundRequestV2(rrId, AttendeeUser).IsSuccess.Should().BeTrue();
        reg.ClearDomainEvents();

        var result = reg.WithdrawRefundRequestV2(rrId, AttendeeUser);

        result.IsFailure.Should().BeTrue();
        reg.DomainEvents.OfType<RefundRequestWithdrawnEvent>().Should().BeEmpty();
    }

    [Fact]
    public void WithdrawRefundRequestV2_WhenWrongUser_ReturnsFailure()
    {
        var reg = BuildRegistrationWithPendingRefund(out var rrId);
        var someoneElse = Guid.NewGuid();

        var result = reg.WithdrawRefundRequestV2(rrId, someoneElse);

        result.IsFailure.Should().BeTrue();
        reg.DomainEvents.OfType<RefundRequestWithdrawnEvent>().Should().BeEmpty();
    }
}
