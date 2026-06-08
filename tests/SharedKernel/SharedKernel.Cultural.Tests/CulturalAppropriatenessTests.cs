namespace LankaConnect.SharedKernel.Cultural.Tests;

/// <summary>
/// Wave4.9.1.10 (2026-06-08): unit tests for the CulturalAppropriateness
/// value object that moved into SharedKernel.Cultural during Wave 2.
/// Per route audit (G7): cultural types are internal; no direct API
/// surface; ArchTest + unit-test-per-moved-type is the coverage.
/// </summary>
public sealed class CulturalAppropriatenessTests
{
    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1.0)]
    [InlineData(1.01)]
    [InlineData(2.0)]
    public void Ctor_Throws_When_Value_OutOfRange(double invalidValue)
    {
        Action act = () => new CulturalAppropriateness(invalidValue, AppropriatenessLevel.Appropriate);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("value");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Ctor_Accepts_Boundary_And_Inner_Values(double validValue)
    {
        var sut = new CulturalAppropriateness(validValue, AppropriatenessLevel.Appropriate);

        sut.Value.Should().Be(validValue);
    }

    [Fact]
    public void Ctor_Defaults_Optional_Args_To_Empty()
    {
        var sut = new CulturalAppropriateness(0.7, AppropriatenessLevel.Appropriate);

        sut.CulturalContext.Should().BeEmpty();
        sut.CulturalFactors.Should().BeEmpty();
    }

    [Fact]
    public void Ctor_Normalizes_Null_Context_To_Empty_String()
    {
        var sut = new CulturalAppropriateness(0.7, AppropriatenessLevel.Appropriate, culturalContext: null!);

        sut.CulturalContext.Should().BeEmpty(
            because: "callers may pass null for context; the VO should normalize to '' for equality + serialization safety.");
    }

    [Theory]
    [InlineData(0.6, true)]
    [InlineData(0.7, true)]
    [InlineData(0.59, false)]
    [InlineData(0.0, false)]
    public void IsAppropriate_Crosses_Threshold_At_0p6(double value, bool expected)
    {
        var sut = new CulturalAppropriateness(value, AppropriatenessLevel.Appropriate);

        sut.IsAppropriate.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.8, true)]
    [InlineData(0.9, true)]
    [InlineData(0.79, false)]
    public void IsHighlyAppropriate_Crosses_Threshold_At_0p8(double value, bool expected)
    {
        var sut = new CulturalAppropriateness(value, AppropriatenessLevel.Appropriate);

        sut.IsHighlyAppropriate.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.39, true)]
    [InlineData(0.0, true)]
    [InlineData(0.4, false)]
    [InlineData(0.7, false)]
    public void IsInappropriate_Crosses_Threshold_Below_0p4(double value, bool expected)
    {
        var sut = new CulturalAppropriateness(value, AppropriatenessLevel.Appropriate);

        sut.IsInappropriate.Should().Be(expected);
    }

    public static IEnumerable<object[]> StaticFactoryCases() => new[]
    {
        new object[] { CulturalAppropriateness.HighlyAppropriate(), 0.9, AppropriatenessLevel.HighlyAppropriate },
        new object[] { CulturalAppropriateness.Appropriate(),       0.7, AppropriatenessLevel.Appropriate       },
        new object[] { CulturalAppropriateness.MildConcern(),       0.6, AppropriatenessLevel.MildConcern       },
        new object[] { CulturalAppropriateness.ModerateConcern(),   0.4, AppropriatenessLevel.ModerateConcern   },
        new object[] { CulturalAppropriateness.HighConcern(),       0.2, AppropriatenessLevel.HighConcern       },
        new object[] { CulturalAppropriateness.Inappropriate(),     0.1, AppropriatenessLevel.Inappropriate     },
    };

    [Theory]
    [MemberData(nameof(StaticFactoryCases))]
    public void StaticFactories_Produce_Expected_Value_And_Level(
        CulturalAppropriateness sut, double expectedValue, AppropriatenessLevel expectedLevel)
    {
        sut.Value.Should().Be(expectedValue);
        sut.Level.Should().Be(expectedLevel);
    }

    [Fact]
    public void Equality_Identical_Components_Are_Equal()
    {
        var a = new CulturalAppropriateness(0.7, AppropriatenessLevel.Appropriate, "ceremony");
        var b = new CulturalAppropriateness(0.7, AppropriatenessLevel.Appropriate, "ceremony");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_Different_Value_Distinguishes()
    {
        var a = new CulturalAppropriateness(0.7, AppropriatenessLevel.Appropriate, "ceremony");
        var b = new CulturalAppropriateness(0.8, AppropriatenessLevel.Appropriate, "ceremony");

        a.Should().NotBe(b);
    }
}
