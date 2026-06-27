using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.ReorderSignUpItems;
using LankaConnect.Domain.Common;
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
/// Phase 6A.132: Handler tests for ReorderSignUpItemsCommand.
/// Contract: aggregate owns set-equality validation. Handler verifies event/list
/// existence, delegates to SignUpList.ReorderItems, and commits. No test mutates
/// domain state directly — we read back DisplayOrder to confirm persistence path.
/// </summary>
public class ReorderSignUpItemsCommandHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<ReorderSignUpItemsCommandHandler>> _mockLogger;
    private readonly ReorderSignUpItemsCommandHandler _handler;

    public ReorderSignUpItemsCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<ReorderSignUpItemsCommandHandler>>();
        _handler = new ReorderSignUpItemsCommandHandler(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    private static Event CreateTestEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var organizerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(7).AddHours(4);
        return Event.Create(title, description, startDate, endDate, organizerId, 100).Value;
    }

    private static (Event ev, SignUpList list, List<SignUpItem> items) CreateEventWithItems(int count)
    {
        var ev = CreateTestEvent();
        var list = SignUpList.CreateWithCategories("Food", "Food sign-up list", true, false, false).Value;
        ev.AddSignUpList(list);

        var items = new List<SignUpItem>();
        for (int i = 0; i < count; i++)
        {
            var item = list.AddItem($"Item {i}", 1, SignUpItemCategory.Mandatory).Value;
            items.Add(item);
        }

        return (ev, list, items);
    }

    [Fact]
    public async Task Handle_WithExactSet_ShouldReorderAndCommit()
    {
        var (ev, list, items) = CreateEventWithItems(3);
        var reversedIds = items.Select(i => i.Id).Reverse().ToList();

        var command = new ReorderSignUpItemsCommand(ev.Id, list.Id, reversedIds);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        items[0].DisplayOrder.Should().Be(2);
        items[1].DisplayOrder.Should().Be(1);
        items[2].DisplayOrder.Should().Be(0);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventNotFound_ShouldReturnFailureAndNotCommit()
    {
        var command = new ReorderSignUpItemsCommand(
            Guid.NewGuid(), Guid.NewGuid(), new List<Guid> { Guid.NewGuid() });

        _mockEventRepository.Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain($"Event with ID {command.EventId} not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSignUpListNotFound_ShouldReturnFailureAndNotCommit()
    {
        var ev = CreateTestEvent();
        var unknownListId = Guid.NewGuid();

        var command = new ReorderSignUpItemsCommand(ev.Id, unknownListId, new List<Guid> { Guid.NewGuid() });

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain($"Sign-up list with ID {unknownListId} not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSetMismatch_ShouldReturnDomainFailureAndNotCommit()
    {
        var (ev, list, items) = CreateEventWithItems(3);
        var tampered = items.Take(2).Select(i => i.Id).Append(Guid.NewGuid()).ToList();

        var command = new ReorderSignUpItemsCommand(ev.Id, list.Id, tampered);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("do not match"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCountMismatch_ShouldReturnDomainFailure()
    {
        var (ev, list, items) = CreateEventWithItems(3);
        var tooFew = items.Take(2).Select(i => i.Id).ToList();

        var command = new ReorderSignUpItemsCommand(ev.Id, list.Id, tooFew);

        _mockEventRepository.Setup(x => x.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Expected 3 item IDs"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
