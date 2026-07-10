using FluentAssertions;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Commands.SaveLayoutAsTemplate;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Slice 8 S8.9b: <see cref="SaveLayoutAsTemplateCommandHandler"/>. Verifies the
/// integration between authorization, source loading, the domain
/// <see cref="VenueLayout.CloneAsTemplate"/> factory, persistence, and metric
/// emission. Domain-level seat-fidelity correctness lives in
/// <c>VenueLayoutCloneAsTemplateTests</c>; this file guards the handler wiring.
/// </summary>
public class SaveLayoutAsTemplateCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IVenueLayoutRepository> _mockLayoutRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<ILayoutMetrics> _mockMetrics = new();
    private readonly SaveLayoutAsTemplateCommandHandler _sut;

    public SaveLayoutAsTemplateCommandHandlerTests()
    {
        _sut = new SaveLayoutAsTemplateCommandHandler(
            _mockAuth.Object,
            _mockLayoutRepo.Object,
            _mockUow.Object,
            _mockMetrics.Object,
            Mock.Of<ILogger<SaveLayoutAsTemplateCommandHandler>>());
    }

    private static VenueLayout BuildSourceLayout(Guid sourceOwnerId, Guid eventId)
    {
        var layout = VenueLayout.Create("Source", LayoutType.Theater, sourceOwnerId, eventId).Value;
        var zone = layout.AddZone("Front", "#ff0000", 0).Value;
        layout.GenerateTheaterSeats(zone.Id, rows: 1, seatsPerRow: 2);
        return layout;
    }

    [Fact]
    public async Task Handle_Should_Forward_Forbidden_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Forbidden("denied"));

        var command = new SaveLayoutAsTemplateCommand(Guid.NewGuid(), Guid.NewGuid(), "My Template");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_NewOwnerUserId_Empty()
    {
        var command = new SaveLayoutAsTemplateCommand(Guid.NewGuid(), Guid.Empty, "My Template");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        // Should never reach auth on validation reject.
        _mockAuth.Verify(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_TemplateName_Whitespace()
    {
        var command = new SaveLayoutAsTemplateCommand(Guid.NewGuid(), Guid.NewGuid(), "   ");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _mockAuth.Verify(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Source_Missing()
    {
        var sourceId = Guid.NewGuid();
        var someLayout = VenueLayout.Create("X", LayoutType.Theater, Guid.NewGuid()).Value;

        _mockAuth.Setup(a => a.AuthorizeAsync(sourceId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(someLayout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(sourceId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((VenueLayout?)null);

        var command = new SaveLayoutAsTemplateCommand(sourceId, Guid.NewGuid(), "Copy");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Persist_Cloned_Layout_And_Return_Dto_On_Success()
    {
        var sourceOwnerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var source = BuildSourceLayout(sourceOwnerId, eventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(source.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(source));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(source.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(source);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        VenueLayout? captured = null;
        _mockLayoutRepo.Setup(r => r.AddAsync(It.IsAny<VenueLayout>(), It.IsAny<CancellationToken>()))
                       .Callback((VenueLayout vl, CancellationToken _) => captured = vl)
                       .Returns(Task.CompletedTask);

        var command = new SaveLayoutAsTemplateCommand(source.Id, newOwnerId, "Source (Template)");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Source (Template)");
        result.Value.IsTemplate.Should().BeTrue();
        result.Value.EventId.Should().BeNull();
        result.Value.CreatedByUserId.Should().Be(newOwnerId);

        captured.Should().NotBeNull();
        captured!.IsTemplate.Should().BeTrue();
        captured.EventId.Should().BeNull();
        captured.CreatedByUserId.Should().Be(newOwnerId);
        captured.Id.Should().NotBe(source.Id);
        captured.Zones.Should().HaveCount(source.Zones.Count);
        captured.Zones[0].Seats.Should().HaveCount(source.Zones[0].Seats.Count);

        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Emit_LayoutCreated_Metric_On_Success()
    {
        var source = BuildSourceLayout(Guid.NewGuid(), Guid.NewGuid());
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(source));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(source);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new SaveLayoutAsTemplateCommand(source.Id, Guid.NewGuid(), "Copy");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mockMetrics.Verify(m => m.LayoutCreated(LayoutType.Theater, false), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Not_Emit_Metric_On_Persistence_Failure()
    {
        var source = BuildSourceLayout(Guid.NewGuid(), Guid.NewGuid());
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(source));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(source);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("db down"));

        var command = new SaveLayoutAsTemplateCommand(source.Id, Guid.NewGuid(), "Copy");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.Handle(command, CancellationToken.None));

        _mockMetrics.Verify(m => m.LayoutCreated(It.IsAny<LayoutType>(), It.IsAny<bool>()), Times.Never);
    }
}
