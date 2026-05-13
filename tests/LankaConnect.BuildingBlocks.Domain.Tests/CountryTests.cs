using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

public sealed class CountryTests
{
    [Theory]
    [InlineData("LK", "Sri Lanka")]
    [InlineData("US", "United States")]
    [InlineData("IN", "India")]
    [InlineData("GB", "United Kingdom")]
    [InlineData("AU", "Australia")]
    [InlineData("CA", "Canada")]
    public void StaticInstances_HaveExpectedFields(string code, string name)
    {
        var c = Country.FromCode(code);

        c.Code.Should().Be(code);
        c.Name.Should().Be(name);
    }

    [Fact]
    public void All_Returns6Countries()
    {
        Country.All.Should().HaveCount(6);
        Country.All.Select(c => c.Code).Should().BeEquivalentTo(
            new[] { "LK", "US", "IN", "GB", "AU", "CA" });
    }

    [Theory]
    [InlineData("us")]
    [InlineData("US")]
    [InlineData("Us")]
    public void FromCode_CaseInsensitive(string code)
    {
        Country.FromCode(code).Should().Be(Country.US);
    }

    [Fact]
    public void FromCode_Unsupported_Throws()
    {
        Action act = () => Country.FromCode("ZZ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*not in the supported registry*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void FromCode_NullOrEmpty_Throws(string? code)
    {
        Action act = () => Country.FromCode(code!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryFromCode_Known_ReturnsSome()
    {
        var maybe = Country.TryFromCode("LK");

        maybe.HasValue.Should().BeTrue();
        maybe.Value.Should().Be(Country.LK);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ZZ")]
    public void TryFromCode_Unknown_ReturnsNone(string? code)
    {
        Country.TryFromCode(code).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Equality_SameCode_AreEqual()
    {
        Country.US.Should().Be(Country.FromCode("US"));
    }

    [Fact]
    public void Equality_DifferentCodes_NotEqual()
    {
        Country.LK.Should().NotBe(Country.US);
    }

    [Fact]
    public void ToString_ReturnsCode()
    {
        Country.LK.ToString().Should().Be("LK");
    }
}
