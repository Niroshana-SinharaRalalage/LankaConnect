using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// TDD London School: Outside-In test suite for Pricing Strategy System
/// Follows mock-driven development with behavior verification over state testing
/// Tests define contracts and object collaborations through mock expectations
/// </summary>
public class PricingStrategyTests
{
    private readonly Money _basePrice;
    private readonly DateTime _currentDate;
    private readonly DateTime _earlyBirdCutoff;

    public PricingStrategyTests()
    {
        _basePrice = new Money(100m, Currency.USD);
        _currentDate = new DateTime(2025, 1, 1);
        _earlyBirdCutoff = new DateTime(2025, 1, 15);
    }

    #region StandardPricing Tests - Behavior Verification

    [Fact]
    public void StandardPricing_Should_Return_Base_Price_Without_Modification()
    {
        // Arrange
        var pricing = StandardPricing.Create(_basePrice).Value;

        // Act
        var result = pricing.CalculatePrice(1, _currentDate);

        // Assert - Verify behavior: no discount applied
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(100m);
        result.Value.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void StandardPricing_Should_Multiply_Price_By_Quantity()
    {
        // Arrange
        var pricing = StandardPricing.Create(_basePrice).Value;

        // Act
        var result = pricing.CalculatePrice(5, _currentDate);

        // Assert - Verify interaction: correct multiplication behavior
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(500m);
    }

    [Fact]
    public void StandardPricing_Should_Reject_Negative_Quantity()
    {
        // Arrange
        var pricing = StandardPricing.Create(_basePrice).Value;

        // Act
        var result = pricing.CalculatePrice(-1, _currentDate);

        // Assert - Verify error handling behavior
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Quantity must be greater than zero");
    }

    [Fact]
    public void StandardPricing_Should_Reject_Zero_Quantity()
    {
        // Arrange
        var pricing = StandardPricing.Create(_basePrice).Value;

        // Act
        var result = pricing.CalculatePrice(0, _currentDate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Quantity must be greater than zero");
    }

    [Fact]
    public void StandardPricing_Create_Should_Reject_Null_Base_Price()
    {
        // Act
        var result = StandardPricing.Create(null!);

        // Assert - Verify validation contract
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Base price is required");
    }

    [Fact]
    public void StandardPricing_Create_Should_Reject_Zero_Base_Price()
    {
        // Arrange
        var zeroPrice = new Money(0m, Currency.USD);

        // Act
        var result = StandardPricing.Create(zeroPrice);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Base price must be greater than zero");
    }

    #endregion

    #region EarlyBirdPricing Tests - Date-Based Behavior Verification

    [Fact]
    public void EarlyBirdPricing_Should_Apply_Discount_Before_Cutoff_Date()
    {
        // Arrange - Mock scenario: booking before early bird deadline
        var discountPercentage = 20m; // 20% discount
        var bookingDate = new DateTime(2025, 1, 10); // Before cutoff
        var pricing = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff, discountPercentage).Value;

        // Act
        var result = pricing.CalculatePrice(1, bookingDate);

        // Assert - Verify discount applied correctly
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(80m); // 100 - 20% = 80
    }

    [Fact]
    public void EarlyBirdPricing_Should_Not_Apply_Discount_On_Cutoff_Date()
    {
        // Arrange - Mock scenario: booking exactly on deadline
        var discountPercentage = 20m;
        var bookingDate = _earlyBirdCutoff; // Exactly on cutoff
        var pricing = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff, discountPercentage).Value;

        // Act
        var result = pricing.CalculatePrice(1, bookingDate);

        // Assert - Verify behavior: no discount on cutoff date
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(100m); // Full price
    }

    [Fact]
    public void EarlyBirdPricing_Should_Not_Apply_Discount_After_Cutoff_Date()
    {
        // Arrange - Mock scenario: late booking
        var discountPercentage = 20m;
        var bookingDate = new DateTime(2025, 1, 20); // After cutoff
        var pricing = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff, discountPercentage).Value;

        // Act
        var result = pricing.CalculatePrice(1, bookingDate);

        // Assert - Verify collaboration: fallback to standard pricing
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(100m);
    }

    [Fact]
    public void EarlyBirdPricing_Should_Apply_Discount_To_Multiple_Quantities()
    {
        // Arrange
        var discountPercentage = 25m; // 25% discount
        var bookingDate = new DateTime(2025, 1, 5); // Before cutoff
        var pricing = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff, discountPercentage).Value;

        // Act
        var result = pricing.CalculatePrice(4, bookingDate);

        // Assert - Verify interaction: discount then multiply
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(300m); // (100 - 25%) * 4 = 75 * 4 = 300
    }

    [Fact]
    public void EarlyBirdPricing_Create_Should_Reject_Invalid_Discount_Percentage_Too_High()
    {
        // Act
        var result = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff, 101m);

        // Assert - Verify validation contract
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Discount percentage must be between 0 and 100");
    }

    [Fact]
    public void EarlyBirdPricing_Create_Should_Reject_Invalid_Discount_Percentage_Negative()
    {
        // Act
        var result = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff, -10m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Discount percentage must be between 0 and 100");
    }

    [Fact]
    public void EarlyBirdPricing_Create_Should_Accept_Zero_Discount()
    {
        // Act
        var result = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff, 0m);

        // Assert - Verify edge case behavior
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EarlyBirdPricing_Create_Should_Accept_Maximum_Discount()
    {
        // Act
        var result = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff, 100m);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EarlyBirdPricing_Create_Should_Reject_Null_Base_Price()
    {
        // Act
        var result = EarlyBirdPricing.Create(null!, _earlyBirdCutoff, 20m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Base price is required");
    }

    #endregion

    #region GroupDiscountPricing Tests - Threshold-Based Behavior Verification

    [Fact]
    public void GroupDiscountPricing_Should_Not_Apply_Discount_Below_Threshold()
    {
        // Arrange - Mock scenario: individual purchase
        var minGroupSize = 5;
        var discountPercentage = 15m;
        var pricing = GroupDiscountPricing.Create(_basePrice, minGroupSize, discountPercentage).Value;

        // Act
        var result = pricing.CalculatePrice(3, _currentDate);

        // Assert - Verify behavior: no group discount
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(300m); // 100 * 3, no discount
    }

    [Fact]
    public void GroupDiscountPricing_Should_Apply_Discount_At_Threshold()
    {
        // Arrange - Mock scenario: exactly at group size threshold
        var minGroupSize = 5;
        var discountPercentage = 15m;
        var pricing = GroupDiscountPricing.Create(_basePrice, minGroupSize, discountPercentage).Value;

        // Act
        var result = pricing.CalculatePrice(5, _currentDate);

        // Assert - Verify interaction: discount triggered at threshold
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(425m); // (100 - 15%) * 5 = 85 * 5 = 425
    }

    [Fact]
    public void GroupDiscountPricing_Should_Apply_Discount_Above_Threshold()
    {
        // Arrange - Mock scenario: large group booking
        var minGroupSize = 5;
        var discountPercentage = 15m;
        var pricing = GroupDiscountPricing.Create(_basePrice, minGroupSize, discountPercentage).Value;

        // Act
        var result = pricing.CalculatePrice(10, _currentDate);

        // Assert - Verify collaboration: discount applied to all tickets
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(850m); // (100 - 15%) * 10 = 85 * 10 = 850
    }

    [Fact]
    public void GroupDiscountPricing_Should_Handle_Large_Discount_Correctly()
    {
        // Arrange
        var minGroupSize = 3;
        var discountPercentage = 50m; // 50% discount
        var pricing = GroupDiscountPricing.Create(_basePrice, minGroupSize, discountPercentage).Value;

        // Act
        var result = pricing.CalculatePrice(6, _currentDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(300m); // (100 - 50%) * 6 = 50 * 6 = 300
    }

    [Fact]
    public void GroupDiscountPricing_Create_Should_Reject_Invalid_Group_Size_Zero()
    {
        // Act
        var result = GroupDiscountPricing.Create(_basePrice, 0, 15m);

        // Assert - Verify validation contract
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Minimum group size must be greater than 1");
    }

    [Fact]
    public void GroupDiscountPricing_Create_Should_Reject_Invalid_Group_Size_One()
    {
        // Act
        var result = GroupDiscountPricing.Create(_basePrice, 1, 15m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Minimum group size must be greater than 1");
    }

    [Fact]
    public void GroupDiscountPricing_Create_Should_Reject_Invalid_Discount_Percentage()
    {
        // Act
        var result = GroupDiscountPricing.Create(_basePrice, 5, 150m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Discount percentage must be between 0 and 100");
    }

    [Fact]
    public void GroupDiscountPricing_Create_Should_Reject_Null_Base_Price()
    {
        // Act
        var result = GroupDiscountPricing.Create(null!, 5, 15m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Base price is required");
    }

    #endregion

    #region Combined Scenarios - Complex Behavior Verification

    [Fact]
    public void Different_Pricing_Strategies_Should_Calculate_Independently()
    {
        // Arrange - Mock scenario: multiple pricing strategies for same event
        var standardPricing = StandardPricing.Create(_basePrice).Value;
        var earlyBirdPricing = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff, 20m).Value;
        var groupPricing = GroupDiscountPricing.Create(_basePrice, 5, 15m).Value;
        var bookingDate = new DateTime(2025, 1, 10);

        // Act
        var standardResult = standardPricing.CalculatePrice(1, bookingDate);
        var earlyBirdResult = earlyBirdPricing.CalculatePrice(1, bookingDate);
        var groupResult = groupPricing.CalculatePrice(5, bookingDate);

        // Assert - Verify each strategy's independent behavior
        standardResult.Value.Amount.Should().Be(100m);
        earlyBirdResult.Value.Amount.Should().Be(80m);
        groupResult.Value.Amount.Should().Be(425m);
    }

    [Fact]
    public void Pricing_Strategies_Should_Respect_Currency_From_Base_Price()
    {
        // Arrange - Mock scenario: different currencies
        var lkrPrice = new Money(10000m, Currency.LKR);
        var standardPricing = StandardPricing.Create(lkrPrice).Value;
        var earlyBirdPricing = EarlyBirdPricing.Create(lkrPrice, _earlyBirdCutoff, 20m).Value;

        // Act
        var standardResult = standardPricing.CalculatePrice(1, _currentDate);
        var earlyBirdResult = earlyBirdPricing.CalculatePrice(1, new DateTime(2025, 1, 10));

        // Assert - Verify currency preservation in calculations
        standardResult.Value.Currency.Should().Be(Currency.LKR);
        earlyBirdResult.Value.Currency.Should().Be(Currency.LKR);
        earlyBirdResult.Value.Amount.Should().Be(8000m); // 10000 - 20%
    }

    #endregion

    #region Value Object Equality Tests - Contract Verification

    [Fact]
    public void StandardPricing_Should_Be_Equal_With_Same_Base_Price()
    {
        // Arrange
        var pricing1 = StandardPricing.Create(_basePrice).Value;
        var pricing2 = StandardPricing.Create(new Money(100m, Currency.USD)).Value;

        // Assert - Verify value object equality contract
        pricing1.Should().Be(pricing2);
    }

    [Fact]
    public void StandardPricing_Should_Not_Be_Equal_With_Different_Base_Price()
    {
        // Arrange
        var pricing1 = StandardPricing.Create(_basePrice).Value;
        var pricing2 = StandardPricing.Create(new Money(200m, Currency.USD)).Value;

        // Assert
        pricing1.Should().NotBe(pricing2);
    }

    [Fact]
    public void EarlyBirdPricing_Should_Be_Equal_With_Same_Properties()
    {
        // Arrange
        var pricing1 = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff, 20m).Value;
        var pricing2 = EarlyBirdPricing.Create(new Money(100m, Currency.USD), _earlyBirdCutoff, 20m).Value;

        // Assert
        pricing1.Should().Be(pricing2);
    }

    [Fact]
    public void EarlyBirdPricing_Should_Not_Be_Equal_With_Different_Cutoff_Date()
    {
        // Arrange
        var pricing1 = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff, 20m).Value;
        var pricing2 = EarlyBirdPricing.Create(_basePrice, _earlyBirdCutoff.AddDays(5), 20m).Value;

        // Assert
        pricing1.Should().NotBe(pricing2);
    }

    [Fact]
    public void GroupDiscountPricing_Should_Be_Equal_With_Same_Properties()
    {
        // Arrange
        var pricing1 = GroupDiscountPricing.Create(_basePrice, 5, 15m).Value;
        var pricing2 = GroupDiscountPricing.Create(new Money(100m, Currency.USD), 5, 15m).Value;

        // Assert
        pricing1.Should().Be(pricing2);
    }

    [Fact]
    public void GroupDiscountPricing_Should_Not_Be_Equal_With_Different_Group_Size()
    {
        // Arrange
        var pricing1 = GroupDiscountPricing.Create(_basePrice, 5, 15m).Value;
        var pricing2 = GroupDiscountPricing.Create(_basePrice, 10, 15m).Value;

        // Assert
        pricing1.Should().NotBe(pricing2);
    }

    #endregion
}
