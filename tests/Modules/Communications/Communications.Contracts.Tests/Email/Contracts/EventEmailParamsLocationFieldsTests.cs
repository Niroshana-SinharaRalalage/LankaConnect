using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
namespace LankaConnect.Modules.Communications.Contracts.Tests.Email.Contracts;

/// <summary>
/// Phase 7C.2: Tests for the eight decomposed location fields added to
/// <see cref="EventEmailParams"/> so every event-related email can render
/// "{{LocationName}}" / "{{LocationAddress}}" plus an optional secondary
/// location block without resorting to the single-line {{EventLocation}}
/// blob.
///
/// Fields under test:
///   LocationName, LocationAddress, HasLocationName,
///   HasSecondaryLocation, SecondaryLocationLabel,
///   SecondaryLocationName, HasSecondaryLocationName, SecondaryLocationAddress
///
/// The legacy {{EventLocation}} key MUST still be emitted for backward
/// compatibility with any un-migrated templates.
/// </summary>
public class EventEmailParamsLocationFieldsTests
{
    private static EventEmailParams Build() => new()
    {
        EventId = Guid.NewGuid(),
        EventTitle = "Community Meetup",
        EventLocation = "4314 Clark Ave, Cleveland",
        EventStartDate = new DateTime(2026, 2, 15, 17, 0, 0, DateTimeKind.Utc),
        EventStartTime = "5:00 PM",
        EventDetailsUrl = "https://lankaconnect.com/events/abc123"
    };

    [Fact]
    public void Defaults_AllNewLocationFields_AreEmptyOrFalse()
    {
        var p = Build();

        p.LocationName.Should().BeEmpty();
        p.LocationAddress.Should().BeEmpty();
        p.HasLocationName.Should().BeFalse();
        p.HasSecondaryLocation.Should().BeFalse();
        p.SecondaryLocationLabel.Should().BeEmpty();
        p.SecondaryLocationName.Should().BeEmpty();
        p.HasSecondaryLocationName.Should().BeFalse();
        p.SecondaryLocationAddress.Should().BeEmpty();
    }

    [Fact]
    public void ToDictionary_EmitsAllEightDecomposedLocationKeys()
    {
        var p = Build();
        p.LocationName = "Aurora Clubhouse";
        p.LocationAddress = "4314 Clark Ave, Cleveland, Ohio, 44120, USA";
        p.HasLocationName = true;
        p.HasSecondaryLocation = true;
        p.SecondaryLocationLabel = "Parking Lot";
        p.SecondaryLocationName = "Geoga Lake Parking";
        p.HasSecondaryLocationName = true;
        p.SecondaryLocationAddress = "943 Penny Lane, Aurora, OH, 44202, USA";

        var dict = p.ToDictionary();

        dict.Should().ContainKey(EmailTemplateContract.Event.LocationName);
        dict.Should().ContainKey(EmailTemplateContract.Event.LocationAddress);
        dict.Should().ContainKey(EmailTemplateContract.Event.HasLocationName);
        dict.Should().ContainKey(EmailTemplateContract.Event.HasSecondaryLocation);
        dict.Should().ContainKey(EmailTemplateContract.Event.SecondaryLocationLabel);
        dict.Should().ContainKey(EmailTemplateContract.Event.SecondaryLocationName);
        dict.Should().ContainKey(EmailTemplateContract.Event.HasSecondaryLocationName);
        dict.Should().ContainKey(EmailTemplateContract.Event.SecondaryLocationAddress);

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
    public void ToDictionary_StillEmitsLegacyEventLocationKey_ForUnMigratedTemplates()
    {
        var p = Build();
        p.EventLocation = "4314 Clark Ave, Cleveland";

        var dict = p.ToDictionary();

        dict.Should().ContainKey(EmailTemplateContract.Event.EventLocation);
        dict[EmailTemplateContract.Event.EventLocation].Should().Be("4314 Clark Ave, Cleveland");
    }

    [Fact]
    public void ToDictionary_WhenNoSecondary_EmitsFalseFlagsAndEmptyStrings()
    {
        var p = Build();
        p.LocationName = "Aurora Clubhouse";
        p.LocationAddress = "4314 Clark Ave, Cleveland, Ohio, 44120, USA";
        p.HasLocationName = true;

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.Event.HasSecondaryLocation].Should().Be(false);
        dict[EmailTemplateContract.Event.SecondaryLocationLabel].Should().Be(string.Empty);
        dict[EmailTemplateContract.Event.SecondaryLocationName].Should().Be(string.Empty);
        dict[EmailTemplateContract.Event.HasSecondaryLocationName].Should().Be(false);
        dict[EmailTemplateContract.Event.SecondaryLocationAddress].Should().Be(string.Empty);
    }

    [Fact]
    public void Contract_ExposesAllEightLocationConstants()
    {
        EmailTemplateContract.Event.LocationName.Should().Be("LocationName");
        EmailTemplateContract.Event.LocationAddress.Should().Be("LocationAddress");
        EmailTemplateContract.Event.HasLocationName.Should().Be("HasLocationName");
        EmailTemplateContract.Event.HasSecondaryLocation.Should().Be("HasSecondaryLocation");
        EmailTemplateContract.Event.SecondaryLocationLabel.Should().Be("SecondaryLocationLabel");
        EmailTemplateContract.Event.SecondaryLocationName.Should().Be("SecondaryLocationName");
        EmailTemplateContract.Event.HasSecondaryLocationName.Should().Be("HasSecondaryLocationName");
        EmailTemplateContract.Event.SecondaryLocationAddress.Should().Be("SecondaryLocationAddress");
    }
}
