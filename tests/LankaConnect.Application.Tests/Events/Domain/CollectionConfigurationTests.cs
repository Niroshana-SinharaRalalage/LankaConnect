using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Comprehensive unit tests for CollectionConfiguration value object.
/// Covers creation, validation, suggested amounts, min/max ranges, and ValidateAmount().
/// </summary>
public class CollectionConfigurationTests
{
    #region Create() - Enabled with Valid Data

    [Fact]
    public void Create_EnabledWithValidData_ShouldSucceed()
    {
        var suggestedAmounts = new List<decimal> { 5.00m, 10.00m, 25.00m };

        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: 1000.00m,
            showProgress: true,
            suggestedAmounts: suggestedAmounts,
            allowCustomAmount: true,
            minAmount: 2.00m,
            maxAmount: 500.00m,
            collectionMessage: "Help fund our event!",
            showContributorCount: true);

        result.IsSuccess.Should().BeTrue();
        var config = result.Value;
        config.IsEnabled.Should().BeTrue();
        config.GoalAmount.Should().Be(1000.00m);
        config.ShowProgress.Should().BeTrue();
        config.SuggestedAmounts.Should().HaveCount(3);
        config.AllowCustomAmount.Should().BeTrue();
        config.MinAmount.Should().Be(2.00m);
        config.MaxAmount.Should().Be(500.00m);
        config.CollectionMessage.Should().Be("Help fund our event!");
        config.ShowContributorCount.Should().BeTrue();
    }

    #endregion

    #region Create() - Disabled

    [Fact]
    public void Create_Disabled_ShouldReturnDisabledConfig()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: false,
            goalAmount: 1000.00m,
            showProgress: true,
            suggestedAmounts: new List<decimal> { 5m },
            allowCustomAmount: true,
            minAmount: 1m,
            maxAmount: 100m,
            collectionMessage: "ignored");

        result.IsSuccess.Should().BeTrue();
        var config = result.Value;
        config.IsEnabled.Should().BeFalse();
        config.GoalAmount.Should().BeNull();
        config.SuggestedAmounts.Should().BeEmpty();
    }

    [Fact]
    public void Disabled_ShouldReturnConfigWithAllDefaults()
    {
        var config = CollectionConfiguration.Disabled();

        config.IsEnabled.Should().BeFalse();
        config.GoalAmount.Should().BeNull();
        config.ShowProgress.Should().BeFalse();
        config.SuggestedAmounts.Should().BeEmpty();
        config.AllowCustomAmount.Should().BeFalse();
        config.MinAmount.Should().BeNull();
        config.MaxAmount.Should().BeNull();
        config.CollectionMessage.Should().BeNull();
        config.ShowContributorCount.Should().BeFalse();
    }

    #endregion

    #region GoalAmount Validation

    [Fact]
    public void Create_WithNegativeGoalAmount_ShouldFail()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: -100.00m,
            showProgress: false,
            suggestedAmounts: new List<decimal> { 5m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            collectionMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Goal amount");
    }

    [Fact]
    public void Create_WithZeroGoalAmount_ShouldFail()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: 0m,
            showProgress: false,
            suggestedAmounts: new List<decimal> { 5m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            collectionMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Goal amount");
    }

    [Fact]
    public void Create_WithNullGoalAmount_ShouldSucceed()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: new List<decimal> { 5m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            collectionMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.GoalAmount.Should().BeNull();
    }

    #endregion

    #region Suggested Amounts Validation

    [Fact]
    public void Create_WithTooManySuggestedAmounts_ShouldFail()
    {
        var amounts = new List<decimal> { 1m, 2m, 3m, 5m, 10m, 20m }; // 6 > MAX_SUGGESTED_AMOUNTS (5)

        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: amounts,
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            collectionMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("5");
    }

    [Fact]
    public void Create_WithSuggestedAmountBelowMinimum_ShouldFail()
    {
        var amounts = new List<decimal> { 0.50m }; // Below MINIMUM_COLLECTION_AMOUNT (1.00)

        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: amounts,
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            collectionMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Suggested amounts");
    }

    [Fact]
    public void Create_ShouldSortSuggestedAmountsAscending()
    {
        var amounts = new List<decimal> { 25m, 5m, 50m, 10m };

        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: amounts,
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            collectionMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.SuggestedAmounts.Should().BeInAscendingOrder();
        result.Value.SuggestedAmounts.Should().ContainInOrder(5m, 10m, 25m, 50m);
    }

    [Fact]
    public void Create_WithSuggestedAmountBelowMinAmount_ShouldFail()
    {
        var amounts = new List<decimal> { 3m, 10m };

        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: amounts,
            allowCustomAmount: true,
            minAmount: 5.00m,
            maxAmount: null,
            collectionMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("below minimum");
    }

    [Fact]
    public void Create_WithSuggestedAmountAboveMaxAmount_ShouldFail()
    {
        var amounts = new List<decimal> { 5m, 200m };

        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: amounts,
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: 100.00m,
            collectionMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exceed maximum");
    }

    #endregion

    #region No Suggested Amounts AND AllowCustomAmount Validation

    [Fact]
    public void Create_WithNoSuggestedAmountsAndCustomDisabled_ShouldFail()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: null,
            allowCustomAmount: false,
            minAmount: null,
            maxAmount: null,
            collectionMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_WithEmptySuggestedAmountsAndCustomDisabled_ShouldFail()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: new List<decimal>(),
            allowCustomAmount: false,
            minAmount: null,
            maxAmount: null,
            collectionMessage: null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNoSuggestedAmountsButCustomEnabled_ShouldSucceed()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: null,
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            collectionMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllowCustomAmount.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptySuggestedAmountsButCustomEnabled_ShouldSucceed()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: new List<decimal>(),
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            collectionMessage: null);

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Min/Max Amount Validation

    [Fact]
    public void Create_WithMinAmountBelowMinimum_ShouldFail()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: new List<decimal> { 5m },
            allowCustomAmount: true,
            minAmount: 0.50m, // Below MINIMUM_COLLECTION_AMOUNT
            maxAmount: null,
            collectionMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Minimum amount");
    }

    [Fact]
    public void Create_WithMaxAmountLessThanMinAmount_ShouldFail()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: null,
            allowCustomAmount: true,
            minAmount: 50.00m,
            maxAmount: 10.00m,
            collectionMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot exceed");
    }

    [Fact]
    public void Create_WithMaxAmountBelowMinimum_ShouldFail()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: null,
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: 0.50m,
            collectionMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Maximum amount");
    }

    [Fact]
    public void Create_WithEqualMinAndMaxAmount_ShouldSucceed()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: new List<decimal> { 10m },
            allowCustomAmount: true,
            minAmount: 10.00m,
            maxAmount: 10.00m,
            collectionMessage: null);

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Message Validation

    [Fact]
    public void Create_WithMessageExceeding500Chars_ShouldFail()
    {
        var longMessage = new string('x', 501);

        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: new List<decimal> { 5m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            collectionMessage: longMessage);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("500");
    }

    [Fact]
    public void Create_WithMessageExactly500Chars_ShouldSucceed()
    {
        var message = new string('x', 500);

        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: new List<decimal> { 5m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            collectionMessage: message);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldTrimMessage()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true,
            goalAmount: null,
            showProgress: false,
            suggestedAmounts: new List<decimal> { 5m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            collectionMessage: "  Help us!  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.CollectionMessage.Should().Be("Help us!");
    }

    #endregion

    #region ValidateAmount()

    [Fact]
    public void ValidateAmount_WithinRange_ShouldSucceed()
    {
        var config = CollectionConfiguration.Create(
            isEnabled: true, goalAmount: null, showProgress: false,
            suggestedAmounts: new List<decimal> { 5m }, allowCustomAmount: true,
            minAmount: 5.00m, maxAmount: 100.00m, collectionMessage: null).Value;

        var result = config.ValidateAmount(50.00m);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateAmount_BelowMinAmount_ShouldFail()
    {
        var config = CollectionConfiguration.Create(
            isEnabled: true, goalAmount: null, showProgress: false,
            suggestedAmounts: new List<decimal> { 10m }, allowCustomAmount: true,
            minAmount: 10.00m, maxAmount: 100.00m, collectionMessage: null).Value;

        var result = config.ValidateAmount(5.00m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least");
    }

    [Fact]
    public void ValidateAmount_AboveMaxAmount_ShouldFail()
    {
        var config = CollectionConfiguration.Create(
            isEnabled: true, goalAmount: null, showProgress: false,
            suggestedAmounts: new List<decimal> { 5m }, allowCustomAmount: true,
            minAmount: null, maxAmount: 100.00m, collectionMessage: null).Value;

        var result = config.ValidateAmount(150.00m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot exceed");
    }

    [Fact]
    public void ValidateAmount_BelowSystemMinimum_ShouldFail()
    {
        var config = CollectionConfiguration.Create(
            isEnabled: true, goalAmount: null, showProgress: false,
            suggestedAmounts: new List<decimal> { 5m }, allowCustomAmount: true,
            minAmount: null, maxAmount: null, collectionMessage: null).Value;

        var result = config.ValidateAmount(0.50m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least");
    }

    [Fact]
    public void ValidateAmount_WhenDisabled_ShouldFail()
    {
        var config = CollectionConfiguration.Disabled();

        var result = config.ValidateAmount(10.00m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not enabled");
    }

    [Fact]
    public void ValidateAmount_AtExactMinAmount_ShouldSucceed()
    {
        var config = CollectionConfiguration.Create(
            isEnabled: true, goalAmount: null, showProgress: false,
            suggestedAmounts: new List<decimal> { 5m }, allowCustomAmount: true,
            minAmount: 5.00m, maxAmount: null, collectionMessage: null).Value;

        var result = config.ValidateAmount(5.00m);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateAmount_AtExactMaxAmount_ShouldSucceed()
    {
        var config = CollectionConfiguration.Create(
            isEnabled: true, goalAmount: null, showProgress: false,
            suggestedAmounts: new List<decimal> { 5m }, allowCustomAmount: true,
            minAmount: null, maxAmount: 100.00m, collectionMessage: null).Value;

        var result = config.ValidateAmount(100.00m);

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Exactly At MINIMUM_COLLECTION_AMOUNT Boundary

    [Fact]
    public void Create_WithSuggestedAmountAtExactMinimum_ShouldSucceed()
    {
        var amounts = new List<decimal> { 1.00m }; // Exactly MINIMUM_COLLECTION_AMOUNT

        var result = CollectionConfiguration.Create(
            isEnabled: true, goalAmount: null, showProgress: false,
            suggestedAmounts: amounts, allowCustomAmount: false,
            minAmount: null, maxAmount: null, collectionMessage: null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMinAmountAtExactMinimum_ShouldSucceed()
    {
        var result = CollectionConfiguration.Create(
            isEnabled: true, goalAmount: null, showProgress: false,
            suggestedAmounts: new List<decimal> { 1m }, allowCustomAmount: true,
            minAmount: 1.00m, maxAmount: null, collectionMessage: null);

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Five Suggested Amounts (Boundary)

    [Fact]
    public void Create_WithExactlyFiveSuggestedAmounts_ShouldSucceed()
    {
        var amounts = new List<decimal> { 1m, 5m, 10m, 25m, 50m };

        var result = CollectionConfiguration.Create(
            isEnabled: true, goalAmount: null, showProgress: false,
            suggestedAmounts: amounts, allowCustomAmount: false,
            minAmount: null, maxAmount: null, collectionMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.SuggestedAmounts.Should().HaveCount(5);
    }

    #endregion
}
