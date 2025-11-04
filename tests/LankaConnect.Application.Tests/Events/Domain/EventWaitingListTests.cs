using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// TDD RED Phase: Waiting List Feature Tests
/// These tests will fail initially - we'll implement the feature to make them pass
/// </summary>
public class EventWaitingListTests
{
    private Event CreateEventWithCapacity(int capacity, int currentRegistrations = 0)
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var organizerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(8);

        var eventResult = Event.Create(title, description, startDate, endDate, organizerId, capacity);
        var @event = eventResult.Value;

        // Publish the event so we can register users
        @event.Publish();

        // Add registrations to fill capacity
        for (int i = 0; i < currentRegistrations; i++)
        {
            @event.Register(Guid.NewGuid(), 1);
        }

        return @event;
    }

    [Fact]
    public void AddToWaitingList_WhenEventAtCapacity_ShouldSucceed()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);
        var userId = Guid.NewGuid();

        // Act
        var result = @event.AddToWaitingList(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.WaitingList.Should().HaveCount(1);
        @event.WaitingList.First().UserId.Should().Be(userId);
        @event.WaitingList.First().Position.Should().Be(1);
    }

    [Fact]
    public void AddToWaitingList_WhenEventHasCapacity_ShouldFail()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 10, currentRegistrations: 5);
        var userId = Guid.NewGuid();

        // Act
        var result = @event.AddToWaitingList(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Event still has available capacity");
    }

    [Fact]
    public void AddToWaitingList_WhenUserAlreadyOnWaitingList_ShouldFail()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);
        var userId = Guid.NewGuid();
        @event.AddToWaitingList(userId);

        // Act
        var result = @event.AddToWaitingList(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("User is already on the waiting list");
    }

    [Fact]
    public void AddToWaitingList_WhenUserAlreadyRegistered_ShouldFail()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 10, currentRegistrations: 9);
        var userId = Guid.NewGuid();

        // Register user first
        @event.Register(userId, 1); // Now at capacity (10/10)

        // Act
        var result = @event.AddToWaitingList(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("User is already registered for this event");
    }

    [Fact]
    public void AddToWaitingList_MultipleUsers_ShouldMaintainCorrectPositions()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();

        // Act
        @event.AddToWaitingList(user1);
        @event.AddToWaitingList(user2);
        @event.AddToWaitingList(user3);

        // Assert
        @event.WaitingList.Should().HaveCount(3);
        @event.WaitingList.First(w => w.UserId == user1).Position.Should().Be(1);
        @event.WaitingList.First(w => w.UserId == user2).Position.Should().Be(2);
        @event.WaitingList.First(w => w.UserId == user3).Position.Should().Be(3);
    }

    [Fact]
    public void RemoveFromWaitingList_WhenUserOnWaitingList_ShouldSucceed()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);
        var userId = Guid.NewGuid();
        @event.AddToWaitingList(userId);

        // Act
        var result = @event.RemoveFromWaitingList(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.WaitingList.Should().BeEmpty();
    }

    [Fact]
    public void RemoveFromWaitingList_WhenUserNotOnWaitingList_ShouldFail()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);
        var userId = Guid.NewGuid();

        // Act
        var result = @event.RemoveFromWaitingList(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("User is not on the waiting list");
    }

    [Fact]
    public void RemoveFromWaitingList_ShouldResequencePositions()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();

        @event.AddToWaitingList(user1); // Position 1
        @event.AddToWaitingList(user2); // Position 2
        @event.AddToWaitingList(user3); // Position 3

        // Act - Remove user2 (position 2)
        @event.RemoveFromWaitingList(user2);

        // Assert - Positions should be resequenced (1, 2 instead of 1, 3)
        @event.WaitingList.Should().HaveCount(2);
        @event.WaitingList.First(w => w.UserId == user1).Position.Should().Be(1);
        @event.WaitingList.First(w => w.UserId == user3).Position.Should().Be(2); // Was 3, now 2
    }

    [Fact]
    public void CancelRegistration_WhenWaitingListHasUsers_ShouldPromoteNextInLine()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);
        var registeredUser = @event.Registrations.First().UserId;
        var waitingUser = Guid.NewGuid();

        @event.AddToWaitingList(waitingUser);

        // Act
        var result = @event.CancelRegistration(registeredUser);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.CurrentRegistrations.Should().Be(4); // One spot freed
        @event.HasCapacityFor(1).Should().BeTrue(); // Spot available

        // Domain event should be raised for notification
        var domainEvents = @event.GetDomainEvents();
        domainEvents.Should().Contain(e => e.GetType().Name == "WaitingListSpotAvailableDomainEvent");
    }

    [Fact]
    public void PromoteFromWaitingList_WhenUserNotified_ShouldMoveToConfirmed()
    {
        // Arrange - Simulate real scenario: event fills up, user joins waiting list, someone cancels
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);  // Event at capacity
        var userId = Guid.NewGuid();

        // User joins waiting list when event is full
        @event.AddToWaitingList(userId);

        // Simulate someone canceling (freeing up a spot)
        var registeredUser = @event.Registrations.First().UserId;
        @event.CancelRegistration(registeredUser);

        // Now event is 4/5, and user is first on waiting list
        @event.HasCapacityFor(1).Should().BeTrue();

        // Act - User accepts the spot (clicks link from email)
        var result = @event.PromoteFromWaitingList(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.IsUserRegistered(userId).Should().BeTrue();
        @event.WaitingList.Should().NotContain(w => w.UserId == userId);
        @event.CurrentRegistrations.Should().Be(5); // Back to capacity
    }

    [Fact]
    public void PromoteFromWaitingList_WhenNoCapacity_ShouldFail()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);
        var userId = Guid.NewGuid();
        @event.AddToWaitingList(userId);

        // Act
        var result = @event.PromoteFromWaitingList(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("No capacity available to promote user");
    }

    [Fact]
    public void GetWaitingListPosition_WhenUserOnWaitingList_ShouldReturnPosition()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        @event.AddToWaitingList(user1);
        @event.AddToWaitingList(user2);

        // Act
        var position = @event.GetWaitingListPosition(user2);

        // Assert
        position.Should().Be(2);
    }

    [Fact]
    public void GetWaitingListPosition_WhenUserNotOnWaitingList_ShouldReturnZero()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);
        var userId = Guid.NewGuid();

        // Act
        var position = @event.GetWaitingListPosition(userId);

        // Assert
        position.Should().Be(0);
    }

    [Fact]
    public void IsAtCapacity_WhenRegistrationsEqualCapacity_ShouldReturnTrue()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 5);

        // Act & Assert
        @event.IsAtCapacity().Should().BeTrue();
    }

    [Fact]
    public void IsAtCapacity_WhenRegistrationsLessThanCapacity_ShouldReturnFalse()
    {
        // Arrange
        var @event = CreateEventWithCapacity(capacity: 5, currentRegistrations: 3);

        // Act & Assert
        @event.IsAtCapacity().Should().BeFalse();
    }
}
