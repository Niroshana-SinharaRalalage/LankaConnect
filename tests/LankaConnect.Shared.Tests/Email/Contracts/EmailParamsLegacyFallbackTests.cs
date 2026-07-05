using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 7C.2b Chunk 2c — defence-in-depth for the
/// <c>LocationDetails ?? &lt;fallback&gt;</c> pattern inside every
/// event-location <c>*EmailParams.ToDictionary()</c>.
///
/// <para><b>Why this exists.</b> The Chunk 2b inbox smoke (2026-04-23)
/// surfaced a paid-ticket confirmation email whose rendered body showed the
/// LOCATION header with NO value. The template body was correct (DecomposedBlock
/// landed, verified via staging DB probe); the handler path that built the
/// params object failed to call <c>WithLocationDetails(...)</c>. The fallback at
/// <c>LocationEmailProjection.Online with { LegacyFlatString = EventLocation }</c>
/// then produced a projection whose <c>LocationAddress</c> was the empty string,
/// so the decomposed <c>&lt;span&gt;{{LocationAddress}}&lt;/span&gt;</c> rendered
/// as an empty span. Worse than the pre-migration flat address — silent data
/// deletion.</para>
///
/// <para><b>What this guards.</b> For every params class that writes location
/// keys via <see cref="LocationEmailDictionaryWriter"/>, the fallback must
/// project the scalar <c>EventLocation</c> into <c>LocationAddress</c>, so
/// that any handler that forgets <c>WithLocationDetails</c> still produces a
/// non-empty rendered location — the flat pre-decomposition string, not "".
/// Same semantic as the <c>Location?.Address == null</c> branch of
/// <c>EventExtensions.ProjectEmailLocation</c>, which already emits
/// <c>LocationAddress = legacyFlatString</c>.</para>
///
/// <para>Tests pair with the new
/// <see cref="LocationEmailProjection.FromLegacyScalar(string)"/> factory that
/// encodes the fallback semantic in one place so the 9 params classes reference
/// a single helper rather than duplicating the <c>Online with { ... }</c> shape.</para>
/// </summary>
public class EmailParamsLegacyFallbackTests
{
    private const string Scalar = "4314 Clark Ave, Cleveland, Ohio";

    // ──────────────────────────────────────────────────────────────────────
    // 1. The shared factory itself — the single source of truth.
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void FromLegacyScalar_SetsLocationAddress_AndLegacyFlatString_ToScalar()
    {
        var projection = LocationEmailProjection.FromLegacyScalar(Scalar);

        projection.LocationAddress.Should().Be(Scalar,
            "the decomposed template renders LocationAddress; if the handler skipped WithLocationDetails we must still project the legacy scalar into it so the email body is not silently empty");
        projection.LegacyFlatString.Should().Be(Scalar,
            "un-migrated templates still read EventLocation from LegacyFlatString");
        projection.HasLocationName.Should().BeFalse();
        projection.HasSecondaryLocation.Should().BeFalse();
        projection.LocationName.Should().BeEmpty();
    }

    [Fact]
    public void FromLegacyScalar_Null_NormalizesToEmptyString()
    {
        var projection = LocationEmailProjection.FromLegacyScalar(null!);

        projection.LocationAddress.Should().BeEmpty();
        projection.LegacyFlatString.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────
    // 2. Per-params-class fallback behavior — no WithLocationDetails called.
    //
    // If these start failing when a new params class is added, that class is
    // missing the ?? FromLegacyScalar(EventLocation) fallback and will ship
    // the empty-LOCATION regression.
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void TicketConfirmation_Fallback_ProjectsScalarIntoLocationAddress()
    {
        var p = TicketConfirmationEmailParams.Create(
            eventId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            userName: "u",
            contactEmail: "u@x.com",
            eventTitle: "t",
            eventStartDate: DateTime.UtcNow,
            eventStartTime: "6 PM",
            eventLocation: Scalar,
            eventDetailsUrl: "u",
            amountPaid: 1m,
            paymentIntentId: "pi",
            paymentDate: DateTime.UtcNow,
            quantity: 1);

        // NO WithLocationDetails — simulates RegistrationEmailService.cs:205 bug

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationAddress].Should().Be(Scalar);
        dict[EmailTemplateContract.Event.EventLocation].Should().Be(Scalar);
    }

    [Fact]
    public void FreeEventRegistration_Fallback_ProjectsScalarIntoLocationAddress()
    {
        var p = FreeEventRegistrationEmailParams.Create(
            eventId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            userName: "u",
            userEmail: "u@x.com",
            eventTitle: "t",
            eventStartDate: DateTime.UtcNow,
            eventStartTime: "6 PM",
            eventLocation: Scalar,
            eventDetailsUrl: "u",
            registrationDate: DateTime.UtcNow);

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationAddress].Should().Be(Scalar);
        dict[EmailTemplateContract.Event.EventLocation].Should().Be(Scalar);
    }

    [Fact]
    public void RegistrationCancellation_Fallback_ProjectsScalarIntoLocationAddress()
    {
        var p = RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "u",
            userEmail: "u@x.com",
            registrationId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "t",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: null,
            eventLocation: Scalar,
            cancellationReason: "x",
            cancelledAt: DateTime.UtcNow,
            refundStatus: "none");

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationAddress].Should().Be(Scalar);
    }

    [Fact]
    public void EventCancellation_Fallback_ProjectsScalarIntoLocationAddress()
    {
        var p = EventCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "u",
            userEmail: "u@x.com",
            eventId: Guid.NewGuid(),
            eventTitle: "t",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: null,
            eventLocation: Scalar,
            cancellationReason: "x",
            cancelledAt: DateTime.UtcNow,
            organizerName: "o",
            refundsWillBeProcessed: false);

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationAddress].Should().Be(Scalar);
    }

    [Fact]
    public void EventApproval_Fallback_ProjectsScalarIntoLocationAddress()
    {
        var p = EventApprovalEmailParams.Create(
            organizerId: Guid.NewGuid(),
            organizerName: "o",
            organizerEmail: "o@x.com",
            eventId: Guid.NewGuid(),
            eventTitle: "t",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: null,
            eventLocation: Scalar,
            approvedAt: DateTime.UtcNow,
            eventUrl: "https://example.com/e/1",
            eventManageUrl: "https://example.com/e/1/manage");

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationAddress].Should().Be(Scalar);
    }

    [Fact]
    public void EventReminder_Fallback_ProjectsScalarIntoLocationAddress()
    {
        var p = EventReminderEmailParams.Create(
            eventId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            attendeeName: "a",
            attendeeEmail: "a@x.com",
            eventTitle: "t",
            eventStartDate: DateTime.UtcNow,
            eventStartTime: "6 PM",
            eventLocation: Scalar,
            quantity: 1,
            hoursUntilEvent: 24,
            reminderTimeframe: "1 day",
            reminderMessage: "m",
            eventDetailsUrl: "u");

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationAddress].Should().Be(Scalar);
    }

    [Fact]
    public void AttendeesAdded_Fallback_ProjectsScalarIntoLocationAddress()
    {
        var p = AttendeesAddedEmailParams.Create(
            userId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            userName: "u",
            userEmail: "u@x.com",
            eventTitle: "t",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: null,
            eventLocation: Scalar,
            previousCount: 1,
            addedCount: 1,
            newTotalCount: 2,
            additionalAmount: 10m,
            totalPaid: 20m,
            newAttendees: "",
            newAttendeesHtml: "",
            allAttendees: "",
            allAttendeesHtml: "",
            eventDetailsUrl: "u");

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationAddress].Should().Be(Scalar);
    }

    [Fact]
    public void PreliminaryRegistrationPayment_Fallback_ProjectsScalarIntoLocationAddress()
    {
        var p = PreliminaryRegistrationPaymentEmailParams.Create(
            recipientEmail: "u@x.com",
            userName: "u",
            eventId: Guid.NewGuid(),
            eventTitle: "t",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: null,
            eventLocation: Scalar,
            registrationId: Guid.NewGuid(),
            attendeeCount: 1,
            totalAmount: 10m,
            currency: "USD",
            paymentLink: "u",
            expiresAt: DateTime.UtcNow.AddHours(24));

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationAddress].Should().Be(Scalar);
    }

    [Fact]
    public void SignupCommitment_Fallback_ProjectsScalarIntoLocationAddress()
    {
        // Chunk 1 SignupCommitment params — sibling class, same fallback pattern.
        var p = new SignupCommitmentEmailParams
        {
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            UserName = "u",
            UserEmail = "u@x.com",
            EventTitle = "t",
            EventStartDate = DateTime.UtcNow,
            EventLocation = Scalar,
            SignupItem = "i",
            Quantity = 1,
        };

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationAddress].Should().Be(Scalar);
    }
}
