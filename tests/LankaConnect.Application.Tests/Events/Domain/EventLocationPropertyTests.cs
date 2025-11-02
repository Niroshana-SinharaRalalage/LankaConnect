using FluentAssertions;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for Event.Location property and SetLocation method (Epic 2 Phase 1)
/// Tests adding location support to Event aggregate
/// </summary>
public class EventLocationPropertyTests
{
    private Event CreateValidEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var organizerId = Guid.NewGuid();

        return Event.Create(title, description, startDate, endDate, organizerId, 100).Value;
    }

    private EventLocation CreateValidLocation()
    {
        var address = Address.Create("123 Main St", "Los Angeles", "CA", "90001", "USA").Value;
        var coordinates = GeoCoordinate.Create(34.0522m, -118.2437m).Value;
        return EventLocation.Create(address, coordinates).Value;
    }

    #region SetLocation Tests

    [Fact]
    public void SetLocation_WithValidLocation_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var location = CreateValidLocation();

        // Act
        var result = @event.SetLocation(location);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Location.Should().Be(location);
        @event.HasLocation().Should().BeTrue();
    }

    [Fact]
    public void SetLocation_WithNullLocation_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetLocation(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Location cannot be null");
    }

    [Fact]
    public void SetLocation_WhenLocationAlreadySet_ShouldUpdateLocation()
    {
        // Arrange
        var @event = CreateValidEvent();
        var location1 = CreateValidLocation();
        @event.SetLocation(location1);

        var address2 = Address.Create("456 Oak Ave", "San Francisco", "CA", "94102", "USA").Value;
        var coordinates2 = GeoCoordinate.Create(37.7749m, -122.4194m).Value;
        var location2 = EventLocation.Create(address2, coordinates2).Value;

        // Act
        var result = @event.SetLocation(location2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Location.Should().Be(location2);
        @event.Location.Should().NotBe(location1);
    }

    #endregion

    #region HasLocation Tests

    [Fact]
    public void HasLocation_WhenLocationSet_ShouldReturnTrue()
    {
        // Arrange
        var @event = CreateValidEvent();
        var location = CreateValidLocation();
        @event.SetLocation(location);

        // Act & Assert
        @event.HasLocation().Should().BeTrue();
    }

    [Fact]
    public void HasLocation_WhenLocationNotSet_ShouldReturnFalse()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act & Assert
        @event.HasLocation().Should().BeFalse();
    }

    #endregion

    #region Create with Location Tests

    [Fact]
    public void Create_WithLocation_ShouldSucceed()
    {
        // Arrange
        var title = EventTitle.Create("Cultural Festival").Value;
        var description = EventDescription.Create("Annual cultural celebration").Value;
        var startDate = DateTime.UtcNow.AddDays(30);
        var endDate = startDate.AddHours(8);
        var organizerId = Guid.NewGuid();
        var location = CreateValidLocation();

        // Act
        var result = Event.Create(title, description, startDate, endDate, organizerId, 500, location);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Location.Should().Be(location);
        result.Value.HasLocation().Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutLocation_ShouldSucceed()
    {
        // Arrange
        var title = EventTitle.Create("Virtual Event").Value;
        var description = EventDescription.Create("Online only event").Value;
        var startDate = DateTime.UtcNow.AddDays(15);
        var endDate = startDate.AddHours(2);
        var organizerId = Guid.NewGuid();

        // Act
        var result = Event.Create(title, description, startDate, endDate, organizerId, 200);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Location.Should().BeNull("location is optional for virtual events");
        result.Value.HasLocation().Should().BeFalse();
    }

    [Fact]
    public void Create_WithNullLocation_ShouldSucceed()
    {
        // Arrange
        var title = EventTitle.Create("TBD Location Event").Value;
        var description = EventDescription.Create("Location to be announced").Value;
        var startDate = DateTime.UtcNow.AddDays(60);
        var endDate = startDate.AddHours(4);
        var organizerId = Guid.NewGuid();

        // Act
        var result = Event.Create(title, description, startDate, endDate, organizerId, 150, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Location.Should().BeNull();
        result.Value.HasLocation().Should().BeFalse();
    }

    #endregion

    #region RemoveLocation Tests

    [Fact]
    public void RemoveLocation_WhenLocationExists_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var location = CreateValidLocation();
        @event.SetLocation(location);

        // Act
        var result = @event.RemoveLocation();

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Location.Should().BeNull();
        @event.HasLocation().Should().BeFalse();
    }

    [Fact]
    public void RemoveLocation_WhenLocationDoesNotExist_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.RemoveLocation();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Event does not have a location set");
    }

    [Fact]
    public void RemoveLocation_ConvertingToVirtualEvent_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var location = CreateValidLocation();
        @event.SetLocation(location);
        @event.HasLocation().Should().BeTrue();

        // Act - Convert to virtual event by removing location
        var result = @event.RemoveLocation();

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Location.Should().BeNull();
        @event.HasLocation().Should().BeFalse();
    }

    #endregion

    #region Integration with Event Status Tests

    [Fact]
    public void SetLocation_OnDraftEvent_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var location = CreateValidLocation();

        // Act
        var result = @event.SetLocation(location);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Location.Should().Be(location);
    }

    [Fact]
    public void SetLocation_OnPublishedEvent_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.Publish();
        var location = CreateValidLocation();

        // Act
        var result = @event.SetLocation(location);

        // Assert
        result.IsSuccess.Should().BeTrue("organizers can update location even after publishing");
        @event.Location.Should().Be(location);
    }

    #endregion
}
