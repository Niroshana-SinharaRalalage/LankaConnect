using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Commands.AddSignUpItem;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// TDD Tests for AddSignUpItemCommand, Handler, and Validator
/// Phase 6A.121: Dual nullable fields for slot-based signup items
/// </summary>
public class AddSignUpItemCommandHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<AddSignUpItemCommandHandler>> _mockLogger;
    private readonly AddSignUpItemCommandHandler _handler;
    private readonly AddSignUpItemCommandValidator _validator;

    public AddSignUpItemCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<AddSignUpItemCommandHandler>>();
        _handler = new AddSignUpItemCommandHandler(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
        _validator = new AddSignUpItemCommandValidator();
    }

    private Event CreateTestEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var organizerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(7).AddHours(4);
        return Event.Create(title, description, startDate, endDate, organizerId, 100).Value;
    }

    // ==================== HANDLER TESTS ====================

    [Fact]
    public async Task Handle_QuantityBased_WithValidCommand_CreatesItemAndReturnsId()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories("Food", "Food sign-up", true, false, false).Value;
        @event.AddSignUpList(signUpList);

        var command = new AddSignUpItemCommand(
            @event.Id,
            signUpList.Id,
            "Rice",
            SignUpItemType.Quantity,
            SignUpItemCategory.Mandatory,
            TargetQuantity: 10);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);
        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var addedItem = signUpList.Items.First(i => i.Id == result.Value);
        addedItem.ItemType.Should().Be(SignUpItemType.Quantity);
        addedItem.TargetQuantity.Should().Be(10);
        addedItem.AvailableSlots.Should().BeNull();

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SlotBased_WithValidCommand_CreatesItemAndReturnsId()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories("Food", "Food sign-up", true, false, false).Value;
        @event.AddSignUpList(signUpList);

        var command = new AddSignUpItemCommand(
            @event.Id,
            signUpList.Id,
            "Assorted Fruits",
            SignUpItemType.Slot,
            SignUpItemCategory.Mandatory,
            AvailableSlots: 3,
            SuggestedPerSlot: 5);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);
        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var addedItem = signUpList.Items.First(i => i.Id == result.Value);
        addedItem.ItemType.Should().Be(SignUpItemType.Slot);
        addedItem.AvailableSlots.Should().Be(3);
        addedItem.SuggestedPerSlot.Should().Be(5);
        addedItem.TargetQuantity.Should().BeNull();

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SlotBased_WithoutSuggestedPerSlot_CreatesItemSuccessfully()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories("Food", "Food sign-up", true, false, false).Value;
        @event.AddSignUpList(signUpList);

        var command = new AddSignUpItemCommand(
            @event.Id,
            signUpList.Id,
            "Homemade Desserts",
            SignUpItemType.Slot,
            SignUpItemCategory.Mandatory,
            AvailableSlots: 5);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);
        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var addedItem = signUpList.Items.First(i => i.Id == result.Value);
        addedItem.AvailableSlots.Should().Be(5);
        addedItem.SuggestedPerSlot.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistentEvent_ReturnsFailure()
    {
        // Arrange
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Rice",
            SignUpItemType.Quantity,
            SignUpItemCategory.Mandatory,
            TargetQuantity: 10);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentSignUpList_ReturnsFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        var command = new AddSignUpItemCommand(
            @event.Id,
            Guid.NewGuid(), // Non-existent list
            "Rice",
            SignUpItemType.Quantity,
            SignUpItemCategory.Mandatory,
            TargetQuantity: 10);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_QuantityBased_WhenCategoryNotEnabled_ReturnsFailure()
    {
        // Arrange - List only has Mandatory enabled, not Suggested
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories("Food", "Food sign-up", true, false, false).Value;
        @event.AddSignUpList(signUpList);

        var command = new AddSignUpItemCommand(
            @event.Id,
            signUpList.Id,
            "Rice",
            SignUpItemType.Quantity,
            SignUpItemCategory.Suggested, // Category not enabled
            TargetQuantity: 10);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not enabled");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ==================== VALIDATOR TESTS ====================

    [Fact]
    public void Validator_QuantityBased_WithValidData_PassesValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Rice",
            SignUpItemType.Quantity,
            SignUpItemCategory.Mandatory,
            TargetQuantity: 10);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_SlotBased_WithValidData_PassesValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Assorted Fruits",
            SignUpItemType.Slot,
            SignUpItemCategory.Suggested,
            AvailableSlots: 3,
            SuggestedPerSlot: 5);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_SlotBased_WithoutSuggestedPerSlot_PassesValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Homemade Desserts",
            SignUpItemType.Slot,
            SignUpItemCategory.Suggested,
            AvailableSlots: 5);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_QuantityBased_WithMissingTargetQuantity_FailsValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Rice",
            SignUpItemType.Quantity,
            SignUpItemCategory.Mandatory);
        // TargetQuantity not provided

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TargetQuantity);
    }

    [Fact]
    public void Validator_QuantityBased_WithBothFieldsPopulated_FailsValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Rice",
            SignUpItemType.Quantity,
            SignUpItemCategory.Mandatory,
            TargetQuantity: 10,
            AvailableSlots: 3); // Should be null for quantity-based

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AvailableSlots);
    }

    [Fact]
    public void Validator_SlotBased_WithMissingAvailableSlots_FailsValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fruits",
            SignUpItemType.Slot,
            SignUpItemCategory.Suggested);
        // AvailableSlots not provided

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AvailableSlots);
    }

    [Fact]
    public void Validator_SlotBased_WithTargetQuantityAlsoSet_FailsValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fruits",
            SignUpItemType.Slot,
            SignUpItemCategory.Suggested,
            TargetQuantity: 10, // Should be null for slot-based
            AvailableSlots: 3);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TargetQuantity);
    }

    [Fact]
    public void Validator_QuantityBased_WithZeroQuantity_FailsValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Rice",
            SignUpItemType.Quantity,
            SignUpItemCategory.Mandatory,
            TargetQuantity: 0);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TargetQuantity);
    }

    [Fact]
    public void Validator_QuantityBased_WithExcessiveQuantity_FailsValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Rice",
            SignUpItemType.Quantity,
            SignUpItemCategory.Mandatory,
            TargetQuantity: 1001);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TargetQuantity);
    }

    [Fact]
    public void Validator_SlotBased_WithExcessiveSlots_FailsValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fruits",
            SignUpItemType.Slot,
            SignUpItemCategory.Suggested,
            AvailableSlots: 101); // Max 100

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AvailableSlots);
    }

    [Fact]
    public void Validator_SlotBased_WithInvalidSuggestedPerSlot_FailsValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fruits",
            SignUpItemType.Slot,
            SignUpItemCategory.Suggested,
            AvailableSlots: 3,
            SuggestedPerSlot: 0); // Must be > 0

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SuggestedPerSlot);
    }

    [Fact]
    public void Validator_WithEmptyDescription_FailsValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "", // Empty
            SignUpItemType.Quantity,
            SignUpItemCategory.Mandatory,
            TargetQuantity: 10);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ItemDescription);
    }

    [Fact]
    public void Validator_WithDescriptionExceedingMaxLength_FailsValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('A', 201), // 201 chars, max is 200
            SignUpItemType.Quantity,
            SignUpItemCategory.Mandatory,
            TargetQuantity: 10);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ItemDescription);
    }

    [Fact]
    public void Validator_WithEmptyEventId_FailsValidation()
    {
        var command = new AddSignUpItemCommand(
            Guid.Empty, // Invalid
            Guid.NewGuid(),
            "Rice",
            SignUpItemType.Quantity,
            SignUpItemCategory.Mandatory,
            TargetQuantity: 10);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.EventId);
    }
}
