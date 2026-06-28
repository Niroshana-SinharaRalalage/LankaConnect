using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Commands.AddDecoration;
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
/// Slice 5 Chunk 7: POST /api/venue-layouts/{id}/decorations.
/// No structural guard — decorations have no seats. 409 on stale If-Match.
/// </summary>
public class AddDecorationCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IVenueLayoutRepository> _mockRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly AddDecorationCommandHandler _sut;

    public AddDecorationCommandHandlerTests()
    {
        _sut = new AddDecorationCommandHandler(
            _mockAuth.Object,
            _mockRepo.Object,
            _mockUow.Object,
            Mock.Of<ILogger<AddDecorationCommandHandler>>());
    }

    private static VenueLayout CreateLayout() =>
        VenueLayout.Create("Layout", LayoutType.Theater, Guid.NewGuid()).Value;

    [Fact]
    public async Task Handle_Should_Forward_Forbidden_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Forbidden("denied"));

        var command = new AddDecorationCommand(
            Guid.NewGuid(), 1u, DecorationKind.Stage, "Main Stage", 0, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Layout_Missing()
    {
        var layout = CreateLayout();
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((VenueLayout?)null);

        var command = new AddDecorationCommand(
            layout.Id, 1u, DecorationKind.Stage, "Main Stage", 0, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Add_Decoration_On_Success()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new AddDecorationCommand(
            layout.Id, 7u, DecorationKind.Stage, "Main Stage", 0,
            "{\"x\":0,\"y\":0,\"width\":100,\"height\":20}", null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Decorations.Should().HaveCount(1);
        layout.Decorations.Single().Kind.Should().Be(DecorationKind.Stage);
        layout.Decorations.Single().Label.Should().Be("Main Stage");
        _mockRepo.Verify(r => r.SetOriginalRowVersion(layout, 7u), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Fail_On_Domain_Validation_Text_Without_Label()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);

        var command = new AddDecorationCommand(
            layout.Id, 1u, DecorationKind.Text, null, 0, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_DbUpdateConcurrencyException()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("xmin mismatch"));

        var command = new AddDecorationCommand(
            layout.Id, 9u, DecorationKind.Stage, "Main Stage", 0, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
    }
}
