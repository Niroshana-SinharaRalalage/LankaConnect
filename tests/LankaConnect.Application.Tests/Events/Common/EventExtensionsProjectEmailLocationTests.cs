using FluentAssertions;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Common;

/// <summary>
/// Phase 7C.2: Tests for EventExtensions.ProjectEmailLocation, the single source of truth
/// that projects an Event's primary + optional secondary location into an email-ready
/// record. Replaces 13 duplicated GetEventLocationString() helpers across handlers/jobs.
///
/// Matrix covered:
///   HasPrimary    × HasName × HasSecondary × SecondaryType
///   online (null) ×   -     ×      -        ×     -
///   physical      ×  no     ×     no        ×     -
///   physical      ×  yes    ×     no        ×     -
///   physical      ×  yes    ×     yes       × ParkingLot     (named or anonymous)
///   physical      ×  yes    ×     yes       × SecondaryVenue (named or anonymous)
///
/// The label text requirement (user-confirmed 2026-04-20):
///   SecondaryLocationType.ParkingLot      -> "Parking Lot"
///   SecondaryLocationType.SecondaryVenue  -> "Secondary Venue"
/// No trailing colon, no "Address:" suffix.
///
/// The address-format requirement (user-confirmed 2026-04-20):
///   "{Street}, {City}, {State}, {ZipCode}, {Country}" — all five parts, all commas.
/// </summary>
public class EventExtensionsProjectEmailLocationTests
{
    private static Event CreateValidEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var organizerId = Guid.NewGuid();
        return Event.Create(title, description, startDate, endDate, organizerId, 100).Value;
    }

    private static EventLocation PrimaryLocation(string? name = null)
    {
        var address = Address.Create("4314 Clark Ave", "Cleveland", "Ohio", "44120", "USA").Value;
        return EventLocation.Create(address, name: name).Value;
    }

    private static EventSecondaryLocation SecondaryLocation(
        SecondaryLocationType type,
        string? name = null)
    {
        var address = Address.Create("943 Penny Lane", "Aurora", "OH", "44202", "USA").Value;
        var inner = EventLocation.Create(address, name: name).Value;
        return EventSecondaryLocation.Create(type, inner).Value;
    }

    [Fact]
    public void ProjectEmailLocation_NoLocation_ReturnsOnlineEventProjection()
    {
        var @event = CreateValidEvent();

        var projected = @event.ProjectEmailLocation();

        projected.LocationName.Should().BeEmpty();
        projected.LocationAddress.Should().BeEmpty();
        projected.HasLocationName.Should().BeFalse();
        projected.HasSecondaryLocation.Should().BeFalse();
        projected.SecondaryLocationLabel.Should().BeEmpty();
        projected.SecondaryLocationName.Should().BeEmpty();
        projected.HasSecondaryLocationName.Should().BeFalse();
        projected.SecondaryLocationAddress.Should().BeEmpty();
        projected.LegacyFlatString.Should().Be("Online Event");
    }

    [Fact]
    public void ProjectEmailLocation_PhysicalLocation_NoName_ReturnsAddressOnly()
    {
        var @event = CreateValidEvent();
        @event.SetLocation(PrimaryLocation(name: null));

        var projected = @event.ProjectEmailLocation();

        projected.LocationName.Should().BeEmpty();
        projected.LocationAddress.Should().Be("4314 Clark Ave, Cleveland, Ohio, 44120, USA");
        projected.HasLocationName.Should().BeFalse();
        projected.HasSecondaryLocation.Should().BeFalse();
        projected.LegacyFlatString.Should().Be("4314 Clark Ave, Cleveland");
    }

    [Fact]
    public void ProjectEmailLocation_PhysicalLocation_WithName_ReturnsNameAndAddress()
    {
        var @event = CreateValidEvent();
        @event.SetLocation(PrimaryLocation(name: "Aurora Clubhouse"));

        var projected = @event.ProjectEmailLocation();

        projected.LocationName.Should().Be("Aurora Clubhouse");
        projected.LocationAddress.Should().Be("4314 Clark Ave, Cleveland, Ohio, 44120, USA");
        projected.HasLocationName.Should().BeTrue();
        projected.HasSecondaryLocation.Should().BeFalse();
        projected.LegacyFlatString.Should().Be("4314 Clark Ave, Cleveland");
    }

    [Fact]
    public void ProjectEmailLocation_SecondaryParkingLot_Named_EmitsParkingLotLabel()
    {
        var @event = CreateValidEvent();
        @event.SetLocation(PrimaryLocation(name: "Aurora Clubhouse"));
        @event.SetSecondaryLocation(SecondaryLocation(SecondaryLocationType.ParkingLot, name: "Geoga Lake Parking"));

        var projected = @event.ProjectEmailLocation();

        projected.HasSecondaryLocation.Should().BeTrue();
        projected.SecondaryLocationLabel.Should().Be("Parking Lot");
        projected.SecondaryLocationName.Should().Be("Geoga Lake Parking");
        projected.HasSecondaryLocationName.Should().BeTrue();
        projected.SecondaryLocationAddress.Should().Be("943 Penny Lane, Aurora, OH, 44202, USA");
    }

    [Fact]
    public void ProjectEmailLocation_SecondaryParkingLot_Unnamed_EmitsLabelAndEmptyName()
    {
        var @event = CreateValidEvent();
        @event.SetLocation(PrimaryLocation(name: "Aurora Clubhouse"));
        @event.SetSecondaryLocation(SecondaryLocation(SecondaryLocationType.ParkingLot, name: null));

        var projected = @event.ProjectEmailLocation();

        projected.HasSecondaryLocation.Should().BeTrue();
        projected.SecondaryLocationLabel.Should().Be("Parking Lot");
        projected.SecondaryLocationName.Should().BeEmpty();
        projected.HasSecondaryLocationName.Should().BeFalse();
        projected.SecondaryLocationAddress.Should().Be("943 Penny Lane, Aurora, OH, 44202, USA");
    }

    [Fact]
    public void ProjectEmailLocation_SecondaryVenue_Named_EmitsSecondaryVenueLabel()
    {
        var @event = CreateValidEvent();
        @event.SetLocation(PrimaryLocation(name: "Aurora Clubhouse"));
        @event.SetSecondaryLocation(SecondaryLocation(SecondaryLocationType.SecondaryVenue, name: "The Annex"));

        var projected = @event.ProjectEmailLocation();

        projected.HasSecondaryLocation.Should().BeTrue();
        projected.SecondaryLocationLabel.Should().Be("Secondary Venue");
        projected.SecondaryLocationName.Should().Be("The Annex");
        projected.HasSecondaryLocationName.Should().BeTrue();
        projected.SecondaryLocationAddress.Should().Be("943 Penny Lane, Aurora, OH, 44202, USA");
    }

    [Fact]
    public void ProjectEmailLocation_SecondaryVenue_Unnamed_EmitsLabelAndEmptyName()
    {
        var @event = CreateValidEvent();
        @event.SetLocation(PrimaryLocation(name: "Aurora Clubhouse"));
        @event.SetSecondaryLocation(SecondaryLocation(SecondaryLocationType.SecondaryVenue, name: null));

        var projected = @event.ProjectEmailLocation();

        projected.HasSecondaryLocation.Should().BeTrue();
        projected.SecondaryLocationLabel.Should().Be("Secondary Venue");
        projected.SecondaryLocationName.Should().BeEmpty();
        projected.HasSecondaryLocationName.Should().BeFalse();
    }

    [Fact]
    public void ProjectEmailLocation_LegacyFlatString_MatchesExistingGetLocationDisplayString()
    {
        // Guarantees templates that still reference {{EventLocation}} keep rendering
        // byte-for-byte identical to the old behavior.
        var @event = CreateValidEvent();
        @event.SetLocation(PrimaryLocation(name: "Aurora Clubhouse"));

        var projected = @event.ProjectEmailLocation();

        projected.LegacyFlatString.Should().Be(@event.GetLocationDisplayString());
    }

    [Fact]
    public void ProjectEmailLocation_PhysicalLocation_NoSecondary_LabelIsEmpty()
    {
        var @event = CreateValidEvent();
        @event.SetLocation(PrimaryLocation(name: "Aurora Clubhouse"));

        var projected = @event.ProjectEmailLocation();

        projected.SecondaryLocationLabel.Should().BeEmpty();
        projected.SecondaryLocationName.Should().BeEmpty();
        projected.SecondaryLocationAddress.Should().BeEmpty();
        projected.HasSecondaryLocationName.Should().BeFalse();
    }
}
