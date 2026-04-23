using FluentAssertions;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 7C.2b Chunk 2 — every registration/lifecycle email params class gains a
/// <c>LocationDetails</c> property, a <c>WithLocationDetails(projection)</c> fluent
/// setter, and rewrites <c>ToDictionary()</c> to emit the 8 decomposed location keys
/// via <see cref="LocationEmailDictionaryWriter"/>. The pattern mirrors what already
/// ships for <see cref="FreeEventRegistrationEmailParams"/> and
/// <see cref="SignupCommitmentEmailParams"/>. These tests are the RED-green-refactor
/// driver for the 7-class refactor.
///
/// <para>One fact per class — each exercises: (a) fluent setter returns <c>this</c>,
/// (b) setter throws on null, (c) legacy <c>EventLocation</c> string is overwritten
/// with <see cref="LocationEmailProjection.LegacyFlatString"/>, (d)
/// <c>ToDictionary()</c> emits all 8 decomposed keys after the setter fires.</para>
/// </summary>
public class RegistrationLifecycleLocationDetailsTests
{
    private static LocationEmailProjection BuildProjection() => new(
        LocationName: "Aurora Clubhouse",
        LocationAddress: "4314 Clark Ave, Cleveland, Ohio, 44120, USA",
        HasLocationName: true,
        HasSecondaryLocation: true,
        SecondaryLocationLabel: "Parking Lot",
        SecondaryLocationName: "Geoga Lake Parking",
        HasSecondaryLocationName: true,
        SecondaryLocationAddress: "943 Penny Lane, Aurora, OH, 44202, USA",
        LegacyFlatString: "4314 Clark Ave, Cleveland");

    private static void AssertAll8DecomposedKeys(IDictionary<string, object> dict)
    {
        dict[EmailTemplateContract.Event.LocationName].Should().Be("Aurora Clubhouse");
        dict[EmailTemplateContract.Event.LocationAddress].Should().Be("4314 Clark Ave, Cleveland, Ohio, 44120, USA");
        dict[EmailTemplateContract.Event.HasLocationName].Should().Be(true);
        dict[EmailTemplateContract.Event.HasSecondaryLocation].Should().Be(true);
        dict[EmailTemplateContract.Event.SecondaryLocationLabel].Should().Be("Parking Lot");
        dict[EmailTemplateContract.Event.SecondaryLocationName].Should().Be("Geoga Lake Parking");
        dict[EmailTemplateContract.Event.HasSecondaryLocationName].Should().Be(true);
        dict[EmailTemplateContract.Event.SecondaryLocationAddress].Should().Be("943 Penny Lane, Aurora, OH, 44202, USA");
        dict[EmailTemplateContract.Event.EventLocation].Should().Be("4314 Clark Ave, Cleveland");
    }

    // ──────────────────────────────────────────────────────────────────────
    // 1. TicketConfirmationEmailParams (paid-event registration)
    // ──────────────────────────────────────────────────────────────────────
    [Fact]
    public void TicketConfirmation_WithLocationDetails_SetsAll8DecomposedKeys()
    {
        var p = TicketConfirmationEmailParams.Create(
            eventId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            userName: "Test User",
            contactEmail: "user@example.com",
            eventTitle: "Paid Event",
            eventStartDate: new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
            eventStartTime: "6:00 PM",
            eventLocation: "legacy",
            eventDetailsUrl: "https://example.com/events/1",
            amountPaid: 25m,
            paymentIntentId: "pi_123",
            paymentDate: new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc),
            quantity: 2);

        var returned = p.WithLocationDetails(BuildProjection());

        returned.Should().BeSameAs(p);
        p.LocationDetails.Should().NotBeNull();
        p.EventLocation.Should().Be("4314 Clark Ave, Cleveland");
        AssertAll8DecomposedKeys(p.ToDictionary());
    }

    [Fact]
    public void TicketConfirmation_WithLocationDetails_Null_Throws()
    {
        var p = TicketConfirmationEmailParams.Create(
            Guid.NewGuid(), Guid.NewGuid(), "u", "u@x.com", "t",
            DateTime.UtcNow, "6 PM", "loc", "url", 1m, "pi", DateTime.UtcNow, 1);
        Action act = () => p.WithLocationDetails(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ──────────────────────────────────────────────────────────────────────
    // 2. RegistrationCancellationEmailParams
    // ──────────────────────────────────────────────────────────────────────
    [Fact]
    public void RegistrationCancellation_WithLocationDetails_SetsAll8DecomposedKeys()
    {
        var p = RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "User",
            userEmail: "u@example.com",
            registrationId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Cancelled Event",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: null,
            eventLocation: "legacy",
            cancellationReason: "user-initiated",
            cancelledAt: DateTime.UtcNow,
            refundStatus: "none");

        var returned = p.WithLocationDetails(BuildProjection());

        returned.Should().BeSameAs(p);
        p.EventLocation.Should().Be("4314 Clark Ave, Cleveland");
        AssertAll8DecomposedKeys(p.ToDictionary());
    }

    // ──────────────────────────────────────────────────────────────────────
    // 3. EventCancellationEmailParams (all-attendee broadcast)
    // ──────────────────────────────────────────────────────────────────────
    [Fact]
    public void EventCancellation_WithLocationDetails_SetsAll8DecomposedKeys()
    {
        var p = EventCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "Attendee",
            userEmail: "a@example.com",
            eventId: Guid.NewGuid(),
            eventTitle: "Cancelled Event",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: null,
            eventLocation: "legacy",
            cancellationReason: "weather",
            cancelledAt: DateTime.UtcNow,
            organizerName: "Organizer",
            refundsWillBeProcessed: true);

        var returned = p.WithLocationDetails(BuildProjection());

        returned.Should().BeSameAs(p);
        p.EventLocation.Should().Be("4314 Clark Ave, Cleveland");
        AssertAll8DecomposedKeys(p.ToDictionary());
    }

    // ──────────────────────────────────────────────────────────────────────
    // 4. EventApprovalEmailParams (organizer notification)
    // ──────────────────────────────────────────────────────────────────────
    [Fact]
    public void EventApproval_WithLocationDetails_SetsAll8DecomposedKeys()
    {
        var p = EventApprovalEmailParams.Create(
            organizerId: Guid.NewGuid(),
            organizerName: "Organizer",
            organizerEmail: "o@example.com",
            eventId: Guid.NewGuid(),
            eventTitle: "Approved Event",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: null,
            eventLocation: "legacy",
            approvedAt: DateTime.UtcNow,
            eventUrl: "https://example.com/events/1",
            eventManageUrl: "https://example.com/events/1/manage");

        var returned = p.WithLocationDetails(BuildProjection());

        returned.Should().BeSameAs(p);
        p.EventLocation.Should().Be("4314 Clark Ave, Cleveland");
        AssertAll8DecomposedKeys(p.ToDictionary());
    }

    // ──────────────────────────────────────────────────────────────────────
    // 5. EventReminderEmailParams — NOTE: currently uses `Location` not `EventLocation`.
    //    Chunk 2 normalizes it to EventLocation so the contract is uniform across the
    //    family (single canonical {{EventLocation}} legacy placeholder).
    // ──────────────────────────────────────────────────────────────────────
    [Fact]
    public void EventReminder_WithLocationDetails_SetsAll8DecomposedKeys()
    {
        var p = EventReminderEmailParams.Create(
            eventId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            attendeeName: "Attendee",
            attendeeEmail: "a@example.com",
            eventTitle: "Upcoming Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            eventStartTime: "7:00 PM",
            eventLocation: "legacy",
            quantity: 1,
            hoursUntilEvent: 168,
            reminderTimeframe: "7 days",
            reminderMessage: "See you soon",
            eventDetailsUrl: "https://example.com/events/1");

        var returned = p.WithLocationDetails(BuildProjection());

        returned.Should().BeSameAs(p);
        p.EventLocation.Should().Be("4314 Clark Ave, Cleveland");
        AssertAll8DecomposedKeys(p.ToDictionary());
    }

    // ──────────────────────────────────────────────────────────────────────
    // 6. AttendeesAddedEmailParams
    // ──────────────────────────────────────────────────────────────────────
    [Fact]
    public void AttendeesAdded_WithLocationDetails_SetsAll8DecomposedKeys()
    {
        var p = AttendeesAddedEmailParams.Create(
            userId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            userName: "User",
            userEmail: "u@example.com",
            eventTitle: "Event",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: null,
            eventLocation: "legacy",
            previousCount: 1,
            addedCount: 2,
            newTotalCount: 3,
            additionalAmount: 10m,
            totalPaid: 30m,
            newAttendees: "A, B",
            newAttendeesHtml: "<p>A, B</p>",
            allAttendees: "X, A, B",
            allAttendeesHtml: "<p>X, A, B</p>",
            eventDetailsUrl: "https://example.com/events/1");

        var returned = p.WithLocationDetails(BuildProjection());

        returned.Should().BeSameAs(p);
        p.EventLocation.Should().Be("4314 Clark Ave, Cleveland");
        AssertAll8DecomposedKeys(p.ToDictionary());
    }

    // ──────────────────────────────────────────────────────────────────────
    // 7. PreliminaryRegistrationPaymentEmailParams
    // ──────────────────────────────────────────────────────────────────────
    [Fact]
    public void PreliminaryRegistrationPayment_WithLocationDetails_SetsAll8DecomposedKeys()
    {
        var p = PreliminaryRegistrationPaymentEmailParams.Create(
            recipientEmail: "u@example.com",
            userName: "User",
            eventId: Guid.NewGuid(),
            eventTitle: "Event",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: null,
            eventLocation: "legacy",
            registrationId: Guid.NewGuid(),
            attendeeCount: 1,
            totalAmount: 25m,
            currency: "USD",
            paymentLink: "https://example.com/pay",
            expiresAt: DateTime.UtcNow.AddDays(1));

        var returned = p.WithLocationDetails(BuildProjection());

        returned.Should().BeSameAs(p);
        p.EventLocation.Should().Be("4314 Clark Ave, Cleveland");
        AssertAll8DecomposedKeys(p.ToDictionary());
    }
}
