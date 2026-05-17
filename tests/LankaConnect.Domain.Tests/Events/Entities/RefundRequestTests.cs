using FluentAssertions;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Phase 6A.148 — RefundRequest aggregate-internal entity tests.
///
/// Pins down: two creation paths (CreatePending attendee / CreateOrganizerInitiated),
/// approve / reject / withdraw transitions, sum(ApprovedAmount) > 0 invariant on Approve
/// (architect F2), ScanGuardOverridden ⇒ OrganizerNotes required (F7), Approve only from
/// Pending, Withdraw attendee-only and Pending-only, BeginProcessing after first Stripe
/// dispatch, MarkCompletedIfAllSettled rolls Request to Completed only when all lines
/// are terminal.
/// </summary>
public class RefundRequestTests
{
    private static Money Usd(decimal amount) => new(amount, Currency.USD);

    private readonly Guid _registrationId = Guid.NewGuid();
    private readonly Guid _attendeeUserId = Guid.NewGuid();
    private readonly Guid _organizerUserId = Guid.NewGuid();
    private readonly Guid _ticketRefId = Guid.NewGuid();
    private readonly Guid _addOnRefId = Guid.NewGuid();

    private RefundRequestLineItemInput TicketLine(decimal amount = 50m) =>
        new(RefundLineItemType.Ticket, _ticketRefId, Usd(amount));

    private RefundRequestLineItemInput AddOnLine(decimal amount = 10m) =>
        new(RefundLineItemType.AddOn, _addOnRefId, Usd(amount));

    // ============================================================
    // CreatePending — attendee path
    // ============================================================

    [Fact]
    public void CreatePending_HappyPath_ReturnsRequestInPendingWithLineItemsInRequested()
    {
        var result = RefundRequest.CreatePending(
            _registrationId,
            _attendeeUserId,
            requesterReason: "Cannot attend",
            lineItems: new[] { TicketLine(), AddOnLine() });

        result.IsSuccess.Should().BeTrue();
        var req = result.Value;
        req.Id.Should().NotBe(Guid.Empty);
        req.RegistrationId.Should().Be(_registrationId);
        req.RequestedByUserId.Should().Be(_attendeeUserId);
        req.IsOrganizerInitiated.Should().BeFalse();
        req.Status.Should().Be(RefundRequestStatus.Pending);
        req.RequesterReason.Should().Be("Cannot attend");
        req.ReviewedByUserId.Should().BeNull();
        req.ReviewedAt.Should().BeNull();
        req.OrganizerNotes.Should().BeNull();
        req.RejectionReason.Should().BeNull();
        req.ScanGuardOverridden.Should().BeFalse();
        req.LineItems.Should().HaveCount(2);
        req.LineItems.Should().AllSatisfy(li =>
            li.Status.Should().Be(RefundLineItemStatus.Requested));
    }

    [Fact]
    public void CreatePending_WithEmptyLineItems_Fails()
    {
        var result = RefundRequest.CreatePending(
            _registrationId, _attendeeUserId, null,
            lineItems: Array.Empty<RefundRequestLineItemInput>());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least one line item");
    }

    [Fact]
    public void CreatePending_RaisesRefundRequestCreatedEvent()
    {
        var result = RefundRequest.CreatePending(
            _registrationId, _attendeeUserId, "reason",
            new[] { TicketLine() });

        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RefundRequestCreatedEvent>();
    }

    // ============================================================
    // CreateOrganizerInitiated — skips Pending, goes to Approved
    // ============================================================

    [Fact]
    public void CreateOrganizerInitiated_HappyPath_StartsApprovedWithAllLinesApproved()
    {
        var result = RefundRequest.CreateOrganizerInitiated(
            _registrationId,
            organizerUserId: _organizerUserId,
            organizerNotes: "Goodwill refund",
            scanGuardOverridden: false,
            lineItems: new[] { TicketLine(), AddOnLine() });

        result.IsSuccess.Should().BeTrue();
        var req = result.Value;
        req.IsOrganizerInitiated.Should().BeTrue();
        req.RequestedByUserId.Should().Be(_organizerUserId);
        req.ReviewedByUserId.Should().Be(_organizerUserId, "organizer-initiated is auto-reviewed");
        req.ReviewedAt.Should().NotBeNull();
        req.Status.Should().Be(RefundRequestStatus.Approved);
        req.OrganizerNotes.Should().Be("Goodwill refund");
        req.LineItems.Should().AllSatisfy(li =>
        {
            li.Status.Should().Be(RefundLineItemStatus.Approved);
            li.ApprovedAmount!.Amount.Should().Be(li.RequestedAmount.Amount);
        });
    }

    [Fact]
    public void CreateOrganizerInitiated_WithScanGuardOverrideAndBlankNotes_Fails()
    {
        // Architect F7: override ⇒ organizer notes mandatory (audit).
        var result = RefundRequest.CreateOrganizerInitiated(
            _registrationId, _organizerUserId,
            organizerNotes: "   ",
            scanGuardOverridden: true,
            lineItems: new[] { TicketLine() });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("OrganizerNotes");
    }

    [Fact]
    public void CreateOrganizerInitiated_WithScanGuardOverrideAndNotes_Succeeds()
    {
        var result = RefundRequest.CreateOrganizerInitiated(
            _registrationId, _organizerUserId,
            organizerNotes: "User missed event due to ER visit; verified.",
            scanGuardOverridden: true,
            lineItems: new[] { TicketLine() });

        result.IsSuccess.Should().BeTrue();
        result.Value.ScanGuardOverridden.Should().BeTrue();
    }

    [Fact]
    public void CreateOrganizerInitiated_RaisesOrganizerInitiatedEvent()
    {
        var result = RefundRequest.CreateOrganizerInitiated(
            _registrationId, _organizerUserId, "notes", false,
            new[] { TicketLine() });

        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrganizerInitiatedRefundCreatedEvent>();
    }

    // ============================================================
    // Approve (Pending → Approved)
    // ============================================================

    [Fact]
    public void Approve_HappyPathFullAmount_TransitionsToApprovedAndLinesToApproved()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine(50m), AddOnLine(10m) }).Value;

        var perLine = req.LineItems.ToDictionary(li => li.Id, li => li.RequestedAmount);
        var result = req.Approve(_organizerUserId, organizerNotes: "approved", perLineApprovedAmounts: perLine);

        result.IsSuccess.Should().BeTrue();
        req.Status.Should().Be(RefundRequestStatus.Approved);
        req.ReviewedByUserId.Should().Be(_organizerUserId);
        req.ReviewedAt.Should().NotBeNull();
        req.OrganizerNotes.Should().Be("approved");
        req.LineItems.Should().AllSatisfy(li => li.Status.Should().Be(RefundLineItemStatus.Approved));
    }

    [Fact]
    public void Approve_PartialPerLine_SetsApprovedAndRejectedCorrectly()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine(50m), AddOnLine(10m) }).Value;
        var ticketLine = req.LineItems.First(l => l.Type == RefundLineItemType.Ticket);
        var addOnLine = req.LineItems.First(l => l.Type == RefundLineItemType.AddOn);

        var result = req.Approve(_organizerUserId, null, new Dictionary<Guid, Money>
        {
            [ticketLine.Id] = Usd(50m),
            [addOnLine.Id] = Usd(0m)
        });

        result.IsSuccess.Should().BeTrue();
        req.Status.Should().Be(RefundRequestStatus.Approved);
        ticketLine.Status.Should().Be(RefundLineItemStatus.Approved);
        addOnLine.Status.Should().Be(RefundLineItemStatus.Rejected);
    }

    [Fact]
    public void Approve_AllLinesZero_Fails()
    {
        // Architect F2: must use /reject endpoint, not /approve with all-zero.
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine(50m), AddOnLine(10m) }).Value;
        var perLine = req.LineItems.ToDictionary(li => li.Id, li => Usd(0m));

        var result = req.Approve(_organizerUserId, null, perLine);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one line item must be approved");
        req.Status.Should().Be(RefundRequestStatus.Pending, "failed approve must not mutate");
    }

    [Fact]
    public void Approve_NotPending_Fails()
    {
        var req = RefundRequest.CreateOrganizerInitiated(
            _registrationId, _organizerUserId, "notes", false,
            new[] { TicketLine() }).Value;

        var result = req.Approve(_organizerUserId, null,
            new Dictionary<Guid, Money> { [req.LineItems[0].Id] = Usd(50m) });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pending");
    }

    [Fact]
    public void Approve_MissingPerLineEntry_TreatedAsRejected()
    {
        // Organizer didn't include an entry for a line — line is rejected (treated as 0).
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine(50m), AddOnLine(10m) }).Value;
        var ticketLine = req.LineItems.First(l => l.Type == RefundLineItemType.Ticket);

        var result = req.Approve(_organizerUserId, null,
            new Dictionary<Guid, Money> { [ticketLine.Id] = Usd(50m) });

        result.IsSuccess.Should().BeTrue();
        var addOnLine = req.LineItems.First(l => l.Type == RefundLineItemType.AddOn);
        addOnLine.Status.Should().Be(RefundLineItemStatus.Rejected);
    }

    [Fact]
    public void Approve_RaisesApprovedEvent()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine() }).Value;
        req.ClearDomainEvents();
        var perLine = new Dictionary<Guid, Money> { [req.LineItems[0].Id] = Usd(50m) };

        req.Approve(_organizerUserId, null, perLine);

        req.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RefundRequestApprovedEvent>();
    }

    // ============================================================
    // Reject
    // ============================================================

    [Fact]
    public void Reject_HappyPath_TransitionsToRejected()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine() }).Value;

        var result = req.Reject(_organizerUserId, "Outside refund window");

        result.IsSuccess.Should().BeTrue();
        req.Status.Should().Be(RefundRequestStatus.Rejected);
        req.RejectionReason.Should().Be("Outside refund window");
        req.ReviewedByUserId.Should().Be(_organizerUserId);
    }

    [Fact]
    public void Reject_BlankReason_Fails()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine() }).Value;

        var result = req.Reject(_organizerUserId, "   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("RejectionReason");
    }

    [Fact]
    public void Reject_NotPending_Fails()
    {
        var req = RefundRequest.CreateOrganizerInitiated(
            _registrationId, _organizerUserId, "notes", false,
            new[] { TicketLine() }).Value;

        var result = req.Reject(_organizerUserId, "reason");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reject_RaisesRejectedEvent()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine() }).Value;
        req.ClearDomainEvents();

        req.Reject(_organizerUserId, "reason");

        req.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RefundRequestRejectedEvent>();
    }

    // ============================================================
    // Withdraw
    // ============================================================

    [Fact]
    public void Withdraw_ByRequester_TransitionsToWithdrawn()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine() }).Value;

        var result = req.Withdraw(_attendeeUserId);

        result.IsSuccess.Should().BeTrue();
        req.Status.Should().Be(RefundRequestStatus.Withdrawn);
    }

    [Fact]
    public void Withdraw_ByDifferentUser_Fails()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine() }).Value;

        var result = req.Withdraw(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("only the requester");
    }

    [Fact]
    public void Withdraw_NotPending_Fails()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine() }).Value;
        req.Approve(_organizerUserId, null,
            new Dictionary<Guid, Money> { [req.LineItems[0].Id] = Usd(50m) });

        var result = req.Withdraw(_attendeeUserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pending");
    }

    [Fact]
    public void Withdraw_RaisesWithdrawnEvent()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine() }).Value;
        req.ClearDomainEvents();

        req.Withdraw(_attendeeUserId);

        req.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RefundRequestWithdrawnEvent>();
    }

    // ============================================================
    // BeginProcessing / MarkCompletedIfAllSettled
    // ============================================================

    [Fact]
    public void BeginProcessing_FromApproved_TransitionsToProcessing()
    {
        var req = RefundRequest.CreateOrganizerInitiated(
            _registrationId, _organizerUserId, "notes", false,
            new[] { TicketLine() }).Value;

        var result = req.BeginProcessing();

        result.IsSuccess.Should().BeTrue();
        req.Status.Should().Be(RefundRequestStatus.Processing);
    }

    [Fact]
    public void BeginProcessing_FromPending_Fails()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine() }).Value;

        var result = req.BeginProcessing();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkCompletedIfAllSettled_AllLinesTerminal_TransitionsToCompleted()
    {
        var req = RefundRequest.CreateOrganizerInitiated(
            _registrationId, _organizerUserId, "notes", false,
            new[] { TicketLine() }).Value;
        req.BeginProcessing();
        req.LineItems[0].MarkProcessing("re_test", "ch_test");
        req.LineItems[0].MarkRefunded(DateTime.UtcNow);

        var result = req.MarkCompletedIfAllSettled();

        result.IsSuccess.Should().BeTrue();
        req.Status.Should().Be(RefundRequestStatus.Completed);
        req.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkCompletedIfAllSettled_RejectedLineCountsAsTerminal()
    {
        var req = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine(), AddOnLine() }).Value;
        var ticketLine = req.LineItems.First(l => l.Type == RefundLineItemType.Ticket);
        var addOnLine = req.LineItems.First(l => l.Type == RefundLineItemType.AddOn);
        req.Approve(_organizerUserId, null, new Dictionary<Guid, Money>
        {
            [ticketLine.Id] = Usd(50m),
            [addOnLine.Id] = Usd(0m)
        });
        req.BeginProcessing();
        ticketLine.MarkProcessing("re_t", "ch_t");
        ticketLine.MarkRefunded(DateTime.UtcNow);

        var result = req.MarkCompletedIfAllSettled();

        result.IsSuccess.Should().BeTrue();
        req.Status.Should().Be(RefundRequestStatus.Completed,
            "rejected lines don't go through Stripe — they count as settled at approval time");
    }

    [Fact]
    public void MarkCompletedIfAllSettled_SomeStillProcessing_StaysProcessing()
    {
        var req = RefundRequest.CreateOrganizerInitiated(
            _registrationId, _organizerUserId, "notes", false,
            new[] { TicketLine(), AddOnLine() }).Value;
        req.BeginProcessing();
        req.LineItems[0].MarkProcessing("re_a", "ch_a");
        req.LineItems[0].MarkRefunded(DateTime.UtcNow);
        req.LineItems[1].MarkProcessing("re_b", "ch_b");
        // line 1 still in Processing

        var result = req.MarkCompletedIfAllSettled();

        result.IsSuccess.Should().BeTrue();
        req.Status.Should().Be(RefundRequestStatus.Processing, "still waiting on one line");
    }

    [Fact]
    public void MarkCompletedIfAllSettled_FailedLineCountsAsTerminal()
    {
        var req = RefundRequest.CreateOrganizerInitiated(
            _registrationId, _organizerUserId, "notes", false,
            new[] { TicketLine(), AddOnLine() }).Value;
        req.BeginProcessing();
        req.LineItems[0].MarkProcessing("re_a", "ch_a");
        req.LineItems[0].MarkRefunded(DateTime.UtcNow);
        req.LineItems[1].MarkProcessing("re_b", "ch_b");
        req.LineItems[1].MarkFailed("card_declined");

        var result = req.MarkCompletedIfAllSettled();

        result.IsSuccess.Should().BeTrue();
        req.Status.Should().Be(RefundRequestStatus.Completed,
            "Failed lines are terminal — request reaches Completed even with partial Stripe failures");
    }

    // ============================================================
    // HasAnyApprovedAmount helper
    // ============================================================

    [Fact]
    public void IsActive_RecognizesPendingApprovedProcessing()
    {
        var pending = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine() }).Value;
        var organizerInit = RefundRequest.CreateOrganizerInitiated(
            _registrationId, _organizerUserId, "notes", false, new[] { TicketLine() }).Value;
        var processing = RefundRequest.CreateOrganizerInitiated(
            _registrationId, _organizerUserId, "notes", false, new[] { TicketLine() }).Value;
        processing.BeginProcessing();
        var rejected = RefundRequest.CreatePending(_registrationId, _attendeeUserId, null,
            new[] { TicketLine() }).Value;
        rejected.Reject(_organizerUserId, "reason");

        pending.IsActive.Should().BeTrue();
        organizerInit.IsActive.Should().BeTrue();
        processing.IsActive.Should().BeTrue();
        rejected.IsActive.Should().BeFalse();
    }
}
