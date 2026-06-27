using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for Event MaxAttendeesPerRegistration feature
/// Issue #51: Allow event organizers to configure max attendees per registration
/// TDD: Written before implementation
/// </summary>
public class EventMaxAttendeesPerRegistrationTests
{
    #region Test Helpers

    private static Event CreateValidEvent(int capacity = 100)
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(8);
        var organizerId = Guid.NewGuid();

        var result = Event.Create(title, description, startDate, endDate, organizerId, capacity);
        return result.Value;
    }

    #endregion

    #region Default Value Tests

    [Fact]
    public void Event_WhenCreated_ShouldHaveDefaultMaxAttendeesPerRegistration()
    {
        // Arrange & Act
        var @event = CreateValidEvent();

        // Assert
        @event.MaxAttendeesPerRegistration.Should().Be(10,
            "default max attendees per registration should be 10 for backward compatibility");
    }

    #endregion

    #region UpdateMaxAttendeesPerRegistration - Success Cases

    [Fact]
    public void UpdateMaxAttendeesPerRegistration_WithValidValue_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent(capacity: 50);

        // Act
        var result = @event.UpdateMaxAttendeesPerRegistration(25);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.MaxAttendeesPerRegistration.Should().Be(25);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    public void UpdateMaxAttendeesPerRegistration_WithVariousValidValues_ShouldSucceed(int maxAttendees)
    {
        // Arrange
        var @event = CreateValidEvent(capacity: 100);

        // Act
        var result = @event.UpdateMaxAttendeesPerRegistration(maxAttendees);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.MaxAttendeesPerRegistration.Should().Be(maxAttendees);
    }

    [Fact]
    public void UpdateMaxAttendeesPerRegistration_ToEventCapacity_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent(capacity: 30);

        // Act - Set max attendees equal to event capacity (entire event in one registration)
        var result = @event.UpdateMaxAttendeesPerRegistration(30);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.MaxAttendeesPerRegistration.Should().Be(30);
    }

    #endregion

    #region UpdateMaxAttendeesPerRegistration - Validation Failure Cases

    [Fact]
    public void UpdateMaxAttendeesPerRegistration_WithZero_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.UpdateMaxAttendeesPerRegistration(0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("at least 1");
    }

    [Fact]
    public void UpdateMaxAttendeesPerRegistration_WithNegativeValue_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.UpdateMaxAttendeesPerRegistration(-5);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("at least 1");
    }

    [Fact]
    public void UpdateMaxAttendeesPerRegistration_ExceedingEventCapacity_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent(capacity: 20);

        // Act - Try to set max attendees higher than event capacity
        var result = @event.UpdateMaxAttendeesPerRegistration(25);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cannot exceed event capacity");
    }

    [Fact]
    public void UpdateMaxAttendeesPerRegistration_ExceedingSystemMaximum_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent(capacity: 1000);

        // Act - Try to set max attendees higher than system maximum (50)
        var result = @event.UpdateMaxAttendeesPerRegistration(100);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cannot exceed 50");
    }

    #endregion

    #region Integration with Event Capacity

    [Fact]
    public void UpdateMaxAttendeesPerRegistration_AfterCapacityReduced_ShouldAutoAdjust()
    {
        // Arrange
        var @event = CreateValidEvent(capacity: 50);
        @event.UpdateMaxAttendeesPerRegistration(40);

        // Act - Reduce event capacity below current max attendees
        var result = @event.UpdateCapacity(30);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Capacity.Should().Be(30);
        // Max attendees should auto-adjust to not exceed new capacity
        @event.MaxAttendeesPerRegistration.Should().BeLessThanOrEqualTo(30);
    }

    #endregion

    #region GetEffectiveMaxAttendeesPerRegistration Tests

    [Fact]
    public void GetEffectiveMaxAttendeesPerRegistration_ShouldReturnMinOfConfiguredAndSpotsLeft()
    {
        // Arrange
        var @event = CreateValidEvent(capacity: 50);
        @event.UpdateMaxAttendeesPerRegistration(25);
        // Simulate some registrations (spots left = capacity - current)
        // For this test, we just verify the method returns the configured value when no registrations

        // Act
        var effective = @event.GetEffectiveMaxAttendeesPerRegistration();

        // Assert
        effective.Should().Be(25, "should return configured value when plenty of spots available");
    }

    #endregion
}
