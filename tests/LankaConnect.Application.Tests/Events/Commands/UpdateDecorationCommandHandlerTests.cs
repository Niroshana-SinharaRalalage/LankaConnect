using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateDecoration;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Slice 5 Chunk 7: PATCH /api/venue-layouts/{id}/decorations/{decorationId}.
/// Optional-fields patch semantics. No structural guard — decorations have no seats.
/// </summary>
public class UpdateDecorationCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IVenueLayoutRepository> _mockRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly UpdateDecorationCommandHandler _sut;

    public UpdateDecorationCommandHandlerTests()
    {
        _sut = new UpdateDecorationCommandHandler(
            _mockAuth.Object,
            _mockRepo.Object,
            _mockUow.Object,
            Mock.Of<ILogger<UpdateDecorationCommandHandler>>());
    }

    private static (VenueLayout layout, VenueDecoration decoration) CreateLayoutWithDecoration()
    {
        var layout = VenueLayout.Create("Layout", LayoutType.Theater, Guid.NewGuid()).Value;
        var decoration = layout.AddDecoration(DecorationKind.Stage, "Main Stage", 0).Value;
        return (layout, decoration);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_All_Fields_Null()
    {
        var command = new UpdateDecorationCommand(
            Guid.NewGuid(), Guid.NewGuid(), 1u,
            null, null, false, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Validation);
        _mockAuth.Verify(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Forward_Forbidden_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Forbidden("denied"));

        var command = new UpdateDecorationCommand(
            Guid.NewGuid(), Guid.NewGuid(), 1u,
            null, "Renamed", false, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Decoration_Missing()
    {
        var (layout, _) = CreateLayoutWithDecoration();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);

        var command = new UpdateDecorationCommand(
            layout.Id, Guid.NewGuid(), 1u,
            null, "Renamed", false, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Update_Label_And_Call_Repo()
    {
        var (layout, decoration) = CreateLayoutWithDecoration();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateDecorationCommand(
            layout.Id, decoration.Id, 5u,
            null, "Renamed Stage", false, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        decoration.Label.Should().Be("Renamed Stage");
        _mockRepo.Verify(r => r.SetOriginalRowVersion(layout, 5u), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Clear_Label_When_ClearLabel_True()
    {
        var (layout, decoration) = CreateLayoutWithDecoration();
        decoration.Label.Should().Be("Main Stage");

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // ClearLabel=true is only allowed for non-Text kinds; Stage is fine.
        var command = new UpdateDecorationCommand(
            layout.Id, decoration.Id, 1u,
            null, null, ClearLabel: true, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        decoration.Label.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Clearing_Label_On_Text_Kind()
    {
        var layout = VenueLayout.Create("Layout", LayoutType.Theater, Guid.NewGuid()).Value;
        var decoration = layout.AddDecoration(DecorationKind.Text, "Hello", 0).Value;

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);

        // Clearing label on a Text decoration is a domain invariant violation.
        var command = new UpdateDecorationCommand(
            layout.Id, decoration.Id, 1u,
            null, null, ClearLabel: true, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        decoration.Label.Should().Be("Hello");
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Update_Geometry_And_Properties()
    {
        var (layout, decoration) = CreateLayoutWithDecoration();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var newGeometry = "{\"x\":10,\"y\":20,\"width\":150,\"height\":30}";
        var newProperties = "{\"color\":\"#ff00ff\"}";
        var command = new UpdateDecorationCommand(
            layout.Id, decoration.Id, 3u,
            null, null, false, null, newGeometry, newProperties);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        decoration.Geometry.Should().Be(newGeometry);
        decoration.Properties.Should().Be(newProperties);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_DbUpdateConcurrencyException()
    {
        var (layout, decoration) = CreateLayoutWithDecoration();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("xmin mismatch"));

        var command = new UpdateDecorationCommand(
            layout.Id, decoration.Id, 9u,
            null, "Stale", false, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
    }
}
