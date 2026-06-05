using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.SharedKernel.Money;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

public sealed class MoneyTests
{
    private static readonly Money Five = new(5m, Currency.USD);
    private static readonly Money Ten = new(10m, Currency.USD);
    private static readonly Money TenLkr = new(10m, Currency.LKR);

    // ---------- Construction ----------

    [Fact]
    public void Constructor_StoresAmountAndCurrency()
    {
        var money = new Money(99.99m, Currency.LKR);

        money.Amount.Should().Be(99.99m);
        money.Currency.Should().Be(Currency.LKR);
    }

    [Fact]
    public void Constructor_NullCurrency_Throws()
    {
        Action act = () => _ = new Money(1m, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Zero_ReturnsZeroAmountInGivenCurrency()
    {
        var zero = Money.Zero(Currency.USD);

        zero.IsZero.Should().BeTrue();
        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be(Currency.USD);
    }

    // ---------- Predicates ----------

    [Fact]
    public void IsZero_OnPositive_False()
    {
        Five.IsZero.Should().BeFalse();
    }

    [Fact]
    public void IsPositive_OnPositive_True()
    {
        Five.IsPositive.Should().BeTrue();
        Five.IsNegative.Should().BeFalse();
    }

    [Fact]
    public void IsNegative_OnNegative_True()
    {
        var negative = new Money(-5m, Currency.USD);

        negative.IsNegative.Should().BeTrue();
        negative.IsPositive.Should().BeFalse();
    }

    // ---------- Rounding + Negate + Abs ----------

    [Fact]
    public void RoundToCurrency_UsesCurrencyDecimalDigits()
    {
        var unrounded = new Money(10.555m, Currency.USD); // 2 digits → rounds to 10.56 (banker's: .55 → even = .56)
        var rounded = unrounded.RoundToCurrency();

        rounded.Amount.Should().Be(10.56m);
    }

    [Fact]
    public void RoundToCurrency_BankersRounding_TiesToEven()
    {
        // 10.5 with 0 digits → rounds to 10 (nearest even)
        // 11.5 with 0 digits → rounds to 12 (nearest even)
        // We're testing the policy via 2 digits: 10.005 → 10.00 (.0 is even)
        var rounded = new Money(10.005m, Currency.USD).RoundToCurrency();
        rounded.Amount.Should().Be(10.00m);
    }

    [Fact]
    public void Negate_FlipsSign()
    {
        var negated = Five.Negate();

        negated.Amount.Should().Be(-5m);
        negated.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Abs_OnNegative_ReturnsPositive()
    {
        var abs = new Money(-5m, Currency.USD).Abs();
        abs.Amount.Should().Be(5m);
    }

    [Fact]
    public void Abs_OnPositive_Unchanged()
    {
        Five.Abs().Amount.Should().Be(5m);
    }

    // ---------- Arithmetic (same currency) ----------

    [Fact]
    public void Addition_SameCurrency_Sums()
    {
        var sum = Five + Ten;
        sum.Amount.Should().Be(15m);
        sum.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Subtraction_SameCurrency_Subtracts()
    {
        var diff = Ten - Five;
        diff.Amount.Should().Be(5m);
    }

    [Fact]
    public void UnaryMinus_Negates()
    {
        var negated = -Five;
        negated.Amount.Should().Be(-5m);
        negated.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Multiplication_ByScalar_Scales()
    {
        (Five * 3m).Amount.Should().Be(15m);
        (3m * Five).Amount.Should().Be(15m);
    }

    [Fact]
    public void Division_ByScalar_Divides()
    {
        (Ten / 2m).Amount.Should().Be(5m);
    }

    [Fact]
    public void Division_ByZero_Throws()
    {
        Action act = () => _ = Ten / 0m;
        act.Should().Throw<DivideByZeroException>();
    }

    // ---------- Arithmetic (cross currency) ----------

    [Fact]
    public void Addition_CrossCurrency_Throws()
    {
        Action act = () => _ = Five + TenLkr;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*USD and LKR*mixed-currency operations are forbidden*");
    }

    [Fact]
    public void Subtraction_CrossCurrency_Throws()
    {
        Action act = () => _ = Five - TenLkr;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Comparison_CrossCurrency_Throws()
    {
        Action act = () => _ = Five < TenLkr;
        act.Should().Throw<InvalidOperationException>();
    }

    // ---------- Comparison ----------

    [Fact]
    public void LessThan_SameCurrency_True()
    {
        (Five < Ten).Should().BeTrue();
        (Ten < Five).Should().BeFalse();
    }

    [Fact]
    public void GreaterThan_SameCurrency_True()
    {
        (Ten > Five).Should().BeTrue();
    }

    [Fact]
    public void LessThanOrEqual_EqualAmounts_True()
    {
        var anotherFive = new Money(5m, Currency.USD);
        (Five <= anotherFive).Should().BeTrue();
        (Five >= anotherFive).Should().BeTrue();
    }

    // ---------- Equality (via ValueObject) ----------

    [Fact]
    public void Equality_SameAmountSameCurrency_AreEqual()
    {
        var a = new Money(5m, Currency.USD);
        var b = new Money(5m, Currency.USD);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentAmount_NotEqual()
    {
        Five.Should().NotBe(Ten);
    }

    [Fact]
    public void Equality_DifferentCurrency_NotEqual()
    {
        Five.Should().NotBe(new Money(5m, Currency.LKR));
    }

    // ---------- ToString ----------

    [Fact]
    public void ToString_FormatsSymbolAmountAndCode()
    {
        var money = new Money(123.45m, Currency.USD);

        money.ToString().Should().Be("$123.45 USD");
    }

    [Fact]
    public void ToString_LKR_FormatsCorrectly()
    {
        var money = new Money(1500m, Currency.LKR);
        money.ToString().Should().Be("Rs1500.00 LKR");
    }
}
