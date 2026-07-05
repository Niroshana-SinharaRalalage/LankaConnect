using FluentAssertions;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Commands.CreateLayoutFromTemplate;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Slice 8 S8.10 — apply-template-to-event handler. Wires the validation
/// gates (caller owns template + caller owns event) on top of the domain
/// <see cref="VenueLayout.CloneFromTemplate"/> factory; correctness of the
/// structural clone itself is covered by the domain CloneFromTemplate tests.
/// </summary>
public class CreateLayoutFromTemplateCommandHandlerTests
{
    private readonly Mock<IVenueLayoutRepository> _mockLayoutRepo = new();
    private readonly Mock<IEventRepository> _mockEventRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<ILayoutMetrics> _mockMetrics = new();
    private readonly CreateLayoutFromTemplateCommandHandler _sut;

    public CreateLayoutFromTemplateCommandHandlerTests()
    {
        _sut = new CreateLayoutFromTemplateCommandHandler(
            _mockLayoutRepo.Object,
            _mockEventRepo.Object,
            _mockUow.Object,
            _mockMetrics.Object,
            Mock.Of<ILogger<CreateLayoutFromTemplateCommandHandler>>());
    }

    private static VenueLayout CreateTemplate(Guid ownerId)
    {
        var template = VenueLayout.Create(
            "Theater Template", LayoutType.Theater, ownerId,
            eventId: null, isTemplate: true).Value;
        var zone = template.AddZone("Front", "#fff", 0).Value;
        template.GenerateTheaterSeats(zone.Id, 1, 2);
        return template;
    }

    private static Event CreateEvent(Guid organizerId)
    {
        // Use the value-object factories that the rest of the test suite uses
        // (see CreateNewsletterCommandHandlerTests for the same pattern).
        return Event.Create(
            EventTitle.Create("Apply-template smoke event").Value,
            EventDescription.Create("Created in test fixture").Value,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2),
            organizerId,
            capacity: 100).Value;
    }

    [Fact]
    public async Task Handle_Should_Reject_Empty_SourceTemplateId()
    {
        var cmd = new CreateLayoutFromTemplateCommand(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Applied");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Reject_Empty_EventId()
    {
        var cmd = new CreateLayoutFromTemplateCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "Applied");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Source_Missing()
    {
        var sourceId = Guid.NewGuid();
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(sourceId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((VenueLayout?)null);

        var cmd = new CreateLayoutFromTemplateCommand(
            sourceId, Guid.NewGuid(), Guid.NewGuid(), "Applied");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Reject_Source_That_Is_Not_A_Template()
    {
        var ownerId = Guid.NewGuid();
        var notATemplate = VenueLayout.Create(
            "Already attached", LayoutType.Theater, ownerId,
            eventId: Guid.NewGuid()).Value;

        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(notATemplate.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(notATemplate);

        var cmd = new CreateLayoutFromTemplateCommand(
            notATemplate.Id, ownerId, Guid.NewGuid(), "Applied");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Validation);
        result.Error.ToLowerInvariant().Should().Contain("template");
    }

    [Fact]
    public async Task Handle_Should_Forbid_When_Caller_Does_Not_Own_Template()
    {
        var realOwner = Guid.NewGuid();
        var thief = Guid.NewGuid();
        var template = CreateTemplate(realOwner);

        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(template.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(template);

        var cmd = new CreateLayoutFromTemplateCommand(
            template.Id, thief, Guid.NewGuid(), "Applied");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Target_Event_Missing()
    {
        var userId = Guid.NewGuid();
        var template = CreateTemplate(userId);
        var eventId = Guid.NewGuid();

        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(template.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(template);
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, false, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Event?)null);

        var cmd = new CreateLayoutFromTemplateCommand(
            template.Id, userId, eventId, "Applied");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Forbid_When_Caller_Is_Not_Event_Organizer()
    {
        var userId = Guid.NewGuid();
        var template = CreateTemplate(userId);
        var otherOrganizerEvent = CreateEvent(Guid.NewGuid()); // different organizer

        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(template.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(template);
        _mockEventRepo.Setup(r => r.GetByIdAsync(otherOrganizerEvent.Id, false, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(otherOrganizerEvent);

        var cmd = new CreateLayoutFromTemplateCommand(
            template.Id, userId, otherOrganizerEvent.Id, "Applied");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
    }

    [Fact]
    public async Task Handle_Should_Persist_Cloned_Layout_On_Success()
    {
        var userId = Guid.NewGuid();
        var template = CreateTemplate(userId);
        var ev = CreateEvent(userId);

        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(template.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(template);
        _mockEventRepo.Setup(r => r.GetByIdAsync(ev.Id, false, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(ev);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        VenueLayout? captured = null;
        _mockLayoutRepo.Setup(r => r.AddAsync(It.IsAny<VenueLayout>(), It.IsAny<CancellationToken>()))
                       .Callback((VenueLayout vl, CancellationToken _) => captured = vl)
                       .Returns(Task.CompletedTask);

        var cmd = new CreateLayoutFromTemplateCommand(
            template.Id, userId, ev.Id, "Applied to event");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.IsTemplate.Should().BeFalse();
        captured.EventId.Should().Be(ev.Id);
        captured.CreatedByUserId.Should().Be(userId);
        captured.Name.Should().Be("Applied to event");
        captured.Id.Should().NotBe(template.Id);
        captured.Zones.Should().HaveCount(template.Zones.Count);
        captured.Zones[0].Seats.Should().HaveCount(template.Zones[0].Seats.Count);

        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockMetrics.Verify(m => m.LayoutCreated(LayoutType.Theater, false), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Default_LayoutName_To_Source_Name_When_Null_Or_Whitespace()
    {
        var userId = Guid.NewGuid();
        var template = CreateTemplate(userId);
        var ev = CreateEvent(userId);

        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(template.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(template);
        _mockEventRepo.Setup(r => r.GetByIdAsync(ev.Id, false, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(ev);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        VenueLayout? captured = null;
        _mockLayoutRepo.Setup(r => r.AddAsync(It.IsAny<VenueLayout>(), It.IsAny<CancellationToken>()))
                       .Callback((VenueLayout vl, CancellationToken _) => captured = vl)
                       .Returns(Task.CompletedTask);

        var cmd = new CreateLayoutFromTemplateCommand(
            template.Id, userId, ev.Id, LayoutName: null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured!.Name.Should().Be(template.Name); // defaulted to source name
    }
}
