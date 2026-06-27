using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.CreateVolunteerList;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 7D.1: TDD coverage for the role-oriented CreateVolunteerListCommandHandler.
/// Covers happy path, empty-roles failure, event-not-found failure, domain-uniqueness
/// failure routed through Event.AddSignUpList((Kind, Category)).
/// </summary>
public class CreateVolunteerListCommandHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateVolunteerListCommandHandler>> _logger = new();

    private CreateVolunteerListCommandHandler BuildHandler() =>
        new(_eventRepository.Object, _unitOfWork.Object, _logger.Object);

    private static Event NewEvent()
    {
        var title = EventTitle.Create("Volunteer Test Event").Value;
        var description = EventDescription.Create("Event for volunteer recruitment tests").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var address = Address.Create("1 Test St", "Colombo", "WP", "00100", "Sri Lanka").Value;
        var location = EventLocation.Create(address).Value;
        var price = Money.Create(0m, Currency.USD).Value;

        return Event.Create(
            title,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            100,
            location,
            ticketPrice: price).Value;
    }

    [Fact]
    public async Task Handle_ValidRoles_CreatesVolunteerListWithKindVolunteers()
    {
        var @event = NewEvent();
        var command = new CreateVolunteerListCommand(
            @event.Id,
            "Food Committee",
            "Help with setup & serving",
            new List<VolunteerRoleDto>
            {
                new("Setup crew", VolunteersNeeded: 5),
                new("Serving", VolunteersNeeded: 3, Notes: "Wear branded t-shirt"),
            });

        _eventRepository.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"expected success but got: {result.Error}");
        result.Value.Should().NotBeEmpty();
        @event.SignUpLists.Should().HaveCount(1);
        var list = @event.SignUpLists.Single();
        list.Kind.Should().Be(SignUpKind.Volunteers);
        list.Category.Should().Be("Food Committee");
        list.Items.Should().HaveCount(2);
        list.Items.Should().OnlyContain(i => i.ItemType == SignUpItemType.Slot);
        list.Items.First(i => i.ItemDescription == "Setup crew").AvailableSlots.Should().Be(5);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyRoles_ReturnsFailureWithoutCommit()
    {
        var @event = NewEvent();
        var command = new CreateVolunteerListCommand(
            @event.Id,
            "Decorations",
            "Help decorate the hall",
            new List<VolunteerRoleDto>());

        _eventRepository.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least one role", because: "CreateVolunteerList factory enforces this");
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EventNotFound_ReturnsFailure()
    {
        var missingId = Guid.NewGuid();
        var command = new CreateVolunteerListCommand(
            missingId,
            "Games",
            "Coordinate games",
            new List<VolunteerRoleDto> { new("Emcee", 1) });

        _eventRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(missingId.ToString());
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CategoryAlreadyUsedAsVolunteers_ReturnsFailure()
    {
        var @event = NewEvent();
        var existing = SignUpList.CreateVolunteerList(
            "Food Committee",
            "Pre-existing volunteer list",
            new List<(string, int, int?, string?)> { ("Prep", 2, null, null) }).Value;
        @event.AddSignUpList(existing);

        var command = new CreateVolunteerListCommand(
            @event.Id,
            "Food Committee",
            "Duplicate attempt",
            new List<VolunteerRoleDto> { new("Servers", 3) });

        _eventRepository.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CategoryExistsAsItems_DifferentKind_Succeeds()
    {
        // Phase 7D.1: uniqueness is (Kind, Category), so the same category name
        // can exist once as Items and once as Volunteers on the same event.
        var @event = NewEvent();
        var existingItems = SignUpList.Create("Food Committee", "Items list", SignUpType.Predefined).Value;
        @event.AddSignUpList(existingItems);

        var command = new CreateVolunteerListCommand(
            @event.Id,
            "Food Committee",
            "Volunteer-kind with same category",
            new List<VolunteerRoleDto> { new("Servers", 3) });

        _eventRepository.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"expected success but got: {result.Error}");
        @event.SignUpLists.Should().HaveCount(2);
        @event.SignUpLists.Should().ContainSingle(l => l.Kind == SignUpKind.Volunteers && l.Category == "Food Committee");
        @event.SignUpLists.Should().ContainSingle(l => l.Kind == SignUpKind.Items && l.Category == "Food Committee");
    }
}
