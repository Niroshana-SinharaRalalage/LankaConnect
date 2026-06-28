using FluentAssertions;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 7C.2 fan-out: Tests for <see cref="SignupCommitmentEmailParams.WithLocationDetails"/>
/// and the 8 decomposed location keys it emits via <c>ToDictionary()</c>.
///
/// Mirrors <see cref="FreeEventRegistrationLocationDetailsTests"/> exactly so every
/// event-email params class that adopts the projection exposes the same contract:
///   - <c>LocationDetails</c> property
///   - <c>WithLocationDetails(LocationEmailProjection)</c> fluent setter (throws on null)
///   - <c>ToDictionary()</c> emits all 8 decomposed keys plus the legacy fallback
///   - <c>ToDictionary()</c> stays safe when no projection was supplied
///
/// Together with the template rewrite migration this removes the GPS-coordinate suffix
/// that <see cref="LankaConnect.Products.LankaEvents.Domain.ValueObjects.EventLocation.ToString"/>
/// currently leaks into the three signup/volunteer commitment emails.
/// </summary>
public class SignupCommitmentEmailParamsLocationDetailsTests
{
    private static SignupCommitmentEmailParams BuildBase() =>
        SignupCommitmentEmailParams.CreateConfirmation(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            eventId: Guid.NewGuid(),
            eventTitle: "Christmas Dinner Dance 2025",
            signupItem: "Rice and Curry",
            quantity: 2,
            eventStartDate: new DateTime(2026, 2, 15, 22, 0, 0, DateTimeKind.Utc),
            timeZoneId: "America/New_York",
            eventLocation: "legacy fallback string",
            eventDetailsUrl: "https://lankaconnect.com/events/abc");

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

    [Fact]
    public void WithLocationDetails_SetsProjectionAndOverridesLegacyEventLocation()
    {
        var p = BuildBase();
        var proj = BuildProjection();

        p.WithLocationDetails(proj);

        p.LocationDetails.Should().Be(proj);
        p.EventLocation.Should().Be("4314 Clark Ave, Cleveland");
    }

    [Fact]
    public void WithLocationDetails_Null_Throws()
    {
        var p = BuildBase();

        Action act = () => p.WithLocationDetails(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDictionary_AfterWithLocationDetails_EmitsAll8DecomposedKeys()
    {
        var p = BuildBase().WithLocationDetails(BuildProjection());

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationName].Should().Be("Aurora Clubhouse");
        dict[EmailTemplateContract.Event.LocationAddress].Should().Be("4314 Clark Ave, Cleveland, Ohio, 44120, USA");
        dict[EmailTemplateContract.Event.HasLocationName].Should().Be(true);
        dict[EmailTemplateContract.Event.HasSecondaryLocation].Should().Be(true);
        dict[EmailTemplateContract.Event.SecondaryLocationLabel].Should().Be("Parking Lot");
        dict[EmailTemplateContract.Event.SecondaryLocationName].Should().Be("Geoga Lake Parking");
        dict[EmailTemplateContract.Event.HasSecondaryLocationName].Should().Be(true);
        dict[EmailTemplateContract.Event.SecondaryLocationAddress].Should().Be("943 Penny Lane, Aurora, OH, 44202, USA");
    }

    [Fact]
    public void ToDictionary_AfterWithLocationDetails_EventLocationMatchesLegacyFlatString()
    {
        var p = BuildBase().WithLocationDetails(BuildProjection());

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.EventLocation].Should().Be("4314 Clark Ave, Cleveland");
    }

    [Fact]
    public void ToDictionary_WithoutWithLocationDetails_ProjectsScalarIntoLocationAddress()
    {
        // Phase 7C.2b Chunk 2c (2026-04-23): un-refactored callers must no
        // longer ship an empty LocationAddress to the decomposed template —
        // silent data loss regressed the paid-ticket confirmation email in
        // the 2026-04-23 inbox smoke. New contract: the scalar EventLocation
        // is projected into LocationAddress so the Venue-less flat string is
        // still rendered by the decomposed template.
        var p = BuildBase();

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationName].Should().Be(string.Empty);
        dict[EmailTemplateContract.Event.LocationAddress].Should().Be("legacy fallback string",
            "un-refactored callers must still render the scalar as a single line instead of nothing");
        dict[EmailTemplateContract.Event.HasLocationName].Should().Be(false);
        dict[EmailTemplateContract.Event.HasSecondaryLocation].Should().Be(false);
        dict[EmailTemplateContract.Event.SecondaryLocationLabel].Should().Be(string.Empty);
        dict[EmailTemplateContract.Event.SecondaryLocationName].Should().Be(string.Empty);
        dict[EmailTemplateContract.Event.HasSecondaryLocationName].Should().Be(false);
        dict[EmailTemplateContract.Event.SecondaryLocationAddress].Should().Be(string.Empty);
        dict[EmailTemplateContract.Event.EventLocation].Should().Be("legacy fallback string");
    }

    [Fact]
    public void WithLocationDetails_ReturnsSameInstance_ForFluentChaining()
    {
        var p = BuildBase();

        var result = p.WithLocationDetails(BuildProjection());

        result.Should().BeSameAs(p);
    }

    [Fact]
    public void WithLocationDetails_WorksAcrossTemplateVariants()
    {
        // The same projection payload must flow through Confirmation / Update /
        // Cancellation / VolunteerConfirmation / VolunteerCancellation without
        // being reset when AsXxx() toggles the template name.
        var proj = BuildProjection();

        var confirmation = BuildBase().AsConfirmation().WithLocationDetails(proj);
        var update = BuildBase().AsUpdate().WithLocationDetails(proj);
        var cancellation = BuildBase().AsCancellation().WithLocationDetails(proj);
        var volConfirm = BuildBase().AsVolunteerConfirmation().WithLocationDetails(proj);
        var volCancel = BuildBase().AsVolunteerCancellation().WithLocationDetails(proj);

        foreach (var p in new[] { confirmation, update, cancellation, volConfirm, volCancel })
        {
            var dict = p.ToDictionary();
            dict[EmailTemplateContract.Event.LocationName].Should().Be("Aurora Clubhouse");
            dict[EmailTemplateContract.Event.HasSecondaryLocation].Should().Be(true);
            dict[EmailTemplateContract.Event.EventLocation].Should().Be("4314 Clark Ave, Cleveland");
        }
    }
}
