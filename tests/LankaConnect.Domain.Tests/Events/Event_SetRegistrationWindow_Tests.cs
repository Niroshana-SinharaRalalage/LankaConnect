using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 6A.153 — Organizer-controlled registration window domain contract.
///
/// Pins the <c>Event.SetRegistrationWindow(opensAt, closesAt)</c> mutator and
/// the internal <c>GetRegistrationAvailability(utcNow)</c> reader. The reader
/// is exercised here too because the three <c>Register*</c> methods all
/// short-circuit on it (covered by their own test files) — guaranteeing the
/// boundary semantics ("opensAt &gt; now blocks; closesAt &lt;= now blocks")
/// stay locked down at the source.
///
/// Decisions locked-in 2026-05-25:
/// - D2: nullable = "always open" (backward-compatible default)
/// - D3: invariants (opens &lt; closes; closes &lt;= start; opens &lt; start)
/// - D4: editable in Planning / Draft / Published; locked elsewhere
/// - D5: no separate pause toggle; pause = `closesAt = now`
/// </summary>
public class Event_SetRegistrationWindow_Tests
{
    private static EventTitle Title() =>
        EventTitle.Create("Phase 6A.153 window test event").Value;

    private static EventDescription Description() =>
        EventDescription.Create("Phase 6A.153 domain coverage").Value;

    /// <summary>
    /// Helper: build a dated Draft event 30 days out. Most window-window
    /// invariants need StartDate set to exercise the cross-checks.
    /// </summary>
    private static Event NewDraftEvent(DateTime? start = null)
    {
        var startDate = start ?? DateTime.UtcNow.AddDays(30);
        return Event.Create(
            Title(), Description(),
            startDate: startDate, endDate: startDate.AddHours(3),
            organizerId: Guid.NewGuid(),
            capacity: 100).Value;
    }

    /// <summary>
    /// Helper: build a TBD (no-date) Planning event. Exercises the "TBD events
    /// accept any window" rule (D3 invariant #4).
    /// </summary>
    private static Event NewTbdPlanningEvent()
    {
        return Event.Create(
            Title(), Description(),
            startDate: null, endDate: null,
            organizerId: Guid.NewGuid(),
            capacity: 100).Value;
    }

    /// <summary>
    /// Helper: force the event into a given status via reflection — the
    /// existing TBD-tests file uses this pattern too (no public setter for
    /// Status outside specific transition methods).
    /// </summary>
    private static void ForceStatus(Event @event, EventStatus status)
    {
        var field = typeof(Event).GetField("<Status>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(@event, status);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Happy path — valid windows write through and persist
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetRegistrationWindow_BothNull_ClearsWindow()
    {
        // Architect D2: null = "always open". Mutator must accept the all-null
        // call so organizers can revert a previously set window back to legacy
        // open-immediately behaviour.
        var @event = NewDraftEvent();

        var result = @event.SetRegistrationWindow(null, null);

        result.IsSuccess.Should().BeTrue();
        @event.RegistrationOpensAt.Should().BeNull();
        @event.RegistrationClosesAt.Should().BeNull();
    }

    [Fact]
    public void SetRegistrationWindow_OpensInFutureClosesNull_PersistsOpens()
    {
        // The simplest target use case: paid event, open registration 14 days
        // before start, leave ClosesAt null (= open until StartDate).
        var @event = NewDraftEvent();
        var opensAt = DateTime.UtcNow.AddDays(7);

        var result = @event.SetRegistrationWindow(opensAt, null);

        result.IsSuccess.Should().BeTrue();
        @event.RegistrationOpensAt.Should().Be(opensAt);
        @event.RegistrationClosesAt.Should().BeNull();
    }

    [Fact]
    public void SetRegistrationWindow_BothSetWithValidOrdering_Persists()
    {
        var @event = NewDraftEvent();
        var opensAt = DateTime.UtcNow.AddDays(7);
        var closesAt = DateTime.UtcNow.AddDays(20);

        var result = @event.SetRegistrationWindow(opensAt, closesAt);

        result.IsSuccess.Should().BeTrue();
        @event.RegistrationOpensAt.Should().Be(opensAt);
        @event.RegistrationClosesAt.Should().Be(closesAt);
    }

    [Fact]
    public void SetRegistrationWindow_NonUtcInput_CoercesToUtc()
    {
        // FE posts ISO-8601 UTC; defensive Kind coercion here protects against
        // any caller that hands us an Unspecified-Kind value (same pattern as
        // the Event constructor's StartDate/EndDate coercion).
        var @event = NewDraftEvent();
        var localTime = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Unspecified);

        var result = @event.SetRegistrationWindow(localTime, null);

        result.IsSuccess.Should().BeTrue();
        @event.RegistrationOpensAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  D3 invariant #1 — opensAt must strictly precede closesAt
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetRegistrationWindow_OpensAfterCloses_Fails()
    {
        var @event = NewDraftEvent();
        var opensAt = DateTime.UtcNow.AddDays(20);
        var closesAt = DateTime.UtcNow.AddDays(7);

        var result = @event.SetRegistrationWindow(opensAt, closesAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("opens-at must be before");
    }

    [Fact]
    public void SetRegistrationWindow_OpensEqualsCloses_Fails()
    {
        // Architect note: strict inequality. An instantaneous open-and-close
        // window is degenerate.
        var @event = NewDraftEvent();
        var moment = DateTime.UtcNow.AddDays(7);

        var result = @event.SetRegistrationWindow(moment, moment);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("opens-at must be before");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  D3 invariant #2 — closesAt must not be after StartDate
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetRegistrationWindow_ClosesAfterStartDate_Fails()
    {
        var @event = NewDraftEvent(start: DateTime.UtcNow.AddDays(10));
        var closesAt = DateTime.UtcNow.AddDays(15); // After StartDate

        var result = @event.SetRegistrationWindow(null, closesAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("closes-at cannot be after");
    }

    [Fact]
    public void SetRegistrationWindow_ClosesEqualsStartDate_Succeeds()
    {
        // Closes exactly at StartDate is fine — registration cuts off right
        // when the event begins. Same boundary the existing "already started"
        // guard uses on StartDate (`<=`).
        var startDate = DateTime.UtcNow.AddDays(10);
        var @event = NewDraftEvent(start: startDate);

        var result = @event.SetRegistrationWindow(null, startDate);

        result.IsSuccess.Should().BeTrue();
        @event.RegistrationClosesAt.Should().Be(startDate);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  D3 invariant #3 — opensAt must be before StartDate
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetRegistrationWindow_OpensAfterStartDate_Fails()
    {
        var @event = NewDraftEvent(start: DateTime.UtcNow.AddDays(10));
        var opensAt = DateTime.UtcNow.AddDays(15); // After StartDate

        var result = @event.SetRegistrationWindow(opensAt, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("opens-at must be before");
    }

    [Fact]
    public void SetRegistrationWindow_OpensEqualsStartDate_Fails()
    {
        // Opens-at-StartDate is functionally "we open the moment the event
        // starts", which the existing StartDate guard rejects immediately.
        // Catch at configuration time so the organizer fixes the form.
        var startDate = DateTime.UtcNow.AddDays(10);
        var @event = NewDraftEvent(start: startDate);

        var result = @event.SetRegistrationWindow(startDate, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("opens-at must be before");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  D3 invariant #4 — TBD events accept any window
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetRegistrationWindow_TbdEvent_AcceptsAnyOpensClosePair()
    {
        // Save-the-date use case: organizer publishes "RSVPs open Jan 15" for
        // an event whose actual date is TBD. No StartDate cross-checks fire.
        var @event = NewTbdPlanningEvent();
        var opensAt = DateTime.UtcNow.AddDays(7);
        var closesAt = DateTime.UtcNow.AddDays(60);

        var result = @event.SetRegistrationWindow(opensAt, closesAt);

        result.IsSuccess.Should().BeTrue();
        @event.RegistrationOpensAt.Should().Be(opensAt);
        @event.RegistrationClosesAt.Should().Be(closesAt);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  D4 — status lockout for Cancelled / Completed / Archived / Active
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(EventStatus.Cancelled)]
    [InlineData(EventStatus.Completed)]
    [InlineData(EventStatus.Archived)]
    [InlineData(EventStatus.Active)]
    public void SetRegistrationWindow_OnTerminalOrInFlightStatus_Fails(EventStatus blockedStatus)
    {
        var @event = NewDraftEvent();
        ForceStatus(@event, blockedStatus);

        var result = @event.SetRegistrationWindow(
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(20));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot be changed");
        result.Error.Should().Contain(blockedStatus.ToString());
    }

    [Theory]
    [InlineData(EventStatus.Planning)]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.Published)]
    public void SetRegistrationWindow_OnEditableStatus_Succeeds(EventStatus editableStatus)
    {
        var @event = NewDraftEvent();
        ForceStatus(@event, editableStatus);

        var result = @event.SetRegistrationWindow(
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(20));

        result.IsSuccess.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  GetRegistrationAvailability reader — boundary contract
    // ─────────────────────────────────────────────────────────────────────────
    //
    //  Reader is `internal` so we exercise it via reflection. The four
    //  Register* methods consume the same reader, so locking down the
    //  boundary semantics here means all four call sites stay in lockstep.

    private static object InvokeAvailability(Event @event, DateTime utcNow)
    {
        var method = typeof(Event).GetMethod(
            "GetRegistrationAvailability",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return method!.Invoke(@event, new object[] { utcNow })!;
    }

    [Fact]
    public void GetRegistrationAvailability_BothNull_IsOpen()
    {
        var @event = NewDraftEvent();

        var state = InvokeAvailability(@event, DateTime.UtcNow);

        state.ToString().Should().Be("Open");
    }

    [Fact]
    public void GetRegistrationAvailability_OpensInFuture_IsNotYetOpen()
    {
        var @event = NewDraftEvent();
        var opensAt = DateTime.UtcNow.AddDays(7);
        @event.SetRegistrationWindow(opensAt, null);

        var state = InvokeAvailability(@event, DateTime.UtcNow);

        state.ToString().Should().Be("NotYetOpen");
    }

    [Fact]
    public void GetRegistrationAvailability_ClosesInPast_IsClosedByOrganizer()
    {
        var @event = NewDraftEvent();
        // Set both ClosesAt and StartDate in the future first (mutator
        // requires that), then push UtcNow forward via reflection? Simpler:
        // pass a `utcNow` value that's after ClosesAt.
        var opensAt = DateTime.UtcNow.AddDays(1);
        var closesAt = DateTime.UtcNow.AddDays(5);
        @event.SetRegistrationWindow(opensAt, closesAt);

        var state = InvokeAvailability(@event, closesAt.AddSeconds(1));

        state.ToString().Should().Be("ClosedByOrganizer");
    }

    [Fact]
    public void GetRegistrationAvailability_StartDatePassed_IsClosedEventStarted_RegardlessOfWindow()
    {
        // StartDate guard takes priority over any window state. Pin this so
        // future changes don't flip the ordering and silently let post-start
        // registrations through.
        var startDate = DateTime.UtcNow.AddDays(10);
        var @event = NewDraftEvent(start: startDate);
        @event.SetRegistrationWindow(DateTime.UtcNow.AddDays(1), null);

        var state = InvokeAvailability(@event, startDate.AddSeconds(1));

        state.ToString().Should().Be("ClosedEventStarted");
    }

    [Fact]
    public void GetRegistrationAvailability_AtExactOpensAt_IsOpen()
    {
        // Boundary: at the exact moment OpensAt hits, registration is open.
        // Semantics use `>` (strict), so `now == opensAt` evaluates Open.
        var @event = NewDraftEvent();
        var opensAt = DateTime.UtcNow.AddDays(7);
        @event.SetRegistrationWindow(opensAt, null);

        var state = InvokeAvailability(@event, opensAt);

        state.ToString().Should().Be("Open");
    }

    [Fact]
    public void GetRegistrationAvailability_AtExactClosesAt_IsClosedByOrganizer()
    {
        // Boundary: at the exact moment ClosesAt hits, registration is
        // closed. Semantics use `<=`, so `now == closesAt` evaluates
        // ClosedByOrganizer. Symmetric-asymmetric pairing with OpensAt is
        // intentional — the FE state machine resolves cleanly at boundary
        // instants (no "both open and closed simultaneously" state).
        var @event = NewDraftEvent();
        var opensAt = DateTime.UtcNow.AddDays(1);
        var closesAt = DateTime.UtcNow.AddDays(5);
        @event.SetRegistrationWindow(opensAt, closesAt);

        var state = InvokeAvailability(@event, closesAt);

        state.ToString().Should().Be("ClosedByOrganizer");
    }
}
