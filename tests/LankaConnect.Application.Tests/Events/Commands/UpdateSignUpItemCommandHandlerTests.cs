using FluentAssertions;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateSignUpItem;
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
/// TDD Tests for UpdateSignUpItemCommand and Handler.
/// Phase 6A.14: Edit Sign-Up Item feature
/// Phase 6A.131: Extended for slot-based edits via server-authoritative type routing.
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

    private (Event ev, SignUpList list) CreateEventWithList(bool mandatory = true, bool preferred = false, bool suggested = false)
    {
        var ev = CreateTestEvent();
        var list = SignUpList.CreateWithCategories("Food", "Food sign-up list", mandatory, preferred, suggested).Value;
        ev.AddSignUpList(list);
        return (ev, list);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateSignUpItem()
    {
        // Arrange
        var (ev, list) = CreateEventWithList();
        var item = list.AddItem("Rice (2 cups)", 5, SignUpItemCategory.Mandatory, "Please bring jasmine rice").Value;

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, item.Id,
            ItemDescription: "Basmati Rice (3 cups)",
            TargetQuantity: 10,
            AvailableSlots: null,
            SuggestedPerSlot: null,
            Notes: "Please bring basmati or jasmine rice");

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.ItemDescription.Should().Be("Basmati Rice (3 cups)");
        item.TargetQuantity.Should().Be(10);
        item.AvailableSlots.Should().BeNull();
        item.Notes.Should().Be("Please bring basmati or jasmine rice");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateSignUpItemCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ItemDescription: "Rice",
            TargetQuantity: 5,
            AvailableSlots: null,
            SuggestedPerSlot: null,
            Notes: null);

        _mockEventRepository.Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
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
        var ev = CreateTestEvent();
        var nonExistentListId = Guid.NewGuid();

        var command = new UpdateSignUpItemCommand(
            ev.Id, nonExistentListId, Guid.NewGuid(),
            ItemDescription: "Rice",
            TargetQuantity: 5,
            AvailableSlots: null,
            SuggestedPerSlot: null,
            Notes: null);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain($"Sign-up list with ID {nonExistentListId} not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSignUpItemNotFound_ShouldReturnFailure()
    {
        // Arrange
        var (ev, list) = CreateEventWithList();
        var nonExistentItemId = Guid.NewGuid();

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, nonExistentItemId,
            ItemDescription: "Rice",
            TargetQuantity: 5,
            AvailableSlots: null,
            SuggestedPerSlot: null,
            Notes: null);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

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
        var (ev, list) = CreateEventWithList();
        var item = list.AddItem("Rice", 5, SignUpItemCategory.Mandatory).Value;

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, item.Id,
            ItemDescription: "",
            TargetQuantity: 5,
            AvailableSlots: null,
            SuggestedPerSlot: null,
            Notes: null);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

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
        var (ev, list) = CreateEventWithList();
        var item = list.AddItem("Rice", 5, SignUpItemCategory.Mandatory).Value;

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, item.Id,
            ItemDescription: "Rice",
            TargetQuantity: 0,
            AvailableSlots: null,
            SuggestedPerSlot: null,
            Notes: null);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

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
        var (ev, list) = CreateEventWithList();
        var item = list.AddItem("Rice", 10, SignUpItemCategory.Mandatory).Value;
        item.AddCommitment(Guid.NewGuid(), 5);

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, item.Id,
            ItemDescription: "Rice",
            TargetQuantity: 3,
            AvailableSlots: null,
            SuggestedPerSlot: null,
            Notes: null);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

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
        var (ev, list) = CreateEventWithList();
        var item = list.AddItem("Rice", 10, SignUpItemCategory.Mandatory).Value;
        item.AddCommitment(Guid.NewGuid(), 5);

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, item.Id,
            ItemDescription: "Basmati Rice",
            TargetQuantity: 20,
            AvailableSlots: null,
            SuggestedPerSlot: null,
            Notes: "Updated notes");

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.ItemDescription.Should().Be("Basmati Rice");
        item.TargetQuantity.Should().Be(20);
        item.GetRemainingQuantity().Should().Be(15);
        item.Notes.Should().Be("Updated notes");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullNotes_ShouldClearNotes()
    {
        // Arrange
        var (ev, list) = CreateEventWithList();
        var item = list.AddItem("Rice", 5, SignUpItemCategory.Mandatory, "Original notes").Value;

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, item.Id,
            ItemDescription: "Rice",
            TargetQuantity: 5,
            AvailableSlots: null,
            SuggestedPerSlot: null,
            Notes: null);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.Notes.Should().BeNull();
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Phase 6A.131: Slot-based update tests (previously a silent coverage gap)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_WhenItemIsSlotBased_WithValidAvailableSlots_ShouldUpdateSlotFields()
    {
        // Arrange
        var (ev, list) = CreateEventWithList();
        var item = list.AddSlotBasedItem(
            itemDescription: "Salmon Curry",
            availableSlots: 3,
            suggestedPerSlot: 2,
            itemCategory: SignUpItemCategory.Mandatory,
            notes: "Half trays").Value;

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, item.Id,
            ItemDescription: "Salmon Curry - Half Tray",
            TargetQuantity: null,
            AvailableSlots: 5,
            SuggestedPerSlot: 3,
            Notes: "Updated notes");

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.ItemDescription.Should().Be("Salmon Curry - Half Tray");
        item.AvailableSlots.Should().Be(5);
        item.SuggestedPerSlot.Should().Be(3);
        item.TargetQuantity.Should().BeNull();
        item.Notes.Should().Be("Updated notes");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenItemIsSlotBased_WithoutSuggestedPerSlot_ShouldPreserveExistingSuggestedPerSlot()
    {
        // Arrange: the inline edit UI omits SuggestedPerSlot — the handler must preserve it.
        var (ev, list) = CreateEventWithList();
        var item = list.AddSlotBasedItem(
            itemDescription: "Salmon Curry",
            availableSlots: 3,
            suggestedPerSlot: 2,
            itemCategory: SignUpItemCategory.Mandatory).Value;

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, item.Id,
            ItemDescription: "Salmon Curry v2",
            TargetQuantity: null,
            AvailableSlots: 4,
            SuggestedPerSlot: null,
            Notes: null);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.AvailableSlots.Should().Be(4);
        item.SuggestedPerSlot.Should().Be(2); // preserved from original
    }

    [Fact]
    public async Task Handle_WhenItemIsSlotBased_ButTargetQuantitySent_ShouldReturnExplicitFailure()
    {
        // Arrange
        var (ev, list) = CreateEventWithList();
        var item = list.AddSlotBasedItem(
            itemDescription: "Salmon Curry",
            availableSlots: 3,
            suggestedPerSlot: null,
            itemCategory: SignUpItemCategory.Mandatory).Value;

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, item.Id,
            ItemDescription: "Salmon Curry",
            TargetQuantity: 5,
            AvailableSlots: null,
            SuggestedPerSlot: null,
            Notes: null);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("slot-based") && e.Contains("AvailableSlots"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenItemIsQuantityBased_ButAvailableSlotsSent_ShouldReturnExplicitFailure()
    {
        // Arrange
        var (ev, list) = CreateEventWithList();
        var item = list.AddItem("Rice", 5, SignUpItemCategory.Mandatory).Value;

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, item.Id,
            ItemDescription: "Rice",
            TargetQuantity: null,
            AvailableSlots: 5,
            SuggestedPerSlot: null,
            Notes: null);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("quantity-based") && e.Contains("TargetQuantity"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenItemIsSlotBased_ReducingBelowFilled_ShouldReturnDomainFailure()
    {
        // Arrange
        var (ev, list) = CreateEventWithList();
        var item = list.AddSlotBasedItem(
            itemDescription: "Half Trays",
            availableSlots: 5,
            suggestedPerSlot: 2,
            itemCategory: SignUpItemCategory.Mandatory).Value;
        item.AddSlotCommitment(Guid.NewGuid(), slotsClaimed: 3);

        var command = new UpdateSignUpItemCommand(
            ev.Id, list.Id, item.Id,
            ItemDescription: "Half Trays",
            TargetQuantity: null,
            AvailableSlots: 2, // below filled (3)
            SuggestedPerSlot: null,
            Notes: null);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Cannot reduce slots below filled amount"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
