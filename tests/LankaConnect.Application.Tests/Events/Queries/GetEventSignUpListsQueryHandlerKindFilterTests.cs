using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Queries.GetEventSignUpLists;
using LankaConnect.Domain.Business.ValueObjects;
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

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// Phase 7D.1 Phase B8: Verify the optional Kind filter partitions an event's
/// sign-up lists correctly and that Kind flows through into the DTO so
/// frontends can discriminate Volunteer rosters from Items lists.
/// </summary>
public class GetEventSignUpListsQueryHandlerKindFilterTests
{
    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<ILogger<GetEventSignUpListsQueryHandler>> _logger = new();

    private GetEventSignUpListsQueryHandler BuildHandler() =>
        new(_eventRepository.Object, _logger.Object);

    private static Event EventWithMixedLists()
    {
        var title = EventTitle.Create("Kind Filter Test Event").Value;
        var description = EventDescription.Create("Mixed Items + Volunteers").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var address = Address.Create("1 Test St", "Colombo", "WP", "00100", "Sri Lanka").Value;
        var location = EventLocation.Create(address).Value;
        var price = Money.Create(0m, Currency.USD).Value;
        var @event = Event.Create(title, description, startDate, endDate, Guid.NewGuid(), 100, location, ticketPrice: price).Value;

        var itemsList = SignUpList.Create("Potluck", "Bring a dish", SignUpType.Predefined).Value;
        @event.AddSignUpList(itemsList);

        var volunteerList = SignUpList.CreateVolunteerList(
            "Cleanup Crew",
            "Post-event cleanup volunteers",
            new List<(string, int, int?, string?)> { ("Sweepers", 4, null, null) }).Value;
        @event.AddSignUpList(volunteerList);

        return @event;
    }

    [Fact]
    public async Task Handle_NoKindFilter_ReturnsBothKinds()
    {
        var @event = EventWithMixedLists();
        _eventRepository.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var result = await BuildHandler().Handle(new GetEventSignUpListsQuery(@event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().ContainSingle(l => l.Kind == SignUpKind.Items);
        result.Value.Should().ContainSingle(l => l.Kind == SignUpKind.Volunteers);
    }

    [Fact]
    public async Task Handle_KindVolunteers_ReturnsOnlyVolunteerLists()
    {
        var @event = EventWithMixedLists();
        _eventRepository.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var result = await BuildHandler().Handle(
            new GetEventSignUpListsQuery(@event.Id, Kind: SignUpKind.Volunteers),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value.Single().Kind.Should().Be(SignUpKind.Volunteers);
        result.Value.Single().Category.Should().Be("Cleanup Crew");
    }

    [Fact]
    public async Task Handle_KindItems_ReturnsOnlyItemsLists()
    {
        var @event = EventWithMixedLists();
        _eventRepository.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var result = await BuildHandler().Handle(
            new GetEventSignUpListsQuery(@event.Id, Kind: SignUpKind.Items),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value.Single().Kind.Should().Be(SignUpKind.Items);
        result.Value.Single().Category.Should().Be("Potluck");
    }
}
