using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Users.ValueObjects;

/// <summary>
/// Cultural interest value object using Enumeration Pattern
/// Represents predefined cultural interests for Sri Lankan diaspora community
/// Architecture: Strongly-typed value object with predefined list (not a database reference table)
/// </summary>
public sealed class CulturalInterest : ValueObject
{
    public string Code { get; internal set; } = null!;
    public string Name { get; internal set; } = null!;

    // Parameterless constructor for EF Core
    private CulturalInterest()
    {
    }

    private CulturalInterest(string code, string name)
    {
        Code = code;
        Name = name;
    }

    // Predefined Cultural Interests for Sri Lankan Diaspora (20 categories as per architect guidance)

    public static readonly CulturalInterest SriLankanCuisine = new("SL_CUISINE", "Sri Lankan Cuisine");
    public static readonly CulturalInterest BuddhistFestivals = new("BUDDHIST_FEST", "Buddhist Festivals & Traditions");
    public static readonly CulturalInterest HinduFestivals = new("HINDU_FEST", "Hindu Festivals & Traditions");
    public static readonly CulturalInterest IslamicFestivals = new("ISLAMIC_FEST", "Islamic Festivals & Traditions");
    public static readonly CulturalInterest ChristianFestivals = new("CHRISTIAN_FEST", "Christian Festivals & Traditions");
    public static readonly CulturalInterest TraditionalDance = new("TRAD_DANCE", "Traditional Dance (Kandyan, Sabaragamuwa, Low Country)");
    public static readonly CulturalInterest CricketCulture = new("CRICKET", "Cricket & Sports");
    public static readonly CulturalInterest AyurvedicWellness = new("AYURVEDA", "Ayurvedic Medicine & Wellness");
    public static readonly CulturalInterest SinhalaMusic = new("SINHALA_MUSIC", "Sinhala Music & Arts");
    public static readonly CulturalInterest TamilMusic = new("TAMIL_MUSIC", "Tamil Music & Arts");
    public static readonly CulturalInterest VesakCelebrations = new("VESAK", "Vesak & Poson Celebrations");
    public static readonly CulturalInterest SinhalaNewYear = new("SINHALA_NY", "Sinhala & Tamil New Year (Aluth Avurudda)");
    public static readonly CulturalInterest TeaCulture = new("TEA_CULTURE", "Ceylon Tea Culture");
    public static readonly CulturalInterest TraditionalArts = new("TRAD_ARTS", "Traditional Arts & Crafts (Masks, Batik, Pottery)");
    public static readonly CulturalInterest SriLankanWeddings = new("SL_WEDDINGS", "Sri Lankan Wedding Traditions");
    public static readonly CulturalInterest TempleArchitecture = new("TEMPLE_ARCH", "Temple Architecture & Heritage Sites");
    public static readonly CulturalInterest SriLankanLiterature = new("SL_LITERATURE", "Sinhala/Tamil Literature & Poetry");
    public static readonly CulturalInterest TraditionalGames = new("TRAD_GAMES", "Traditional Games (Elle, Ankeliya)");
    public static readonly CulturalInterest SriLankanFashion = new("SL_FASHION", "Traditional Dress & Fashion (Saree, Sarong)");
    public static readonly CulturalInterest DiasporaNetworking = new("DIASPORA_NET", "Diaspora Community & Networking");

    /// <summary>
    /// All available cultural interests (immutable list)
    /// </summary>
    public static IReadOnlyList<CulturalInterest> All { get; } = new List<CulturalInterest>
    {
        SriLankanCuisine,
        BuddhistFestivals,
        HinduFestivals,
        IslamicFestivals,
        ChristianFestivals,
        TraditionalDance,
        CricketCulture,
        AyurvedicWellness,
        SinhalaMusic,
        TamilMusic,
        VesakCelebrations,
        SinhalaNewYear,
        TeaCulture,
        TraditionalArts,
        SriLankanWeddings,
        TempleArchitecture,
        SriLankanLiterature,
        TraditionalGames,
        SriLankanFashion,
        DiasporaNetworking
    }.AsReadOnly();

    /// <summary>
    /// Creates a CulturalInterest from a code string
    /// Phase 6A.47: Now supports dynamic EventCategory codes from database
    /// </summary>
    /// <param name="code">Cultural interest code (e.g., "SL_CUISINE" or "Business")</param>
    /// <param name="name">Optional name for dynamic interests (defaults to code)</param>
    /// <returns>Result with CulturalInterest or error</returns>
    public static Result<CulturalInterest> FromCode(string? code, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<CulturalInterest>.Failure("Cultural interest code cannot be null or empty");
        }

        // First check if it matches a predefined interest
        var interest = All.FirstOrDefault(i => i.Code == code);

        if (interest != null)
        {
            return Result<CulturalInterest>.Success(interest);
        }

        // Phase 6A.47: Create dynamic interest from EventCategory database codes
        // This allows EventCategory codes like "Business", "Cultural", etc.
        return Result<CulturalInterest>.Success(new CulturalInterest(code, name ?? code));
    }

    /// <summary>
    /// Value object equality based on Code
    /// </summary>
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
    }

    /// <summary>
    /// Returns the name for display purposes
    /// </summary>
    public override string ToString() => Name;
}
