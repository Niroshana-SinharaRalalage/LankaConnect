using FluentAssertions;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for EventLocation value object (Epic 2 Phase 1 - Day 1)
/// Tests the composition of Address + GeoCoordinate for event locations
/// </summary>
public class EventLocationTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidAddress_ShouldSucceed()
    {
        // Arrange
        var address = Address.Create("123 Main St", "Los Angeles", "CA", "90001", "USA").Value;

        // Act
        var result = EventLocation.Create(address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be(address);
        result.Value.Coordinates.Should().BeNull("coordinates are optional until geocoded");
        result.Value.HasCoordinates().Should().BeFalse();
    }

    [Fact]
    public void Create_WithNullAddress_ShouldFail()
    {
        // Act
        var result = EventLocation.Create(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Address is required");
    }

    [Fact]
    public void Create_WithValidAddressAndCoordinates_ShouldSucceed()
    {
        // Arrange
        var address = Address.Create("456 Oak Ave", "San Francisco", "CA", "94102", "USA").Value;
        var coordinates = GeoCoordinate.Create(37.7749m, -122.4194m).Value;

        // Act
        var result = EventLocation.Create(address, coordinates);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be(address);
        result.Value.Coordinates.Should().Be(coordinates);
        result.Value.HasCoordinates().Should().BeTrue();
    }

    #endregion

    #region WithCoordinates Tests

    [Fact]
    public void WithCoordinates_WhenNoCoordinatesExist_ShouldAddCoordinates()
    {
        // Arrange
        var address = Address.Create("789 Pine St", "Seattle", "WA", "98101", "USA").Value;
        var location = EventLocation.Create(address).Value;
        var coordinates = GeoCoordinate.Create(47.6062m, -122.3321m).Value;

        // Act
        var result = location.WithCoordinates(coordinates);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Coordinates.Should().Be(coordinates);
        result.Value.Address.Should().Be(address, "address should remain unchanged");
        result.Value.HasCoordinates().Should().BeTrue();
    }

    [Fact]
    public void WithCoordinates_WhenCoordinatesAlreadyExist_ShouldUpdateCoordinates()
    {
        // Arrange
        var address = Address.Create("101 Elm St", "Portland", "OR", "97201", "USA").Value;
        var oldCoordinates = GeoCoordinate.Create(45.5231m, -122.6765m).Value;
        var location = EventLocation.Create(address, oldCoordinates).Value;

        var newCoordinates = GeoCoordinate.Create(45.5200m, -122.6800m).Value;

        // Act
        var result = location.WithCoordinates(newCoordinates);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Coordinates.Should().Be(newCoordinates);
        result.Value.Coordinates.Should().NotBe(oldCoordinates);
    }

    [Fact]
    public void WithCoordinates_WithNullCoordinates_ShouldFail()
    {
        // Arrange
        var address = Address.Create("202 Maple Dr", "Denver", "CO", "80201", "USA").Value;
        var location = EventLocation.Create(address).Value;

        // Act
        var result = location.WithCoordinates(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Coordinates cannot be null");
    }

    #endregion

    #region HasCoordinates Tests

    [Fact]
    public void HasCoordinates_WhenCoordinatesExist_ShouldReturnTrue()
    {
        // Arrange
        var address = Address.Create("303 Cedar Ln", "Austin", "TX", "78701", "USA").Value;
        var coordinates = GeoCoordinate.Create(30.2672m, -97.7431m).Value;
        var location = EventLocation.Create(address, coordinates).Value;

        // Act & Assert
        location.HasCoordinates().Should().BeTrue();
    }

    [Fact]
    public void HasCoordinates_WhenCoordinatesDoNotExist_ShouldReturnFalse()
    {
        // Arrange
        var address = Address.Create("404 Birch Rd", "Phoenix", "AZ", "85001", "USA").Value;
        var location = EventLocation.Create(address).Value;

        // Act & Assert
        location.HasCoordinates().Should().BeFalse();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_SameAddressAndCoordinates_ShouldBeEqual()
    {
        // Arrange
        var address = Address.Create("505 Walnut St", "Miami", "FL", "33101", "USA").Value;
        var coordinates = GeoCoordinate.Create(25.7617m, -80.1918m).Value;

        var location1 = EventLocation.Create(address, coordinates).Value;
        var location2 = EventLocation.Create(address, coordinates).Value;

        // Act & Assert
        location1.Should().Be(location2);
        (location1 == location2).Should().BeTrue();
        location1.GetHashCode().Should().Be(location2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentAddresses_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = Address.Create("606 Cherry Ave", "Boston", "MA", "02101", "USA").Value;
        var address2 = Address.Create("707 Peach St", "Boston", "MA", "02101", "USA").Value;
        var coordinates = GeoCoordinate.Create(42.3601m, -71.0589m).Value;

        var location1 = EventLocation.Create(address1, coordinates).Value;
        var location2 = EventLocation.Create(address2, coordinates).Value;

        // Act & Assert
        location1.Should().NotBe(location2);
        (location1 != location2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCoordinates_ShouldNotBeEqual()
    {
        // Arrange
        var address = Address.Create("808 Plum Ct", "Chicago", "IL", "60601", "USA").Value;
        var coordinates1 = GeoCoordinate.Create(41.8781m, -87.6298m).Value;
        var coordinates2 = GeoCoordinate.Create(41.8800m, -87.6300m).Value;

        var location1 = EventLocation.Create(address, coordinates1).Value;
        var location2 = EventLocation.Create(address, coordinates2).Value;

        // Act & Assert
        location1.Should().NotBe(location2);
    }

    [Fact]
    public void Equality_OneWithCoordinatesOneWithout_ShouldNotBeEqual()
    {
        // Arrange
        var address = Address.Create("909 Apple Blvd", "New York", "NY", "10001", "USA").Value;
        var coordinates = GeoCoordinate.Create(40.7128m, -74.0060m).Value;

        var location1 = EventLocation.Create(address).Value;
        var location2 = EventLocation.Create(address, coordinates).Value;

        // Act & Assert
        location1.Should().NotBe(location2);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WithCoordinates_ShouldIncludeAddressAndCoordinates()
    {
        // Arrange
        var address = Address.Create("1010 Grape Way", "Las Vegas", "NV", "89101", "USA").Value;
        var coordinates = GeoCoordinate.Create(36.1699m, -115.1398m).Value;
        var location = EventLocation.Create(address, coordinates).Value;

        // Act
        var result = location.ToString();

        // Assert
        result.Should().Contain("1010 Grape Way");
        result.Should().Contain("Las Vegas");
        result.Should().Contain("36.1699");
        result.Should().Contain("-115.1398");
    }

    [Fact]
    public void ToString_WithoutCoordinates_ShouldShowAddressOnly()
    {
        // Arrange
        var address = Address.Create("1111 Orange Dr", "Atlanta", "GA", "30301", "USA").Value;
        var location = EventLocation.Create(address).Value;

        // Act
        var result = location.ToString();

        // Assert
        result.Should().Contain("1111 Orange Dr");
        result.Should().Contain("Atlanta");
        result.Should().Contain("(coordinates not set)");
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void EventLocation_ShouldBeImmutable()
    {
        // Arrange
        var address = Address.Create("1212 Lemon Ln", "Dallas", "TX", "75201", "USA").Value;
        var coordinates1 = GeoCoordinate.Create(32.7767m, -96.7970m).Value;
        var location = EventLocation.Create(address, coordinates1).Value;

        // Act - Create new location with different coordinates
        var coordinates2 = GeoCoordinate.Create(32.7800m, -96.8000m).Value;
        var updatedLocation = location.WithCoordinates(coordinates2).Value;

        // Assert - Original should remain unchanged
        location.Coordinates.Should().Be(coordinates1, "original location should not be mutated");
        updatedLocation.Coordinates.Should().Be(coordinates2, "new location should have new coordinates");
    }

    #endregion
}
