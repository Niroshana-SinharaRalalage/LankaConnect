using FluentAssertions;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.UnitTests.Events.ValueObjects;

public class RevenueBreakdownTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldCalculateBreakdownCorrectly()
    {
        // Arrange
        var ticketPrice = Money.Create(100m, Currency.USD).Value;
        var taxRate = 0.07m; // 7% California tax

        // Act
        var result = RevenueBreakdown.Create(
            ticketPrice,
            taxRate,
            platformCommissionRate: 0.02m,
            stripeFeeRate: 0.029m,
            stripeFeeFixed: 0.30m
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        var breakdown = result.Value;

        // Verify Gross Amount
        breakdown.GrossAmount.Amount.Should().Be(100m);

        // Verify Sales Tax calculation: $100 - ($100 / 1.07) = $6.54
        breakdown.SalesTaxAmount.Amount.Should().BeApproximately(6.54m, 0.01m);

        // Verify Taxable Amount: $100 - $6.54 = $93.46
        breakdown.TaxableAmount.Amount.Should().BeApproximately(93.46m, 0.01m);

        // Verify Stripe Fee: ($93.46 × 0.029) + $0.30 = $3.01
        breakdown.StripeFeeAmount.Amount.Should().BeApproximately(3.01m, 0.01m);

        // Verify Platform Commission: $93.46 × 0.02 = $1.87
        breakdown.PlatformCommission.Amount.Should().BeApproximately(1.87m, 0.01m);

        // Verify Organizer Payout: $93.46 - $3.01 - $1.87 = $88.58
        breakdown.OrganizerPayout.Amount.Should().BeApproximately(88.58m, 0.01m);

        // Verify rates stored correctly
        breakdown.SalesTaxRate.Should().Be(0.07m);
        breakdown.StripeFeeRate.Should().Be(0.029m);
        breakdown.StripeFeeFixed.Should().Be(0.30m);
        breakdown.PlatformCommissionRate.Should().Be(0.02m);
    }

    [Fact]
    public void Create_WithZeroPrice_ShouldReturnZeroBreakdown()
    {
        // Arrange
        var ticketPrice = Money.Zero(Currency.USD);

        // Act
        var result = RevenueBreakdown.Create(ticketPrice, 0m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var breakdown = result.Value;

        breakdown.GrossAmount.Amount.Should().Be(0m);
        breakdown.SalesTaxAmount.Amount.Should().Be(0m);
        breakdown.StripeFeeAmount.Amount.Should().Be(0m);
        breakdown.PlatformCommission.Amount.Should().Be(0m);
        breakdown.OrganizerPayout.Amount.Should().Be(0m);
    }

    [Fact]
    public void Create_WithNullGrossAmount_ShouldFail()
    {
        // Act
        var result = RevenueBreakdown.Create(null!, 0.07m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Gross amount");
    }

    [Fact]
    public void Create_WithNegativeTaxRate_ShouldFail()
    {
        // Arrange
        var ticketPrice = Money.Create(100m, Currency.USD).Value;

        // Act
        var result = RevenueBreakdown.Create(ticketPrice, -0.05m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("tax rate");
    }

    [Fact]
    public void Create_WithTaxRateOver50Percent_ShouldFail()
    {
        // Arrange
        var ticketPrice = Money.Create(100m, Currency.USD).Value;

        // Act
        var result = RevenueBreakdown.Create(ticketPrice, 0.51m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("tax rate");
    }

    [Theory]
    [InlineData(10.00, 0.07, 8.59)]   // $10 ticket, 7% tax → $8.59 payout
    [InlineData(25.00, 0.08, 21.71)]  // $25 ticket, 8% tax → $21.71 payout
    [InlineData(50.00, 0.065, 44.35)] // $50 ticket, 6.5% tax → $44.35 payout
    [InlineData(100.00, 0.0725, 88.37)] // $100 ticket, 7.25% tax → $88.37 payout
    public void Create_WithVariousPrices_ShouldCalculateCorrectPayout(
        decimal price,
        decimal taxRate,
        decimal expectedPayout)
    {
        // Arrange
        var ticketPrice = Money.Create(price, Currency.USD).Value;

        // Act
        var result = RevenueBreakdown.Create(ticketPrice, taxRate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrganizerPayout.Amount.Should().BeApproximately(expectedPayout, 0.02m);
    }

    [Fact]
    public void Create_WithZeroTaxRate_ShouldCalculateWithoutTax()
    {
        // Arrange
        var ticketPrice = Money.Create(100m, Currency.USD).Value;

        // Act
        var result = RevenueBreakdown.Create(ticketPrice, 0m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var breakdown = result.Value;

        // No sales tax
        breakdown.SalesTaxAmount.Amount.Should().Be(0m);

        // Taxable amount equals gross amount
        breakdown.TaxableAmount.Amount.Should().Be(100m);

        // Stripe Fee: ($100 × 0.029) + $0.30 = $3.20
        breakdown.StripeFeeAmount.Amount.Should().BeApproximately(3.20m, 0.01m);

        // Platform Commission: $100 × 0.02 = $2.00
        breakdown.PlatformCommission.Amount.Should().BeApproximately(2.00m, 0.01m);

        // Organizer Payout: $100 - $3.20 - $2.00 = $94.80
        breakdown.OrganizerPayout.Amount.Should().BeApproximately(94.80m, 0.01m);
    }

    [Fact]
    public void Create_WithVeryLowPrice_ShouldNotProduceNegativePayout()
    {
        // Arrange - Very low ticket price
        var ticketPrice = Money.Create(2.00m, Currency.USD).Value;
        var taxRate = 0.07m;

        // Act
        var result = RevenueBreakdown.Create(ticketPrice, taxRate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var breakdown = result.Value;

        // Even with low price, payout should not be negative
        breakdown.OrganizerPayout.Amount.Should().BeGreaterThan(0m);
        // Corrected calculation: $2 - ($2/1.07 tax) - stripe fee - commission = $1.48
        breakdown.OrganizerPayout.Amount.Should().BeApproximately(1.48m, 0.01m);
    }

    [Fact]
    public void Create_WithPriceTooLowForFees_ShouldFail()
    {
        // Arrange - Price so low that fees exceed it
        var ticketPrice = Money.Create(0.20m, Currency.USD).Value;
        var taxRate = 0.07m;

        // Act
        var result = RevenueBreakdown.Create(ticketPrice, taxRate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("negative payout");
    }

    [Fact]
    public void Create_WithDifferentCurrencies_ShouldPreserveCurrency()
    {
        // Arrange
        var ticketPriceUSD = Money.Create(100m, Currency.USD).Value;
        var ticketPriceLKR = Money.Create(100m, Currency.LKR).Value;

        // Act
        var resultUSD = RevenueBreakdown.Create(ticketPriceUSD, 0.07m);
        var resultLKR = RevenueBreakdown.Create(ticketPriceLKR, 0.07m);

        // Assert
        resultUSD.IsSuccess.Should().BeTrue();
        resultLKR.IsSuccess.Should().BeTrue();

        resultUSD.Value.GrossAmount.Currency.Should().Be(Currency.USD);
        resultUSD.Value.OrganizerPayout.Currency.Should().Be(Currency.USD);

        resultLKR.Value.GrossAmount.Currency.Should().Be(Currency.LKR);
        resultLKR.Value.OrganizerPayout.Currency.Should().Be(Currency.LKR);
    }

    [Fact]
    public void Create_WithCustomCommissionRates_ShouldUseProvidedRates()
    {
        // Arrange
        var ticketPrice = Money.Create(100m, Currency.USD).Value;
        var customPlatformRate = 0.03m; // 3% instead of 2%
        var customStripeFeeRate = 0.05m; // 5% instead of 2.9%
        var customStripeFeeFixed = 0.50m; // $0.50 instead of $0.30

        // Act
        var result = RevenueBreakdown.Create(
            ticketPrice,
            salesTaxRate: 0m,
            platformCommissionRate: customPlatformRate,
            stripeFeeRate: customStripeFeeRate,
            stripeFeeFixed: customStripeFeeFixed
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        var breakdown = result.Value;

        // Platform Commission: $100 × 0.03 = $3.00
        breakdown.PlatformCommission.Amount.Should().BeApproximately(3.00m, 0.01m);

        // Stripe Fee: ($100 × 0.05) + $0.50 = $5.50
        breakdown.StripeFeeAmount.Amount.Should().BeApproximately(5.50m, 0.01m);

        // Organizer Payout: $100 - $3.00 - $5.50 = $91.50
        breakdown.OrganizerPayout.Amount.Should().BeApproximately(91.50m, 0.01m);
    }

    [Fact]
    public void ValueObjectEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var ticketPrice = Money.Create(100m, Currency.USD).Value;
        var breakdown1 = RevenueBreakdown.Create(ticketPrice, 0.07m).Value;
        var breakdown2 = RevenueBreakdown.Create(ticketPrice, 0.07m).Value;

        // Assert
        breakdown1.Should().Be(breakdown2);
        (breakdown1 == breakdown2).Should().BeTrue();
    }

    [Fact]
    public void ValueObjectEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var ticketPrice = Money.Create(100m, Currency.USD).Value;
        var breakdown1 = RevenueBreakdown.Create(ticketPrice, 0.07m).Value;
        var breakdown2 = RevenueBreakdown.Create(ticketPrice, 0.08m).Value; // Different tax rate

        // Assert
        breakdown1.Should().NotBe(breakdown2);
        (breakdown1 == breakdown2).Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldCalculate_TaxInclusivePricing_Correctly()
    {
        // Arrange - Example from requirements: $100 ticket in CA (7.25% tax)
        var ticketPrice = Money.Create(100m, Currency.USD).Value;
        var californiaRate = 0.0725m;

        // Act
        var result = RevenueBreakdown.Create(ticketPrice, californiaRate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var breakdown = result.Value;

        // This test verifies the exact formula from the plan:
        // 1. Gross Amount (GA) = $100.00
        breakdown.GrossAmount.Amount.Should().Be(100.00m);

        // 2. Sales Tax (ST) = $100 - ($100 / 1.0725) = $6.76
        var expectedTax = 100m - (100m / 1.0725m);
        breakdown.SalesTaxAmount.Amount.Should().BeApproximately(expectedTax, 0.01m);

        // 3. Taxable Amount (TA) = $100 - $6.76 = $93.24
        var expectedTaxable = 100m - expectedTax;
        breakdown.TaxableAmount.Amount.Should().BeApproximately(expectedTaxable, 0.01m);

        // 4. Stripe Fee (SF) = ($93.24 × 0.029) + $0.30 = $3.00
        var expectedStripeFee = (expectedTaxable * 0.029m) + 0.30m;
        breakdown.StripeFeeAmount.Amount.Should().BeApproximately(expectedStripeFee, 0.01m);

        // 5. Platform Commission (PC) = $93.24 × 0.02 = $1.86
        var expectedCommission = expectedTaxable * 0.02m;
        breakdown.PlatformCommission.Amount.Should().BeApproximately(expectedCommission, 0.01m);

        // 6. Organizer Payout (OP) = $93.24 - $3.00 - $1.86 = $88.38
        var expectedPayout = expectedTaxable - expectedStripeFee - expectedCommission;
        breakdown.OrganizerPayout.Amount.Should().BeApproximately(expectedPayout, 0.01m);
    }

    [Theory]
    [InlineData(0.0, 0.0, 0.0, 100.00)] // Free event equivalent
    [InlineData(0.02, 0.029, 0.30, 94.80)] // Default rates, no tax
    [InlineData(0.05, 0.035, 0.50, 91.00)] // Higher commission/fees, no tax: $100 - $5 - $4 = $91
    public void Create_WithVariousRateCombinations_ShouldCalculateCorrectly(
        decimal platformRate,
        decimal stripeFeeRate,
        decimal stripeFeeFixed,
        decimal expectedPayoutNoTax)
    {
        // Arrange
        var ticketPrice = Money.Create(100m, Currency.USD).Value;

        // Act
        var result = RevenueBreakdown.Create(
            ticketPrice,
            salesTaxRate: 0m,
            platformCommissionRate: platformRate,
            stripeFeeRate: stripeFeeRate,
            stripeFeeFixed: stripeFeeFixed
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrganizerPayout.Amount.Should().BeApproximately(expectedPayoutNoTax, 0.01m);
    }
}
