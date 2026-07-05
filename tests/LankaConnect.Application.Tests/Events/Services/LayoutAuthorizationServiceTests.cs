using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using FluentAssertions;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.Domain.Business.ValueObjects;
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

namespace LankaConnect.Application.Tests.Events.Services;

/// <summary>
/// Slice 5 Chunk 2: Two-branch authorization for VenueLayout.
/// Event-attached layouts → owner/co-organizer check via Event.IsOrganizer(userId).
/// Template layouts (EventId == null) → owner check via VenueLayout.CreatedByUserId.
/// Admin bypasses both branches.
/// Missing layout → NotFound. Unauthenticated user → Forbidden.
/// </summary>
public class LayoutAuthorizationServiceTests
{
    private readonly Mock<IVenueLayoutRepository> _mockLayoutRepo = new();
    private readonly Mock<IEventRepository> _mockEventRepo = new();
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly LayoutAuthorizationService _sut;

    public LayoutAuthorizationServiceTests()
    {
        _sut = new LayoutAuthorizationService(
            _mockLayoutRepo.Object,
            _mockEventRepo.Object,
            _mockCurrentUser.Object,
            Mock.Of<ILogger<LayoutAuthorizationService>>());
    }

    private static Event CreateEventForOrganizer(Guid organizerId)
    {
        var title = EventTitle.Create("Test").Value;
        var description = EventDescription.Create("Desc").Value;
        var start = DateTime.UtcNow.AddDays(5);
        var end = start.AddHours(2);
        var address = Address.Create("1 St", "LA", "CA", "90001", "USA").Value;
        var location = EventLocation.Create(address).Value;
        return Event.Create(title, description, start, end, organizerId, 100, location).Value;
    }

    private static VenueLayout CreateLayout(Guid ownerUserId, Guid? eventId, bool isTemplate)
    {
        return VenueLayout.Create(
            name: "Layout 1",
            layoutType: LayoutType.Theater,
            createdByUserId: ownerUserId,
            eventId: eventId,
            isTemplate: isTemplate).Value;
    }

    // ---------- Unauthenticated / not found paths ----------

    [Fact]
    public async Task Authorize_Should_Return_Forbidden_When_User_Not_Authenticated()
    {
        _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.Empty);
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var result = await _sut.AuthorizeAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
    }

    [Fact]
    public async Task Authorize_Should_Return_NotFound_When_Layout_Missing()
    {
        var userId = Guid.NewGuid();
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockLayoutRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((VenueLayout?)null);

        var result = await _sut.AuthorizeAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    // ---------- Event-attached branch ----------

    [Fact]
    public async Task Authorize_Should_Allow_EventAttached_Layout_Owner()
    {
        var organizerId = Guid.NewGuid();
        var ev = CreateEventForOrganizer(organizerId);
        var layout = CreateLayout(organizerId, ev.Id, isTemplate: false);

        _mockCurrentUser.Setup(x => x.UserId).Returns(organizerId);
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.IsAdmin).Returns(false);
        _mockLayoutRepo.Setup(r => r.GetByIdAsync(layout.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(ev);

        var result = await _sut.AuthorizeAsync(layout.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_Should_Reject_EventAttached_Layout_NonOwner()
    {
        var organizerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var ev = CreateEventForOrganizer(organizerId);
        var layout = CreateLayout(organizerId, ev.Id, isTemplate: false);

        _mockCurrentUser.Setup(x => x.UserId).Returns(otherUserId);
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.IsAdmin).Returns(false);
        _mockLayoutRepo.Setup(r => r.GetByIdAsync(layout.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(ev);

        var result = await _sut.AuthorizeAsync(layout.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
    }

    [Fact]
    public async Task Authorize_Should_Return_NotFound_When_Event_Missing_For_EventAttached_Layout()
    {
        var organizerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var layout = CreateLayout(organizerId, eventId, isTemplate: false);

        _mockCurrentUser.Setup(x => x.UserId).Returns(organizerId);
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.IsAdmin).Returns(false);
        _mockLayoutRepo.Setup(r => r.GetByIdAsync(layout.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Event?)null);

        var result = await _sut.AuthorizeAsync(layout.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    // ---------- Template branch ----------

    [Fact]
    public async Task Authorize_Should_Allow_Template_Layout_Owner()
    {
        var ownerId = Guid.NewGuid();
        var layout = CreateLayout(ownerId, eventId: null, isTemplate: true);

        _mockCurrentUser.Setup(x => x.UserId).Returns(ownerId);
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.IsAdmin).Returns(false);
        _mockLayoutRepo.Setup(r => r.GetByIdAsync(layout.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var result = await _sut.AuthorizeAsync(layout.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_Should_Reject_Template_Layout_NonOwner()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var layout = CreateLayout(ownerId, eventId: null, isTemplate: true);

        _mockCurrentUser.Setup(x => x.UserId).Returns(otherUserId);
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.IsAdmin).Returns(false);
        _mockLayoutRepo.Setup(r => r.GetByIdAsync(layout.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var result = await _sut.AuthorizeAsync(layout.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
    }

    // ---------- Admin bypass ----------

    [Fact]
    public async Task Authorize_Should_Allow_Admin_On_EventAttached_Layout()
    {
        var organizerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var ev = CreateEventForOrganizer(organizerId);
        var layout = CreateLayout(organizerId, ev.Id, isTemplate: false);

        _mockCurrentUser.Setup(x => x.UserId).Returns(adminId);
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.IsAdmin).Returns(true);
        _mockLayoutRepo.Setup(r => r.GetByIdAsync(layout.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var result = await _sut.AuthorizeAsync(layout.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Admin short-circuit: should NOT need to load the event.
        _mockEventRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Authorize_Should_Allow_Admin_On_Template_Layout()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var layout = CreateLayout(ownerId, eventId: null, isTemplate: true);

        _mockCurrentUser.Setup(x => x.UserId).Returns(adminId);
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.IsAdmin).Returns(true);
        _mockLayoutRepo.Setup(r => r.GetByIdAsync(layout.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var result = await _sut.AuthorizeAsync(layout.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ---------- Overload: authorize with pre-loaded layout ----------

    [Fact]
    public async Task AuthorizeLayout_Overload_Should_Not_Reload_From_Repo()
    {
        var organizerId = Guid.NewGuid();
        var ev = CreateEventForOrganizer(organizerId);
        var layout = CreateLayout(organizerId, ev.Id, isTemplate: false);

        _mockCurrentUser.Setup(x => x.UserId).Returns(organizerId);
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.IsAdmin).Returns(false);
        _mockEventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(ev);

        var result = await _sut.AuthorizeLayoutAsync(layout, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mockLayoutRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
