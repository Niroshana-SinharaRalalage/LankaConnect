using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Commands.AssignTier;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Slice 5 Chunk 8: POST /api/venue-layouts/{id}/tier-assignments. Two-branch
/// authorization, in-memory RowVersion concurrency check, polymorphic target
/// validation, idempotent assign. No xmin bump (tier owns the assignment).
/// </summary>
public class AssignTierCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IVenueLayoutRepository> _mockLayoutRepo = new();
    private readonly Mock<IEventRepository> _mockEventRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly AssignTierCommandHandler _sut;

    public AssignTierCommandHandlerTests()
    {
        _sut = new AssignTierCommandHandler(
            _mockAuth.Object,
            _mockLayoutRepo.Object,
            _mockEventRepo.Object,
            _mockUow.Object,
            Mock.Of<ILogger<AssignTierCommandHandler>>());
    }

    private static VenueLayout CreateLayoutWithEvent(Guid eventId)
    {
        return VenueLayout.Create("Layout", LayoutType.Theater, Guid.NewGuid(), eventId).Value;
    }

    private static VenueZone AddZone(VenueLayout layout, string name = "Front")
    {
        return layout.AddZone(name, "#ff0000", 0).Value;
    }

    private static VenueTable AddTable(VenueLayout layout, string label = "T1")
    {
        return layout.AddTable(label, TableShape.Round, 8, 0).Value;
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

        var command = new AssignTierCommand(
            Guid.NewGuid(), 1u, Guid.NewGuid(), AssignableKind.Zone, Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Layout_Missing()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((VenueLayout?)null);

        var command = new AssignTierCommand(
            layout.Id, 1u, Guid.NewGuid(), AssignableKind.Zone, Guid.NewGuid());

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

        // layout.RowVersion default is 0 — stale client sends 99
        var command = new AssignTierCommand(
            layout.Id, 99u, Guid.NewGuid(), AssignableKind.Zone, Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Zone_Not_On_Layout()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var command = new AssignTierCommand(
            layout.Id, layout.RowVersion, Guid.NewGuid(), AssignableKind.Zone, Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Zone");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Table_Not_On_Layout()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var command = new AssignTierCommand(
            layout.Id, layout.RowVersion, Guid.NewGuid(), AssignableKind.Table, Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Table");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Tier_Missing()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTierWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((TicketTier?)null);

        var command = new AssignTierCommand(
            layout.Id, layout.RowVersion, Guid.NewGuid(), AssignableKind.Zone, zone.Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("tier");
    }

    [Fact]
    public async Task Handle_Should_Return_Validation_When_Tier_Belongs_To_Different_Event()
    {
        var eventId = Guid.NewGuid();
        var otherEventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout);
        var foreignTier = CreateTier(otherEventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTierWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(foreignTier);

        var command = new AssignTierCommand(
            layout.Id, layout.RowVersion, foreignTier.Id, AssignableKind.Zone, zone.Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Validation);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Assign_Zone_On_Success()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout);
        var tier = CreateTier(eventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTierWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(tier);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new AssignTierCommand(
            layout.Id, layout.RowVersion, tier.Id, AssignableKind.Zone, zone.Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tier.Assignments.Should().ContainSingle()
            .Which.AssignableId.Should().Be(zone.Id);
        tier.Assignments.Single().AssignableKind.Should().Be(AssignableKind.Zone);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Critical: we do NOT bump the layout's xmin — tier assignments don't mutate the layout row.
        _mockLayoutRepo.Verify(r => r.SetOriginalRowVersion(It.IsAny<VenueLayout>(), It.IsAny<uint>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Assign_Table_On_Success()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var table = AddTable(layout);
        var tier = CreateTier(eventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTierWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(tier);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new AssignTierCommand(
            layout.Id, layout.RowVersion, tier.Id, AssignableKind.Table, table.Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tier.Assignments.Should().ContainSingle()
            .Which.AssignableId.Should().Be(table.Id);
        tier.Assignments.Single().AssignableKind.Should().Be(AssignableKind.Table);
    }

    [Fact]
    public async Task Handle_Should_Be_Idempotent_On_Reassignment()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout);
        var tier = CreateTier(eventId);

        // Pre-seed the assignment so the second call is a no-op.
        tier.AssignToZone(zone.Id).IsSuccess.Should().BeTrue();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTierWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(tier);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var command = new AssignTierCommand(
            layout.Id, layout.RowVersion, tier.Id, AssignableKind.Zone, zone.Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tier.Assignments.Should().ContainSingle(); // still only one
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_DbUpdateConcurrencyException()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout);
        var tier = CreateTier(eventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTierWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(tier);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("xmin mismatch"));

        var command = new AssignTierCommand(
            layout.Id, layout.RowVersion, tier.Id, AssignableKind.Zone, zone.Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
    }
}
