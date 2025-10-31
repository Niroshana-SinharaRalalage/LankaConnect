using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Domain;

/// <summary>
/// TDD RED Phase - Tests for User.UpdateLocation() method
/// </summary>
public class UserUpdateLocationTests
{
    private User CreateValidUser()
    {
        var email = Email.Create("test@example.com").Value;
        return User.Create(email, "John", "Doe").Value;
    }

    [Fact]
    public void UpdateLocation_WithValidLocation_ShouldSucceed()
    {
        // Arrange
        var user = CreateValidUser();
        var location = UserLocation.Create("Los Angeles", "California", "90001", "United States").Value;

        // Act
        var result = user.UpdateLocation(location);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Location.Should().Be(location);
        user.Location!.City.Should().Be("Los Angeles");
        user.Location.State.Should().Be("California");
        user.Location.ZipCode.Should().Be("90001");
        user.Location.Country.Should().Be("United States");
    }

    [Fact]
    public void UpdateLocation_WithValidLocation_ShouldRaiseDomainEvent()
    {
        // Arrange
        var user = CreateValidUser();
        var location = UserLocation.Create("Los Angeles", "California", "90001", "United States").Value;

        // Act
        var result = user.UpdateLocation(location);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvents = user.GetDomainEvents();
        domainEvents.Should().ContainSingle(e => e is UserLocationUpdatedEvent);

        var locationEvent = domainEvents.OfType<UserLocationUpdatedEvent>().First();
        locationEvent.UserId.Should().Be(user.Id);
        locationEvent.Email.Should().Be("test@example.com");
        locationEvent.City.Should().Be("Los Angeles");
        locationEvent.State.Should().Be("California");
        locationEvent.Country.Should().Be("United States");
    }

    [Fact]
    public void UpdateLocation_WithNullLocation_ShouldSucceed()
    {
        // Arrange
        var user = CreateValidUser();
        var location = UserLocation.Create("Toronto", "Ontario", "M5H 2N2", "Canada").Value;
        user.UpdateLocation(location);

        // Act - User can clear location (privacy choice)
        var result = user.UpdateLocation(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Location.Should().BeNull();
    }

    [Fact]
    public void UpdateLocation_WithNullLocation_ShouldNotRaiseDomainEvent()
    {
        // Arrange
        var user = CreateValidUser();

        // Clear any existing domain events
        user.ClearDomainEvents();

        // Act - Clearing location (privacy choice)
        var result = user.UpdateLocation(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvents = user.GetDomainEvents();
        domainEvents.Should().NotContain(e => e is UserLocationUpdatedEvent);
    }

    [Fact]
    public void UpdateLocation_ShouldUpdateExistingLocation()
    {
        // Arrange
        var user = CreateValidUser();
        var originalLocation = UserLocation.Create("Los Angeles", "California", "90001", "United States").Value;
        user.UpdateLocation(originalLocation);

        // Clear domain events from first update
        user.ClearDomainEvents();

        var newLocation = UserLocation.Create("San Francisco", "California", "94102", "United States").Value;

        // Act
        var result = user.UpdateLocation(newLocation);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Location.Should().Be(newLocation);
        user.Location!.City.Should().Be("San Francisco");
        user.Location.ZipCode.Should().Be("94102");
    }

    [Fact]
    public void UpdateLocation_ShouldMarkEntityAsUpdated()
    {
        // Arrange
        var user = CreateValidUser();
        var originalUpdatedAt = user.UpdatedAt ?? DateTime.UtcNow.AddHours(-1);

        // Small delay to ensure UpdatedAt changes
        System.Threading.Thread.Sleep(10);

        var location = UserLocation.Create("Toronto", "Ontario", "M5H 2N2", "Canada").Value;

        // Act
        user.UpdateLocation(location);

        // Assert
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt!.Value.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateLocation_WithInternationalLocation_ShouldSucceed()
    {
        // Arrange
        var user = CreateValidUser();
        var location = UserLocation.Create("London", "England", "SW1A 1AA", "United Kingdom").Value;

        // Act
        var result = user.UpdateLocation(location);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Location.Should().NotBeNull();
        user.Location!.City.Should().Be("London");
        user.Location.Country.Should().Be("United Kingdom");
    }

    [Fact]
    public void UpdateLocation_MultipleUpdates_ShouldRaiseMultipleEvents()
    {
        // Arrange
        var user = CreateValidUser();
        var location1 = UserLocation.Create("Los Angeles", "California", "90001", "United States").Value;
        var location2 = UserLocation.Create("Toronto", "Ontario", "M5H 2N2", "Canada").Value;

        // Act
        user.UpdateLocation(location1);
        user.UpdateLocation(location2);

        // Assert
        var domainEvents = user.GetDomainEvents();
        var locationEvents = domainEvents.OfType<UserLocationUpdatedEvent>().ToList();
        locationEvents.Should().HaveCount(2);

        locationEvents[0].City.Should().Be("Los Angeles");
        locationEvents[0].Country.Should().Be("United States");

        locationEvents[1].City.Should().Be("Toronto");
        locationEvents[1].Country.Should().Be("Canada");
    }

    [Fact]
    public void NewUser_ShouldHaveNullLocation()
    {
        // Arrange & Act
        var user = CreateValidUser();

        // Assert
        user.Location.Should().BeNull();
    }
}
