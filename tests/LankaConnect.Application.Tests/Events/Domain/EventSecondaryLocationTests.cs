using FluentAssertions;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for EventSecondaryLocation value object — Phase 7C.1
/// Wraps EventLocation + SecondaryLocationType (ParkingLot / SecondaryVenue)
/// </summary>
public class EventSecondaryLocationTests
{
    private static EventLocation BuildLocation(string name = "Lot A")
    {
        var address = Address.Create("111 Side St", "Los Angeles", "CA", "90001", "USA").Value;
        return EventLocation.Create(address, name: name).Value;
    }

    [Fact]
    public void Create_WithParkingLotType_Succeeds()
    {
        // Arrange
        var location = BuildLocation("North Lot");

        // Act
        var result = EventSecondaryLocation.Create(SecondaryLocationType.ParkingLot, location);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(SecondaryLocationType.ParkingLot);
        result.Value.Location.Should().Be(location);
    }

    [Fact]
    public void Create_WithSecondaryVenueType_Succeeds()
    {
        // Arrange
        var location = BuildLocation("Annex Hall");

        // Act
        var result = EventSecondaryLocation.Create(SecondaryLocationType.SecondaryVenue, location);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(SecondaryLocationType.SecondaryVenue);
        result.Value.Location.Should().Be(location);
    }

    [Fact]
    public void Create_WithNullLocation_Fails()
    {
        // Act
        var result = EventSecondaryLocation.Create(SecondaryLocationType.ParkingLot, null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Location"));
    }

    [Fact]
    public void Create_WithUndefinedEnumValue_Fails()
    {
        // Arrange
        var location = BuildLocation();
        var invalidType = (SecondaryLocationType)999;

        // Act
        var result = EventSecondaryLocation.Create(invalidType, location);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("type", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Equality_SameTypeAndLocation_AreEqual()
    {
        // Arrange
        var loc = BuildLocation();
        var a = EventSecondaryLocation.Create(SecondaryLocationType.ParkingLot, loc).Value;
        var b = EventSecondaryLocation.Create(SecondaryLocationType.ParkingLot, loc).Value;

        // Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentType_NotEqual()
    {
        // Arrange
        var loc = BuildLocation();
        var a = EventSecondaryLocation.Create(SecondaryLocationType.ParkingLot, loc).Value;
        var b = EventSecondaryLocation.Create(SecondaryLocationType.SecondaryVenue, loc).Value;

        // Assert
        a.Should().NotBe(b);
    }
}
