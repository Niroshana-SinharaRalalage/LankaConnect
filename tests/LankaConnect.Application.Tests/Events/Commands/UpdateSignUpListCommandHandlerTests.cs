using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.UpdateSignUpList;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// TDD Tests for UpdateSignUpListCommand and Handler
/// Phase 6A.13: Edit Sign-Up List feature
/// </summary>
public class UpdateSignUpListCommandHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly UpdateSignUpListCommandHandler _handler;

    public UpdateSignUpListCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new UpdateSignUpListCommandHandler(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object);
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
    public async Task Handle_WithValidCommand_ShouldUpdateSignUpList()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories(
            "Food",
            "Original description",
            true,
            false,
            false).Value;
        @event.AddSignUpList(signUpList);

        var command = new UpdateSignUpListCommand(
            @event.Id,
            signUpList.Id,
            "Food and Drinks",
            "Updated description",
            true,
            true,
            false);

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
        signUpList.Category.Should().Be("Food and Drinks");
        signUpList.Description.Should().Be("Updated description");
        signUpList.HasMandatoryItems.Should().BeTrue();
        signUpList.HasPreferredItems.Should().BeTrue();
        signUpList.HasSuggestedItems.Should().BeFalse();
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateSignUpListCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Food",
            "Description",
            true,
            false,
            false);

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

        var command = new UpdateSignUpListCommand(
            @event.Id,
            nonExistentSignUpListId,
            "Food",
            "Description",
            true,
            false,
            false);

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
    public async Task Handle_WithEmptyCategory_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories(
            "Food",
            "Description",
            true,
            false,
            false).Value;
        @event.AddSignUpList(signUpList);

        var command = new UpdateSignUpListCommand(
            @event.Id,
            signUpList.Id,
            "",
            "Description",
            true,
            false,
            false);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Category cannot be empty");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DisablingCategoryWithItems_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories(
            "Food",
            "Description",
            true,
            true,
            false).Value;
        signUpList.AddItem("Rice", 10, SignUpItemCategory.Mandatory);
        @event.AddSignUpList(signUpList);

        var command = new UpdateSignUpListCommand(
            @event.Id,
            signUpList.Id,
            "Food",
            "Description",
            false, // Try to disable Mandatory category
            true,
            false);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Cannot disable Mandatory category because it contains items");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EnablingNewCategory_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var signUpList = SignUpList.CreateWithCategories(
            "Food",
            "Description",
            true,
            false,
            false).Value;
        signUpList.AddItem("Rice", 10, SignUpItemCategory.Mandatory);
        @event.AddSignUpList(signUpList);

        var command = new UpdateSignUpListCommand(
            @event.Id,
            signUpList.Id,
            "Food",
            "Description",
            true,
            true, // Enable Preferred category
            true); // Enable Suggested category

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
        signUpList.HasMandatoryItems.Should().BeTrue();
        signUpList.HasPreferredItems.Should().BeTrue();
        signUpList.HasSuggestedItems.Should().BeTrue();
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
