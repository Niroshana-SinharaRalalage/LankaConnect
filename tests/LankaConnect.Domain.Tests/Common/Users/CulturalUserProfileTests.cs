using FluentAssertions;
using LankaConnect.Domain.Common.Users;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Tests.Common.Users;

/// <summary>
/// TDD RED PHASE: Tests for CulturalUserProfile (16 references - Tier 1 Priority)
/// These tests establish the contract for Sri Lankan cultural user profiling
/// </summary>
public class CulturalUserProfileTests
{
    [Test]
    public void CulturalUserProfile_ShouldHaveRequiredProperties()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var languages = new List<SouthAsianLanguage> { SouthAsianLanguage.Sinhala, SouthAsianLanguage.English };
        var preferences = new CulturalPreferences(
            primaryReligion: Religion.Buddhism,
            dietaryRestrictions: new[] { "Vegetarian" },
            culturalEvents: new[] { "Vesak", "Poson" }
        );

        // Act
        var profile = new CulturalUserProfile(
            userId: userId,
            primaryEthnicity: SriLankanEthnicity.Sinhala,
            languages: languages,
            region: GeographicRegion.WesternProvince,
            preferences: preferences,
            diasporaStatus: new DiasporaConnection(
                isInDiaspora: false,
                homelandConnectionStrength: 1.0,
                yearsAbroad: 0
            )
        );

        // Assert
        profile.UserId.Should().Be(userId);
        profile.PrimaryEthnicity.Should().Be(SriLankanEthnicity.Sinhala);
        profile.Languages.Should().HaveCount(2);
        profile.Region.Should().Be(GeographicRegion.WesternProvince);
        profile.Preferences.Should().NotBeNull();
        profile.DiasporaStatus.Should().NotBeNull();
        profile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void CulturalUserProfile_ShouldSupportAllSriLankanEthnicities()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var basePreferences = new CulturalPreferences(Religion.Buddhism, Array.Empty<string>(), Array.Empty<string>());

        // Act
        var sinhalaProfile = new CulturalUserProfile(
            userId, SriLankanEthnicity.Sinhala,
            new[] { SouthAsianLanguage.Sinhala, SouthAsianLanguage.English },
            GeographicRegion.WesternProvince, basePreferences, DiasporaConnection.Homeland);

        var tamilProfile = new CulturalUserProfile(
            userId, SriLankanEthnicity.Tamil,
            new[] { SouthAsianLanguage.Tamil, SouthAsianLanguage.English },
            GeographicRegion.NorthernProvince, basePreferences, DiasporaConnection.Homeland);

        var muslimProfile = new CulturalUserProfile(
            userId, SriLankanEthnicity.Moor,
            new[] { SouthAsianLanguage.Tamil, SouthAsianLanguage.English },
            GeographicRegion.EasternProvince, basePreferences, DiasporaConnection.Homeland);

        var burgherProfile = new CulturalUserProfile(
            userId, SriLankanEthnicity.Burgher,
            new[] { SouthAsianLanguage.English },
            GeographicRegion.WesternProvince, basePreferences, DiasporaConnection.Homeland);

        // Assert
        sinhalaProfile.GetCulturalAffinityScore(CulturalEvent.Vesak).Should().BeGreaterThan(0.8);
        tamilProfile.GetCulturalAffinityScore(CulturalEvent.TamilNewYear).Should().BeGreaterThan(0.8);
        muslimProfile.GetCulturalAffinityScore(CulturalEvent.Eid).Should().BeGreaterThan(0.8);
        burgherProfile.GetCulturalAffinityScore(CulturalEvent.Christmas).Should().BeGreaterThan(0.6);
    }

    [Test]
    public void CulturalUserProfile_ShouldValidateLanguageConsistency()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var inconsistentLanguages = new List<SouthAsianLanguage> { SouthAsianLanguage.Hindi }; // Not typical for Sri Lanka

        // Act
        var createWithInconsistentLanguages = () => new CulturalUserProfile(
            userId,
            SriLankanEthnicity.Sinhala,
            inconsistentLanguages,
            GeographicRegion.WesternProvince,
            new CulturalPreferences(Religion.Buddhism, Array.Empty<string>(), Array.Empty<string>()),
            DiasporaConnection.Homeland
        );

        // Assert - Should warn but not fail for diaspora flexibility
        var profile = createWithInconsistentLanguages.Should().NotThrow().Subject;
        profile.HasLanguageInconsistency().Should().BeTrue();
        profile.GetLanguageConsistencyWarnings().Should().Contain("Hindi not typical for Sri Lankan Sinhala ethnicity");
    }

    [Test]
    public void CulturalUserProfile_ShouldCalculateDiasporaEngagementLevel()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var diasporaConnection = new DiasporaConnection(
            isInDiaspora: true,
            homelandConnectionStrength: 0.7,
            yearsAbroad: 15
        );

        var profile = new CulturalUserProfile(
            userId,
            SriLankanEthnicity.Tamil,
            new[] { SouthAsianLanguage.Tamil, SouthAsianLanguage.English },
            GeographicRegion.International,
            new CulturalPreferences(Religion.Hinduism, Array.Empty<string>(), new[] { "TamilNewYear", "Deepavali" }),
            diasporaConnection
        );

        // Act
        var engagementLevel = profile.CalculateDiasporaEngagementLevel();

        // Assert
        engagementLevel.Should().BeGreaterThan(0.0);
        engagementLevel.Should().BeLessThan(1.0);
        // Strong connection (0.7) but long absence (15 years) should moderate engagement
        engagementLevel.Should().BeInRange(0.5, 0.8);
    }

    [Test]
    public void CulturalUserProfile_ShouldRecommendCulturalContent()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var preferences = new CulturalPreferences(
            primaryReligion: Religion.Buddhism,
            dietaryRestrictions: new[] { "Vegetarian" },
            culturalEvents: new[] { "Vesak", "Poson", "Esala" }
        );

        var profile = new CulturalUserProfile(
            userId,
            SriLankanEthnicity.Sinhala,
            new[] { SouthAsianLanguage.Sinhala, SouthAsianLanguage.English },
            GeographicRegion.CentralProvince,
            preferences,
            DiasporaConnection.Homeland
        );

        // Act
        var recommendations = profile.GetCulturalContentRecommendations();

        // Assert
        recommendations.Should().NotBeEmpty();
        recommendations.Should().Contain(r => r.ContentType == CulturalContentType.ReligiousEvent);
        recommendations.Should().Contain(r => r.ContentType == CulturalContentType.TraditionalFestival);
        recommendations.Should().Contain(r => r.Language == SouthAsianLanguage.Sinhala);
        recommendations.All(r => r.IsVegetarianFriendly).Should().BeTrue();
    }

    [Test]
    public void CulturalUserProfile_ShouldDetectCulturalConflicts()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var conflictingPreferences = new CulturalPreferences(
            primaryReligion: Religion.Buddhism,
            dietaryRestrictions: new[] { "Halal" }, // Inconsistent with Buddhism
            culturalEvents: new[] { "Vesak", "Eid" } // Mixed religious events
        );

        var profile = new CulturalUserProfile(
            userId,
            SriLankanEthnicity.Sinhala,
            new[] { SouthAsianLanguage.Sinhala },
            GeographicRegion.WesternProvince,
            conflictingPreferences,
            DiasporaConnection.Homeland
        );

        // Act
        var conflicts = profile.DetectCulturalConflicts();

        // Assert
        conflicts.Should().NotBeEmpty();
        conflicts.Should().Contain(c => c.ConflictType == CulturalConflictType.ReligionDietaryMismatch);
        conflicts.Should().Contain(c => c.ConflictType == CulturalConflictType.ReligionEventMismatch);
    }

    [Test]
    public void CulturalUserProfile_ShouldUpdatePreferences()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var originalPreferences = new CulturalPreferences(
            primaryReligion: Religion.Buddhism,
            dietaryRestrictions: Array.Empty<string>(),
            culturalEvents: new[] { "Vesak" }
        );

        var profile = new CulturalUserProfile(
            userId,
            SriLankanEthnicity.Sinhala,
            new[] { SouthAsianLanguage.Sinhala },
            GeographicRegion.WesternProvince,
            originalPreferences,
            DiasporaConnection.Homeland
        );

        var updatedPreferences = new CulturalPreferences(
            primaryReligion: Religion.Buddhism,
            dietaryRestrictions: new[] { "Vegetarian" },
            culturalEvents: new[] { "Vesak", "Poson" }
        );

        // Act
        var updatedProfile = profile.UpdatePreferences(updatedPreferences);

        // Assert
        updatedProfile.Preferences.DietaryRestrictions.Should().Contain("Vegetarian");
        updatedProfile.Preferences.CulturalEvents.Should().HaveCount(2);
        updatedProfile.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        updatedProfile.Version.Should().BeGreaterThan(profile.Version);
    }

    [Test]
    public void CulturalUserProfile_ShouldValidateRequiredFields()
    {
        // Arrange & Act
        var createWithNullUserId = () => new CulturalUserProfile(
            null!, // Invalid null userId
            SriLankanEthnicity.Sinhala,
            new[] { SouthAsianLanguage.Sinhala },
            GeographicRegion.WesternProvince,
            new CulturalPreferences(Religion.Buddhism, Array.Empty<string>(), Array.Empty<string>()),
            DiasporaConnection.Homeland
        );

        // Assert
        createWithNullUserId.Should().Throw<ArgumentNullException>()
            .WithMessage("User ID cannot be null");
    }
}

/// <summary>
/// Supporting types for cultural user profile testing
/// </summary>
public record CulturalPreferences(
    Religion PrimaryReligion,
    IReadOnlyList<string> DietaryRestrictions,
    IReadOnlyList<string> CulturalEvents
);

public record DiasporaConnection(
    bool IsInDiaspora,
    double HomelandConnectionStrength,
    int YearsAbroad
)
{
    public static DiasporaConnection Homeland => new(false, 1.0, 0);
}

public record CulturalContentRecommendation(
    CulturalContentType ContentType,
    SouthAsianLanguage Language,
    bool IsVegetarianFriendly,
    double RelevanceScore
);

public record CulturalConflict(
    CulturalConflictType ConflictType,
    string Description,
    string Resolution
);

public enum SriLankanEthnicity
{
    Sinhala,
    Tamil,
    Moor,
    Burgher,
    Malay,
    Mixed
}

public enum Religion
{
    Buddhism,
    Hinduism,
    Islam,
    Christianity,
    Other
}

public enum CulturalEvent
{
    Vesak,
    Poson,
    Esala,
    TamilNewYear,
    Eid,
    Christmas,
    Deepavali
}

public enum CulturalContentType
{
    ReligiousEvent,
    TraditionalFestival,
    CulturalHeritage,
    LanguageLearning,
    DiasporaStory
}

public enum CulturalConflictType
{
    ReligionDietaryMismatch,
    ReligionEventMismatch,
    LanguageEthnicityMismatch,
    RegionTraditionMismatch
}