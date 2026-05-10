using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 8X.3 — internal registration paths must reject ExternalPaid events at the
/// domain boundary. Defence-in-depth: the application validator (Slice 8X.4) also
/// rejects, but the domain is the last line and the architect-locked guard message
/// must be exact so frontend/email rendering can branch on it.
/// </summary>
public class Event_RegisterBlockedForExternalPaid_Tests
{
    private static Event CreateExternalPaidEvent()
    {
        var title = EventTitle.Create("ExternalPaid registration block test").Value;
        var description = EventDescription.Create("Phase 8X.3 domain test").Value;
        var ev = Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), capacity: 100).Value;

        var pricing = TicketPricing.CreateDualPrice(
            Money.Create(25m, Currency.USD).Value,
            Money.Create(10m, Currency.USD).Value,
            childAgeLimit: 12).Value;
        var externalReg = ExternalRegistration.Create(
            "https://eventbrite.com/e/test-12345", "Pay at door", "Eventbrite").Value;

        ev.SetExternalPayment(externalReg, pricing).IsSuccess.Should().BeTrue();
        ev.Publish().IsSuccess.Should().BeTrue();
        return ev;
    }

    private static AttendeeDetails Attendee(string name) =>
        AttendeeDetails.Create(name, AgeCategory.Adult, gender: null,
            ticketTierId: null, ticketTierName: null).Value;

    private static RegistrationContact Contact() =>
        RegistrationContact.Create("buyer@example.com", "555-0100", null).Value;

    [Fact]
    public void RegisterWithAttendees_OnExternalPaidEvent_Fails()
    {
        var ev = CreateExternalPaidEvent();

        var result = ev.RegisterWithAttendees(
            userId: Guid.NewGuid(),
            attendees: new[] { Attendee("Alice") },
            contact: Contact());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("external registration");
    }

    [Fact]
    public void RegisterWithHeadCount_OnExternalPaidEvent_Fails()
    {
        var ev = CreateExternalPaidEvent();
        // ExternalPaid forces RegistrationMode = NoRegistration. RegisterWithHeadCount
        // also rejects NoRegistration, so we need to assert the ExternalPaid guard fires
        // FIRST (before the mode guard). The guard message is the architect-locked
        // "external registration" string, not the NoRegistration mode message.

        var headCount = HeadCountBreakdown.ForTotalOnly(2).Value;
        var result = ev.RegisterWithHeadCount(
            userId: Guid.NewGuid(),
            leadAttendeeName: "Alice",
            headCount: headCount,
            contact: Contact());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("external registration");
    }
}
