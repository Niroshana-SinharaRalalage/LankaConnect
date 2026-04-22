using FluentAssertions;
using LankaConnect.Application.Events.Commands.CreateLayoutFromPreset;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Presets;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Slice 6 Chunk S6.3: POST /api/venue-layouts/from-preset.
/// Builds a preset layout, persists it, and emits layout.preset_selected +
/// layout.created (from_preset=true). Event-attachment requires event ownership.
/// </summary>
public class CreateLayoutFromPresetCommandHandlerTests
{
    private readonly Mock<IVenueLayoutRepository> _mockRepo = new();
    private readonly Mock<IEventRepository> _mockEventRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<ILayoutMetrics> _mockMetrics = new();
    private readonly CreateLayoutFromPresetCommandHandler _sut;

    public CreateLayoutFromPresetCommandHandlerTests()
    {
        _sut = new CreateLayoutFromPresetCommandHandler(
            _mockRepo.Object,
            _mockEventRepo.Object,
            _mockUow.Object,
            _mockMetrics.Object,
            Mock.Of<ILogger<CreateLayoutFromPresetCommandHandler>>());
    }

    private static Event CreateEventOwnedBy(Guid organizerId)
    {
        var title = EventTitle.Create("From-Preset Test Event").Value;
        var description = EventDescription.Create("From-Preset Test").Value;
        var address = Address.Create("1 Main", "Houston", "TX", "77001", "USA").Value;
        var location = EventLocation.Create(address).Value;
        var price = Money.Create(50m, Currency.USD).Value;
        return Event.Create(
            title,
            description,
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow.AddDays(14).AddHours(2),
            organizerId,
            100,
            location,
            ticketPrice: price).Value;
    }

    [Fact]
    public async Task Handle_Should_Fail_When_PresetId_Is_Empty()
    {
        var cmd = new CreateLayoutFromPresetCommand(PresetId: "", CreatedByUserId: Guid.NewGuid(), EventId: null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<VenueLayout>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_UserId_Is_Empty()
    {
        var cmd = new CreateLayoutFromPresetCommand(
            PresetId: LayoutPresets.TheaterClassicId,
            CreatedByUserId: Guid.Empty,
            EventId: null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_For_Unknown_Preset()
    {
        var cmd = new CreateLayoutFromPresetCommand("not-a-real-preset", Guid.NewGuid(), null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        _mockMetrics.Verify(m => m.PresetSelected(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Template_Path_Should_Persist_And_Emit_Both_Metrics()
    {
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var cmd = new CreateLayoutFromPresetCommand(
            LayoutPresets.TheaterClassicId,
            Guid.NewGuid(),
            EventId: null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsTemplate.Should().BeTrue();
        result.Value.EventId.Should().BeNull();
        result.Value.LayoutType.Should().Be(LayoutType.Theater.ToString());
        result.Value.TotalCapacity.Should().Be(200);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<VenueLayout>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockMetrics.Verify(m => m.PresetSelected(LayoutPresets.TheaterClassicId), Times.Once);
        _mockMetrics.Verify(m => m.LayoutCreated(LayoutType.Theater, true), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Event_Does_Not_Exist()
    {
        _mockEventRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var cmd = new CreateLayoutFromPresetCommand(
            LayoutPresets.BanquetRound8Id,
            Guid.NewGuid(),
            EventId: Guid.NewGuid());

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<VenueLayout>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Forbidden_When_Caller_Does_Not_Own_Event()
    {
        var otherOwnerId = Guid.NewGuid();
        var @event = CreateEventOwnedBy(otherOwnerId);

        _mockEventRepo
            .Setup(r => r.GetByIdAsync(@event.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var cmd = new CreateLayoutFromPresetCommand(
            LayoutPresets.BanquetRound8Id,
            CreatedByUserId: Guid.NewGuid(),
            EventId: @event.Id);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<VenueLayout>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Event_Attached_Should_Persist_And_Emit_Both_Metrics()
    {
        var userId = Guid.NewGuid();
        var @event = CreateEventOwnedBy(userId);

        _mockEventRepo
            .Setup(r => r.GetByIdAsync(@event.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var cmd = new CreateLayoutFromPresetCommand(
            LayoutPresets.BanquetRound8Id,
            userId,
            EventId: @event.Id);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EventId.Should().Be(@event.Id);
        result.Value.IsTemplate.Should().BeFalse();
        result.Value.Tables.Should().HaveCount(15);
        result.Value.TotalCapacity.Should().Be(120);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<VenueLayout>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockMetrics.Verify(m => m.PresetSelected(LayoutPresets.BanquetRound8Id), Times.Once);
        _mockMetrics.Verify(m => m.LayoutCreated(LayoutType.Banquet, true), Times.Once);
    }
}
