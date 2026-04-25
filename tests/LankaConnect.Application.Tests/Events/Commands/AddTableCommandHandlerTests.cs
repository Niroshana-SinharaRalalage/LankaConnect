using FluentAssertions;
using LankaConnect.Application.Events.Commands.AddTable;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Slice 5 Chunk 6: POST /api/venue-layouts/{id}/tables.
/// AddTable auto-generates seats. Guard is NOT invoked because adding a new
/// table does not mutate pre-existing seats. 409 on stale If-Match.
/// </summary>
public class AddTableCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IVenueLayoutRepository> _mockRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly AddTableCommandHandler _sut;

    public AddTableCommandHandlerTests()
    {
        _sut = new AddTableCommandHandler(
            _mockAuth.Object,
            _mockRepo.Object,
            _mockUow.Object,
            Mock.Of<ILogger<AddTableCommandHandler>>());
    }

    private static VenueLayout CreateLayout() =>
        VenueLayout.Create("Layout", LayoutType.Banquet, Guid.NewGuid()).Value;

    [Fact]
    public async Task Handle_Should_Forward_Forbidden_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Forbidden("denied"));

        var command = new AddTableCommand(Guid.NewGuid(), 1u, "T1", TableShape.Round, 8, 0, null, null, 0);

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

        var command = new AddTableCommand(layout.Id, 1u, "T1", TableShape.Round, 8, 0, null, null, 0);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Generate_RoundTable_With_Seats()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new AddTableCommand(layout.Id, 7u, "T1", TableShape.Round, 8, 0, null, null, 0);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Tables.Should().HaveCount(1);
        layout.Tables.Single().Seats.Should().HaveCount(8);
        _mockRepo.Verify(r => r.SetOriginalRowVersion(layout, 7u), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Generate_SquareTable_With_Seats()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new AddTableCommand(layout.Id, 1u, "HeadTable", TableShape.Square, 8, 0, null, null, 0);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Tables.Single().Seats.Should().HaveCount(8);
    }

    [Fact]
    public async Task Handle_Should_Fail_On_Domain_Validation()
    {
        var layout = CreateLayout();
        // Seed a duplicate label so the AddTable call fails the domain invariant.
        layout.AddTable("DUP", TableShape.Round, 6, 0);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);

        var command = new AddTableCommand(layout.Id, 1u, "DUP", TableShape.Round, 8, 1, null, null, 0);

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

        var command = new AddTableCommand(layout.Id, 9u, "T1", TableShape.Round, 8, 0, null, null, 0);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
    }
}
