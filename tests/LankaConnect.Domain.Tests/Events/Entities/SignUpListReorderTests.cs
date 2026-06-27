using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Phase 6A.132 — DisplayOrder assignment + ReorderItems aggregate operation.
/// The aggregate owns ordering: items are appended to the tail on add, and
/// <see cref="SignUpList.ReorderItems"/> enforces exact-set equality so the
/// frontend must send every ID exactly once to succeed.
/// </summary>
public class SignUpListReorderTests
{
    private static SignUpList CreateListWithItems(int count)
    {
        var result = SignUpList.CreateWithCategories(
            category: "Food",
            description: "Please bring a dish",
            hasMandatoryItems: true,
            hasPreferredItems: false,
            hasSuggestedItems: false);
        result.IsSuccess.Should().BeTrue();
        var list = result.Value;

        for (int i = 0; i < count; i++)
        {
            var add = list.AddItem($"Item {i}", 1, SignUpItemCategory.Mandatory);
            add.IsSuccess.Should().BeTrue();
        }

        return list;
    }

    [Fact]
    public void AddItem_Should_AssignSequentialDisplayOrderStartingAtZero()
    {
        var list = CreateListWithItems(3);

        list.Items.Select(i => i.DisplayOrder).Should().Equal(new[] { 0, 1, 2 },
            "the aggregate appends new items and assigns DisplayOrder in insertion order");
    }

    [Fact]
    public void AddSlotBasedItem_Should_AppendWithNextDisplayOrder()
    {
        var list = CreateListWithItems(2);

        var slotAdd = list.AddSlotBasedItem("Volunteers", 3, null, SignUpItemCategory.Mandatory);

        slotAdd.IsSuccess.Should().BeTrue();
        slotAdd.Value.DisplayOrder.Should().Be(2,
            "slot-based additions share the same DisplayOrder sequence as quantity-based adds");
    }

    [Fact]
    public void CreateVolunteerList_AssignsSequentialDisplayOrder()
    {
        var roles = new[]
        {
            ("Setup Crew", 3, (int?)null, (string?)null),
            ("Greeters", 2, (int?)null, "Friendly faces at the door"),
            ("Cleanup", 4, (int?)null, (string?)null),
        };

        var result = SignUpList.CreateVolunteerList(
            category: "Volunteers",
            description: "Help us run the event",
            roles: roles);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(i => i.DisplayOrder).Should().Equal(new[] { 0, 1, 2 },
            "volunteer roles route through AddSlotBasedItem and must inherit the same sequential DisplayOrder invariant as classic items");
    }

    [Fact]
    public void ReorderItems_WithExactSet_Should_AssignOrderByPosition()
    {
        var list = CreateListWithItems(3);
        var original = list.Items.ToList();
        var reversedIds = original.Select(i => i.Id).Reverse().ToList();

        var result = list.ReorderItems(reversedIds);

        result.IsSuccess.Should().BeTrue();
        original[0].DisplayOrder.Should().Be(2);
        original[1].DisplayOrder.Should().Be(1);
        original[2].DisplayOrder.Should().Be(0);
    }

    [Fact]
    public void ReorderItems_OnSuccess_Should_RaiseSignUpItemsReorderedEvent()
    {
        var list = CreateListWithItems(2);
        var ids = list.Items.Select(i => i.Id).Reverse().ToList();

        var result = list.ReorderItems(ids);

        result.IsSuccess.Should().BeTrue();
        var domainEvent = list.DomainEvents.OfType<SignUpItemsReorderedEvent>().SingleOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.SignUpListId.Should().Be(list.Id);
        domainEvent.OrderedItemIds.Should().Equal(ids);
    }

    [Fact]
    public void ReorderItems_WithMissingId_Should_ReturnFailure()
    {
        var list = CreateListWithItems(3);
        var incomplete = list.Items.Take(2).Select(i => i.Id).ToList();

        var result = list.ReorderItems(incomplete);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Expected 3 item IDs");
    }

    [Fact]
    public void ReorderItems_WithExtraId_Should_ReturnFailure()
    {
        var list = CreateListWithItems(2);
        var withExtra = list.Items.Select(i => i.Id).Append(Guid.NewGuid()).ToList();

        var result = list.ReorderItems(withExtra);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Expected 2 item IDs");
    }

    [Fact]
    public void ReorderItems_WithUnknownId_Should_ReturnFailure()
    {
        var list = CreateListWithItems(2);
        var tampered = list.Items.Take(1).Select(i => i.Id).Append(Guid.NewGuid()).ToList();

        var result = list.ReorderItems(tampered);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("do not match");
    }

    [Fact]
    public void ReorderItems_WithDuplicateIds_Should_ReturnFailure()
    {
        var list = CreateListWithItems(3);
        var firstId = list.Items[0].Id;
        var duplicates = new List<Guid> { firstId, firstId, list.Items[1].Id };

        var result = list.ReorderItems(duplicates);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("duplicates");
    }

    [Fact]
    public void ReorderItems_WithEmptyList_Should_ReturnFailure()
    {
        var list = CreateListWithItems(2);

        var result = list.ReorderItems(new List<Guid>());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }
}
