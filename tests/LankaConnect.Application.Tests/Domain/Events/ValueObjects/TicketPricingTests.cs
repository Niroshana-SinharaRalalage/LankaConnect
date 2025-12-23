using FluentAssertions;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.UnitTests.Events.ValueObjects;

public class TicketPricingTests
{
    [Fact]
    public void Create_WithValidAdultAndChildPrices_ShouldSucceed()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(25.00m, Currency.USD).Value;
        var childAgeLimit = 12;

        // Act
        var result = TicketPricing.Create(adultPrice, childPrice, childAgeLimit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdultPrice.Should().Be(adultPrice);
        result.Value.ChildPrice.Should().Be(childPrice);
        result.Value.ChildAgeLimit.Should().Be(childAgeLimit);
        result.Value.HasChildPricing.Should().BeTrue();
    }

    [Fact]
    public void Create_WithAdultPriceOnly_ShouldSucceed()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;

        // Act
        var result = TicketPricing.Create(adultPrice, null, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdultPrice.Should().Be(adultPrice);
        result.Value.ChildPrice.Should().BeNull();
        result.Value.ChildAgeLimit.Should().BeNull();
        result.Value.HasChildPricing.Should().BeFalse();
    }

    [Fact]
    public void Create_WithNullAdultPrice_ShouldFail()
    {
        // Act
        var result = TicketPricing.Create(null, null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Adult price");
    }

    [Fact]
    public void Create_WithChildPriceButNoAgeLimit_ShouldFail()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(25.00m, Currency.USD).Value;

        // Act
        var result = TicketPricing.Create(adultPrice, childPrice, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("age limit");
    }

    [Fact]
    public void Create_WithAgeLimitButNoChildPrice_ShouldFail()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;

        // Act
        var result = TicketPricing.Create(adultPrice, null, 12);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Child price");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(19)]
    [InlineData(100)]
    public void Create_WithInvalidChildAgeLimit_ShouldFail(int invalidAge)
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(25.00m, Currency.USD).Value;

        // Act
        var result = TicketPricing.Create(adultPrice, childPrice, invalidAge);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("between 1 and 18");
    }

    [Fact]
    public void Create_WithChildPriceGreaterThanAdultPrice_ShouldFail()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(75.00m, Currency.USD).Value;

        // Act
        var result = TicketPricing.Create(adultPrice, childPrice, 12);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot be greater than");
    }

    [Fact]
    public void Create_WithDifferentCurrencies_ShouldFail()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(25.00m, Currency.LKR).Value;

        // Act
        var result = TicketPricing.Create(adultPrice, childPrice, 12);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("same currency");
    }

    [Theory]
    [InlineData(5, true)]   // Child
    [InlineData(12, true)]  // Exactly at limit (inclusive)
    [InlineData(13, false)] // Adult
    [InlineData(0, false)]  // Invalid age
    public void IsChildAge_WithVariousAges_ShouldReturnCorrectResult(int age, bool expected)
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(25.00m, Currency.USD).Value;
        var pricing = TicketPricing.Create(adultPrice, childPrice, 12).Value;

        // Act
        var result = pricing.IsChildAge(age);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsChildAge_WhenNoChildPricing_ShouldAlwaysReturnFalse()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var pricing = TicketPricing.Create(adultPrice, null, null).Value;

        // Act & Assert
        pricing.IsChildAge(5).Should().BeFalse();
        pricing.IsChildAge(12).Should().BeFalse();
        pricing.IsChildAge(18).Should().BeFalse();
    }

    [Fact]
    public void CalculateForCategory_ForChild_ShouldReturnChildPrice()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(25.00m, Currency.USD).Value;
        var pricing = TicketPricing.Create(adultPrice, childPrice, 12).Value;

        // Act
        var result = pricing.CalculateForCategory(AgeCategory.Child);

        // Assert
        result.Should().Be(childPrice);
    }

    [Fact]
    public void CalculateForCategory_ForAdult_ShouldReturnAdultPrice()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(25.00m, Currency.USD).Value;
        var pricing = TicketPricing.Create(adultPrice, childPrice, 12).Value;

        // Act
        var result = pricing.CalculateForCategory(AgeCategory.Adult);

        // Assert
        result.Should().Be(adultPrice);
    }

    [Fact]
    public void CalculateForCategory_WithNoChildPricing_ShouldReturnAdultPrice()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var pricing = TicketPricing.Create(adultPrice, null, null).Value;

        // Act
        var result = pricing.CalculateForCategory(AgeCategory.Child);

        // Assert
        result.Should().Be(adultPrice);
    }

    [Fact]
    public void ValueObjectEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(25.00m, Currency.USD).Value;
        var pricing1 = TicketPricing.Create(adultPrice, childPrice, 12).Value;
        var pricing2 = TicketPricing.Create(adultPrice, childPrice, 12).Value;

        // Assert
        pricing1.Should().Be(pricing2);
        (pricing1 == pricing2).Should().BeTrue();
    }

    [Fact]
    public void ValueObjectEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice1 = Money.Create(25.00m, Currency.USD).Value;
        var childPrice2 = Money.Create(30.00m, Currency.USD).Value;
        var pricing1 = TicketPricing.Create(adultPrice, childPrice1, 12).Value;
        var pricing2 = TicketPricing.Create(adultPrice, childPrice2, 12).Value;

        // Assert
        pricing1.Should().NotBe(pricing2);
        (pricing1 == pricing2).Should().BeFalse();
    }
}
