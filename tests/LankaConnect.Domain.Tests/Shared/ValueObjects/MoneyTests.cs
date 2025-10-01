using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Domain.Tests.Shared.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmount_ShouldReturnSuccess()
    {
        var result = Money.Create(25.50m, Currency.USD);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(25.50m, result.Value.Amount);
        Assert.Equal(Currency.USD, result.Value.Currency);
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldReturnFailure()
    {
        var result = Money.Create(-10.00m, Currency.USD);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Amount cannot be negative", result.Errors);
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldReturnSuccess()
    {
        var result = Money.Create(0m, Currency.USD);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.Amount);
    }

    [Fact]
    public void Add_WithSameCurrency_ShouldReturnSum()
    {
        var money1 = Money.Create(15.25m, Currency.USD).Value;
        var money2 = Money.Create(10.75m, Currency.USD).Value;
        
        var result = money1.Add(money2);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(26.00m, result.Value.Amount);
        Assert.Equal(Currency.USD, result.Value.Currency);
    }

    [Fact]
    public void Add_WithDifferentCurrency_ShouldReturnFailure()
    {
        var money1 = Money.Create(15.25m, Currency.USD).Value;
        var money2 = Money.Create(10.75m, Currency.LKR).Value;
        
        var result = money1.Add(money2);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Cannot add money with different currencies", result.Errors);
    }

    [Fact]
    public void Subtract_WithSameCurrency_ShouldReturnDifference()
    {
        var money1 = Money.Create(25.00m, Currency.USD).Value;
        var money2 = Money.Create(10.50m, Currency.USD).Value;
        
        var result = money1.Subtract(money2);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(14.50m, result.Value.Amount);
    }

    [Fact]
    public void Subtract_ResultingInNegative_ShouldReturnFailure()
    {
        var money1 = Money.Create(10.00m, Currency.USD).Value;
        var money2 = Money.Create(15.00m, Currency.USD).Value;
        
        var result = money1.Subtract(money2);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Subtraction would result in negative amount", result.Errors);
    }

    [Fact]
    public void Multiply_WithPositiveMultiplier_ShouldReturnProduct()
    {
        var money = Money.Create(12.50m, Currency.USD).Value;
        
        var result = money.Multiply(3);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(37.50m, result.Value.Amount);
        Assert.Equal(Currency.USD, result.Value.Currency);
    }

    [Fact]
    public void Multiply_WithNegativeMultiplier_ShouldReturnFailure()
    {
        var money = Money.Create(12.50m, Currency.USD).Value;
        
        var result = money.Multiply(-2);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Multiplier cannot be negative", result.Errors);
    }

    [Fact]
    public void IsGreaterThan_WithSameCurrency_ShouldCompareCorrectly()
    {
        var money1 = Money.Create(25.00m, Currency.USD).Value;
        var money2 = Money.Create(15.00m, Currency.USD).Value;
        
        Assert.True(money1.IsGreaterThan(money2));
        Assert.False(money2.IsGreaterThan(money1));
    }

    [Fact]
    public void Equality_WithSameAmountAndCurrency_ShouldBeEqual()
    {
        var money1 = Money.Create(25.50m, Currency.USD).Value;
        var money2 = Money.Create(25.50m, Currency.USD).Value;
        
        Assert.Equal(money1, money2);
    }

    [Fact]
    public void Equality_WithDifferentCurrency_ShouldNotBeEqual()
    {
        var money1 = Money.Create(25.50m, Currency.USD).Value;
        var money2 = Money.Create(25.50m, Currency.LKR).Value;
        
        Assert.NotEqual(money1, money2);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var money = Money.Create(25.50m, Currency.USD).Value;
        
        var result = money.ToString();
        
        Assert.Equal("$25.50", result);
    }

    [Fact]
    public void ToString_WithLKR_ShouldReturnFormattedString()
    {
        var money = Money.Create(5000.00m, Currency.LKR).Value;
        
        var result = money.ToString();
        
        Assert.Equal("Rs 5,000.00", result);
    }
}