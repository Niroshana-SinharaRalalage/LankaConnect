using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Phase 7D.1 — Volunteer Signup feature (TDD).
/// Volunteer lists reuse the SignUpList aggregate via the SignUpKind discriminator rather than
/// a parallel aggregate. The CreateVolunteerList factory enforces the invariants that make a
/// list behave as a volunteer recruitment list:
///   - Kind = Volunteers
///   - Slot-based items only (1 slot = 1 volunteer); quantity-based items are rejected
///   - User-submitted "open" roles are disabled
/// </summary>
public class SignUpListVolunteerTests
{
    [Fact]
    public void Create_Default_Should_Return_SignUpList_With_Kind_Items()
    {
        var result = SignUpList.Create("Food", "Please bring a dish", SignUpType.Open);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(SignUpKind.Items,
            "existing factories preserve backward compatibility by defaulting to Items");
    }

    [Fact]
    public void CreateWithCategoriesAndItems_Default_Should_Return_SignUpList_With_Kind_Items()
    {
        var items = new[]
        {
            ("Rice", SignUpItemType.Quantity, SignUpItemCategory.Mandatory, (int?)10, (int?)null, (int?)null, (string?)null)
        };

        var result = SignUpList.CreateWithCategoriesAndItems(
            category: "Food",
            description: "Please bring a dish",
            hasMandatoryItems: true,
            hasPreferredItems: false,
            hasSuggestedItems: false,
            items: items);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(SignUpKind.Items);
    }

    [Fact]
    public void CreateVolunteerList_WithValidSlotBasedRoles_Should_Return_Success_With_Kind_Volunteers()
    {
        var roles = new[]
        {
            ("Food Committee", 5, (int?)null, (string?)null),
            ("Decorations", 5, (int?)null, (string?)null),
            ("Games and Entertainment", 4, (int?)null, (string?)null)
        };

        var result = SignUpList.CreateVolunteerList(
            category: "Volunteer Roles",
            description: "Help make this event a success",
            roles: roles);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(SignUpKind.Volunteers);
        result.Value.Category.Should().Be("Volunteer Roles");
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().OnlyContain(i => i.ItemType == SignUpItemType.Slot,
            "volunteer roles are slot-based — each slot represents one volunteer");
        result.Value.Items.Select(i => i.AvailableSlots).Should().BeEquivalentTo(new int?[] { 5, 5, 4 });
    }

    [Fact]
    public void CreateVolunteerList_Should_Enable_Only_Mandatory_Category_Flag()
    {
        var roles = new[] { ("Food Committee", 5, (int?)null, (string?)null) };

        var result = SignUpList.CreateVolunteerList("Volunteers", "Help out", roles);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasMandatoryItems.Should().BeTrue();
        result.Value.HasSuggestedItems.Should().BeFalse();
        result.Value.HasOpenItems.Should().BeFalse(
            "user-submitted volunteer roles are disabled by product decision");
    }

    [Fact]
    public void CreateVolunteerList_WithEmptyRoles_Should_Return_Failure()
    {
        var result = SignUpList.CreateVolunteerList(
            category: "Volunteers",
            description: "Help out",
            roles: Array.Empty<(string, int, int?, string?)>());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least one", "a volunteer list is useless without roles");
    }

    [Fact]
    public void CreateVolunteerList_WithEmptyCategory_Should_Return_Failure()
    {
        var roles = new[] { ("Food Committee", 5, (int?)null, (string?)null) };

        var result = SignUpList.CreateVolunteerList(
            category: "",
            description: "Help out",
            roles: roles);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CreateVolunteerList_WithZeroSlots_Should_Return_Failure()
    {
        var roles = new[] { ("Food Committee", 0, (int?)null, (string?)null) };

        var result = SignUpList.CreateVolunteerList("Volunteers", "Help out", roles);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void VolunteerList_AddItem_QuantityBased_Should_Return_Failure()
    {
        var roles = new[] { ("Food Committee", 5, (int?)null, (string?)null) };
        var list = SignUpList.CreateVolunteerList("Volunteers", "Help out", roles).Value;

        var addResult = list.AddItem("Extra Role", 3, SignUpItemCategory.Mandatory);

        addResult.IsFailure.Should().BeTrue();
        addResult.Error.Should().Contain("volunteer",
            "quantity-based items violate the volunteer-list invariant (1 slot = 1 volunteer)");
    }

    [Fact]
    public void VolunteerList_AddSlotBasedItem_Should_Succeed()
    {
        var roles = new[] { ("Food Committee", 5, (int?)null, (string?)null) };
        var list = SignUpList.CreateVolunteerList("Volunteers", "Help out", roles).Value;

        var addResult = list.AddSlotBasedItem("Cleanup Crew", 3, null, SignUpItemCategory.Mandatory);

        addResult.IsSuccess.Should().BeTrue();
        list.Items.Should().HaveCount(2);
    }

    [Fact]
    public void VolunteerList_AddOpenItem_Should_Return_Failure()
    {
        var roles = new[] { ("Food Committee", 5, (int?)null, (string?)null) };
        var list = SignUpList.CreateVolunteerList("Volunteers", "Help out", roles).Value;

        var addResult = list.AddOpenItem(Guid.NewGuid(), "My custom role", 1);

        addResult.IsFailure.Should().BeTrue(
            "user-submitted open roles are disabled for volunteer lists — organizer-defined only");
    }

    [Fact]
    public void VolunteerList_SlotCommitment_Should_Raise_Event_With_Kind_Volunteers()
    {
        var roles = new[] { ("Food Committee", 5, (int?)null, (string?)null) };
        var list = SignUpList.CreateVolunteerList("Volunteers", "Help out", roles).Value;
        var item = list.Items.Single();
        item.ClearDomainEvents(); // strip factory-time events

        var commitResult = item.AddSlotCommitment(
            userId: Guid.NewGuid(),
            slotsClaimed: 1,
            kind: LankaConnect.Products.LankaEvents.Domain.Enums.SignUpKind.Volunteers);

        commitResult.IsSuccess.Should().BeTrue();
        var evt = item.DomainEvents.OfType<LankaConnect.Products.LankaEvents.Domain.DomainEvents.UserCommittedToSignUpEvent>().Single();
        evt.Kind.Should().Be(LankaConnect.Products.LankaEvents.Domain.Enums.SignUpKind.Volunteers,
            "Kind must propagate to the domain event so downstream handlers route to the volunteer template");
        evt.SlotsClaimed.Should().Be(1);
    }

    [Fact]
    public void ItemsList_QuantityCommitment_Default_Should_Raise_Event_With_Kind_Items()
    {
        var items = new[]
        {
            ("Rice", SignUpItemType.Quantity, SignUpItemCategory.Mandatory, (int?)10, (int?)null, (int?)null, (string?)null)
        };
        var list = SignUpList.CreateWithCategoriesAndItems(
            category: "Food",
            description: "Bring a dish",
            hasMandatoryItems: true,
            hasPreferredItems: false,
            hasSuggestedItems: false,
            items: items).Value;
        var item = list.Items.Single();
        item.ClearDomainEvents();

        var commitResult = item.AddCommitment(userId: Guid.NewGuid(), commitQuantity: 2);

        commitResult.IsSuccess.Should().BeTrue();
        var evt = item.DomainEvents.OfType<LankaConnect.Products.LankaEvents.Domain.DomainEvents.UserCommittedToSignUpEvent>().Single();
        evt.Kind.Should().Be(LankaConnect.Products.LankaEvents.Domain.Enums.SignUpKind.Items,
            "default Items kind preserves back-compat when no kind is passed");
    }

    [Fact]
    public void CreateVolunteerList_WithNotes_Should_Propagate_Notes()
    {
        var roles = new[]
        {
            ("Food Committee", 5, (int?)null, (string?)"Arrive 1h before event")
        };

        var result = SignUpList.CreateVolunteerList("Volunteers", "Help out", roles);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single().Notes.Should().Be("Arrive 1h before event");
    }
}
