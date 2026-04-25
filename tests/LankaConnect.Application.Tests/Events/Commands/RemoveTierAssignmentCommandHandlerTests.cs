using FluentAssertions;
using LankaConnect.Application.Events.Commands.RemoveTierAssignment;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Slice 5 Chunk 8: DELETE /api/venue-layouts/{id}/tier-assignments/...
/// Missing tuple surfaces as 404 so the UI can detect stale state.
/// </summary>
public class RemoveTierAssignmentCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IVenueLayoutRepository> _mockLayoutRepo = new();
    private readonly Mock<IEventRepository> _mockEventRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly RemoveTierAssignmentCommandHandler _sut;

    public RemoveTierAssignmentCommandHandlerTests()
    {
        _sut = new RemoveTierAssignmentCommandHandler(
            _mockAuth.Object,
            _mockLayoutRepo.Object,
            _mockEventRepo.Object,
            _mockUow.Object,
            Mock.Of<ILogger<RemoveTierAssignmentCommandHandler>>());
    }

    private static VenueLayout CreateLayoutWithEvent(Guid eventId)
    {
        return VenueLayout.Create("Layout", LayoutType.Theater, Guid.NewGuid(), eventId).Value;
    }

    private static VenueZone AddZone(VenueLayout layout, string name = "Front")
    {
        return layout.AddZone(name, "#ff0000", 0).Value;
    }

    private static TicketTier CreateTier(Guid eventId)
    {
        var adultPrice = Money.Create(100m, Currency.USD).Value;
        return TicketTier.Create(eventId, "VIP", "VIP tier", adultPrice, null, null, 30, 10, 1).Value;
    }

    [Fact]
    public async Task Handle_Should_Forward_Forbidden_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Forbidden("denied"));

        var command = new RemoveTierAssignmentCommand(
            Guid.NewGuid(), 1u, Guid.NewGuid(), AssignableKind.Zone, Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Layout_Missing()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(CreateLayoutWithEvent(Guid.NewGuid())));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((VenueLayout?)null);

        var command = new RemoveTierAssignmentCommand(
            Guid.NewGuid(), 1u, Guid.NewGuid(), AssignableKind.Zone, Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_Stale_RowVersion()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var command = new RemoveTierAssignmentCommand(
            layout.Id, 99u, Guid.NewGuid(), AssignableKind.Zone, Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Tier_Missing()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTierWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((TicketTier?)null);

        var command = new RemoveTierAssignmentCommand(
            layout.Id, layout.RowVersion, Guid.NewGuid(), AssignableKind.Zone, Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Assignment_Does_Not_Exist()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout);
        var tier = CreateTier(eventId); // no assignments

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTierWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(tier);

        var command = new RemoveTierAssignmentCommand(
            layout.Id, layout.RowVersion, tier.Id, AssignableKind.Zone, zone.Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Remove_Existing_Assignment_On_Success()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout);
        var tier = CreateTier(eventId);
        tier.AssignToZone(zone.Id).IsSuccess.Should().BeTrue();
        tier.Assignments.Should().ContainSingle();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTierWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(tier);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new RemoveTierAssignmentCommand(
            layout.Id, layout.RowVersion, tier.Id, AssignableKind.Zone, zone.Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tier.Assignments.Should().BeEmpty();
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        // No xmin bump on the layout — tier owns its Assignments collection.
        _mockLayoutRepo.Verify(r => r.SetOriginalRowVersion(It.IsAny<VenueLayout>(), It.IsAny<uint>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_DbUpdateConcurrencyException()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout);
        var tier = CreateTier(eventId);
        tier.AssignToZone(zone.Id);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTierWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(tier);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("xmin mismatch"));

        var command = new RemoveTierAssignmentCommand(
            layout.Id, layout.RowVersion, tier.Id, AssignableKind.Zone, zone.Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
    }
}
