using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 8YA.1 — TBD-dates lifecycle.
///
/// Architect verdict 2026-05-08: organizers can create events without committing
/// to start/end dates yet. New <see cref="EventStatus.Planning"/> state models
/// the dates-not-yet-known intent; <see cref="Event.SetDates"/> transitions
/// Planning → Draft when both dates are filled.
///
/// User decisions (Q1=A, Q2=A, Q3=A, Q4=A):
/// - Q1=A: TBD events CAN be Published (publicly listed with "Date TBD" badge).
/// - Q2=A: Registration is BLOCKED on TBD events regardless of status.
/// - Q3=A: Featured/Nearby/Upcoming carousels exclude TBD (Phase 4 query-handler change).
/// - Q4=A: Silent transition Planning → Draft when dates added (no email).
///
/// These tests pin the domain contract — Phase 2 builds Application/email behaviour
/// on top of these guarantees, so weakening any of them silently breaks downstream
/// pipelines.
/// </summary>
public class Event_TbdDates_Tests
{
    private static EventTitle Title() =>
        EventTitle.Create("TBD-dates test event").Value;

    private static EventDescription Description() =>
        EventDescription.Create("Phase 8YA.1 domain coverage").Value;

    // ─────────────────────────────────────────────────────────────────────────
    //  Create — branching on date presence
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithBothDatesNull_StartsInPlanning()
    {
        var result = Event.Create(
            Title(), Description(),
            startDate: null, endDate: null,
            organizerId: Guid.NewGuid(),
            capacity: 100);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(EventStatus.Planning);
        result.Value.StartDate.Should().BeNull();
        result.Value.EndDate.Should().BeNull();
    }

    [Fact]
    public void Create_WithBothDatesSet_StartsInDraft()
    {
        var result = Event.Create(
            Title(), Description(),
            startDate: DateTime.UtcNow.AddDays(7),
            endDate: DateTime.UtcNow.AddDays(8),
            organizerId: Guid.NewGuid(),
            capacity: 100);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(EventStatus.Draft);
        result.Value.StartDate.Should().NotBeNull();
        result.Value.EndDate.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithMixedDates_StartOnly_Fails()
    {
        var result = Event.Create(
            Title(), Description(),
            startDate: DateTime.UtcNow.AddDays(7),
            endDate: null,
            organizerId: Guid.NewGuid(),
            capacity: 100);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("both", because: "TBD requires both dates to be null OR both set");
    }

    [Fact]
    public void Create_WithMixedDates_EndOnly_Fails()
    {
        var result = Event.Create(
            Title(), Description(),
            startDate: null,
            endDate: DateTime.UtcNow.AddDays(8),
            organizerId: Guid.NewGuid(),
            capacity: 100);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("both");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SetDates — Planning → Draft transition
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetDates_FromPlanning_TransitionsToDraft()
    {
        var ev = CreatePlanningEvent();
        ev.Status.Should().Be(EventStatus.Planning);

        var start = DateTime.UtcNow.AddDays(7);
        var end = DateTime.UtcNow.AddDays(8);
        var result = ev.SetDates(start, end);

        result.IsSuccess.Should().BeTrue();
        ev.Status.Should().Be(EventStatus.Draft);
        ev.StartDate.Should().NotBeNull();
        ev.EndDate.Should().NotBeNull();
    }

    [Fact]
    public void SetDates_FromDraft_KeepsStatus_UpdatesDates()
    {
        var ev = CreateDraftEvent();
        var newStart = DateTime.UtcNow.AddDays(14);
        var newEnd = DateTime.UtcNow.AddDays(15);

        var result = ev.SetDates(newStart, newEnd);

        result.IsSuccess.Should().BeTrue();
        ev.Status.Should().Be(EventStatus.Draft); // No status change when already Draft
        ev.StartDate!.Value.Should().BeCloseTo(newStart, TimeSpan.FromSeconds(1));
        ev.EndDate!.Value.Should().BeCloseTo(newEnd, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetDates_EndBeforeStart_Fails()
    {
        var ev = CreatePlanningEvent();
        var result = ev.SetDates(
            startDate: DateTime.UtcNow.AddDays(8),
            endDate: DateTime.UtcNow.AddDays(7));

        result.IsFailure.Should().BeTrue();
        ev.Status.Should().Be(EventStatus.Planning); // Unchanged on failure
    }

    [Fact]
    public void SetDates_StartInPast_Fails()
    {
        var ev = CreatePlanningEvent();
        var result = ev.SetDates(
            startDate: DateTime.UtcNow.AddDays(-1),
            endDate: DateTime.UtcNow.AddDays(7));

        result.IsFailure.Should().BeTrue();
        ev.Status.Should().Be(EventStatus.Planning);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Registration — Q2=A blocks TBD events
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Register_OnPlanningEvent_Fails()
    {
        var ev = CreatePlanningEvent();

        var result = ev.Register(Guid.NewGuid(), quantity: 1);

        result.IsFailure.Should().BeTrue();
        // Q2=A: registration blocked when no confirmed dates. The exact "unpublished"
        // message would also fire here — what matters is the registration is rejected.
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Register_OnPublishedTbdEvent_Succeeds()
    {
        // Phase 8YB.6 (2026-05-09): Q2=A overturned. Product-owner-locked rule:
        // "Even though it is a date or venue TBD event treat it as a regular event."
        // Registration is allowed on TBD-Published events as long as registration
        // is otherwise enabled. The "already started" guard short-circuits when
        // StartDate is null (TBD events have no past anchor to compare against).
        var ev = CreatePlanningEvent();
        var publishResult = ev.Publish();
        publishResult.IsSuccess.Should().BeTrue();
        ev.Status.Should().Be(EventStatus.Published);
        ev.StartDate.Should().BeNull();

        var result = ev.Register(Guid.NewGuid(), quantity: 1);

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
    }

    [Fact]
    public void RegisterWithAttendees_OnPublishedTbdEvent_Succeeds()
    {
        // Phase 8YB.6: multi-attendee Mode-A path also accepts TBD events.
        // The HTTP RSVP endpoint routes new-format requests through this method,
        // so the smoke matrix C23/C24 cells exercise this code path specifically.
        var ev = CreatePlanningEvent();
        // Mark as free explicitly — the multi-attendee path runs CalculatePriceForAttendees
        // which requires either Free or pricing-configured. The TBD-specific assertion is
        // the same regardless of payment mode (Phase 8YB.6 D7=A is mode-agnostic).
        ev.SetAsFreeEvent().IsSuccess.Should().BeTrue();
        ev.Publish().IsSuccess.Should().BeTrue();

        var attendee = AttendeeDetails.Create("TBD Multi-Attendee", AgeCategory.Adult).Value;
        var contact = RegistrationContact.Create(
            email: "smoke-multi-tbd@example.com",
            phoneNumber: "+15555550101",
            address: null).Value;

        var result = ev.RegisterWithAttendees(
            userId: Guid.NewGuid(),
            attendees: new[] { attendee },
            contact: contact);

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
    }

    [Fact]
    public void RegisterAnonymous_OnPublishedTbdEvent_Succeeds()
    {
        // Phase 8YB.6: anonymous registration on TBD events follows the same rule
        // as authenticated registration — allowed when status is Published.
        var ev = CreatePlanningEvent();
        ev.Publish().IsSuccess.Should().BeTrue();

        var attendeeInfo = AttendeeInfo.Create(
            name: "Smoke Tester",
            age: 30,
            address: "100 Main St",
            email: "smoke-tbd@example.com",
            phoneNumber: "+15555550100").Value;

        var result = ev.RegisterAnonymous(attendeeInfo, quantity: 1);

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Conflict overlap — null-safe short-circuit
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void HasSchedulingConflict_OnTbdEvent_ReturnsFailure()
    {
        var tbd = CreatePlanningEvent();
        var dated = CreateDraftEvent();

        var result = tbd.HasSchedulingConflict(dated);

        // No dates means no overlap can be computed; the existing API returns
        // Failure when there is no conflict, so an "unknown" outcome surfaces
        // through the same channel without ever claiming a false-positive overlap.
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void HasSchedulingConflict_TbdVsTbd_ReturnsFailure()
    {
        var a = CreatePlanningEvent();
        var b = CreatePlanningEvent();

        var result = a.HasSchedulingConflict(b);

        result.IsFailure.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Activation — TBD published event cannot be activated
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ActivateEvent_OnPublishedTbdEvent_Fails()
    {
        var ev = CreatePlanningEvent();
        ev.Publish();

        var result = ev.ActivateEvent();

        result.IsFailure.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Phase 8YB.5 — Unpublish revert path (E16)
    //
    //  TBD-Published events must revert to Planning, NOT Draft, when unpublished.
    //  Reverting to Draft creates an impossible "Draft × null-dates" state per the
    //  Phase 8YA.1 invariant ("Draft only when dates set"). Architect-locked
    //  decision 2026-05-09 D1=A bundle.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Unpublish_FromPublishedTbd_RevertsToPlanning()
    {
        // Phase 8YB.5 (E16): TBD-Published → Unpublish must revert to Planning,
        // not Draft. Draft × null-dates is an impossible cell in the lifecycle matrix.
        var ev = CreatePlanningEvent();
        ev.Publish().IsSuccess.Should().BeTrue();
        ev.Status.Should().Be(EventStatus.Published);
        ev.StartDate.Should().BeNull();

        var result = ev.Unpublish();

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.Status.Should().Be(EventStatus.Planning);
        ev.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void Unpublish_FromPublished_WithDates_RevertsToDraft()
    {
        // Regression guard: dated event Published → Unpublish stays on the existing
        // Draft revert path. Phase 6A.41 behaviour preserved.
        var ev = CreateDraftEvent();
        ev.Publish().IsSuccess.Should().BeTrue();
        ev.Status.Should().Be(EventStatus.Published);
        ev.StartDate.Should().NotBeNull();

        var result = ev.Unpublish();

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.Status.Should().Be(EventStatus.Draft);
        ev.PublishedAt.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Phase 8YB.5 — Postpone domain tighten (D6)
    //
    //  Postpone() on a TBD-Published event is semantically incoherent
    //  ("postponed from when?"). Tighten the rule to require StartDate.HasValue.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Postpone_OnPublishedTbdEvent_Fails()
    {
        // Phase 8YB.5 (D6): Postpone requires a confirmed start date — postponing
        // an event without dates is semantically meaningless.
        var ev = CreatePlanningEvent();
        ev.Publish().IsSuccess.Should().BeTrue();
        ev.Status.Should().Be(EventStatus.Published);

        var result = ev.Postpone("Venue unavailable");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("date",
            because: "the rejection must explain dates are required for postponement");
    }

    [Fact]
    public void Postpone_OnPublishedDatedEvent_Succeeds()
    {
        // Regression guard: dated Published → Postpone keeps working as before.
        var ev = CreateDraftEvent();
        ev.Publish().IsSuccess.Should().BeTrue();

        var result = ev.Postpone("Venue unavailable");

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.Status.Should().Be(EventStatus.Postponed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Builders
    // ─────────────────────────────────────────────────────────────────────────

    private static Event CreatePlanningEvent() =>
        Event.Create(
            Title(), Description(),
            startDate: null, endDate: null,
            organizerId: Guid.NewGuid(),
            capacity: 100).Value;

    private static Event CreateDraftEvent() =>
        Event.Create(
            Title(), Description(),
            startDate: DateTime.UtcNow.AddDays(7),
            endDate: DateTime.UtcNow.AddDays(8),
            organizerId: Guid.NewGuid(),
            capacity: 100).Value;
}
