using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Badges.DTOs;
using LankaConnect.Products.LankaEvents.Application.Badges.Queries.GetEventBadges;
using LankaConnect.Products.LankaEvents.Domain.Badges;
using LankaConnect.Products.LankaEvents.Domain.Badges.Enums;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using Moq;

#pragma warning disable CS0618

namespace LankaConnect.Application.Tests.Badges.Queries;

/// <summary>
/// Wave 6.5.f.5-hotfix2d acceptance test (architect ruling §6, Rule 5j.3 coverage gap).
///
/// The previous handler dereferenced `eb.Badge` via EF navigation. That worked when
/// EventRepository routed through AppDbContext (Badge mapped as principal). Wave 6.5.f.5
/// cut EventRepository to LankaEventsDbContext, where Badge is Ignored — `eb.Badge`
/// materializes as null, the .Where(eb => eb.Badge != null) filter dropped everything,
/// and the endpoint silently returned empty lists.
///
/// This test constructs an Event with two EventBadges, mocks both repositories, and
/// asserts the returned DTOs contain hydrated Badge data for both. Coverage that Rule 5j.3
/// requires when a HasOne/HasMany block is deleted from an EF config — this is the
/// specific test that would have caught the hotfix1 regression at test time.
/// </summary>
public class GetEventBadgesQueryHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepo = new();
    private readonly Mock<IBadgeRepository> _badgeRepo = new();

    private GetEventBadgesQueryHandler CreateHandler()
        => new(_eventRepo.Object, _badgeRepo.Object);

    private static Badge CreateBadge(string name, int displayOrder)
    {
        var result = Badge.Create(
            name: name,
            imageUrl: $"https://storage.example.com/badges/{name}.png",
            blobName: $"badges/{name}.png",
            position: BadgePosition.TopRight,
            displayOrder: displayOrder,
            createdByUserId: Guid.NewGuid());
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public async Task Handle_EventWithTwoBadges_ReturnsHydratedDtos()
    {
        var eventId = Guid.NewGuid();
        var badge1 = CreateBadge("featured", 1);
        var badge2 = CreateBadge("new", 2);

        // Reflection to assign Ids on Badges (private set) — simulates DB-loaded state.
        typeof(Badge).BaseType!.GetProperty("Id")!.SetValue(badge1, Guid.NewGuid());
        typeof(Badge).BaseType!.GetProperty("Id")!.SetValue(badge2, Guid.NewGuid());

        var eventBadge1 = EventBadge.Create(eventId, badge1.Id, Guid.NewGuid()).Value;
        var eventBadge2 = EventBadge.Create(eventId, badge2.Id, Guid.NewGuid()).Value;

        // Construct a stub Event with the two EventBadges attached. Use reflection to
        // populate the private _badges list because the domain doesn't expose a public
        // constructor path that avoids raising domain events during test setup.
        var stubEvent = (Event)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(Event));
        var badgesField = typeof(Event).GetField("_badges",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        badgesField!.SetValue(stubEvent, new List<EventBadge> { eventBadge1, eventBadge2 });
        typeof(Event).BaseType!.GetProperty("Id")!.SetValue(stubEvent, eventId);

        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stubEvent);
        _badgeRepo.Setup(r => r.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Badge, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Badge> { badge1, badge2 });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetEventBadgesQuery { EventId = eventId }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(d => d.BadgeId == badge1.Id && d.Badge!.Name == "featured");
        result.Value.Should().Contain(d => d.BadgeId == badge2.Id && d.Badge!.Name == "new");

        _badgeRepo.Verify(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Badge, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EventWithNoBadges_ReturnsEmptyList_WithoutCallingBadgeRepo()
    {
        var eventId = Guid.NewGuid();
        var stubEvent = (Event)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(Event));
        var badgesField = typeof(Event).GetField("_badges",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        badgesField!.SetValue(stubEvent, new List<EventBadge>());
        typeof(Event).BaseType!.GetProperty("Id")!.SetValue(stubEvent, eventId);

        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stubEvent);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetEventBadgesQuery { EventId = eventId }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        _badgeRepo.Verify(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Badge, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EventNotFound_ReturnsFailure()
    {
        var eventId = Guid.NewGuid();
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetEventBadgesQuery { EventId = eventId }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(eventId.ToString());
    }
}
