using LankaConnect.Products.LankaEvents.Domain.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.Entities;

public class DonationConfigurationTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidData_Should_Return_Success()
    {
        // Arrange & Act
        var result = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 10m, 25m, 50m },
            allowCustomAmount: true,
            minAmount: 5m,
            maxAmount: 500m,
            donationMessage: "Support our community event!");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var config = result.Value;
        config.IsEnabled.Should().BeTrue();
        config.SuggestedAmounts.Should().HaveCount(3);
        config.SuggestedAmounts.Should().BeInAscendingOrder();
        config.AllowCustomAmount.Should().BeTrue();
        config.MinAmount.Should().Be(5m);
        config.MaxAmount.Should().Be(500m);
        config.DonationMessage.Should().Be("Support our community event!");
    }

    [Fact]
    public void Create_WhenDisabled_Should_Return_DisabledConfig()
    {
        // Act
        var result = DonationConfiguration.Create(
            isEnabled: false,
            suggestedAmounts: new List<decimal> { 10m, 25m },
            allowCustomAmount: true,
            minAmount: 5m,
            maxAmount: 500m,
            donationMessage: "Help us!");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsEnabled.Should().BeFalse();
        result.Value.SuggestedAmounts.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithMoreThanMaxSuggestedAmounts_Should_Fail()
    {
        // Act
        var result = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 10m, 25m, 50m, 100m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            donationMessage: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"Maximum {DonationConfiguration.MAX_SUGGESTED_AMOUNTS}");
    }

    [Fact]
    public void Create_WithSuggestedAmountBelowMinimum_Should_Fail()
    {
        // Act
        var result = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 0.50m, 25m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            donationMessage: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least");
    }

    [Fact]
    public void Create_WithNoSuggestedAmountsAndNoCustom_Should_Fail()
    {
        // Act
        var result = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: null,
            allowCustomAmount: false,
            minAmount: null,
            maxAmount: null,
            donationMessage: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("suggested amounts or allow custom");
    }

    [Fact]
    public void Create_WithCustomAmountOnly_Should_Succeed()
    {
        // Act
        var result = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: null,
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            donationMessage: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AllowCustomAmount.Should().BeTrue();
        result.Value.SuggestedAmounts.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithMinGreaterThanMax_Should_Fail()
    {
        // Act
        // Note: suggestedAmounts must be empty (not [25m]) so we reach the min>max
        // validation (line 131 in DonationConfiguration.cs). Otherwise the earlier
        // "suggested amounts cannot be below minimum" check short-circuits the test.
        var result = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal>(),
            allowCustomAmount: true,
            minAmount: 100m,
            maxAmount: 50m,
            donationMessage: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot exceed maximum");
    }

    [Fact]
    public void Create_WithMinBelowSystemMinimum_Should_Fail()
    {
        // Act
        var result = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 25m },
            allowCustomAmount: true,
            minAmount: 0.50m,
            maxAmount: null,
            donationMessage: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least");
    }

    [Fact]
    public void Create_WithMessageExceedingMaxLength_Should_Fail()
    {
        // Arrange
        var longMessage = new string('x', DonationConfiguration.MAX_MESSAGE_LENGTH + 1);

        // Act
        var result = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 25m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            donationMessage: longMessage);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"{DonationConfiguration.MAX_MESSAGE_LENGTH} characters");
    }

    [Fact]
    public void Create_SortsSuggestedAmountsAscending()
    {
        // Arrange - provide amounts in random order
        var result = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 50m, 10m, 25m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            donationMessage: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SuggestedAmounts.Should().ContainInOrder(10m, 25m, 50m);
    }

    #endregion

    #region Disabled Tests

    [Fact]
    public void Disabled_Should_Return_DisabledConfig()
    {
        // Act
        var config = DonationConfiguration.Disabled();

        // Assert
        config.IsEnabled.Should().BeFalse();
        config.SuggestedAmounts.Should().BeEmpty();
        config.AllowCustomAmount.Should().BeFalse();
        config.MinAmount.Should().BeNull();
        config.MaxAmount.Should().BeNull();
        config.DonationMessage.Should().BeNull();
    }

    #endregion

    #region ValidateAmount Tests

    [Fact]
    public void ValidateAmount_WhenDisabled_Should_Fail()
    {
        // Arrange
        var config = DonationConfiguration.Disabled();

        // Act
        var result = config.ValidateAmount(25m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not enabled");
    }

    [Fact]
    public void ValidateAmount_WithinRange_Should_Succeed()
    {
        // Arrange
        var config = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 25m },
            allowCustomAmount: true,
            minAmount: 10m,
            maxAmount: 100m,
            donationMessage: null).Value;

        // Act
        var result = config.ValidateAmount(50m);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateAmount_BelowMin_Should_Fail()
    {
        // Arrange
        var config = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 25m },
            allowCustomAmount: true,
            minAmount: 10m,
            maxAmount: 100m,
            donationMessage: null).Value;

        // Act
        var result = config.ValidateAmount(5m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least");
    }

    [Fact]
    public void ValidateAmount_AboveMax_Should_Fail()
    {
        // Arrange
        var config = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 25m },
            allowCustomAmount: true,
            minAmount: 10m,
            maxAmount: 100m,
            donationMessage: null).Value;

        // Act
        var result = config.ValidateAmount(150m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot exceed");
    }

    [Fact]
    public void ValidateAmount_BelowSystemMinimum_Should_Fail()
    {
        // Arrange
        var config = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 25m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            donationMessage: null).Value;

        // Act
        var result = config.ValidateAmount(0.50m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least");
    }

    #endregion
}
