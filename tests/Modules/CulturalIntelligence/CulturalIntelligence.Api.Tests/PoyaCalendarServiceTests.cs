using LankaConnect.Modules.CulturalIntelligence.Contracts.Services;
using LankaConnect.Modules.CulturalIntelligence.Infrastructure.Services;

namespace LankaConnect.Modules.CulturalIntelligence.Api.Tests;

/// <summary>
/// Wave 8.5 GAP-1 Part B (2026-07-19): unit tests for the real seed-file-backed
/// <see cref="PoyaCalendarService"/> that supersedes StubCulturalCalendar.
///
/// Tests cover:
/// - Known-good major poya dates round-trip (Vesak 2026 = 2026-05-31 in the seeded
///   Sri Lankan government calendar) — this is the D-13 Option A closure invariant.
/// - Non-poya date returns false / null.
/// - Category-driven ClassifyEventNature semantics.
/// - GetEventAppropriateness slots (religious/secular/major-poya conflict path).
/// - GetSignificantDates round-trip.
/// - ValidateEventAgainstCalendar surface + suggestion generation.
/// - FestivalPeriod lookup (major poya = 3-day window, monthly = 1-day).
/// </summary>
public sealed class PoyaCalendarServiceTests
{
    private readonly ICulturalCalendar _sut = new PoyaCalendarService();

    // ------- Known-good poya dates ---------------------------------------

    [Fact]
    public void IsPoyaDay_ReturnsTrue_ForSeededVesak2026()
    {
        _sut.IsPoyaDay(new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc))
            .Should().BeTrue(because: "Vesak 2026 is seeded as 2026-05-31 per the seed file.");
    }

    [Fact]
    public void IsPoyaDay_ReturnsTrue_ForSeededPoson2026()
    {
        _sut.IsPoyaDay(new DateTime(2026, 6, 29, 0, 0, 0, DateTimeKind.Utc))
            .Should().BeTrue(because: "Poson 2026 is seeded as 2026-06-29.");
    }

    [Fact]
    public void IsPoyaDay_ReturnsFalse_ForNonPoyaDate()
    {
        _sut.IsPoyaDay(new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc))
            .Should().BeFalse(because: "mid-May 2026 is not a poya day in the seeded calendar.");
    }

    [Fact]
    public void IsPoyaDay_IgnoresTimeComponent()
    {
        _sut.IsPoyaDay(new DateTime(2026, 5, 31, 14, 30, 45, DateTimeKind.Utc))
            .Should().BeTrue(because: "poya-day match uses .Date so time-of-day is irrelevant.");
    }

    [Fact]
    public void IsPoyaDay_ReturnsFalse_ForYearNotSeeded()
    {
        _sut.IsPoyaDay(new DateTime(2029, 5, 1, 0, 0, 0, DateTimeKind.Utc))
            .Should().BeFalse(because: "seed data covers 2026-2028; 2029 must return false.");
    }

    // ------- ClassifyEventNature -----------------------------------------

    [Fact]
    public void ClassifyEventNature_ReturnsReligious_ForCategoryReligious()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null, Category: "Religious");
        _sut.ClassifyEventNature(ctx).Should().Be(EventNature.Religious);
    }

    [Fact]
    public void ClassifyEventNature_ReturnsSecular_ForPartyCategoryKeyword()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null, Category: "New Year Party");
        _sut.ClassifyEventNature(ctx).Should().Be(EventNature.Secular);
    }

    [Fact]
    public void ClassifyEventNature_ReturnsReligious_ForTempleKeyword()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null, Category: "Temple Dhamma Talk");
        _sut.ClassifyEventNature(ctx).Should().Be(EventNature.Religious);
    }

    [Fact]
    public void ClassifyEventNature_DefaultsToCultural_WhenCategoryNull()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null);
        _sut.ClassifyEventNature(ctx).Should().Be(EventNature.Cultural);
    }

    // ------- GetEventAppropriateness -------------------------------------

    [Fact]
    public void GetEventAppropriateness_ReligiousOnPoya_ReturnsHighScore()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null, Category: "Religious");
        var result = _sut.GetEventAppropriateness(ctx, new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc));
        result.Value.Should().Be(0.95, because: "religious events on any poya day get the highest appropriateness slot.");
    }

    [Fact]
    public void GetEventAppropriateness_SecularOnMajorPoya_ReturnsConflictScore()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null, Category: "Party");
        var result = _sut.GetEventAppropriateness(ctx, new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc));
        result.Value.Should().Be(0.35,
            because: "Secular events on major poya (Vesak) trigger the conflict-signal slot.");
    }

    [Fact]
    public void GetEventAppropriateness_NonPoyaDate_ReturnsNeutralScore()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null, Category: "Religious");
        var result = _sut.GetEventAppropriateness(ctx, new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc));
        result.Value.Should().Be(0.7, because: "no poya-day match returns the neutral 0.7 default.");
    }

    // ------- GetSignificantDates -----------------------------------------

    [Fact]
    public void GetSignificantDates_Returns12DatesFor2026()
    {
        var dates = _sut.GetSignificantDates(2026);
        dates.Should().HaveCount(12, because: "the 2026 calendar has 12 monthly poya days seeded.");
    }

    [Fact]
    public void GetSignificantDates_TagsVesakAsCritical()
    {
        var dates = _sut.GetSignificantDates(2026);
        var vesak = dates.FirstOrDefault(d => d.Name.Contains("Vesak", StringComparison.OrdinalIgnoreCase));
        vesak.Should().NotBeNull();
        vesak!.Level.Should().Be(SignificanceLevel.Critical, because: "Vesak is a major poya day.");
    }

    [Fact]
    public void GetSignificantDates_TagsDuruthuAsHigh()
    {
        var dates = _sut.GetSignificantDates(2026);
        var duruthu = dates.FirstOrDefault(d => d.Name.Contains("Duruthu", StringComparison.OrdinalIgnoreCase));
        duruthu.Should().NotBeNull();
        duruthu!.Level.Should().Be(SignificanceLevel.High, because: "monthly poyas are High (non-Critical).");
    }

    [Fact]
    public void GetSignificantDates_ReturnsEmpty_ForYearNotSeeded()
    {
        _sut.GetSignificantDates(2030).Should().BeEmpty(because: "2030 is beyond the seeded range.");
    }

    // ------- ValidateEventAgainstCalendar --------------------------------

    [Fact]
    public void ValidateEventAgainstCalendar_ReturnsInvalid_ForSecularEventOnVesak()
    {
        var ctx = new EventCulturalContext(
            Guid.NewGuid(),
            StartDate: new DateTime(2026, 5, 31, 20, 0, 0, DateTimeKind.Utc),
            Category: "Party");

        var result = _sut.ValidateEventAgainstCalendar(ctx);
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("Vesak");
        result.Suggestions.Should().NotBeNull();
        result.Suggestions!.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateEventAgainstCalendar_ReturnsValid_ForReligiousEventOnPoya()
    {
        var ctx = new EventCulturalContext(
            Guid.NewGuid(),
            StartDate: new DateTime(2026, 5, 31, 20, 0, 0, DateTimeKind.Utc),
            Category: "Religious");

        var result = _sut.ValidateEventAgainstCalendar(ctx);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateEventAgainstCalendar_ReturnsValid_ForTbdEvent()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null, Category: "Party");
        var result = _sut.ValidateEventAgainstCalendar(ctx);
        result.IsValid.Should().BeTrue(because: "no date → no calendar conflict possible.");
    }

    // ------- GetFestivalPeriod -------------------------------------------

    [Fact]
    public void GetFestivalPeriod_Vesak2026_ReturnsThreeDayWindow()
    {
        var period = _sut.GetFestivalPeriod("Vesak", 2026);
        (period.EndDate - period.StartDate).Days.Should().Be(2,
            because: "major poyas (Vesak/Poson/Esala/Unduwap) get a 3-day window (2 days between start and end inclusive).");
        period.Name.Should().Contain("Vesak");
    }

    [Fact]
    public void GetFestivalPeriod_Duruthu2026_ReturnsSingleDayWindow()
    {
        var period = _sut.GetFestivalPeriod("Duruthu", 2026);
        period.StartDate.Should().Be(period.EndDate,
            because: "monthly poyas get a single-day window.");
        period.Name.Should().Contain("Duruthu");
    }

    [Fact]
    public void GetFestivalPeriod_UnknownFestival_ReturnsFallback()
    {
        var period = _sut.GetFestivalPeriod("NotAFestival", 2026);
        period.StartDate.Year.Should().Be(2026);
        period.Name.Should().Be("NotAFestival");
    }

    // ------- IsOptimalFestivalTiming -------------------------------------

    [Fact]
    public void IsOptimalFestivalTiming_ReturnsTrue_WhenEventStartInWindow()
    {
        var ctx = new EventCulturalContext(
            Guid.NewGuid(),
            StartDate: new DateTime(2026, 5, 31, 12, 0, 0, DateTimeKind.Utc));
        var period = _sut.GetFestivalPeriod("Vesak", 2026);
        _sut.IsOptimalFestivalTiming(ctx, period).Should().BeTrue();
    }

    [Fact]
    public void IsOptimalFestivalTiming_ReturnsFalse_WhenEventStartOutsideWindow()
    {
        var ctx = new EventCulturalContext(
            Guid.NewGuid(),
            StartDate: new DateTime(2026, 12, 31, 12, 0, 0, DateTimeKind.Utc));
        var period = _sut.GetFestivalPeriod("Vesak", 2026);
        _sut.IsOptimalFestivalTiming(ctx, period).Should().BeFalse();
    }

    [Fact]
    public void IsOptimalFestivalTiming_ReturnsFalse_WhenEventStartIsNull()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null);
        var period = _sut.GetFestivalPeriod("Vesak", 2026);
        _sut.IsOptimalFestivalTiming(ctx, period).Should().BeFalse();
    }

    // ------- CalculateAppropriateness with cultural background -----------

    [Fact]
    public void CalculateAppropriateness_BuddhistBackground_BoostsReligiousEventOnPoya()
    {
        var ctx = new EventCulturalContext(
            Guid.NewGuid(),
            StartDate: new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            Category: "Religious");

        var buddhist = _sut.CalculateAppropriateness(ctx, "Buddhist").Value;
        var other = _sut.CalculateAppropriateness(ctx, "Other").Value;

        buddhist.Should().BeGreaterThan(other,
            because: "Buddhist background gets a +0.10 modifier on religious poya-day events (capped at 1.0).");
    }

    [Fact]
    public void CalculateAppropriateness_UnknownBackground_ReturnsBaseScore()
    {
        var ctx = new EventCulturalContext(
            Guid.NewGuid(),
            StartDate: new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            Category: "Cultural");

        var result = _sut.CalculateAppropriateness(ctx, "None");
        result.Value.Should().Be(0.7, because: "non-poya cultural events return neutral base with no background modifier.");
    }

    // ------- DiasporaFriendliness ----------------------------------------

    [Fact]
    public void GetDiasporaFriendliness_SecularEvent_IsVeryHigh()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null, Category: "Party");
        _sut.GetDiasporaFriendliness(ctx).Should().Be(DiasporaFriendliness.VeryHigh);
    }

    [Fact]
    public void GetDiasporaFriendliness_ReligiousEvent_IsModerate()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null, Category: "Religious");
        _sut.GetDiasporaFriendliness(ctx).Should().Be(DiasporaFriendliness.Moderate);
    }

    [Fact]
    public void GetDiasporaFriendliness_CulturalEvent_IsHigh()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null, Category: "Cultural");
        _sut.GetDiasporaFriendliness(ctx).Should().Be(DiasporaFriendliness.High);
    }

    // ------- ClassifyEventType -------------------------------------------

    [Fact]
    public void ClassifyEventType_ReturnsCategory_WhenNonNull()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null, Category: "Religious");
        _sut.ClassifyEventType(ctx).Should().Be("Religious");
    }

    [Fact]
    public void ClassifyEventType_ReturnsGeneral_WhenCategoryNull()
    {
        var ctx = new EventCulturalContext(Guid.NewGuid(), StartDate: null);
        _sut.ClassifyEventType(ctx).Should().Be("General");
    }
}
