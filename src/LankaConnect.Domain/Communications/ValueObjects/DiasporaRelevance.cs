using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing diaspora relevance and geographic customization
/// Enables cultural events to be tailored for specific diaspora communities
/// </summary>
public sealed class DiasporaRelevance : ValueObject
{
    public DiasporaLocation Location { get; }
    public string TimeZone { get; }
    public CulturalCommunitySize CommunitySize { get; }
    public bool IsGloballyRelevant { get; }
    public IEnumerable<string> RelevantRegions { get; }
    public CulturalAdaptationLevel AdaptationLevel { get; }
    public string CulturalContext { get; }

    private DiasporaRelevance(
        DiasporaLocation location,
        string timeZone,
        CulturalCommunitySize communitySize,
        bool isGloballyRelevant,
        IEnumerable<string> relevantRegions,
        CulturalAdaptationLevel adaptationLevel,
        string culturalContext)
    {
        Location = location;
        TimeZone = timeZone;
        CommunitySize = communitySize;
        IsGloballyRelevant = isGloballyRelevant;
        RelevantRegions = relevantRegions;
        AdaptationLevel = adaptationLevel;
        CulturalContext = culturalContext;
    }

    /// <summary>
    /// Creates diaspora relevance for Bay Area community
    /// </summary>
    public static DiasporaRelevance CreateBayArea()
    {
        return new DiasporaRelevance(
            DiasporaLocation.Create("Bay Area", "California", "USA").Value,
            "America/Los_Angeles",
            CulturalCommunitySize.Large,
            false,
            new[] { "California", "West Coast", "North America" },
            CulturalAdaptationLevel.High,
            "Tech-savvy diaspora community with strong temple networks"
        );
    }

    /// <summary>
    /// Creates diaspora relevance for Toronto community
    /// </summary>
    public static DiasporaRelevance CreateToronto()
    {
        return new DiasporaRelevance(
            DiasporaLocation.Create("Toronto", "Ontario", "Canada").Value,
            "America/Toronto",
            CulturalCommunitySize.Large,
            false,
            new[] { "Ontario", "Eastern Canada", "North America" },
            CulturalAdaptationLevel.High,
            "Established diaspora community with multicultural integration"
        );
    }

    /// <summary>
    /// Creates diaspora relevance for London community
    /// </summary>
    public static DiasporaRelevance CreateLondon()
    {
        return new DiasporaRelevance(
            DiasporaLocation.Create("London", "England", "UK").Value,
            "Europe/London",
            CulturalCommunitySize.Medium,
            false,
            new[] { "England", "UK", "Europe" },
            CulturalAdaptationLevel.Medium,
            "Historical diaspora community with traditional observances"
        );
    }

    /// <summary>
    /// Creates global relevance for major cultural events
    /// </summary>
    public static DiasporaRelevance CreateGlobal()
    {
        return new DiasporaRelevance(
            DiasporaLocation.Global(),
            "UTC",
            CulturalCommunitySize.Global,
            true,
            new[] { "Global", "Worldwide", "All Regions" },
            CulturalAdaptationLevel.Universal,
            "Universal cultural significance for all diaspora communities"
        );
    }

    /// <summary>
    /// Creates diaspora relevance with specific cultural significance
    /// </summary>
    public static DiasporaRelevance CreateWithCulturalSignificance(string culturalContext)
    {
        return new DiasporaRelevance(
            DiasporaLocation.Global(),
            "UTC",
            CulturalCommunitySize.Global,
            true,
            new[] { "Global" },
            CulturalAdaptationLevel.Universal,
            culturalContext
        );
    }

    /// <summary>
    /// Creates custom diaspora relevance for specific location
    /// </summary>
    public static Result<DiasporaRelevance> CreateCustom(
        DiasporaLocation location,
        string timeZone,
        CulturalCommunitySize communitySize,
        string culturalContext)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            return Result<DiasporaRelevance>.Failure("Time zone cannot be empty");

        var regions = DetermineRelevantRegions(location);
        var adaptationLevel = DetermineAdaptationLevel(communitySize);

        return Result<DiasporaRelevance>.Success(new DiasporaRelevance(
            location, timeZone, communitySize, false,
            regions, adaptationLevel, culturalContext));
    }

    /// <summary>
    /// Determines if event is relevant for specific location
    /// </summary>
    public bool IsRelevantForLocation(string targetLocation)
    {
        if (IsGloballyRelevant)
            return true;

        if (Location.City.Equals(targetLocation, StringComparison.OrdinalIgnoreCase))
            return true;

        if (Location.State.Equals(targetLocation, StringComparison.OrdinalIgnoreCase))
            return true;

        if (Location.Country.Equals(targetLocation, StringComparison.OrdinalIgnoreCase))
            return true;

        return RelevantRegions.Any(region => 
            region.Equals(targetLocation, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Converts event time to local diaspora time zone
    /// </summary>
    public DateTime ConvertToLocalTime(DateTime utcTime)
    {
        try
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZoneInfo);
        }
        catch
        {
            // Fallback to UTC if time zone conversion fails
            return utcTime;
        }
    }

    /// <summary>
    /// Gets community engagement score (0-100)
    /// </summary>
    public int GetCommunityEngagementScore()
    {
        var baseScore = CommunitySize switch
        {
            CulturalCommunitySize.Small => 30,
            CulturalCommunitySize.Medium => 50,
            CulturalCommunitySize.Large => 75,
            CulturalCommunitySize.Global => 100,
            _ => 25
        };

        var adaptationBonus = AdaptationLevel switch
        {
            CulturalAdaptationLevel.Low => 0,
            CulturalAdaptationLevel.Medium => 10,
            CulturalAdaptationLevel.High => 20,
            CulturalAdaptationLevel.Universal => 25,
            _ => 0
        };

        return Math.Min(100, baseScore + adaptationBonus);
    }

    private static IEnumerable<string> DetermineRelevantRegions(DiasporaLocation location)
    {
        var regions = new List<string> { location.City, location.State, location.Country };

        // Add broader regional classifications
        if (location.Country == "USA" || location.Country == "Canada")
            regions.Add("North America");
        else if (location.Country == "UK" || location.Country == "Germany" || location.Country == "France")
            regions.Add("Europe");
        else if (location.Country == "Australia" || location.Country == "New Zealand")
            regions.Add("Oceania");

        return regions.Distinct();
    }

    private static CulturalAdaptationLevel DetermineAdaptationLevel(CulturalCommunitySize communitySize)
    {
        return communitySize switch
        {
            CulturalCommunitySize.Small => CulturalAdaptationLevel.Low,
            CulturalCommunitySize.Medium => CulturalAdaptationLevel.Medium,
            CulturalCommunitySize.Large => CulturalAdaptationLevel.High,
            CulturalCommunitySize.Global => CulturalAdaptationLevel.Universal,
            _ => CulturalAdaptationLevel.Low
        };
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Location;
        yield return TimeZone;
        yield return CommunitySize;
        yield return IsGloballyRelevant;
        yield return string.Join(",", RelevantRegions.OrderBy(r => r));
        yield return AdaptationLevel;
        yield return CulturalContext;
    }
}

/// <summary>
/// Diaspora location with geographic hierarchy
/// </summary>
public sealed class DiasporaLocation : ValueObject
{
    public string City { get; }
    public string State { get; }
    public string Country { get; }
    public bool IsGlobal { get; }

    private DiasporaLocation(string city, string state, string country, bool isGlobal = false)
    {
        City = city;
        State = state;
        Country = country;
        IsGlobal = isGlobal;
    }

    /// <summary>
    /// Creates specific diaspora location
    /// </summary>
    public static Result<DiasporaLocation> Create(string city, string state, string country)
    {
        if (string.IsNullOrWhiteSpace(city))
            return Result<DiasporaLocation>.Failure("City cannot be empty");

        if (string.IsNullOrWhiteSpace(state))
            return Result<DiasporaLocation>.Failure("State/Province cannot be empty");

        if (string.IsNullOrWhiteSpace(country))
            return Result<DiasporaLocation>.Failure("Country cannot be empty");

        return Result<DiasporaLocation>.Success(new DiasporaLocation(city, state, country));
    }

    /// <summary>
    /// Creates global location for universal events
    /// </summary>
    public static DiasporaLocation Global()
    {
        return new DiasporaLocation("Global", "Global", "Global", true);
    }

    public override string ToString()
    {
        return IsGlobal ? "Global" : $"{City}, {State}, {Country}";
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return City;
        yield return State;
        yield return Country;
        yield return IsGlobal;
    }
}

/// <summary>
/// Cultural community size classifications
/// </summary>
public enum CulturalCommunitySize
{
    Small,      // < 1,000 people
    Medium,     // 1,000 - 10,000 people
    Large,      // 10,000 - 100,000 people
    Global      // > 100,000 people worldwide
}

/// <summary>
/// Cultural adaptation levels for diaspora communities
/// </summary>
public enum CulturalAdaptationLevel
{
    Low,        // Minimal local adaptation
    Medium,     // Moderate local adaptation
    High,       // High local adaptation
    Universal   // Global relevance with local customization
}