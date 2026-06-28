using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateLayout;
using LankaConnect.Application.Events.Services;
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
/// Slice 5 Chunk 4: TDD for PUT /api/venue-layouts/{id}.
/// The handler owns: authorize-or-forward-error, apply name/canvas changes, set the
/// RowVersion OriginalValue for optimistic concurrency, commit, map
/// DbUpdateConcurrencyException → HTTP 409.
/// </summary>
public class UpdateLayoutCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IVenueLayoutRepository> _mockRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly UpdateLayoutCommandHandler _sut;

    public UpdateLayoutCommandHandlerTests()
    {
        _sut = new UpdateLayoutCommandHandler(
            _mockAuth.Object,
            _mockRepo.Object,
            _mockUow.Object,
            Mock.Of<ILogger<UpdateLayoutCommandHandler>>());
    }

    private static VenueLayout CreateLayout() =>
        VenueLayout.Create("Original", LayoutType.Theater, Guid.NewGuid()).Value;

    [Fact]
    public async Task Handle_Should_Fail_When_Both_Name_And_Canvas_Are_Null()
    {
        var command = new UpdateLayoutCommand(Guid.NewGuid(), 1u, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Validation);
        _mockAuth.Verify(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Forward_Forbidden_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Forbidden("Authentication required"));

        var command = new UpdateLayoutCommand(Guid.NewGuid(), 1u, "New Name", null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Forward_NotFound_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.NotFound("Venue layout not found"));

        var command = new UpdateLayoutCommand(Guid.NewGuid(), 1u, "New Name", null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Update_Name_And_Commit_On_Happy_Path()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateLayoutCommand(layout.Id, 42u, "Renamed Layout", null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Name.Should().Be("Renamed Layout");
        _mockRepo.Verify(r => r.SetOriginalRowVersion(layout, 42u), Times.Once);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Update_Canvas_And_Commit_On_Happy_Path()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateLayoutCommand(
            layout.Id,
            7u,
            Name: null,
            Canvas: new UpdateLayoutCanvasRequest(1600, 1000, 1.25, "#abcdef"));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Canvas.Width.Should().Be(1600);
        layout.Canvas.Height.Should().Be(1000);
        layout.Canvas.Scale.Should().Be(1.25);
        layout.Canvas.BackgroundColor.Should().Be("#abcdef");
        _mockRepo.Verify(r => r.SetOriginalRowVersion(layout, 7u), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Validation_Error_When_Name_Is_Empty()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));

        var command = new UpdateLayoutCommand(layout.Id, 1u, "   ", null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Validation);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Validation_Error_When_Canvas_Invalid()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));

        var command = new UpdateLayoutCommand(
            layout.Id,
            1u,
            Name: null,
            Canvas: new UpdateLayoutCanvasRequest(50, 800, 1.0, "#ffffff")); // width below min

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Validation);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_When_DbUpdateConcurrencyException_Thrown()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("xmin mismatch"));

        var command = new UpdateLayoutCommand(layout.Id, 99u, "Another Name", null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        result.Error.Should().Contain("modified");
    }

    [Fact]
    public async Task Handle_Should_Rethrow_Non_Concurrency_Exceptions()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("db down"));

        var command = new UpdateLayoutCommand(layout.Id, 1u, "Boom", null);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
