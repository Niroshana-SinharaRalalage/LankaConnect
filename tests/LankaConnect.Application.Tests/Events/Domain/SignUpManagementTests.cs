using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// TDD Tests for Sign-Up Management Feature
/// Users can volunteer to bring items (food, gifts, logistics) to events
/// Similar to SignupGenius functionality
/// </summary>
public class SignUpManagementTests
{
    #region Test Helpers

    private Event CreateTestEvent()
    {
        var title = EventTitle.Create("Community Potluck").Value;
        var description = EventDescription.Create("Community gathering with food sharing").Value;
        var organizerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(7).AddHours(4);

        var eventResult = Event.Create(title, description, startDate, endDate, organizerId, 100);
        return eventResult.Value;
    }

    #endregion

    #region SignUpList Entity Creation Tests (RED Phase)

    [Fact]
    public void CreateSignUpList_WithValidData_ShouldSucceed()
    {
        // Arrange
        var category = "Food";
        var description = "Please bring your favorite dish to share";
        var signUpType = SignUpType.Open; // Users can specify what they're bringing

        // Act
        var result = SignUpList.Create(category, description, signUpType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be(category);
        result.Value.Description.Should().Be(description);
        result.Value.SignUpType.Should().Be(signUpType);
        result.Value.Commitments.Should().BeEmpty();
    }

    [Fact]
    public void CreateSignUpList_WithEmptyCategory_ShouldFail()
    {
        // Act
        var result = SignUpList.Create("", "Description", SignUpType.Open);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Category cannot be empty");
    }

    [Fact]
    public void CreateSignUpList_WithPredefinedItems_ShouldSucceed()
    {
        // Arrange
        var category = "Food";
        var description = "Select a dish to bring";
        var predefinedItems = new List<string> { "Rice", "Curry", "Dessert", "Drinks" };

        // Act
        var result = SignUpList.CreateWithPredefinedItems(category, description, predefinedItems);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SignUpType.Should().Be(SignUpType.Predefined);
        result.Value.PredefinedItems.Should().HaveCount(4);
        result.Value.PredefinedItems.Should().Contain("Rice");
    }

    [Fact]
    public void CreateSignUpList_WithEmptyPredefinedItems_ShouldFail()
    {
        // Arrange
        var emptyList = new List<string>();

        // Act
        var result = SignUpList.CreateWithPredefinedItems("Food", "Description", emptyList);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Predefined items list cannot be empty");
    }

    #endregion

    #region SignUpCommitment Tests (User Sign-Up Actions)

    [Fact]
    public void UserCommitsToOpenSignUp_WithValidData_ShouldSucceed()
    {
        // Arrange
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        var userId = Guid.NewGuid();
        var itemDescription = "Homemade lasagna";
        var quantity = 1;

        // Act
        var result = signUpList.AddCommitment(userId, itemDescription, quantity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        signUpList.Commitments.Should().HaveCount(1);
        signUpList.Commitments.First().UserId.Should().Be(userId);
        signUpList.Commitments.First().ItemDescription.Should().Be(itemDescription);
        signUpList.Commitments.First().Quantity.Should().Be(quantity);
    }

    [Fact]
    public void UserCommitsToPredefinedSignUp_WithValidItem_ShouldSucceed()
    {
        // Arrange
        var predefinedItems = new List<string> { "Rice", "Curry", "Dessert" };
        var signUpList = SignUpList.CreateWithPredefinedItems("Food", "Select a dish", predefinedItems).Value;
        var userId = Guid.NewGuid();

        // Act
        var result = signUpList.AddCommitment(userId, "Rice", 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        signUpList.Commitments.Should().HaveCount(1);
        signUpList.Commitments.First().ItemDescription.Should().Be("Rice");
    }

    [Fact]
    public void UserCommitsToPredefinedSignUp_WithInvalidItem_ShouldFail()
    {
        // Arrange
        var predefinedItems = new List<string> { "Rice", "Curry" };
        var signUpList = SignUpList.CreateWithPredefinedItems("Food", "Select", predefinedItems).Value;
        var userId = Guid.NewGuid();

        // Act
        var result = signUpList.AddCommitment(userId, "Pizza", 1); // Not in predefined list

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Item 'Pizza' is not in the predefined items list");
    }

    [Fact]
    public void UserCommitsTwice_ShouldFail()
    {
        // Arrange
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        var userId = Guid.NewGuid();
        signUpList.AddCommitment(userId, "Salad", 1);

        // Act - User tries to commit again
        var result = signUpList.AddCommitment(userId, "Bread", 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("User has already committed to this sign-up");
    }

    [Fact]
    public void UserCancelsCommitment_ShouldSucceed()
    {
        // Arrange
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        var userId = Guid.NewGuid();
        signUpList.AddCommitment(userId, "Salad", 1);

        // Act
        var result = signUpList.CancelCommitment(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        signUpList.Commitments.Should().BeEmpty();
    }

    [Fact]
    public void UserCancelsNonExistentCommitment_ShouldFail()
    {
        // Arrange
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        var userId = Guid.NewGuid();

        // Act
        var result = signUpList.CancelCommitment(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("User has no commitment to cancel");
    }

    #endregion

    #region Event Aggregate Integration Tests

    [Fact]
    public void AddSignUpList_ToEvent_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;

        // Act
        var result = @event.AddSignUpList(signUpList);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.SignUpLists.Should().HaveCount(1);
        @event.SignUpLists.First().Should().Be(signUpList);
    }

    [Fact]
    public void AddMultipleSignUpLists_ToEvent_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var foodSignUp = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        var giftSignUp = SignUpList.Create("Gifts", "Bring a gift", SignUpType.Open).Value;
        var logisticsSignUp = SignUpList.Create("Logistics", "Help with setup", SignUpType.Open).Value;

        // Act
        @event.AddSignUpList(foodSignUp);
        @event.AddSignUpList(giftSignUp);
        var result = @event.AddSignUpList(logisticsSignUp);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.SignUpLists.Should().HaveCount(3);
    }

    [Fact]
    public void RemoveSignUpList_FromEvent_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        @event.AddSignUpList(signUpList);

        // Act
        var result = @event.RemoveSignUpList(signUpList.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.SignUpLists.Should().BeEmpty();
    }

    [Fact]
    public void RemoveSignUpList_WithCommitments_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        @event.AddSignUpList(signUpList);

        var userId = Guid.NewGuid();
        signUpList.AddCommitment(userId, "Salad", 1);

        // Act
        var result = @event.RemoveSignUpList(signUpList.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Cannot remove sign-up list with existing commitments");
    }

    #endregion

    #region Domain Events Tests

    [Fact]
    public void AddCommitment_ShouldRaiseDomainEvent()
    {
        // Arrange
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        var userId = Guid.NewGuid();
        signUpList.ClearDomainEvents();

        // Act
        signUpList.AddCommitment(userId, "Salad", 1);

        // Assert
        var domainEvents = signUpList.DomainEvents;
        domainEvents.Should().HaveCount(1);
        domainEvents.First().Should().BeOfType<UserCommittedToSignUpEvent>();

        var commitEvent = (UserCommittedToSignUpEvent)domainEvents.First();
        commitEvent.UserId.Should().Be(userId);
        commitEvent.ItemDescription.Should().Be("Salad");
    }

    [Fact]
    public void CancelCommitment_ShouldRaiseDomainEvent()
    {
        // Arrange
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        var userId = Guid.NewGuid();
        signUpList.AddCommitment(userId, "Salad", 1);
        signUpList.ClearDomainEvents();

        // Act
        signUpList.CancelCommitment(userId);

        // Assert
        var domainEvents = signUpList.DomainEvents;
        domainEvents.Should().HaveCount(1);
        domainEvents.First().Should().BeOfType<UserCancelledSignUpCommitmentEvent>();

        var cancelEvent = (UserCancelledSignUpCommitmentEvent)domainEvents.First();
        cancelEvent.UserId.Should().Be(userId);
    }

    #endregion

    #region Query Tests (Get Commitments)

    [Fact]
    public void GetUserCommitment_WhenExists_ShouldReturnCommitment()
    {
        // Arrange
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        var userId = Guid.NewGuid();
        signUpList.AddCommitment(userId, "Salad", 1);

        // Act
        var commitment = signUpList.GetUserCommitment(userId);

        // Assert
        commitment.Should().NotBeNull();
        commitment!.ItemDescription.Should().Be("Salad");
    }

    [Fact]
    public void GetUserCommitment_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        var userId = Guid.NewGuid();

        // Act
        var commitment = signUpList.GetUserCommitment(userId);

        // Assert
        commitment.Should().BeNull();
    }

    [Fact]
    public void HasUserCommitted_WhenCommitted_ShouldReturnTrue()
    {
        // Arrange
        var signUpList = SignUpList.Create("Food", "Bring a dish", SignUpType.Open).Value;
        var userId = Guid.NewGuid();
        signUpList.AddCommitment(userId, "Salad", 1);

        // Act
        var hasCommitted = signUpList.HasUserCommitted(userId);

        // Assert
        hasCommitted.Should().BeTrue();
    }

    #endregion
}
