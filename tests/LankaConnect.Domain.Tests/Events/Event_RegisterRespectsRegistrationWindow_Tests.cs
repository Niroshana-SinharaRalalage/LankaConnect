using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 6A.153 — registration-window guard on the three <c>Register*</c>
/// methods. Pins the post-Status / post-StartDate / pre-Capacity ordering
/// across <see cref="Event.Register"/>, <see cref="Event.RegisterAnonymous"/>,
/// and <see cref="Event.RegisterWithAttendees"/>.
///
/// Boundary semantics (locked by <c>Event_SetRegistrationWindow_Tests</c>):
/// - OpensAt &gt; now → block as NotYetOpen
/// - ClosesAt &lt;= now → block as ClosedByOrganizer
/// - Both null → legacy behaviour (open)
///
/// Window cascade ordering (architect-locked):
/// Status → StartDate → ExternalPaid → External-mode → Window → RegistrationMode → ...
/// — pinned by <see cref="RegisterWithAttendees_ExternalPaidAndWindowSet_ExternalPaidErrorWins"/>.
/// </summary>
public class Event_RegisterRespectsRegistrationWindow_Tests
{
    private static EventTitle Title() =>
        EventTitle.Create("Phase 6A.153 register-window test event").Value;

    private static EventDescription Description() =>
        EventDescription.Create("Phase 6A.153 coverage").Value;

    /// <summary>
    /// Helper: build a Published event 30 days out (so Status guard passes
    /// and StartDate guard doesn't fire). Default 100-capacity.
    /// </summary>
    private static Event NewPublishedEvent(DateTime? start = null)
    {
        var startDate = start ?? DateTime.UtcNow.AddDays(30);
        var @event = Event.Create(
            Title(), Description(),
            startDate: startDate, endDate: startDate.AddHours(3),
            organizerId: Guid.NewGuid(),
            capacity: 100).Value;
        // RegisterWithAttendees calls CalculatePriceForAttendees which throws
        // unless the event is explicitly free or has pricing configured. The
        // window-guard tests don't care about pricing — flip to free so the
        // pricing branch is a no-op and we exercise the window guard alone.
        @event.SetAsFreeEvent();
        ForceStatus(@event, EventStatus.Published);
        return @event;
    }

    private static void ForceStatus(Event @event, EventStatus status)
    {
        var field = typeof(Event).GetField("<Status>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(@event, status);
    }

    private static AttendeeInfo Attendee(string emailLocal = "anon")
    {
        var email = $"{emailLocal}+{Guid.NewGuid():N}@example.test";
        return AttendeeInfo.Create(
            name: $"Anon {emailLocal}",
            age: 30,
            address: "123 Test St",
            email: email,
            phoneNumber: "+15551234567").Value;
    }

    private static AttendeeDetails Detail(string name = "Lead")
    {
        // CreateFromAge is the simpler helper — domain assigns AgeCategory
        // from the int age. Name + age is enough for the window guard tests
        // (we never assert attendee-content, only the Register* result).
        return AttendeeDetails.CreateFromAge(name: name, age: 30).Value;
    }

    private static RegistrationContact Contact(string emailLocal = "contact")
    {
        var email = $"{emailLocal}+{Guid.NewGuid():N}@example.test";
        return RegistrationContact.Create(
            email: email,
            phoneNumber: "+15551234567",
            address: "123 Test St").Value;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Register(userId, qty) — authenticated single-attendee path
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Register_WhenWindowNotYetOpen_FailsWithIsoTimestamp()
    {
        var @event = NewPublishedEvent();
        @event.SetRegistrationWindow(DateTime.UtcNow.AddDays(5), null);

        var result = @event.Register(Guid.NewGuid(), quantity: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Registration for this event opens at");
        // ISO-8601 timestamp suffix lets the FE error-fallback parse and
        // re-format in the event's local timezone if the DTO path is
        // bypassed (curl, third-party clients, etc.).
        result.Error.Should().Contain("T");
        result.Error.Should().Contain("Z");
    }

    [Fact]
    public void Register_WhenWindowClosedByOrganizer_Fails()
    {
        var @event = NewPublishedEvent();
        // Set both opens (past) and closes (past) so domain accepts the
        // mutator (opens < closes invariant) but `now` is after closes.
        @event.SetRegistrationWindow(
            DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddSeconds(-1));

        var result = @event.Register(Guid.NewGuid(), quantity: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Registration for this event has closed");
    }

    [Fact]
    public void Register_WhenWindowOpen_Succeeds()
    {
        var @event = NewPublishedEvent();
        @event.SetRegistrationWindow(
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(20));

        var result = @event.Register(Guid.NewGuid(), quantity: 1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Register_WhenWindowBothNull_LegacyBehaviour()
    {
        var @event = NewPublishedEvent();
        // Window untouched — both fields null. Legacy path: registration open.

        var result = @event.Register(Guid.NewGuid(), quantity: 1);

        result.IsSuccess.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RegisterAnonymous(attendeeInfo, qty) — anonymous single-attendee
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RegisterAnonymous_WhenWindowNotYetOpen_FailsWithIsoTimestamp()
    {
        var @event = NewPublishedEvent();
        @event.SetRegistrationWindow(DateTime.UtcNow.AddDays(5), null);

        var result = @event.RegisterAnonymous(Attendee(), quantity: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Registration for this event opens at");
    }

    [Fact]
    public void RegisterAnonymous_WhenWindowClosedByOrganizer_Fails()
    {
        var @event = NewPublishedEvent();
        @event.SetRegistrationWindow(
            DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddSeconds(-1));

        var result = @event.RegisterAnonymous(Attendee(), quantity: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Registration for this event has closed");
    }

    [Fact]
    public void RegisterAnonymous_WhenWindowOpen_Succeeds()
    {
        var @event = NewPublishedEvent();
        @event.SetRegistrationWindow(
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(20));

        var result = @event.RegisterAnonymous(Attendee(), quantity: 1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RegisterAnonymous_WhenWindowBothNull_LegacyBehaviour()
    {
        var @event = NewPublishedEvent();

        var result = @event.RegisterAnonymous(Attendee(), quantity: 1);

        result.IsSuccess.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RegisterWithAttendees — primary multi-attendee path (anon + auth)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RegisterWithAttendees_WhenWindowNotYetOpen_FailsWithIsoTimestamp()
    {
        var @event = NewPublishedEvent();
        @event.SetRegistrationWindow(DateTime.UtcNow.AddDays(5), null);
        var detail = Detail();

        var result = @event.RegisterWithAttendees(
            userId: null, attendees: new[] { detail }, contact: Contact());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Registration for this event opens at");
    }

    [Fact]
    public void RegisterWithAttendees_WhenWindowClosedByOrganizer_Fails()
    {
        var @event = NewPublishedEvent();
        @event.SetRegistrationWindow(
            DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddSeconds(-1));
        var detail = Detail();

        var result = @event.RegisterWithAttendees(
            userId: null, attendees: new[] { detail }, contact: Contact());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Registration for this event has closed");
    }

    [Fact]
    public void RegisterWithAttendees_WhenWindowOpen_Succeeds()
    {
        var @event = NewPublishedEvent();
        @event.SetRegistrationWindow(
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(20));
        var detail = Detail();

        var result = @event.RegisterWithAttendees(
            userId: null, attendees: new[] { detail }, contact: Contact());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RegisterWithAttendees_WhenWindowBothNull_LegacyBehaviour()
    {
        var @event = NewPublishedEvent();
        var detail = Detail();

        var result = @event.RegisterWithAttendees(
            userId: null, attendees: new[] { detail }, contact: Contact());

        result.IsSuccess.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Cascade ordering — ExternalPaid dominates Window (architect test #12)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RegisterWithAttendees_ExternalPaidAndWindowSet_ExternalPaidErrorWins()
    {
        // Architect-locked: cascade order is Status → StartDate → ExternalPaid
        // → External-mode → Window → RegistrationMode. So an ExternalPaid
        // event with a not-yet-open window must return the ExternalPaid
        // error, not the window error — the external vendor (Eventbrite,
        // Stripe Payment Link, etc.) controls availability via its own URL,
        // and our window is irrelevant for that flow.
        var @event = NewPublishedEvent();
        // Force ExternalPaid via reflection (SetExternalPayment requires a
        // URL value object; the simplest way to assert ordering is to set
        // the backing field directly).
        var pmField = typeof(Event).GetField("<PaymentMode>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        pmField!.SetValue(@event, EventPaymentMode.ExternalPaid);

        @event.SetRegistrationWindow(DateTime.UtcNow.AddDays(5), null);
        var detail = Detail();

        var result = @event.RegisterWithAttendees(
            userId: null, attendees: new[] { detail }, contact: Contact());

        result.IsFailure.Should().BeTrue();
        // The ExternalPaid guard returns ExternalRegistrationGuardMessage —
        // assert by absence of the window error suffix to keep this test
        // independent of the exact ExternalPaid message text.
        result.Error.Should().NotContain("Registration for this event opens at");
        result.Error.Should().NotContain("has closed");
    }
}
