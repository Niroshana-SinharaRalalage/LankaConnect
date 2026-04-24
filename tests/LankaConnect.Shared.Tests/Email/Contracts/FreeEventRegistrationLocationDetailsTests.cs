using FluentAssertions;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 7C.2: Tests for <see cref="FreeEventRegistrationEmailParams.WithLocationDetails"/>
/// and the 8 decomposed location keys it emits via <c>ToDictionary()</c>.
///
/// The free-event registration template is the pilot for this phase. Once this class
/// passes, the same pattern is fanned out to the other 12 event-email params classes.
/// </summary>
public class FreeEventRegistrationLocationDetailsTests
{
    private static FreeEventRegistrationEmailParams BuildBase() =>
        FreeEventRegistrationEmailParams.Create(
            eventId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            eventTitle: "Community Meetup",
            eventStartDate: new DateTime(2026, 2, 15, 22, 0, 0, DateTimeKind.Utc),
            eventStartTime: "5:00 PM",
            eventLocation: "legacy fallback string",
            eventDetailsUrl: "https://lankaconnect.com/events/abc",
            registrationDate: new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));

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
        // longer ship an empty LocationAddress to the decomposed template.
        // Previously this test asserted LocationAddress=="" — which was the
        // silent data-loss bug that caused the paid-ticket confirmation email
        // to render LOCATION-header-with-no-value. The new contract projects
        // the scalar EventLocation into LocationAddress (matching the
        // Location?.Address == null branch of ProjectEmailLocation).
        var p = BuildBase();

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.LocationName].Should().Be(string.Empty);
        dict[EmailTemplateContract.Event.LocationAddress].Should().Be("legacy fallback string",
            "un-refactored callers must render the scalar as a single line instead of nothing");
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
}
