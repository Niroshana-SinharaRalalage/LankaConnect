using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Extended Cultural Event for Multi-Cultural Calendar Engine
/// Supports diverse South Asian diaspora communities beyond Sri Lankan heritage
/// Phase 8 Global Platform Expansion component
/// </summary>
public class CulturalEvent : ValueObject
{
    public DateTime Date { get; init; }
    public string EnglishName { get; init; }
    public string NativeName { get; init; }
    public string SecondaryName { get; init; }
    public CulturalCommunity PrimaryCommunity { get; init; }
    public IEnumerable<CulturalCommunity> SecondaryCommunities { get; init; }
    public CalendarSystem CalendarSystem { get; init; }
    public CulturalEventType EventType { get; init; }
    
    // Event Classification Properties
    public bool IsMajorFestival { get; init; }
    public bool IsReligiousObservance { get; init; }
    public bool IsCulturalCelebration { get; init; }
    public bool IsNationalHoliday { get; init; }
    public bool IsRegionalEvent { get; init; }
    public bool IsDiasporaAdaptation { get; init; }
    
    // Buddhist/Hindu Calendar Specific Properties
    public bool IsMajorPoya { get; init; }
    public bool IsPoyaday { get; init; }
    
    // Multi-lingual support
    public IEnumerable<MultiLingualName> MultiLingualNames { get; init; }
    public string Description { get; init; }
    public IEnumerable<string> CelebrationTraditions { get; init; }
    public IEnumerable<string> FoodTraditions { get; init; }
    public IEnumerable<string> CulturalSignificance { get; init; }
    
    // Cross-cultural properties
    public bool HasCrossCulturalAppeals { get; init; }
    public decimal CulturalSensitivityScore { get; init; }
    public IEnumerable<CulturalAdaptation> DiasporaAdaptations { get; init; }

    protected CulturalEvent() 
    {
        // Default constructor for derived types
        EnglishName = string.Empty;
        NativeName = string.Empty;
        SecondaryName = string.Empty;
        MultiLingualNames = new List<MultiLingualName>();
        Description = string.Empty;
        CelebrationTraditions = new List<string>();
        FoodTraditions = new List<string>();
        CulturalSignificance = new List<string>();
        SecondaryCommunities = new List<CulturalCommunity>();
        DiasporaAdaptations = new List<CulturalAdaptation>();
    }

    public CulturalEvent(
        DateTime date,
        string englishName,
        string nativeName,
        string secondaryName,
        CulturalCommunity primaryCommunity,
        CalendarSystem calendarSystem = CalendarSystem.GregorianCalendar,
        CulturalEventType eventType = CulturalEventType.Cultural,
        bool isMajorFestival = false,
        bool isReligiousObservance = false,
        bool isCulturalCelebration = true,
        bool isNationalHoliday = false,
        bool isRegionalEvent = false,
        bool isDiasporaAdaptation = false,
        bool isMajorPoya = false,
        bool isPoyaday = false,
        IEnumerable<CulturalCommunity>? secondaryCommunities = null,
        IEnumerable<MultiLingualName>? multiLingualNames = null,
        string description = "",
        IEnumerable<string>? celebrationTraditions = null,
        IEnumerable<string>? foodTraditions = null,
        IEnumerable<string>? culturalSignificance = null,
        bool hasCrossCulturalAppeals = false,
        decimal culturalSensitivityScore = 0.8m,
        IEnumerable<CulturalAdaptation>? diasporaAdaptations = null)
    {
        Date = date;
        EnglishName = englishName;
        NativeName = nativeName;
        SecondaryName = secondaryName ?? string.Empty;
        PrimaryCommunity = primaryCommunity;
        SecondaryCommunities = secondaryCommunities?.ToList() ?? new List<CulturalCommunity>();
        CalendarSystem = calendarSystem;
        EventType = eventType;
        
        IsMajorFestival = isMajorFestival;
        IsReligiousObservance = isReligiousObservance;
        IsCulturalCelebration = isCulturalCelebration;
        IsNationalHoliday = isNationalHoliday;
        IsRegionalEvent = isRegionalEvent;
        IsDiasporaAdaptation = isDiasporaAdaptation;
        IsMajorPoya = isMajorPoya;
        IsPoyaday = isPoyaday;
        
        MultiLingualNames = multiLingualNames?.ToList() ?? new List<MultiLingualName>();
        Description = description;
        CelebrationTraditions = celebrationTraditions?.ToList() ?? new List<string>();
        FoodTraditions = foodTraditions?.ToList() ?? new List<string>();
        CulturalSignificance = culturalSignificance?.ToList() ?? new List<string>();
        
        HasCrossCulturalAppeals = hasCrossCulturalAppeals;
        CulturalSensitivityScore = culturalSensitivityScore;
        DiasporaAdaptations = diasporaAdaptations?.ToList() ?? new List<CulturalAdaptation>();
    }

    /// <summary>
    /// Create Diwali event for Indian Hindu communities
    /// Celebrated across North and South Indian diaspora
    /// </summary>
    public static CulturalEvent CreateDiwali(DateTime date, CulturalCommunity community)
    {
        var multiLingualNames = new[]
        {
            new MultiLingualName(Language.English, "Diwali"),
            new MultiLingualName(Language.Hindi, "दीवाली"),
            new MultiLingualName(Language.Tamil, "தீபாவளி"),
            new MultiLingualName(Language.Telugu, "దీపావళి"),
            new MultiLingualName(Language.Gujarati, "દિવાળી")
        };

        var celebrations = new[]
        {
            "Lighting oil lamps (diyas)",
            "Fireworks and sparklers",
            "Rangoli art and decorations",
            "Exchange of sweets and gifts",
            "Lakshmi puja (worship)",
            "Family gatherings and feasts"
        };

        var foods = new[]
        {
            "Mithai (assorted Indian sweets)",
            "Samosas and savory snacks",
            "Regional specialties by community",
            "Gulab jamun and rasgulla",
            "Kaju katli and barfi"
        };

        var significance = new[]
        {
            "Victory of light over darkness",
            "Triumph of good over evil",
            "Worship of Goddess Lakshmi for prosperity",
            "Cultural unity across Hindu communities",
            "New beginnings and fresh starts"
        };

        return new CulturalEvent(
            date: date,
            englishName: "Diwali - Festival of Lights",
            nativeName: community == CulturalCommunity.IndianHinduSouth ? "Deepavali" : "Diwali",
            secondaryName: "Festival of Lights",
            primaryCommunity: community,
            calendarSystem: CalendarSystem.HinduLunisolar,
            eventType: CulturalEventType.Religious,
            isMajorFestival: true,
            isReligiousObservance: true,
            isCulturalCelebration: true,
            secondaryCommunities: new[] { CulturalCommunity.IndianSikh, CulturalCommunity.IndianJain },
            multiLingualNames: multiLingualNames,
            description: "Most significant Hindu festival celebrating the triumph of light over darkness",
            celebrationTraditions: celebrations,
            foodTraditions: foods,
            culturalSignificance: significance,
            hasCrossCulturalAppeals: true,
            culturalSensitivityScore: 0.95m
        );
    }

    /// <summary>
    /// Create Eid ul-Fitr event for Muslim communities
    /// Celebrated across Pakistani, Bangladeshi, and other Muslim diaspora
    /// </summary>
    public static CulturalEvent CreateEidUlFitr(DateTime date, CulturalCommunity community)
    {
        var multiLingualNames = new[]
        {
            new MultiLingualName(Language.English, "Eid ul-Fitr"),
            new MultiLingualName(Language.Arabic, "عيد الفطر"),
            new MultiLingualName(Language.Urdu, "عید الفطر"),
            new MultiLingualName(Language.Bengali, "ঈদুল ফিতর")
        };

        var celebrations = new[]
        {
            "Morning Eid prayers at mosque",
            "Community gatherings and feasts",
            "Giving of Zakat al-Fitr (charity)",
            "Exchange of gifts and Eid greetings",
            "New clothes and traditional attire",
            "Visiting family and friends"
        };

        var foods = new[]
        {
            "Biryani and traditional rice dishes",
            "Sewaiyan (sweet vermicelli)",
            "Dates and traditional sweets",
            "Community-specific delicacies",
            "Haleem and other festive foods"
        };

        var significance = new[]
        {
            "End of holy month of Ramadan",
            "Celebration of spiritual purification",
            "Community solidarity and charity",
            "Gratitude for completing the fast",
            "Renewal of faith and relationships"
        };

        return new CulturalEvent(
            date: date,
            englishName: "Eid ul-Fitr",
            nativeName: "عيد الفطر",
            secondaryName: "Festival of Breaking the Fast",
            primaryCommunity: community,
            calendarSystem: community == CulturalCommunity.PakistaniSunniMuslim ? CalendarSystem.IslamicPakistani : CalendarSystem.IslamicBangladeshi,
            eventType: CulturalEventType.Religious,
            isMajorFestival: true,
            isReligiousObservance: true,
            isCulturalCelebration: true,
            secondaryCommunities: new[] { CulturalCommunity.SriLankanMuslim, CulturalCommunity.IndianMuslim },
            multiLingualNames: multiLingualNames,
            description: "Most important Islamic festival marking the end of Ramadan",
            celebrationTraditions: celebrations,
            foodTraditions: foods,
            culturalSignificance: significance,
            hasCrossCulturalAppeals: true,
            culturalSensitivityScore: 0.98m
        );
    }

    /// <summary>
    /// Create Vaisakhi event for Sikh communities
    /// Important for Punjabi diaspora cultural identity
    /// </summary>
    public static CulturalEvent CreateVaisakhi(DateTime date)
    {
        var multiLingualNames = new[]
        {
            new MultiLingualName(Language.English, "Vaisakhi"),
            new MultiLingualName(Language.Punjabi, "ਵਿਸਾਖੀ"),
            new MultiLingualName(Language.Hindi, "बैसाखी")
        };

        var celebrations = new[]
        {
            "Nagar Kirtan (religious procession)",
            "Langar (community free meal)",
            "Kirtan (devotional singing)",
            "Bhangra and Giddha folk dances",
            "Reading from Guru Granth Sahib",
            "Community service and charity"
        };

        var significance = new[]
        {
            "Formation of Khalsa by Guru Gobind Singh",
            "Sikh New Year celebration",
            "Harvest festival in Punjab",
            "Renewal of Sikh faith and identity",
            "Community unity and service"
        };

        return new CulturalEvent(
            date: date,
            englishName: "Vaisakhi",
            nativeName: "ਵਿਸਾਖੀ",
            secondaryName: "Sikh New Year and Khalsa Formation Day",
            primaryCommunity: CulturalCommunity.IndianSikh,
            calendarSystem: CalendarSystem.NanakshahiCalendar,
            eventType: CulturalEventType.Religious,
            isMajorFestival: true,
            isReligiousObservance: true,
            isCulturalCelebration: true,
            secondaryCommunities: new[] { CulturalCommunity.IndianHinduNorth },
            multiLingualNames: multiLingualNames,
            description: "Sikh festival celebrating the formation of the Khalsa and harvest season",
            celebrationTraditions: celebrations,
            culturalSignificance: significance,
            hasCrossCulturalAppeals: false, // More specific to Sikh community
            culturalSensitivityScore: 0.85m
        );
    }

    /// <summary>
    /// Create Pohela Boishakh (Bengali New Year) for Bangladeshi community
    /// Cultural bridge between Muslim and Hindu Bengali traditions
    /// </summary>
    public static CulturalEvent CreatePohelaBoishakh(DateTime date, CulturalCommunity community)
    {
        var multiLingualNames = new[]
        {
            new MultiLingualName(Language.English, "Pohela Boishakh"),
            new MultiLingualName(Language.Bengali, "পহেলা বৈশাখ")
        };

        var celebrations = new[]
        {
            "Traditional Bengali music and dance",
            "Mangal Shobhajatra (colorful procession)",
            "Wearing traditional Bengali attire",
            "Cultural programs and poetry",
            "Traditional Bengali cuisine",
            "Business and social gatherings"
        };

        var foods = new[]
        {
            "Panta bhat (fermented rice)",
            "Hilsha fish preparations",
            "Bengali sweets (mishti)",
            "Traditional vegetarian dishes",
            "Regional Bengali delicacies"
        };

        var significance = new[]
        {
            "Beginning of Bengali calendar year",
            "Cultural identity preservation",
            "Unity across religious communities",
            "Agricultural season celebration",
            "Bengali heritage and language pride"
        };

        return new CulturalEvent(
            date: date,
            englishName: "Pohela Boishakh",
            nativeName: "পহেলা বৈশাখ",
            secondaryName: "Bengali New Year",
            primaryCommunity: community,
            calendarSystem: CalendarSystem.BengaliCalendar,
            eventType: CulturalEventType.Cultural,
            isMajorFestival: true,
            isCulturalCelebration: true,
            secondaryCommunities: new[] { CulturalCommunity.IndianHinduBengali },
            multiLingualNames: multiLingualNames,
            description: "Bengali New Year celebration transcending religious boundaries",
            celebrationTraditions: celebrations,
            foodTraditions: foods,
            culturalSignificance: significance,
            hasCrossCulturalAppeals: true,
            culturalSensitivityScore: 0.90m
        );
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Date;
        yield return EnglishName;
        yield return NativeName;
        yield return PrimaryCommunity;
        yield return CalendarSystem;
        yield return EventType;
    }
}

// Supporting types for CulturalEvent
public record MultiLingualName(Language Language, string Name);

public enum CulturalEventCategory
{
    Religious = 1,
    Cultural = 2,
    Seasonal = 3,
    National = 4,
    Community = 5,
    Enterprise = 6,
    Educational = 7,
    Interfaith = 8
}