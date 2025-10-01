using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Multi-Cultural Community value object for Phase 8 Global Platform Expansion
/// Represents diverse South Asian diaspora communities with 6M+ population reach
/// Revenue Impact: $8.5M-$15.2M additional annual revenue potential
/// </summary>
public sealed class MultiCulturalCommunity : ValueObject
{
    public CulturalCommunity CommunityType { get; }
    public IEnumerable<Language> PrimaryLanguages { get; }
    public IEnumerable<Language> SecondaryLanguages { get; }
    public IEnumerable<ReligiousAffiliation> ReligiousAffiliations { get; }
    public CalendarSystem PrimaryCalendarSystem { get; }
    public IEnumerable<CalendarSystem> SecondaryCalendarSystems { get; }
    public GeographicDistribution DiasporaDistribution { get; }
    public CulturalObservanceLevel ObservanceLevel { get; }
    public bool HasRegionalVariations { get; }
    public IEnumerable<string> RegionalVariations { get; }

    private MultiCulturalCommunity(
        CulturalCommunity communityType,
        IEnumerable<Language> primaryLanguages,
        IEnumerable<Language> secondaryLanguages,
        IEnumerable<ReligiousAffiliation> religiousAffiliations,
        CalendarSystem primaryCalendarSystem,
        IEnumerable<CalendarSystem> secondaryCalendarSystems,
        GeographicDistribution diasporaDistribution,
        CulturalObservanceLevel observanceLevel,
        bool hasRegionalVariations,
        IEnumerable<string> regionalVariations)
    {
        CommunityType = communityType;
        PrimaryLanguages = primaryLanguages?.ToList() ?? new List<Language>();
        SecondaryLanguages = secondaryLanguages?.ToList() ?? new List<Language>();
        ReligiousAffiliations = religiousAffiliations?.ToList() ?? new List<ReligiousAffiliation>();
        PrimaryCalendarSystem = primaryCalendarSystem;
        SecondaryCalendarSystems = secondaryCalendarSystems?.ToList() ?? new List<CalendarSystem>();
        DiasporaDistribution = diasporaDistribution;
        ObservanceLevel = observanceLevel;
        HasRegionalVariations = hasRegionalVariations;
        RegionalVariations = regionalVariations?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Create Indian Hindu community with North Indian variations
    /// Target: 2.4M+ Indian Americans
    /// </summary>
    public static MultiCulturalCommunity CreateIndianHinduNorth(GeographicDistribution distribution)
    {
        return new MultiCulturalCommunity(
            CulturalCommunity.IndianHinduNorth,
            new[] { Language.Hindi, Language.English },
            new[] { Language.Punjabi, Language.Gujarati, Language.Bengali },
            new[] { ReligiousAffiliation.Hindu },
            CalendarSystem.HinduLunisolar,
            new[] { CalendarSystem.GregorianCalendar },
            distribution,
            CulturalObservanceLevel.High,
            true,
            new[] { "Delhi", "Punjab", "Rajasthan", "Uttar Pradesh", "Gujarat", "Maharashtra" });
    }

    /// <summary>
    /// Create Indian Hindu community with South Indian variations
    /// Target: 1.8M+ South Indian Americans
    /// </summary>
    public static MultiCulturalCommunity CreateIndianHinduSouth(GeographicDistribution distribution)
    {
        return new MultiCulturalCommunity(
            CulturalCommunity.IndianHinduSouth,
            new[] { Language.Tamil, Language.Telugu, Language.English },
            new[] { Language.Malayalam, Language.Kannada },
            new[] { ReligiousAffiliation.Hindu },
            CalendarSystem.TamilCalendar,
            new[] { CalendarSystem.TeluguCalendar, CalendarSystem.malayalamCalendar, CalendarSystem.GregorianCalendar },
            distribution,
            CulturalObservanceLevel.High,
            true,
            new[] { "Tamil Nadu", "Andhra Pradesh", "Karnataka", "Kerala" });
    }

    /// <summary>
    /// Create Pakistani Muslim community with cultural traditions
    /// Target: 500K+ Pakistani Americans
    /// </summary>
    public static MultiCulturalCommunity CreatePakistaniMuslim(GeographicDistribution distribution)
    {
        return new MultiCulturalCommunity(
            CulturalCommunity.PakistaniSunniMuslim,
            new[] { Language.Urdu, Language.English },
            new[] { Language.Punjabi, Language.Sindhi, Language.Pashto },
            new[] { ReligiousAffiliation.Sunni, ReligiousAffiliation.Shia },
            CalendarSystem.IslamicPakistani,
            new[] { CalendarSystem.GregorianCalendar },
            distribution,
            CulturalObservanceLevel.High,
            true,
            new[] { "Punjab", "Sindh", "Balochistan", "Khyber Pakhtunkhwa" });
    }

    /// <summary>
    /// Create Bangladeshi Muslim community with Bengali cultural heritage
    /// Target: 200K+ Bangladeshi Americans
    /// </summary>
    public static MultiCulturalCommunity CreateBangladeshiMuslim(GeographicDistribution distribution)
    {
        return new MultiCulturalCommunity(
            CulturalCommunity.BangladeshiSunniMuslim,
            new[] { Language.Bengali, Language.English },
            new[] { Language.Arabic },
            new[] { ReligiousAffiliation.Sunni },
            CalendarSystem.BengaliCalendar,
            new[] { CalendarSystem.IslamicBangladeshi, CalendarSystem.GregorianCalendar },
            distribution,
            CulturalObservanceLevel.High,
            true,
            new[] { "Dhaka", "Chittagong", "Sylhet", "Rangpur", "Khulna" });
    }

    /// <summary>
    /// Create Sikh community with Punjabi cultural traditions
    /// Target: 500K+ Sikh Americans
    /// </summary>
    public static MultiCulturalCommunity CreateSikhCommunity(GeographicDistribution distribution)
    {
        return new MultiCulturalCommunity(
            CulturalCommunity.IndianSikh,
            new[] { Language.Punjabi, Language.English },
            new[] { Language.Hindi },
            new[] { ReligiousAffiliation.Sikh },
            CalendarSystem.NanakshahiCalendar,
            new[] { CalendarSystem.HinduLunisolar, CalendarSystem.GregorianCalendar },
            distribution,
            CulturalObservanceLevel.High,
            true,
            new[] { "Punjab India", "Punjab Pakistan", "California Central Valley", "New York Metro" });
    }

    /// <summary>
    /// Create multi-cultural blended South Asian community
    /// Target: Cross-cultural families and second-generation diaspora
    /// </summary>
    public static MultiCulturalCommunity CreateMultiCulturalSouthAsian(GeographicDistribution distribution)
    {
        return new MultiCulturalCommunity(
            CulturalCommunity.MultiCulturalSouthAsian,
            new[] { Language.English },
            new[] { Language.Hindi, Language.Urdu, Language.Tamil, Language.Bengali, Language.Punjabi },
            new[] { ReligiousAffiliation.Hindu, ReligiousAffiliation.Sunni, ReligiousAffiliation.Sikh, ReligiousAffiliation.Buddhist },
            CalendarSystem.DiasporaHybrid,
            new[] { CalendarSystem.GregorianCalendar, CalendarSystem.HinduLunisolar, CalendarSystem.IslamicHijri },
            distribution,
            CulturalObservanceLevel.Medium,
            true,
            new[] { "Second Generation", "Inter-cultural Families", "Diaspora Blended" });
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CommunityType;
        yield return string.Join(",", PrimaryLanguages.OrderBy(l => l));
        yield return string.Join(",", SecondaryLanguages.OrderBy(l => l));
        yield return string.Join(",", ReligiousAffiliations.OrderBy(r => r));
        yield return PrimaryCalendarSystem;
        yield return string.Join(",", SecondaryCalendarSystems.OrderBy(c => c));
        yield return DiasporaDistribution;
        yield return ObservanceLevel;
        yield return HasRegionalVariations;
        yield return string.Join(",", RegionalVariations.OrderBy(r => r));
    }
}

// Supporting enums and value objects for Multi-Cultural expansion
public enum Language
{
    English = 1,
    Sinhala = 2,
    Tamil = 3,
    Hindi = 10,
    Urdu = 11,
    Bengali = 12,
    Punjabi = 13,
    Sindhi = 14,
    Gujarati = 15,
    Marathi = 16,
    Telugu = 17,
    Malayalam = 18,
    Kannada = 19,
    Pashto = 20,
    Arabic = 21,
    Persian = 22
}

public enum ReligiousAffiliation
{
    Buddhist = 1,
    Hindu = 2,
    Sunni = 3,
    Shia = 4,
    Christian = 5,
    Sikh = 6,
    Jain = 7,
    Secular = 8
}

public enum CulturalObservanceLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4
}

public record GeographicDistribution
{
    public IEnumerable<string> MajorCities { get; init; } = new List<string>();
    public IEnumerable<string> States { get; init; } = new List<string>();
    public int EstimatedPopulation { get; init; }
    public decimal PopulationGrowthRate { get; init; }
    public IEnumerable<string> CommunityOrganizations { get; init; } = new List<string>();
}