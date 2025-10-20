using System;
using System.Collections.Generic;
using System.Linq;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Shared;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Common.Users;

/// <summary>
/// Cultural user profile for Sri Lankan diaspora community
/// Represents comprehensive cultural identity including ethnicity, language, region, and diaspora status
/// Created during emergency session to resolve 30 missing type errors
/// </summary>
public class CulturalUserProfile
{
    public UserId UserId { get; private set; }
    public SriLankanEthnicity PrimaryEthnicity { get; private set; }
    public IReadOnlyList<SouthAsianLanguage> Languages { get; private set; }
    public GeographicRegion Region { get; private set; }
    public CulturalPreferences Preferences { get; private set; }
    public DiasporaConnection DiasporaStatus { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public int Version { get; private set; }

    public CulturalUserProfile(
        UserId userId,
        SriLankanEthnicity primaryEthnicity,
        IEnumerable<SouthAsianLanguage> languages,
        GeographicRegion region,
        CulturalPreferences preferences,
        DiasporaConnection diasporaStatus)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId), "User ID cannot be null");
        PrimaryEthnicity = primaryEthnicity;
        Languages = languages.ToList().AsReadOnly();
        Region = region;
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        DiasporaStatus = diasporaStatus ?? throw new ArgumentNullException(nameof(diasporaStatus));
        CreatedAt = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;
        Version = 1;
    }

    public double GetCulturalAffinityScore(CulturalEvent culturalEvent)
    {
        // Simplified implementation for emergency - proper scoring to be implemented
        return 0.8;
    }

    public bool HasLanguageInconsistency()
    {
        // Simplified implementation for emergency
        return false;
    }

    public IReadOnlyList<string> GetLanguageConsistencyWarnings()
    {
        // Simplified implementation for emergency
        return new List<string>().AsReadOnly();
    }

    public double CalculateDiasporaEngagementLevel()
    {
        // Simplified implementation for emergency
        if (!DiasporaStatus.IsInDiaspora) return 1.0;

        var connectionFactor = DiasporaStatus.HomelandConnectionStrength;
        var timeFactor = Math.Max(0, 1.0 - (DiasporaStatus.YearsAbroad / 50.0));
        return (connectionFactor + timeFactor) / 2.0;
    }

    public IReadOnlyList<CulturalContentRecommendation> GetCulturalContentRecommendations()
    {
        // Simplified implementation for emergency
        return new List<CulturalContentRecommendation>().AsReadOnly();
    }

    public IReadOnlyList<CulturalConflict> DetectCulturalConflicts()
    {
        // Simplified implementation for emergency
        return new List<CulturalConflict>().AsReadOnly();
    }

    public CulturalUserProfile UpdatePreferences(CulturalPreferences updatedPreferences)
    {
        return new CulturalUserProfile(
            UserId,
            PrimaryEthnicity,
            Languages,
            Region,
            updatedPreferences,
            DiasporaStatus)
        {
            CreatedAt = this.CreatedAt,
            LastUpdated = DateTime.UtcNow,
            Version = this.Version + 1
        };
    }
}

// Supporting value objects
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

// Supporting enums
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
