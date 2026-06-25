using System.Reflection;
using FluentAssertions;
using LankaConnect.Application.Events.Commands.DeleteLayout;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Slice 5 Chunk 9: DELETE /api/venue-layouts/{id}. Covers the four safety gates
/// (authorize → concurrency → structural guard → event detach) plus the persistence
/// concurrency path.
/// </summary>
public class DeleteLayoutCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IStructuralEditGuard> _mockGuard = new();
    private readonly Mock<IVenueLayoutRepository> _mockLayoutRepo = new();
    private readonly Mock<IEventRepository> _mockEventRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<ILayoutMetrics> _mockMetrics = new();
    private readonly DeleteLayoutCommandHandler _sut;

    public DeleteLayoutCommandHandlerTests()
    {
        _sut = new DeleteLayoutCommandHandler(
            _mockAuth.Object,
            _mockGuard.Object,
            _mockLayoutRepo.Object,
            _mockEventRepo.Object,
            _mockUow.Object,
            _mockMetrics.Object,
            Mock.Of<ILogger<DeleteLayoutCommandHandler>>());
    }

    private static VenueLayout CreateLayout(Guid? eventId = null)
    {
        return VenueLayout.Create("Layout", LayoutType.Theater, Guid.NewGuid(), eventId).Value;
    }

    private static Event CreateEvent()
    {
        var title = EventTitle.Create("Chunk 9 Event").Value;
        var description = EventDescription.Create("Chunk 9 Event").Value;
        var address = Address.Create("1 Main", "Houston", "TX", "77001", "USA").Value;
        var location = EventLocation.Create(address).Value;
        var price = Money.Create(100m, Currency.USD).Value;
        return Event.Create(
            title,
            description,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2),
            Guid.NewGuid(),
            100,
            location,
            ticketPrice: price).Value;
    }

    private static void SetBackingField<T>(T obj, string propertyName, object? value)
    {
        var backingFieldName = $"<{propertyName}>k__BackingField";
        var type = typeof(T);
        FieldInfo? field = null;
        while (type != null)
        {
            field = type.GetField(backingFieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) break;
            type = type.BaseType;
        }
        field!.SetValue(obj, value);
    }

    private static void AttachLayoutToEvent(Event @event, Guid layoutId)
    {
        SetBackingField(@event, nameof(Event.SeatingMode), SeatingMode.AssignedSeating);
        SetBackingField(@event, nameof(Event.VenueLayoutId), (Guid?)layoutId);
    }

    private static void AddConfirmedRegistration(Event @event)
    {
        var registration = Registration.Create(@event.Id, Guid.NewGuid(), 1).Value;
        var field = typeof(Event).GetField("_registrations",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var list = (List<Registration>)field.GetValue(@event)!;
        list.Add(registration);
    }

    [Fact]
    public async Task Handle_Should_Forward_Forbidden_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Forbidden("denied"));

        var command = new DeleteLayoutCommand(Guid.NewGuid(), 1u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
        _mockLayoutRepo.Verify(r => r.Remove(It.IsAny<VenueLayout>()), Times.Never);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            It.IsAny<Guid>(), StructuralEditRejectionReason.AuthFailed), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Layout_Missing()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(CreateLayout()));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((VenueLayout?)null);

        var command = new DeleteLayoutCommand(Guid.NewGuid(), 1u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        _mockLayoutRepo.Verify(r => r.Remove(It.IsAny<VenueLayout>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_Stale_RowVersion()
    {
        var layout = CreateLayout();
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var command = new DeleteLayoutCommand(layout.Id, 99u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        _mockGuard.Verify(g => g.CheckSeatsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockLayoutRepo.Verify(r => r.Remove(It.IsAny<VenueLayout>()), Times.Never);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.ConcurrencyConflict), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_StructuralEditRejected_When_Guard_Fails()
    {
        var layout = CreateLayout();
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.StructuralEditRejected("seats reserved"));

        var command = new DeleteLayoutCommand(layout.Id, layout.RowVersion);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
        _mockEventRepo.Verify(r => r.GetByIdAsync(
            It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockLayoutRepo.Verify(r => r.Remove(It.IsAny<VenueLayout>()), Times.Never);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.SeatsReserved), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Delete_Template_Layout_Without_Loading_Event()
    {
        // Template: layout.EventId is null, so the handler must skip the event-detach branch.
        var layout = CreateLayout(eventId: null);
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteLayoutCommand(layout.Id, layout.RowVersion);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mockEventRepo.Verify(r => r.GetByIdAsync(
            It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockLayoutRepo.Verify(r => r.SetOriginalRowVersion(layout, layout.RowVersion), Times.Once);
        _mockLayoutRepo.Verify(r => r.Remove(layout), Times.Once);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Detach_Event_And_Delete_When_Event_Has_No_Registrations()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayout(eventId);
        var @event = CreateEvent();
        SetBackingField(@event, "Id", eventId);
        AttachLayoutToEvent(@event, layout.Id);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, true, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(@event);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteLayoutCommand(layout.Id, layout.RowVersion);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        @event.SeatingMode.Should().Be(SeatingMode.GeneralAdmission);
        @event.VenueLayoutId.Should().BeNull();
        _mockLayoutRepo.Verify(r => r.SetOriginalRowVersion(layout, layout.RowVersion), Times.Once);
        _mockLayoutRepo.Verify(r => r.Remove(layout), Times.Once);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_StructuralEditRejected_When_Event_Has_Registrations()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayout(eventId);
        var @event = CreateEvent();
        SetBackingField(@event, "Id", eventId);
        AttachLayoutToEvent(@event, layout.Id);
        AddConfirmedRegistration(@event);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, true, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(@event);

        var command = new DeleteLayoutCommand(layout.Id, layout.RowVersion);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
        @event.SeatingMode.Should().Be(SeatingMode.AssignedSeating);  // unchanged
        _mockLayoutRepo.Verify(r => r.Remove(It.IsAny<VenueLayout>()), Times.Never);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Proceed_When_Owning_Event_Is_Missing()
    {
        // Layout references an event that no longer exists — log warning, continue the delete.
        var eventId = Guid.NewGuid();
        var layout = CreateLayout(eventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, true, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Event?)null);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteLayoutCommand(layout.Id, layout.RowVersion);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mockLayoutRepo.Verify(r => r.Remove(layout), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_DbUpdateConcurrencyException()
    {
        var layout = CreateLayout();
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("xmin stale"));

        var command = new DeleteLayoutCommand(layout.Id, layout.RowVersion);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.ConcurrencyConflict), Times.Once);
    }
}
