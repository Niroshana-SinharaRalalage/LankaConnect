using FluentAssertions;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// TDD tests for SignUpItem entity with dual nullable fields (Phase 6A.121)
/// Tests quantity-based and slot-based item creation, calculation methods, and type safety
/// </summary>
public class SignUpItemTests
{
    private readonly Guid _signUpListId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    #region CreateQuantityBased Tests (Phase 1.1)

    [Fact]
    public void CreateQuantityBased_WithValidData_Should_Return_Success()
    {
        // Arrange & Act
        var result = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "Rice",
            10,
            SignUpItemCategory.Mandatory);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TargetQuantity.Should().Be(10);
        result.Value.AvailableSlots.Should().BeNull();
        result.Value.SuggestedPerSlot.Should().BeNull();
        result.Value.ItemType.Should().Be(SignUpItemType.Quantity);
        result.Value.ItemDescription.Should().Be("Rice");
        result.Value.ItemCategory.Should().Be(SignUpItemCategory.Mandatory);
    }

    [Fact]
    public void CreateQuantityBased_WithNotes_Should_Set_Notes()
    {
        // Arrange & Act
        var result = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "Rice",
            10,
            SignUpItemCategory.Mandatory,
            notes: "Non-spicy preferred");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Notes.Should().Be("Non-spicy preferred");
    }

    [Fact]
    public void CreateQuantityBased_WithZeroQuantity_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "Rice",
            0,
            SignUpItemCategory.Mandatory);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Target quantity must be between 1 and 1000");
    }

    [Fact]
    public void CreateQuantityBased_WithNegativeQuantity_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "Rice",
            -5,
            SignUpItemCategory.Mandatory);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Target quantity must be between 1 and 1000");
    }

    [Fact]
    public void CreateQuantityBased_WithQuantityExceeding1000_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "Rice",
            1001,
            SignUpItemCategory.Mandatory);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Target quantity must be between 1 and 1000");
    }

    [Fact]
    public void CreateQuantityBased_WithEmptySignUpListId_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateQuantityBased(
            Guid.Empty,
            "Rice",
            10,
            SignUpItemCategory.Mandatory);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sign-up list ID is required");
    }

    [Fact]
    public void CreateQuantityBased_WithEmptyDescription_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "",
            10,
            SignUpItemCategory.Mandatory);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Item description is required");
    }

    [Fact]
    public void CreateQuantityBased_WithWhitespaceDescription_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "   ",
            10,
            SignUpItemCategory.Mandatory);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Item description is required");
    }

    #endregion

    #region CreateSlotBased Tests (Phase 1.2)

    [Fact]
    public void CreateSlotBased_WithValidData_Should_Return_Success()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Assorted Fruits",
            3,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvailableSlots.Should().Be(3);
        result.Value.TargetQuantity.Should().BeNull();
        result.Value.SuggestedPerSlot.Should().BeNull();
        result.Value.ItemType.Should().Be(SignUpItemType.Slot);
        result.Value.ItemDescription.Should().Be("Assorted Fruits");
        result.Value.ItemCategory.Should().Be(SignUpItemCategory.Suggested);
    }

    [Fact]
    public void CreateSlotBased_WithSuggestedPerSlot_Should_Set_SuggestedPerSlot()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Rice",
            3,
            suggestedPerSlot: 5,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvailableSlots.Should().Be(3);
        result.Value.SuggestedPerSlot.Should().Be(5);
        result.Value.TargetQuantity.Should().BeNull();
        result.Value.ItemType.Should().Be(SignUpItemType.Slot);
    }

    [Fact]
    public void CreateSlotBased_WithNotes_Should_Set_Notes()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Homemade Desserts",
            5,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested,
            notes: "Any homemade dessert appreciated");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Notes.Should().Be("Any homemade dessert appreciated");
    }

    [Fact]
    public void CreateSlotBased_WithZeroSlots_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Fruits",
            0,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Available slots must be between 1 and 100");
    }

    [Fact]
    public void CreateSlotBased_WithNegativeSlots_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Fruits",
            -3,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Available slots must be between 1 and 100");
    }

    [Fact]
    public void CreateSlotBased_WithSlotsExceeding100_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Fruits",
            101,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Available slots must be between 1 and 100");
    }

    [Fact]
    public void CreateSlotBased_WithZeroSuggestedPerSlot_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Rice",
            3,
            suggestedPerSlot: 0,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Suggested quantity per slot must be between 1 and 1000");
    }

    [Fact]
    public void CreateSlotBased_WithNegativeSuggestedPerSlot_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Rice",
            3,
            suggestedPerSlot: -5,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Suggested quantity per slot must be between 1 and 1000");
    }

    [Fact]
    public void CreateSlotBased_WithSuggestedPerSlotExceeding1000_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Rice",
            3,
            suggestedPerSlot: 1001,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Suggested quantity per slot must be between 1 and 1000");
    }

    [Fact]
    public void CreateSlotBased_WithEmptySignUpListId_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            Guid.Empty,
            "Fruits",
            3,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sign-up list ID is required");
    }

    [Fact]
    public void CreateSlotBased_WithEmptyDescription_Should_Return_Failure()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "",
            3,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Item description is required");
    }

    #endregion

    #region Calculation Methods Tests (Phase 1.3)

    [Fact]
    public void GetRemainingQuantity_OnQuantityBasedItem_WithNoCommitments_Should_Return_FullQuantity()
    {
        // Arrange
        var item = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "Rice",
            10,
            SignUpItemCategory.Mandatory).Value;

        // Act
        var remaining = item.GetRemainingQuantity();

        // Assert
        remaining.Should().Be(10);
    }

    [Fact]
    public void GetRemainingQuantity_OnSlotBasedItem_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var item = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Fruits",
            3,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested).Value;

        // Act & Assert
        var act = () => item.GetRemainingQuantity();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Use GetRemainingSlots()*");
    }

    [Fact]
    public void GetRemainingSlots_OnSlotBasedItem_WithNoCommitments_Should_Return_AllSlots()
    {
        // Arrange
        var item = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Fruits",
            3,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested).Value;

        // Act
        var remaining = item.GetRemainingSlots();

        // Assert
        remaining.Should().Be(3);
    }

    [Fact]
    public void GetRemainingSlots_OnQuantityBasedItem_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var item = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "Rice",
            10,
            SignUpItemCategory.Mandatory).Value;

        // Act & Assert
        var act = () => item.GetRemainingSlots();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Use GetRemainingQuantity()*");
    }

    [Fact]
    public void GetEstimatedTotalQuantity_OnSlotBasedItem_WithSuggestedPerSlot_Should_Return_Estimate()
    {
        // Arrange
        var item = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Rice",
            3,
            suggestedPerSlot: 5,
            SignUpItemCategory.Suggested).Value;

        // Act
        var estimated = item.GetEstimatedTotalQuantity();

        // Assert
        // With no commitments, estimated should be 0 (0 filled slots * 5)
        estimated.Should().Be(0);
    }

    [Fact]
    public void GetEstimatedTotalQuantity_OnSlotBasedItem_WithoutSuggestedPerSlot_Should_Return_Null()
    {
        // Arrange
        var item = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Fruits",
            3,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested).Value;

        // Act
        var estimated = item.GetEstimatedTotalQuantity();

        // Assert
        estimated.Should().BeNull();
    }

    [Fact]
    public void GetEstimatedTotalQuantity_OnQuantityBasedItem_Should_Return_Null()
    {
        // Arrange
        var item = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "Rice",
            10,
            SignUpItemCategory.Mandatory).Value;

        // Act
        var estimated = item.GetEstimatedTotalQuantity();

        // Assert
        estimated.Should().BeNull();
    }

    #endregion

    #region ItemType Computed Property Tests

    [Fact]
    public void ItemType_OnQuantityBasedItem_Should_Be_Quantity()
    {
        // Arrange
        var item = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "Rice",
            10,
            SignUpItemCategory.Mandatory).Value;

        // Act & Assert
        item.ItemType.Should().Be(SignUpItemType.Quantity);
    }

    [Fact]
    public void ItemType_OnSlotBasedItem_Should_Be_Slot()
    {
        // Arrange
        var item = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Fruits",
            3,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested).Value;

        // Act & Assert
        item.ItemType.Should().Be(SignUpItemType.Slot);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CreateQuantityBased_WithMaxValidQuantity_Should_Return_Success()
    {
        // Arrange & Act
        var result = SignUpItem.CreateQuantityBased(
            _signUpListId,
            "Rice",
            1000,
            SignUpItemCategory.Mandatory);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TargetQuantity.Should().Be(1000);
    }

    [Fact]
    public void CreateSlotBased_WithMaxValidSlots_Should_Return_Success()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Fruits",
            100,
            suggestedPerSlot: null,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvailableSlots.Should().Be(100);
    }

    [Fact]
    public void CreateSlotBased_WithMaxValidSuggestedPerSlot_Should_Return_Success()
    {
        // Arrange & Act
        var result = SignUpItem.CreateSlotBased(
            _signUpListId,
            "Rice",
            3,
            suggestedPerSlot: 1000,
            SignUpItemCategory.Suggested);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SuggestedPerSlot.Should().Be(1000);
    }

    #endregion

    #region Phase 6A.125: Slot-Based Commitment Tests

    [Fact]
    public void AddSlotCommitment_OnSlotBasedItem_Should_Succeed()
    {
        // Arrange
        var item = SignUpItem.CreateSlotBased(
            _signUpListId, "Fruits", 3, null, SignUpItemCategory.Suggested).Value;
        var userId = Guid.NewGuid();

        // Act
        var result = item.AddSlotCommitment(userId, 1, "Bringing apples");

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.Commitments.Should().HaveCount(1);
        item.Commitments[0].SlotsClaimed.Should().Be(1);
        item.Commitments[0].PhysicalQuantity.Should().BeNull();
        item.Commitments[0].Notes.Should().Be("Bringing apples");
    }

    [Fact]
    public void AddSlotCommitment_OnQuantityBasedItem_Should_Fail()
    {
        // Arrange
        var item = SignUpItem.CreateQuantityBased(
            _signUpListId, "Rice", 10, SignUpItemCategory.Mandatory).Value;
        var userId = Guid.NewGuid();

        // Act
        var result = item.AddSlotCommitment(userId, 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("quantity-based");
    }

    [Fact]
    public void AddCommitment_OnSlotBasedItem_Should_Fail()
    {
        // Arrange
        var item = SignUpItem.CreateSlotBased(
            _signUpListId, "Fruits", 3, null, SignUpItemCategory.Suggested).Value;
        var userId = Guid.NewGuid();

        // Act
        var result = item.AddCommitment(userId, 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("slot-based");
    }

    [Fact]
    public void AddSlotCommitment_ExceedingRemainingSlots_Should_Fail()
    {
        // Arrange - AvailableSlots=2 means 2 DIFFERENT PEOPLE can commit (user count semantics)
        var item = SignUpItem.CreateSlotBased(
            _signUpListId, "Fruits", 2, null, SignUpItemCategory.Suggested).Value;

        // Fill both slots (2 separate users)
        item.AddSlotCommitment(Guid.NewGuid(), 1);
        item.AddSlotCommitment(Guid.NewGuid(), 1);

        // Act - 3rd user tries to claim a slot (no slots left)
        var result = item.AddSlotCommitment(Guid.NewGuid(), 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("remaining");
    }

    [Fact]
    public void AddSlotCommitment_DuplicateUser_Should_Fail()
    {
        // Arrange
        var item = SignUpItem.CreateSlotBased(
            _signUpListId, "Fruits", 3, null, SignUpItemCategory.Suggested).Value;
        var userId = Guid.NewGuid();
        item.AddSlotCommitment(userId, 1);

        // Act
        var result = item.AddSlotCommitment(userId, 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already committed");
    }

    [Fact]
    public void UpdateSlotCommitment_ShouldUpdateSlotsClaimed()
    {
        // Arrange
        var item = SignUpItem.CreateSlotBased(
            _signUpListId, "Fruits", 5, null, SignUpItemCategory.Suggested).Value;
        var userId = Guid.NewGuid();
        item.AddSlotCommitment(userId, 1);

        // Act
        var result = item.UpdateSlotCommitment(userId, 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.Commitments[0].SlotsClaimed.Should().Be(3);
    }

    [Fact]
    public void UpdateSlotCommitment_WithZeroSlots_ShouldCancelCommitment()
    {
        // Arrange
        var item = SignUpItem.CreateSlotBased(
            _signUpListId, "Fruits", 3, null, SignUpItemCategory.Suggested).Value;
        var userId = Guid.NewGuid();
        item.AddSlotCommitment(userId, 2);

        // Act - zero means cancel
        var result = item.UpdateSlotCommitment(userId, 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.Commitments.Should().BeEmpty();
    }

    [Fact]
    public void CancelCommitment_OnSlotBasedItem_ShouldSucceed()
    {
        // Arrange
        var item = SignUpItem.CreateSlotBased(
            _signUpListId, "Fruits", 3, null, SignUpItemCategory.Suggested).Value;
        var userId = Guid.NewGuid();
        item.AddSlotCommitment(userId, 2);

        // Act
        var result = item.CancelCommitment(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.Commitments.Should().BeEmpty();
        item.GetRemainingSlots().Should().Be(3);
    }

    [Fact]
    public void GetRemainingSlots_AfterMultipleCommitments_ShouldTrackCorrectly()
    {
        // Arrange - 5 slots means 5 different PEOPLE can commit
        var item = SignUpItem.CreateSlotBased(
            _signUpListId, "Fruits", 5, null, SignUpItemCategory.Suggested).Value;

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        // Act
        item.AddSlotCommitment(user1, 2);  // user1 commits (claims 2 slot-units but occupies 1 "slot")
        item.AddSlotCommitment(user2, 1);  // user2 commits (claims 1 slot-unit)

        // GetRemainingSlots = AvailableSlots - count of distinct users with SlotsClaimed > 0
        // = 5 - 2 users = 3 remaining slots for other people
        item.GetRemainingSlots().Should().Be(3);
    }

    #endregion
}
