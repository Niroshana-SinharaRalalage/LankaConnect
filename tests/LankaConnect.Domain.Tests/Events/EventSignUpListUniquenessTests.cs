using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 7D.1 — the uniqueness invariant on <see cref="Event.AddSignUpList"/> must key on
/// (Kind, Category) rather than Category alone so organizers can run, e.g., an "Items:Food"
/// list and a "Volunteers:Food Committee" list on the same event without collision.
/// </summary>
public class EventSignUpListUniquenessTests
{
    private static SignUpList NewItemsList(string category) =>
        SignUpList.Create(category, "desc", SignUpType.Open).Value;

    private static SignUpList NewVolunteerList(string category)
    {
        var roles = new[] { ("Lead", 3, (int?)null, (string?)null) };
        return SignUpList.CreateVolunteerList(category, "desc", roles).Value;
    }

    [Fact]
    public void AddSignUpList_SameCategory_DifferentKind_Should_Succeed()
    {
        var ev = Event.CreateDefault();

        var first = ev.AddSignUpList(NewItemsList("Food"));
        var second = ev.AddSignUpList(NewVolunteerList("Food"));

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue(
            "uniqueness should key on (Kind, Category) — an Items list and a Volunteers list may share a category label");
    }

    [Fact]
    public void AddSignUpList_SameCategory_SameKind_Should_Fail()
    {
        var ev = Event.CreateDefault();

        ev.AddSignUpList(NewItemsList("Food")).IsSuccess.Should().BeTrue();
        var duplicate = ev.AddSignUpList(NewItemsList("Food"));

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Should().Contain("already exists");
    }

    [Fact]
    public void AddSignUpList_SameCategory_SameKind_Volunteers_Should_Fail()
    {
        var ev = Event.CreateDefault();

        ev.AddSignUpList(NewVolunteerList("Crew")).IsSuccess.Should().BeTrue();
        var duplicate = ev.AddSignUpList(NewVolunteerList("Crew"));

        duplicate.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddSignUpList_SameCategory_CaseInsensitive_SameKind_Should_Fail()
    {
        var ev = Event.CreateDefault();

        ev.AddSignUpList(NewItemsList("Food")).IsSuccess.Should().BeTrue();
        var duplicate = ev.AddSignUpList(NewItemsList("FOOD"));

        duplicate.IsFailure.Should().BeTrue(
            "existing behavior was case-insensitive on Category — (Kind, Category) must preserve that");
    }
}
