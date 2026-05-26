using AutoMapper;
using FluentAssertions;
using LankaConnect.Application.Common.Mappings;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Mappings;

/// <summary>
/// Phase 6A.153 — verifies that <see cref="EventMappingProfile"/> populates
/// <see cref="EventDto.RegistrationAvailability"/> + raw window timestamps
/// correctly across the four states the FE state-machine consumes:
///
/// <list type="bullet">
///   <item><c>"open"</c> — no window set, or now is inside the window</item>
///   <item><c>"not-yet-open"</c> — OpensAt is in the future</item>
///   <item><c>"closed-by-organizer"</c> — ClosesAt is in the past (and StartDate is not)</item>
///   <item><c>"closed-event-started"</c> — StartDate is in the past (window irrelevant)</item>
/// </list>
///
/// Boundary semantics + state machine are pinned by
/// <c>Event_SetRegistrationWindow_Tests</c> in the domain test project. This
/// mapper test ensures the wire-format string mapping stays in lockstep so the
/// FE never sees a stale or inconsistent string.
/// </summary>
public class EventMappingProfileRegistrationAvailabilityTests
{
    private readonly IMapper _mapper;

    public EventMappingProfileRegistrationAvailabilityTests()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<EventMappingProfile>());
        _mapper = configuration.CreateMapper();
    }

    private static Event NewFutureFreeEvent(DateTime? start = null)
    {
        var title = EventTitle.Create("Phase 6A.153 mapper test").Value;
        var description = EventDescription.Create("registration-availability mapping").Value;
        var startDate = start ?? DateTime.UtcNow.AddDays(30);
        var endDate = startDate.AddHours(3);
        var @event = Event.Create(title, description, startDate, endDate, Guid.NewGuid(), 100).Value;
        // The mapper reads IsFreeEvent; flipping to free keeps tests free of
        // pricing setup noise. ComputeRegistrationAvailability doesn't care
        // about pricing, but the broader EventMappingProfile does.
        @event.SetAsFreeEvent().IsSuccess.Should().BeTrue();
        return @event;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  "open" — null window OR now inside window
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Maps_Open_WhenWindowIsBothNull()
    {
        var @event = NewFutureFreeEvent();
        // Window untouched: both fields null. Legacy default.

        var dto = _mapper.Map<EventDto>(@event);

        dto.RegistrationAvailability.Should().Be("open");
        dto.RegistrationOpensAt.Should().BeNull();
        dto.RegistrationClosesAt.Should().BeNull();
    }

    [Fact]
    public void Maps_Open_WhenNowIsBetweenOpensAndCloses()
    {
        var @event = NewFutureFreeEvent();
        @event.SetRegistrationWindow(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(20));

        var dto = _mapper.Map<EventDto>(@event);

        dto.RegistrationAvailability.Should().Be("open");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  "not-yet-open" — OpensAt is in the future
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Maps_NotYetOpen_WhenOpensAtIsInFuture()
    {
        var @event = NewFutureFreeEvent();
        var opensAt = DateTime.UtcNow.AddDays(7);
        @event.SetRegistrationWindow(opensAt, null);

        var dto = _mapper.Map<EventDto>(@event);

        dto.RegistrationAvailability.Should().Be("not-yet-open");
        // Raw timestamp must surface so the FE can format in local timezone
        // (without it the FE can't render "Registration opens [LOCAL DATE]").
        dto.RegistrationOpensAt.Should().Be(opensAt);
        dto.RegistrationClosesAt.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  "closed-by-organizer" — ClosesAt in past, StartDate still future
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Maps_ClosedByOrganizer_WhenClosesAtPassedButEventNotStarted()
    {
        // StartDate must remain in the future for this branch to fire — the
        // "closed-event-started" branch takes priority once StartDate has
        // passed. This is the "organizer paused registration" use case (D5).
        var @event = NewFutureFreeEvent(start: DateTime.UtcNow.AddDays(30));
        @event.SetRegistrationWindow(
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddSeconds(-1));

        var dto = _mapper.Map<EventDto>(@event);

        dto.RegistrationAvailability.Should().Be("closed-by-organizer");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  "closed-event-started" — StartDate in past, any window
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Maps_ClosedEventStarted_WhenStartDatePassed_RegardlessOfWindow()
    {
        // Reflection-set StartDate to the past — Event.Create rejects past
        // start dates by design. The mapper still needs to handle this
        // gracefully because background-aged events sit at past StartDate
        // (Phase 6A.152 visibility fix lives on this exact state).
        var @event = NewFutureFreeEvent();
        var sdField = typeof(Event).GetField("<StartDate>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        sdField!.SetValue(@event, (DateTime?)DateTime.UtcNow.AddDays(-1));

        var dto = _mapper.Map<EventDto>(@event);

        dto.RegistrationAvailability.Should().Be("closed-event-started",
            "StartDate guard takes priority over any window state");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Wire shape — raw timestamps preserved verbatim
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Maps_RawTimestamps_OutputUnchanged()
    {
        // The FE formats locally via TimeZoneId; the mapper must not
        // round-trip / re-anchor the UTC timestamps. Pin this so a careless
        // mapping change (e.g. .ToLocalTime()) doesn't silently shift the
        // FE's displayed time.
        //
        // Use relative dates so the test never fails when the calendar
        // rolls past hard-coded literals — and so the window mutator's
        // "closesAt <= StartDate" invariant always holds (we set the event
        // StartDate to 365 days out so a 30-day window fits cleanly inside).
        var farFutureStart = DateTime.UtcNow.AddDays(365);
        var @event = NewFutureFreeEvent(start: farFutureStart);
        var opensAt = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(60), DateTimeKind.Utc);
        var closesAt = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(90), DateTimeKind.Utc);
        var setResult = @event.SetRegistrationWindow(opensAt, closesAt);
        setResult.IsSuccess.Should().BeTrue("test fixture must pass mutator invariants before asserting mapping");

        var dto = _mapper.Map<EventDto>(@event);

        dto.RegistrationOpensAt.Should().Be(opensAt);
        dto.RegistrationClosesAt.Should().Be(closesAt);
    }
}
