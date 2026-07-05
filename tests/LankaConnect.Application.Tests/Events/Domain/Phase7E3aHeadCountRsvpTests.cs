using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 7E.3a — domain tests for the head-count RSVP path on free events.
///
/// Coverage:
/// 1. <see cref="Event.RegisterWithHeadCount"/> succeeds for free B-mode events (B1/B2/B3/B4).
/// 2. The Mode-A path (<see cref="Event.RegisterWithAttendees"/>) defensively rejects B/C modes
///    so a stale client cannot create a Registration row that contradicts the event mode.
/// 3. <see cref="Event.RegisterWithHeadCount"/> rejects DetailedAttendees + NoRegistration modes
///    (defensive — handler already dispatches by mode but we enforce here too).
/// 4. Paid B-mode events fail with a clear "deferred to 7E.3b" message (scope discipline).
/// 5. Capacity guard uses <see cref="HeadCountBreakdown.Total"/> via the
///    <see cref="Registration.GetAttendeeCount"/> canonical aggregator (per 7E.0 §2 audit).
/// 6. Duplicate registration check works the same way as the multi-attendee path
///    (architect: cross-path UserId + email guard).
/// </summary>
public class Phase7E3aHeadCountRsvpTests
{
    private readonly DateTime _start = DateTime.UtcNow.AddDays(7);
    private readonly DateTime _end = DateTime.UtcNow.AddDays(7).AddHours(2);

    private Event CreatePublishedEvent(RegistrationMode mode, int capacity = 50, bool isFree = true)
    {
        var ev = Event.Create(
            EventTitle.Create("Phase 7E.3a Test Event").Value,
            EventDescription.Create("Head-count RSVP tests").Value,
            _start, _end, Guid.NewGuid(),
            capacity).Value;

        if (isFree) ev.SetAsFreeEvent();

        var setMode = ev.SetRegistrationMode(mode);
        setMode.IsSuccess.Should().BeTrue($"SetRegistrationMode to {mode} should succeed for a fresh event");

        ev.Publish();
        ev.Status.Should().Be(EventStatus.Published);
        return ev;
    }

    private static RegistrationContact Contact(string email = "lead@example.com") =>
        RegistrationContact.Create(email, "555-0100", null).Value;

    [Fact]
    public void RegisterWithHeadCount_ModeB1Free_SucceedsAndIncreasesSpotsLeftByTotal()
    {
        var ev = CreatePublishedEvent(RegistrationMode.HeadCountOnly, capacity: 10);
        var hc = HeadCountBreakdown.ForTotalOnly(3).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Niroshana", hc, Contact());

        result.IsSuccess.Should().BeTrue();
        ev.CurrentRegistrations.Should().Be(3, "GetAttendeeCount honours HeadCount.Total");
        ev.Registrations.Should().HaveCount(1);
        ev.Registrations[0].LeadAttendeeName.Should().Be("Niroshana");
        ev.Registrations[0].HeadCount!.Total.Should().Be(3);
        ev.Registrations[0].Attendees.Should().BeEmpty("Mode B does not populate the attendee collection");
    }

    [Fact]
    public void RegisterWithHeadCount_ModeB2Free_SucceedsWithDemographicAxis()
    {
        var ev = CreatePublishedEvent(RegistrationMode.HeadCountByAge, capacity: 10);
        var hc = HeadCountBreakdown.ForByAge(adults: 2, children: 1).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", hc, Contact());

        result.IsSuccess.Should().BeTrue();
        ev.Registrations[0].HeadCount!.Demographics!.Adults.Should().Be(2);
        ev.Registrations[0].HeadCount!.Demographics!.Children.Should().Be(1);
    }

    [Fact]
    public void RegisterWithHeadCount_ModeB3Free_Succeeds()
    {
        var ev = CreatePublishedEvent(RegistrationMode.HeadCountByGender, capacity: 10);
        var hc = HeadCountBreakdown.ForByGender(males: 2, females: 1).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", hc, Contact());

        result.IsSuccess.Should().BeTrue();
        ev.Registrations[0].HeadCount!.Demographics!.Males.Should().Be(2);
        ev.Registrations[0].HeadCount!.Demographics!.Females.Should().Be(1);
    }

    [Fact]
    public void RegisterWithHeadCount_ModeB4Free_Succeeds()
    {
        var ev = CreatePublishedEvent(RegistrationMode.HeadCountByAgeAndGender, capacity: 10);
        var hc = HeadCountBreakdown.ForByAgeAndGender(adultMales: 1, adultFemales: 1, childMales: 1, childFemales: 0).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", hc, Contact());

        result.IsSuccess.Should().BeTrue();
        ev.Registrations[0].HeadCount!.Total.Should().Be(3);
        ev.Registrations[0].HeadCount!.Demographics!.AdultMales.Should().Be(1);
    }

    [Fact]
    public void RegisterWithHeadCount_RejectsDetailedAttendeesMode()
    {
        // DetailedAttendees registration should go through RegisterWithAttendees, not RegisterWithHeadCount.
        var ev = CreatePublishedEvent(RegistrationMode.DetailedAttendees, capacity: 10);
        var hc = HeadCountBreakdown.ForTotalOnly(2).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", hc, Contact());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("detailed-attendee"));
    }

    [Fact]
    public void RegisterWithHeadCount_RejectsNoRegistrationMode()
    {
        // Mode C produces no Registration row — RSVP attempts are rejected outright.
        var ev = CreatePublishedEvent(RegistrationMode.NoRegistration, capacity: 10);
        var hc = HeadCountBreakdown.ForTotalOnly(2).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", hc, Contact());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("not required"));
    }

    [Fact]
    public void RegisterWithHeadCount_PaidEvent_NowSucceeds_AfterPhase7E3b()
    {
        // Phase 7E.3b shipped paid B-mode + Stripe checkout. Paid B1 RSVP now succeeds and
        // creates a Preliminary registration with TotalPrice = Total × ticketPrice. The earlier
        // 7E.3a-era failure-message assertion is obsolete after the gate was lifted.
        var ev = Event.Create(
            EventTitle.Create("Paid B Event").Value,
            EventDescription.Create("Paid B").Value,
            _start, _end, Guid.NewGuid(),
            capacity: 10,
            ticketPrice: Money.Create(15m, Currency.USD).Value).Value;
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();
        ev.Publish();

        var hc = HeadCountBreakdown.ForTotalOnly(2).Value;
        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", hc, Contact());

        result.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        var registration = ev.Registrations.Single();
        registration.TotalPrice!.Amount.Should().Be(30m, "2 × $15");
        registration.Status.Should().Be(RegistrationStatus.Preliminary,
            "paid event registration awaits Stripe webhook before Confirmed");
    }

    [Fact]
    public void RegisterWithHeadCount_RespectsCapacityGuard()
    {
        var ev = CreatePublishedEvent(RegistrationMode.HeadCountOnly, capacity: 5);
        var hc = HeadCountBreakdown.ForTotalOnly(6).Value; // exceeds capacity

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", hc, Contact());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("capacity"));
    }

    [Fact]
    public void RegisterWithHeadCount_RespectsMaxAttendeesPerRegistration()
    {
        // Default MaxAttendeesPerRegistration is 10. Total > that should be rejected.
        var ev = CreatePublishedEvent(RegistrationMode.HeadCountOnly, capacity: 100);
        var hc = HeadCountBreakdown.ForTotalOnly(11).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", hc, Contact());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Maximum"));
    }

    [Fact]
    public void RegisterWithHeadCount_DuplicateUserId_Rejected()
    {
        var ev = CreatePublishedEvent(RegistrationMode.HeadCountByAge, capacity: 50);
        var userId = Guid.NewGuid();
        ev.RegisterWithHeadCount(userId, "Lead", HeadCountBreakdown.ForByAge(2, 1).Value, Contact()).IsSuccess.Should().BeTrue();

        var second = ev.RegisterWithHeadCount(userId, "Lead", HeadCountBreakdown.ForByAge(1, 1).Value, Contact("other@example.com"));

        second.IsFailure.Should().BeTrue();
        second.Errors.Should().Contain(e => e.Contains("already registered"));
    }

    [Fact]
    public void RegisterWithHeadCount_DuplicateEmail_Rejected_AnonymousVsAuth()
    {
        var ev = CreatePublishedEvent(RegistrationMode.HeadCountByAge, capacity: 50);

        // Anonymous registers first.
        ev.RegisterWithHeadCount(null, "Anon", HeadCountBreakdown.ForByAge(1, 1).Value, Contact("shared@example.com"))
            .IsSuccess.Should().BeTrue();

        // Authenticated user with same email — cross-path duplicate detection rejects.
        var second = ev.RegisterWithHeadCount(Guid.NewGuid(), "Auth",
            HeadCountBreakdown.ForByAge(1, 1).Value, Contact("shared@example.com"));

        second.IsFailure.Should().BeTrue();
        second.Errors.Should().Contain(e => e.Contains("email is already registered"));
    }

    [Fact]
    public void RegisterWithAttendees_DefensivelyRejectsBMode()
    {
        // Architect §6 hot-spot: stale clients hitting the legacy Mode-A path on a B-mode event
        // would create Registration rows that contradict the event's mode. This test enforces
        // that the domain method rejects the call with a clear redirect message.
        var ev = CreatePublishedEvent(RegistrationMode.HeadCountByAge, capacity: 50);

        var attendee = AttendeeDetails.Create("John Doe", AgeCategory.Adult).Value;
        var result = ev.RegisterWithAttendees(Guid.NewGuid(), new[] { attendee }, Contact());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Contains("HeadCountByAge") || e.Contains("head-count"));
    }

    [Fact]
    public void RegisterWithAttendees_DefensivelyRejectsNoRegistrationMode()
    {
        var ev = CreatePublishedEvent(RegistrationMode.NoRegistration, capacity: 50);

        var attendee = AttendeeDetails.Create("John Doe", AgeCategory.Adult).Value;
        var result = ev.RegisterWithAttendees(Guid.NewGuid(), new[] { attendee }, Contact());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("not required"));
    }

    [Fact]
    public void RegisterWithAttendees_StillSucceeds_OnDetailedAttendeesMode()
    {
        // Regression: Mode-A events must keep working exactly as before.
        var ev = CreatePublishedEvent(RegistrationMode.DetailedAttendees, capacity: 50);

        var attendee = AttendeeDetails.Create("John Doe", AgeCategory.Adult).Value;
        var result = ev.RegisterWithAttendees(Guid.NewGuid(), new[] { attendee }, Contact());

        result.IsSuccess.Should().BeTrue("legacy Mode-A flow must remain unchanged");
        ev.CurrentRegistrations.Should().Be(1);
    }
}
