using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

public sealed class GuardTests
{
    // ---------- NotNull<T> ----------

    [Fact]
    public void NotNull_NonNullReference_ReturnsValue()
    {
        var s = "hello";
        Guard.NotNull(s, nameof(s)).Should().BeSameAs(s);
    }

    [Fact]
    public void NotNull_Null_ThrowsArgumentNullException()
    {
        string? s = null;
        Action act = () => Guard.NotNull(s, nameof(s));

        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("s");
    }

    // ---------- NotNullOrWhitespace ----------

    [Fact]
    public void NotNullOrWhitespace_ValidString_ReturnsValue()
    {
        Guard.NotNullOrWhitespace("hello", "p").Should().Be("hello");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void NotNullOrWhitespace_InvalidString_Throws(string? value)
    {
        Action act = () => Guard.NotNullOrWhitespace(value, "p");

        if (value is null)
        {
            // ThrowIfNullOrWhitespace throws ArgumentException for empty/whitespace + ArgumentNullException for null
            act.Should().Throw<ArgumentException>(); // both ArgumentNullException and ArgumentException pass
        }
        else
        {
            act.Should().Throw<ArgumentException>();
        }
    }

    // ---------- NotEmpty (Guid) ----------

    [Fact]
    public void NotEmpty_NonEmptyGuid_ReturnsValue()
    {
        var g = Guid.NewGuid();
        Guard.NotEmpty(g, "p").Should().Be(g);
    }

    [Fact]
    public void NotEmpty_EmptyGuid_Throws()
    {
        Action act = () => Guard.NotEmpty(Guid.Empty, "p");

        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("p");
    }

    // ---------- NotNegative (int) ----------

    [Fact]
    public void NotNegative_Int_ZeroIsAllowed()
    {
        Guard.NotNegative(0, "p").Should().Be(0);
    }

    [Fact]
    public void NotNegative_Int_PositiveAllowed()
    {
        Guard.NotNegative(42, "p").Should().Be(42);
    }

    [Fact]
    public void NotNegative_Int_NegativeThrows()
    {
        Action act = () => Guard.NotNegative(-1, "p");

        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("p");
    }

    // ---------- Positive (int) ----------

    [Fact]
    public void Positive_Int_PositiveAllowed()
    {
        Guard.Positive(1, "p").Should().Be(1);
    }

    [Fact]
    public void Positive_Int_ZeroThrows()
    {
        Action act = () => Guard.Positive(0, "p");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Positive_Int_NegativeThrows()
    {
        Action act = () => Guard.Positive(-1, "p");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------- NotNegative (decimal) ----------

    [Fact]
    public void NotNegative_Decimal_ZeroAllowed()
    {
        Guard.NotNegative(0m, "p").Should().Be(0m);
    }

    [Fact]
    public void NotNegative_Decimal_PositiveAllowed()
    {
        Guard.NotNegative(1.50m, "p").Should().Be(1.50m);
    }

    [Fact]
    public void NotNegative_Decimal_NegativeThrows()
    {
        Action act = () => Guard.NotNegative(-0.01m, "p");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------- InRange (int) ----------

    [Theory]
    [InlineData(0, 0, 10)]
    [InlineData(5, 0, 10)]
    [InlineData(10, 0, 10)]
    public void InRange_InsideBounds_ReturnsValue(int value, int min, int max)
    {
        Guard.InRange(value, min, max, "p").Should().Be(value);
    }

    [Theory]
    [InlineData(-1, 0, 10)]
    [InlineData(11, 0, 10)]
    [InlineData(100, 0, 10)]
    public void InRange_OutsideBounds_Throws(int value, int min, int max)
    {
        Action act = () => Guard.InRange(value, min, max, "p");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
