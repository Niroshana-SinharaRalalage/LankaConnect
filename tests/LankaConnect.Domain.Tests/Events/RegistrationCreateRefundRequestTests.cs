using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 6A.148 — <c>Registration.CreateRefundRequest</c> entry-point tests.
///
/// Aggregate is responsible for enforcing all preconditions before delegating to
/// <see cref="RefundRequest.CreatePending"/> or <see cref="RefundRequest.CreateOrganizerInitiated"/>.
///
/// Architect-mandated invariants pinned here:
/// - F1: single-active-request guard (Pending | Approved | Processing all block)
/// - F7: scan-guard override requires non-empty OrganizerNotes (delegated to entity)
/// - F9: line items must be unique per (Type, ReferenceId)
/// - Scan guard blocks attendee path when <c>anyTicketsScanned == true</c>
/// - No date-based guard (no-show rule #3 — post-event refunds allowed if no scans)
/// - Registration.Status flips to PendingRefundApproval on success
/// </summary>
public class RegistrationCreateRefundRequestTests
{
    private static readonly Guid AttendeeUser = Guid.NewGuid();
    private static readonly Guid OrganizerUser = Guid.NewGuid();
    private static readonly Guid TicketRef = Guid.NewGuid();
    private static readonly Guid AddOnRef = Guid.NewGuid();
    private readonly Guid _eventId = Guid.NewGuid();

    private static Money Usd(decimal amount) => new(amount, Currency.USD);

    private Registration BuildPaidConfirmedRegistration()
    {
        var alice = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female).Value;
        var contact = RegistrationContact.Create(
            "alice@example.com", "8609780124", null, null, false).Value;
        var price = Money.Create(50m, Currency.USD).Value;
        var reg = Registration.CreateWithAttendees(
            _eventId, AttendeeUser, new[] { alice }, contact, price, isPaidEvent: true).Value;
        reg.CompletePayment("pi_test_refund").IsSuccess.Should().BeTrue();
        reg.ClearDomainEvents();
        return reg;
    }

    private static IReadOnlyList<RefundRequestLineItemInput> TicketOnly() =>
        new[] { new RefundRequestLineItemInput(RefundLineItemType.Ticket, TicketRef, Usd(50m)) };

    private static IReadOnlyList<RefundRequestLineItemInput> TicketAndAddOn() => new[]
    {
        new RefundRequestLineItemInput(RefundLineItemType.Ticket, TicketRef, Usd(50m)),
        new RefundRequestLineItemInput(RefundLineItemType.AddOn,  AddOnRef,  Usd(10m))
    };

    // ============================================================
    // Attendee happy path
    // ============================================================

    [Fact]
    public void CreateRefundRequest_AttendeePath_HappyPath_LeavesRegistrationConfirmed()
    {
        // Post-rework: cancel and refund are decoupled. CreateRefundRequest no longer
        // mutates Registration.Status — the registration stays in whatever lifecycle
        // state it was in (Confirmed for standalone refund, Cancelled for cancel+refund).
        var reg = BuildPaidConfirmedRegistration();

        var result = reg.CreateRefundRequest(
            requestedByUserId: AttendeeUser,
            isOrganizerInitiated: false,
            requesterReason: "Cannot attend",
            organizerNotes: null,
            overrideScanGuard: false,
            anyTicketsScanned: false,
            lineItems: TicketOnly());

        result.IsSuccess.Should().BeTrue();
        reg.Status.Should().Be(RegistrationStatus.Confirmed,
            "decoupled: refund request creation must NOT mutate Registration.Status");
        reg.RefundRequests.Should().ContainSingle();
        result.Value.Status.Should().Be(RefundRequestStatus.Pending);
        result.Value.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public void CreateRefundRequest_AttendeePath_OnCancelledRegistration_Succeeds()
    {
        // Compound cancel+refund path: caller cancels the registration first (Cancelled),
        // then creates a Pending refund request on the same row. The aggregate now allows
        // this combination because the two lifecycles are decoupled.
        var reg = BuildPaidConfirmedRegistration();
        reg.Cancel();
        reg.Status.Should().Be(RegistrationStatus.Cancelled);

        var result = reg.CreateRefundRequest(
            AttendeeUser, false, "Cannot attend", null, false, false, TicketOnly());

        result.IsSuccess.Should().BeTrue();
        reg.Status.Should().Be(RegistrationStatus.Cancelled, "cancel state preserved");
        reg.RefundRequests.Should().ContainSingle();
        result.Value.Status.Should().Be(RefundRequestStatus.Pending);
    }

    [Fact]
    public void CreateRefundRequest_OrganizerPath_OnCancelledRegistration_Fails()
    {
        // Only the attendee path is allowed to create a refund on a Cancelled registration
        // (the cancel+refund compound). Organizer-initiated on a Cancelled registration is
        // disallowed — organizer should only act on live (Confirmed) registrations.
        var reg = BuildPaidConfirmedRegistration();
        reg.Cancel();

        var result = reg.CreateRefundRequest(
            OrganizerUser, true, null, "notes", false, false, TicketOnly());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not eligible");
    }

    [Fact]
    public void CreateRefundRequest_AttendeePath_PopulatesEventIdInDomainEvent()
    {
        var reg = BuildPaidConfirmedRegistration();

        var result = reg.CreateRefundRequest(
            AttendeeUser, false, "reason", null, false, false, TicketOnly());

        var raised = reg.DomainEvents.OfType<RefundRequestCreatedEvent>().Single();
        raised.EventId.Should().Be(_eventId,
            "aggregate must populate EventId since entity factory doesn't know it");
        raised.RegistrationId.Should().Be(reg.Id);
        raised.RefundRequestId.Should().Be(result.Value.Id);
    }

    // ============================================================
    // Status / payment preconditions
    // ============================================================

    [Fact]
    public void CreateRefundRequest_AttendeePath_OnRefundedRegistration_Fails()
    {
        // After registration is fully Refunded (terminal money state), no new refund
        // requests can be created. Allowed states: Confirmed (organizer or attendee
        // path) and Cancelled (attendee path only).
        var reg = BuildPaidConfirmedRegistration();
        reg.RefundPayment();

        var result = reg.CreateRefundRequest(
            AttendeeUser, false, null, null, false, false, TicketOnly());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not eligible");
    }

    [Fact]
    public void CreateRefundRequest_PaymentNotCompleted_Fails()
    {
        var alice = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female).Value;
        var contact = RegistrationContact.Create("alice@example.com", "8609780124", null, null, false).Value;
        var price = Money.Create(0m, Currency.USD).Value;
        // Free event => PaymentStatus.NotRequired
        var reg = Registration.CreateWithAttendees(
            _eventId, AttendeeUser, new[] { alice }, contact, price, isPaidEvent: false).Value;

        var result = reg.CreateRefundRequest(
            AttendeeUser, false, null, null, false, false, TicketOnly());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Completed");
    }

    // ============================================================
    // Scan guard
    // ============================================================

    [Fact]
    public void CreateRefundRequest_AttendeePath_TicketScanned_Fails()
    {
        var reg = BuildPaidConfirmedRegistration();

        var result = reg.CreateRefundRequest(
            AttendeeUser, false, null, null,
            overrideScanGuard: false,
            anyTicketsScanned: true,
            lineItems: TicketOnly());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("scanned");
        reg.Status.Should().Be(RegistrationStatus.Confirmed, "failed creation must not mutate");
    }

    [Fact]
    public void CreateRefundRequest_OrganizerPath_TicketScanned_WithOverrideAndNotes_Succeeds()
    {
        var reg = BuildPaidConfirmedRegistration();

        var result = reg.CreateRefundRequest(
            OrganizerUser, true, null,
            organizerNotes: "Verified no-show; user confirmed ER visit.",
            overrideScanGuard: true,
            anyTicketsScanned: true,
            lineItems: TicketOnly());

        result.IsSuccess.Should().BeTrue();
        reg.Status.Should().Be(RegistrationStatus.Confirmed,
            "decoupled: refund creation does not mutate Registration.Status");
        result.Value.ScanGuardOverridden.Should().BeTrue();
        result.Value.Status.Should().Be(RefundRequestStatus.Approved);
    }

    [Fact]
    public void CreateRefundRequest_OrganizerPath_TicketScanned_WithoutOverride_Fails()
    {
        var reg = BuildPaidConfirmedRegistration();

        var result = reg.CreateRefundRequest(
            OrganizerUser, true, null, "notes",
            overrideScanGuard: false,
            anyTicketsScanned: true,
            lineItems: TicketOnly());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("scanned");
    }

    [Fact]
    public void CreateRefundRequest_OrganizerPath_TicketScanned_OverrideWithoutNotes_Fails()
    {
        // Architect F7 — override + blank notes is rejected at the entity layer.
        var reg = BuildPaidConfirmedRegistration();

        var result = reg.CreateRefundRequest(
            OrganizerUser, true, null,
            organizerNotes: "   ",
            overrideScanGuard: true,
            anyTicketsScanned: true,
            lineItems: TicketOnly());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("OrganizerNotes");
    }

    // ============================================================
    // Single-active-request guard (architect F1)
    // ============================================================

    [Fact]
    public void CreateRefundRequest_SecondRequestWhilePending_Fails()
    {
        var reg = BuildPaidConfirmedRegistration();
        reg.CreateRefundRequest(AttendeeUser, false, null, null, false, false, TicketOnly());

        var second = reg.CreateRefundRequest(
            AttendeeUser, false, null, null, false, false, TicketOnly());

        second.IsFailure.Should().BeTrue();
        second.Error.Should().Contain("already an active refund request");
    }

    [Fact]
    public void CreateRefundRequest_SecondRequestAfterRejectedFirst_Succeeds()
    {
        // Post-rework: Registration.Status is never mutated by CreateRefundRequest,
        // so the only guard for a second request is HasActiveRefundRequest. Once the
        // first is Rejected, a second can be created.
        var reg = BuildPaidConfirmedRegistration();
        var first = reg.CreateRefundRequest(
            AttendeeUser, false, null, null, false, false, TicketOnly()).Value;
        first.Reject(OrganizerUser, "outside policy");

        var second = reg.CreateRefundRequest(
            AttendeeUser, false, null, null, false, false, TicketOnly());

        second.IsSuccess.Should().BeTrue();
        reg.RefundRequests.Should().HaveCount(2, "rejected request is kept for audit");
    }

    // ============================================================
    // Line item validation
    // ============================================================

    [Fact]
    public void CreateRefundRequest_EmptyLineItems_Fails()
    {
        var reg = BuildPaidConfirmedRegistration();

        var result = reg.CreateRefundRequest(
            AttendeeUser, false, null, null, false, false,
            Array.Empty<RefundRequestLineItemInput>());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CreateRefundRequest_DuplicateLineItemReference_Fails()
    {
        // Architect F9 — one line per (Type, ReferenceId). No aggregation by type.
        var reg = BuildPaidConfirmedRegistration();
        var dup = new[]
        {
            new RefundRequestLineItemInput(RefundLineItemType.AddOn, AddOnRef, Usd(10m)),
            new RefundRequestLineItemInput(RefundLineItemType.AddOn, AddOnRef, Usd(5m))
        };

        var result = reg.CreateRefundRequest(
            AttendeeUser, false, null, null, false, false, dup);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("duplicate");
    }

    [Fact]
    public void CreateRefundRequest_DifferentTypesSameRefId_IsAllowed()
    {
        // (Ticket, refX) and (AddOn, refX) are distinct buckets — no collision.
        var reg = BuildPaidConfirmedRegistration();
        var sharedId = Guid.NewGuid();
        var inputs = new[]
        {
            new RefundRequestLineItemInput(RefundLineItemType.Ticket, sharedId, Usd(50m)),
            new RefundRequestLineItemInput(RefundLineItemType.AddOn,  sharedId, Usd(10m))
        };

        var result = reg.CreateRefundRequest(
            AttendeeUser, false, null, null, false, false, inputs);

        result.IsSuccess.Should().BeTrue();
    }

    // ============================================================
    // HasActiveRefundRequest invariant
    // ============================================================

    [Fact]
    public void HasActiveRefundRequest_TrueWhenPending()
    {
        var reg = BuildPaidConfirmedRegistration();
        reg.CreateRefundRequest(AttendeeUser, false, null, null, false, false, TicketOnly());

        reg.HasActiveRefundRequest.Should().BeTrue();
    }

    [Fact]
    public void HasActiveRefundRequest_FalseWhenAllRequestsTerminal()
    {
        var reg = BuildPaidConfirmedRegistration();
        var req = reg.CreateRefundRequest(
            AttendeeUser, false, null, null, false, false, TicketOnly()).Value;
        req.Reject(OrganizerUser, "outside policy");

        reg.HasActiveRefundRequest.Should().BeFalse();
    }

    // ============================================================
    // Vestigial helpers — MoveToRefundRequestedFromApproval and MoveToConfirmedFromApproval
    // are no longer called by the new decoupled flow. The methods remain in the aggregate
    // for safety (in case any in-flight staging row needs them) but their preconditions
    // are unreachable under the current flow. They are NOT tested here intentionally.
    // ============================================================
}
