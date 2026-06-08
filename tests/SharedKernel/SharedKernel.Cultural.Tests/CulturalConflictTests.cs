namespace LankaConnect.SharedKernel.Cultural.Tests;

/// <summary>
/// Wave4.9.1.10 (2026-06-08): unit tests for the CulturalConflict value
/// object focusing on the highest-value invariants: factory outputs,
/// auto-resolve logic, and sensitivity-score enum mapping.
/// </summary>
public sealed class CulturalConflictTests
{
    [Fact]
    public void NoConflict_Produces_HasConflict_False()
    {
        var sut = CulturalConflict.NoConflict();

        sut.HasConflict.Should().BeFalse();
        sut.ConflictReason.Should().BeEmpty();
        sut.RecommendedStrategy.Should().Be(CulturalResolutionStrategy.NoAction);
        sut.ConflictSeverity.Should().Be(ReligiousObservanceLevel.None);
    }

    [Fact]
    public void CreateBuddhistConflict_Sets_HasConflict_True_And_Provides_Alternatives()
    {
        var proposed = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc);

        var sut = CulturalConflict.CreateBuddhistConflict(
            proposed, CulturalEventType.Poyaday, ReligiousObservanceLevel.High);

        sut.HasConflict.Should().BeTrue();
        sut.ConflictReason.Should().Contain("Poyaday");
        sut.AlternativeTimeSlots.Should().NotBeEmpty();
        sut.RecommendedStrategy.Should().Be(CulturalResolutionStrategy.RescheduleRecommended);
    }

    [Fact]
    public void CreateBuddhistConflict_VesakPoya_Highest_Severity_Yields_MustReschedule()
    {
        var proposed = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc);

        var sut = CulturalConflict.CreateBuddhistConflict(
            proposed, CulturalEventType.VesakPoya, ReligiousObservanceLevel.Highest);

        sut.RecommendedStrategy.Should().Be(CulturalResolutionStrategy.MustReschedule);
    }

    [Fact]
    public void CreateHinduConflict_Sets_HasConflict_True()
    {
        var proposed = new DateTime(2026, 10, 24, 19, 0, 0, DateTimeKind.Utc);

        var sut = CulturalConflict.CreateHinduConflict(
            proposed, CulturalEventType.Deepavali, ReligiousObservanceLevel.High);

        sut.HasConflict.Should().BeTrue();
        sut.ConflictReason.Should().Contain("Deepavali");
        sut.AlternativeTimeSlots.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateCulturalConflict_Generic_Uses_CustomReason()
    {
        var proposed = new DateTime(2026, 4, 14, 12, 0, 0, DateTimeKind.Utc);

        var sut = CulturalConflict.CreateCulturalConflict(
            proposed, CulturalEventType.TamilNewYear, customReason: "Family-time block", severity: ReligiousObservanceLevel.Medium);

        sut.HasConflict.Should().BeTrue();
        sut.ConflictReason.Should().Be("Family-time block");
        sut.RecommendedStrategy.Should().Be(CulturalResolutionStrategy.RescheduleRecommended);
        sut.CulturalGuidance.Should().Contain("TamilNewYear");
    }

    [Theory]
    [InlineData(ReligiousObservanceLevel.None, true)]
    [InlineData(ReligiousObservanceLevel.Low, true)]
    [InlineData(ReligiousObservanceLevel.Medium, true)]
    [InlineData(ReligiousObservanceLevel.High, false)]
    [InlineData(ReligiousObservanceLevel.Highest, false)]
    public void CanAutoResolve_Allows_Only_Up_To_Medium_Severity(
        ReligiousObservanceLevel severity, bool expected)
    {
        var sut = CulturalConflict.CreateCulturalConflict(
            DateTime.UtcNow, CulturalEventType.Community, "test", severity);

        sut.CanAutoResolve().Should().Be(expected);
    }

    [Theory]
    [InlineData(ReligiousObservanceLevel.None, 0)]
    [InlineData(ReligiousObservanceLevel.Low, 25)]
    [InlineData(ReligiousObservanceLevel.Medium, 50)]
    [InlineData(ReligiousObservanceLevel.High, 75)]
    [InlineData(ReligiousObservanceLevel.Highest, 100)]
    public void GetSensitivityScore_Maps_Each_Severity_To_Expected_Score(
        ReligiousObservanceLevel severity, int expectedScore)
    {
        var sut = CulturalConflict.CreateCulturalConflict(
            DateTime.UtcNow, CulturalEventType.Community, "test", severity);

        sut.GetSensitivityScore().Should().Be(expectedScore);
    }

    [Fact]
    public void Equality_NoConflict_Instances_Are_Equal()
    {
        var a = CulturalConflict.NoConflict();
        var b = CulturalConflict.NoConflict();

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
