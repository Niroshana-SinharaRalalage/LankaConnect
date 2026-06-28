using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Phase 6A.140 regression guard. UserCommittedToSignUpEvent gained two
/// optional fields — ContactEmail and ContactName — so the email-confirmation
/// handler can fall back to form-submitted contact info when the commitment
/// was created anonymously (UserId is a deterministic GUID with no row in
/// the Users table — the handler used to fail-silent for that case).
///
/// These tests pin down that AddCommitment / AddSlotCommitment actually
/// forward the contact info onto the domain event. If a future refactor
/// drops the forwarding, anonymous committers will silently stop receiving
/// confirmation emails again.
/// </summary>
public class SignUpItem_DomainEventContactInfo_Tests
{
    private readonly Guid _signUpListId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void AddCommitment_ForwardsContactEmailAndNameOnDomainEvent()
    {
        // Arrange
        var item = SignUpItem.CreateQuantityBased(
            _signUpListId, "Rice", 10, SignUpItemCategory.Mandatory).Value;
        item.ClearDomainEvents();

        // Act
        var result = item.AddCommitment(
            _userId,
            commitQuantity: 3,
            commitNotes: "10am drop-off",
            contactName: "Niro Sample",
            contactEmail: "niro@example.com",
            contactPhone: "+1-555-1234",
            kind: SignUpKind.Items);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var raised = item.DomainEvents.OfType<UserCommittedToSignUpEvent>().Single();
        raised.ContactEmail.Should().Be("niro@example.com");
        raised.ContactName.Should().Be("Niro Sample");
        raised.UserId.Should().Be(_userId);
        raised.PhysicalQuantity.Should().Be(3);
    }

    [Fact]
    public void AddCommitment_WithoutContactInfo_StillSucceedsAndLeavesContactFieldsNull()
    {
        var item = SignUpItem.CreateQuantityBased(
            _signUpListId, "Rice", 10, SignUpItemCategory.Mandatory).Value;
        item.ClearDomainEvents();

        var result = item.AddCommitment(_userId, commitQuantity: 1);

        result.IsSuccess.Should().BeTrue();
        var raised = item.DomainEvents.OfType<UserCommittedToSignUpEvent>().Single();
        raised.ContactEmail.Should().BeNull();
        raised.ContactName.Should().BeNull();
    }

    [Fact]
    public void AddSlotCommitment_ForwardsContactEmailAndNameOnDomainEvent()
    {
        var item = SignUpItem.CreateSlotBased(
            _signUpListId, "Volunteer Slot", availableSlots: 5, suggestedPerSlot: null, SignUpItemCategory.Mandatory).Value;
        item.ClearDomainEvents();

        var result = item.AddSlotCommitment(
            _userId,
            slotsClaimed: 2,
            contactName: "Volunteer Sample",
            contactEmail: "vol@example.com",
            contactPhone: null,
            kind: SignUpKind.Volunteers);

        result.IsSuccess.Should().BeTrue();
        var raised = item.DomainEvents.OfType<UserCommittedToSignUpEvent>().Single();
        raised.ContactEmail.Should().Be("vol@example.com");
        raised.ContactName.Should().Be("Volunteer Sample");
        raised.SlotsClaimed.Should().Be(2);
        raised.Kind.Should().Be(SignUpKind.Volunteers);
    }
}
