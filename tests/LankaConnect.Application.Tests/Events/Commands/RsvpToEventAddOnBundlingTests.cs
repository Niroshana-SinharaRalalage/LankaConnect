using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 6A.137D: Tests for add-on bundling during registration.
/// Tests the AddOnSelectionDto record and RsvpToEventCommand extensions.
/// </summary>
public class RsvpToEventAddOnBundlingTests
{
    #region AddOnSelectionDto Tests

    [Fact]
    public void AddOnSelectionDto_ShouldCreateWithValidData()
    {
        // Arrange
        var definitionId = Guid.NewGuid();
        var quantity = 3;

        // Act
        var selection = new AddOnSelectionDto(definitionId, quantity);

        // Assert
        selection.DefinitionId.Should().Be(definitionId);
        selection.Quantity.Should().Be(quantity);
    }

    [Fact]
    public void AddOnSelectionDto_ShouldSupportEquality()
    {
        // Arrange
        var definitionId = Guid.NewGuid();
        var selection1 = new AddOnSelectionDto(definitionId, 2);
        var selection2 = new AddOnSelectionDto(definitionId, 2);

        // Act & Assert (record equality)
        selection1.Should().Be(selection2);
    }

    [Fact]
    public void AddOnSelectionDto_DifferentQuantity_ShouldNotBeEqual()
    {
        // Arrange
        var definitionId = Guid.NewGuid();
        var selection1 = new AddOnSelectionDto(definitionId, 2);
        var selection2 = new AddOnSelectionDto(definitionId, 3);

        // Act & Assert
        selection1.Should().NotBe(selection2);
    }

    #endregion

    #region RsvpToEventCommand with AddOns Tests

    [Fact]
    public void RsvpToEventCommand_WithNoAddOns_ShouldDefaultToNull()
    {
        // Arrange & Act
        var command = new RsvpToEventCommand(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        command.AddOnSelections.Should().BeNull();
    }

    [Fact]
    public void RsvpToEventCommand_WithAddOns_ShouldIncludeSelections()
    {
        // Arrange
        var addOns = new List<AddOnSelectionDto>
        {
            new(Guid.NewGuid(), 1),
            new(Guid.NewGuid(), 3),
        };

        // Act
        var command = new RsvpToEventCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AddOnSelections: addOns);

        // Assert
        command.AddOnSelections.Should().NotBeNull();
        command.AddOnSelections.Should().HaveCount(2);
        command.AddOnSelections![0].Quantity.Should().Be(1);
        command.AddOnSelections![1].Quantity.Should().Be(3);
    }

    [Fact]
    public void RsvpToEventCommand_WithDonationAndAddOns_ShouldIncludeBoth()
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
            AddOnSelections: addOns);

        // Assert
        command.DonationAmount.Should().Be(25.00m);
        command.DonorName.Should().Be("Test Donor");
        command.AddOnSelections.Should().HaveCount(1);
        command.AddOnSelections![0].Quantity.Should().Be(2);
    }

    [Fact]
    public void RsvpToEventCommand_BackwardCompatibility_ExistingFieldsUnaffected()
    {
        // Arrange & Act — existing callers without AddOnSelections should still work
        var command = new RsvpToEventCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Quantity: 2,
            Email: "test@example.com",
            PhoneNumber: "1234567890",
            DonationAmount: 10.00m);

        // Assert
        command.Quantity.Should().Be(2);
        command.Email.Should().Be("test@example.com");
        command.PhoneNumber.Should().Be("1234567890");
        command.DonationAmount.Should().Be(10.00m);
        command.AddOnSelections.Should().BeNull(); // Default
    }

    #endregion
}
