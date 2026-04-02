using FluentAssertions;
using LankaConnect.Application.Events.Commands.RsvpToEvent;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 6A.137E: Tests for collection/sponsor bundling during registration.
/// Tests RsvpToEventCommand extensions for collection and sponsor fields.
/// </summary>
public class RsvpToEventCollectionSponsorBundlingTests
{
    #region Collection Bundling Tests

    [Fact]
    public void RsvpToEventCommand_WithNoCollection_ShouldDefaultToNull()
    {
        // Arrange & Act
        var command = new RsvpToEventCommand(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        command.CollectionAmount.Should().BeNull();
        command.CollectionNotes.Should().BeNull();
    }

    [Fact]
    public void RsvpToEventCommand_WithCollection_ShouldIncludeCollectionFields()
    {
        // Arrange & Act
        var command = new RsvpToEventCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CollectionAmount: 50.00m,
            CollectionNotes: "Happy to contribute");

        // Assert
        command.CollectionAmount.Should().Be(50.00m);
        command.CollectionNotes.Should().Be("Happy to contribute");
    }

    [Fact]
    public void RsvpToEventCommand_WithCollectionAmountOnly_ShouldAllowNullNotes()
    {
        // Arrange & Act
        var command = new RsvpToEventCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CollectionAmount: 25.00m);

        // Assert
        command.CollectionAmount.Should().Be(25.00m);
        command.CollectionNotes.Should().BeNull();
    }

    #endregion

    #region Sponsor Bundling Tests

    [Fact]
    public void RsvpToEventCommand_WithNoSponsor_ShouldDefaultToNull()
    {
        // Arrange & Act
        var command = new RsvpToEventCommand(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        command.SponsorAmount.Should().BeNull();
        command.SponsorOrganization.Should().BeNull();
        command.SponsorNotes.Should().BeNull();
    }

    [Fact]
    public void RsvpToEventCommand_WithSponsor_ShouldIncludeSponsorFields()
    {
        // Arrange & Act
        var command = new RsvpToEventCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SponsorAmount: 500.00m,
            SponsorOrganization: "Acme Corp",
            SponsorNotes: "Gold sponsor");

        // Assert
        command.SponsorAmount.Should().Be(500.00m);
        command.SponsorOrganization.Should().Be("Acme Corp");
        command.SponsorNotes.Should().Be("Gold sponsor");
    }

    [Fact]
    public void RsvpToEventCommand_WithSponsorAmountOnly_ShouldAllowNullOrgAndNotes()
    {
        // Arrange & Act
        var command = new RsvpToEventCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SponsorAmount: 100.00m);

        // Assert
        command.SponsorAmount.Should().Be(100.00m);
        command.SponsorOrganization.Should().BeNull();
        command.SponsorNotes.Should().BeNull();
    }

    #endregion

    #region Combined Bundling Tests

    [Fact]
    public void RsvpToEventCommand_WithAllFinancialTypes_ShouldIncludeAll()
    {
        // Arrange
        var addOns = new List<AddOnSelectionDto>
        {
            new(Guid.NewGuid(), 2),
        };

        // Act
        var command = new RsvpToEventCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DonationAmount: 25.00m,
            DonorName: "Test Donor",
            AddOnSelections: addOns,
            CollectionAmount: 50.00m,
            CollectionNotes: "For the event",
            SponsorAmount: 200.00m,
            SponsorOrganization: "Test Corp",
            SponsorNotes: "Bronze sponsor");

        // Assert — all financial types present
        command.DonationAmount.Should().Be(25.00m);
        command.DonorName.Should().Be("Test Donor");
        command.AddOnSelections.Should().HaveCount(1);
        command.CollectionAmount.Should().Be(50.00m);
        command.CollectionNotes.Should().Be("For the event");
        command.SponsorAmount.Should().Be(200.00m);
        command.SponsorOrganization.Should().Be("Test Corp");
        command.SponsorNotes.Should().Be("Bronze sponsor");
    }

    [Fact]
    public void RsvpToEventCommand_BackwardCompatibility_ExistingFieldsUnaffectedByNewFields()
    {
        // Arrange & Act — existing callers without collection/sponsor should still work
        var command = new RsvpToEventCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Quantity: 2,
            Email: "test@example.com",
            PhoneNumber: "1234567890",
            DonationAmount: 10.00m);

        // Assert — existing fields work, new fields default to null
        command.Quantity.Should().Be(2);
        command.Email.Should().Be("test@example.com");
        command.DonationAmount.Should().Be(10.00m);
        command.CollectionAmount.Should().BeNull();
        command.SponsorAmount.Should().BeNull();
        command.AddOnSelections.Should().BeNull();
    }

    #endregion
}
