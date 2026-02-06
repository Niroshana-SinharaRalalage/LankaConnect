using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.UpdateSignUpItem;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// TDD Tests for UpdateSignUpItemCommand and Handler
/// Phase 6A.14: Edit Sign-Up Item feature
/// </summary>
public class UpdateSignUpItemCommandHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<UpdateSignUpItemCommandHandler>> _mockLogger;
    private readonly UpdateSignUpItemCommandHandler _handler;

    public UpdateSignUpItemCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<UpdateSignUpItemCommandHandler>>();
        _handler = new UpdateSignUpItemCommandHandler(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    private Event CreateTestEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var organizerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(7).AddHours(4);

        var eventResult = Event.Create(title, description, startDate, endDate, organizerId, 100);
        return eventResult.Value;
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateSignUpItem()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories(
            "Food",
            "Food sign-up list",
            true,
            false,
            false).Value;
        @event.AddSignUpList(signUpList);

        var itemResult = signUpList.AddItem("Rice (2 cups)", 5, SignUpItemCategory.Mandatory, "Please bring jasmine rice");
        var item = itemResult.Value;

        var command = new UpdateSignUpItemCommand(
            @event.Id,
            signUpList.Id,
            item.Id,
            "Basmati Rice (3 cups)",
            10,
            "Please bring basmati or jasmine rice");

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
        item.ItemDescription.Should().Be("Basmati Rice (3 cups)");
        item.Quantity.Should().Be(10);
        item.Notes.Should().Be("Please bring basmati or jasmine rice");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateSignUpItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Rice",
            5,
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain($"Event with ID {command.EventId} not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSignUpListNotFound_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        var nonExistentSignUpListId = Guid.NewGuid();

        var command = new UpdateSignUpItemCommand(
            @event.Id,
            nonExistentSignUpListId,
            Guid.NewGuid(),
            "Rice",
            5,
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain($"Sign-up list with ID {nonExistentSignUpListId} not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSignUpItemNotFound_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories(
            "Food",
            "Food sign-up list",
            true,
            false,
            false).Value;
        @event.AddSignUpList(signUpList);

        var nonExistentItemId = Guid.NewGuid();

        var command = new UpdateSignUpItemCommand(
            @event.Id,
            signUpList.Id,
            nonExistentItemId,
            "Rice",
            5,
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain($"Sign-up item with ID {nonExistentItemId} not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyDescription_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories(
            "Food",
            "Food sign-up list",
            true,
            false,
            false).Value;
        @event.AddSignUpList(signUpList);

        var itemResult = signUpList.AddItem("Rice", 5, SignUpItemCategory.Mandatory);
        var item = itemResult.Value;

        var command = new UpdateSignUpItemCommand(
            @event.Id,
            signUpList.Id,
            item.Id,
            "",
            5,
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Item description is required");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidQuantity_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories(
            "Food",
            "Food sign-up list",
            true,
            false,
            false).Value;
        @event.AddSignUpList(signUpList);

        var itemResult = signUpList.AddItem("Rice", 5, SignUpItemCategory.Mandatory);
        var item = itemResult.Value;

        var command = new UpdateSignUpItemCommand(
            @event.Id,
            signUpList.Id,
            item.Id,
            "Rice",
            0, // Invalid quantity
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Quantity must be greater than 0");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReducingQuantityBelowCommitted_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories(
            "Food",
            "Food sign-up list",
            true,
            false,
            false).Value;
        @event.AddSignUpList(signUpList);

        var itemResult = signUpList.AddItem("Rice", 10, SignUpItemCategory.Mandatory);
        var item = itemResult.Value;

        // User commits to 5 units
        var userId = Guid.NewGuid();
        item.AddCommitment(userId, 5);

        var command = new UpdateSignUpItemCommand(
            @event.Id,
            signUpList.Id,
            item.Id,
            "Rice",
            3, // Try to reduce to 3, but 5 already committed
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Cannot reduce quantity below committed amount (5)");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_IncreasingQuantityWithCommitments_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories(
            "Food",
            "Food sign-up list",
            true,
            false,
            false).Value;
        @event.AddSignUpList(signUpList);

        var itemResult = signUpList.AddItem("Rice", 10, SignUpItemCategory.Mandatory);
        var item = itemResult.Value;

        // User commits to 5 units
        var userId = Guid.NewGuid();
        item.AddCommitment(userId, 5);

        var command = new UpdateSignUpItemCommand(
            @event.Id,
            signUpList.Id,
            item.Id,
            "Basmati Rice",
            20, // Increase to 20 (5 committed, 15 remaining)
            "Updated notes");

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
        item.ItemDescription.Should().Be("Basmati Rice");
        item.Quantity.Should().Be(20);
        item.RemainingQuantity.Should().Be(15); // 20 total - 5 committed
        item.Notes.Should().Be("Updated notes");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullNotes_ShouldClearNotes()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories(
            "Food",
            "Food sign-up list",
            true,
            false,
            false).Value;
        @event.AddSignUpList(signUpList);

        var itemResult = signUpList.AddItem("Rice", 5, SignUpItemCategory.Mandatory, "Original notes");
        var item = itemResult.Value;

        var command = new UpdateSignUpItemCommand(
            @event.Id,
            signUpList.Id,
            item.Id,
            "Rice",
            5,
            null); // Clear notes

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
        item.Notes.Should().BeNull();
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
