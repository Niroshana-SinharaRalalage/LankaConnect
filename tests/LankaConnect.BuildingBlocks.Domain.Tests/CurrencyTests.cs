using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.SharedKernel.Money;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("USD", "US Dollar", "$")]
    [InlineData("LKR", "Sri Lankan Rupee", "Rs")]
    [InlineData("INR", "Indian Rupee", "₹")]
    [InlineData("GBP", "British Pound", "£")]
    [InlineData("EUR", "Euro", "€")]
    [InlineData("AUD", "Australian Dollar", "A$")]
    [InlineData("CAD", "Canadian Dollar", "C$")]
    public void StaticInstances_HaveExpectedFields(string code, string name, string symbol)
    {
        var c = Currency.FromCode(code);

        c.Code.Should().Be(code);
        c.Name.Should().Be(name);
        c.Symbol.Should().Be(symbol);
        c.DecimalDigits.Should().Be(2);
    }

    [Fact]
    public void All_Returns7Currencies()
    {
        Currency.All.Should().HaveCount(7);
        Currency.All.Select(c => c.Code).Should().BeEquivalentTo(
            new[] { "USD", "LKR", "INR", "GBP", "EUR", "AUD", "CAD" });
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("USD")]
    [InlineData("UsD")]
    public void FromCode_CaseInsensitive(string code)
    {
        Currency.FromCode(code).Should().Be(Currency.USD);
    }

    [Fact]
    public void FromCode_Unsupported_Throws()
    {
        Action act = () => Currency.FromCode("JPY");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*not in the supported registry*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void FromCode_NullOrEmpty_Throws(string? code)
    {
        Action act = () => Currency.FromCode(code!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryFromCode_Known_ReturnsSome()
    {
        var maybe = Currency.TryFromCode("LKR");

        maybe.HasValue.Should().BeTrue();
        maybe.Value.Should().Be(Currency.LKR);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("XYZ")]
    public void TryFromCode_Unknown_ReturnsNone(string? code)
    {
        Currency.TryFromCode(code).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Equality_SameCode_AreEqual()
    {
        var a = Currency.USD;
        var b = Currency.FromCode("USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCodes_NotEqual()
    {
        Currency.USD.Should().NotBe(Currency.LKR);
        (Currency.USD == Currency.LKR).Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsCode()
    {
        Currency.USD.ToString().Should().Be("USD");
    }
}
