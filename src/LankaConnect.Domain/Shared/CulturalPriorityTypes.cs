namespace LankaConnect.Domain.Shared;

/// <summary>
/// Sacred priority level for cultural content and events
/// </summary>
public enum SacredPriorityLevel
{
    /// <summary>
    /// Regular cultural content with standard processing
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Important cultural content requiring careful handling
    /// </summary>
    Important = 2,

    /// <summary>
    /// High priority cultural content with elevated processing
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical cultural content requiring immediate attention
    /// </summary>
    Critical = 4,

    /// <summary>
    /// Sacred content requiring the highest level of protection and reverence
    /// </summary>
    Sacred = 5,

    /// <summary>
    /// Ultra-sacred content such as religious texts and ceremonies
    /// </summary>
    UltraSacred = 6
}

/// <summary>
/// Generational cohort for cultural affinity analysis
/// </summary>
public enum GenerationalCohort
{
    /// <summary>
    /// Traditional generation (60+ years)
    /// </summary>
    Traditional = 1,

    /// <summary>
    /// Bridge generation (40-60 years) balancing tradition and modernity
    /// </summary>
    Bridge = 2,

    /// <summary>
    /// Modern generation (25-40 years) with digital native characteristics
    /// </summary>
    Modern = 3,

    /// <summary>
    /// Digital generation (18-25 years) fully integrated with technology
    /// </summary>
    Digital = 4,

    /// <summary>
    /// Next generation (under 18 years) growing up in hybrid cultural environments
    /// </summary>
    NextGeneration = 5,

    /// <summary>
    /// Mixed generational family units
    /// </summary>
    MixedFamily = 6,

    /// <summary>
    /// Unknown or not specified
    /// </summary>
    Unknown = 99
}

/// <summary>
/// Sacred content type classification
/// </summary>
public enum SacredContentType
{
    /// <summary>
    /// Religious scriptures and holy texts
    /// </summary>
    ReligiousScripture = 1,

    /// <summary>
    /// Prayer texts and mantras
    /// </summary>
    Prayers = 2,

    /// <summary>
    /// Ceremonial rituals and practices
    /// </summary>
    Ceremonies = 3,

    /// <summary>
    /// Religious festivals and celebrations
    /// </summary>
    Festivals = 4,

    /// <summary>
    /// Sacred sites and pilgrimage locations
    /// </summary>
    SacredSites = 5,

    /// <summary>
    /// Religious teachings and wisdom
    /// </summary>
    Teachings = 6,

    /// <summary>
    /// Traditional cultural practices
    /// </summary>
    CulturalTraditions = 7,

    /// <summary>
    /// Ancient wisdom and folklore
    /// </summary>
    AncientWisdom = 8,

    /// <summary>
    /// Religious music and chants
    /// </summary>
    SacredMusic = 9,

    /// <summary>
    /// Symbolic and artistic representations
    /// </summary>
    SacredArt = 10,

    /// <summary>
    /// Other sacred content
    /// </summary>
    Other = 99
}

/// <summary>
/// Extensions for SacredPriorityLevel enum
/// </summary>
public static class SacredPriorityLevelExtensions
{
    /// <summary>
    /// Gets the processing priority weight for the sacred content
    /// </summary>
    /// <param name="level">The sacred priority level</param>
    /// <returns>Processing weight (higher = more priority)</returns>
    public static int GetProcessingWeight(this SacredPriorityLevel level)
    {
        return level switch
        {
            SacredPriorityLevel.Standard => 1,
            SacredPriorityLevel.Important => 2,
            SacredPriorityLevel.High => 4,
            SacredPriorityLevel.Critical => 8,
            SacredPriorityLevel.Sacred => 16,
            SacredPriorityLevel.UltraSacred => 32,
            _ => 1
        };
    }

    /// <summary>
    /// Determines if the content requires special cultural validation
    /// </summary>
    /// <param name="level">The sacred priority level</param>
    /// <returns>True if special validation is required</returns>
    public static bool RequiresSpecialValidation(this SacredPriorityLevel level)
    {
        return level >= SacredPriorityLevel.Critical;
    }
}

/// <summary>
/// Extensions for GenerationalCohort enum
/// </summary>
public static class GenerationalCohortExtensions
{
    /// <summary>
    /// Gets the typical age range for the generational cohort
    /// </summary>
    /// <param name="cohort">The generational cohort</param>
    /// <returns>Age range description</returns>
    public static string GetAgeRange(this GenerationalCohort cohort)
    {
        return cohort switch
        {
            GenerationalCohort.Traditional => "60+ years",
            GenerationalCohort.Bridge => "40-60 years",
            GenerationalCohort.Modern => "25-40 years",
            GenerationalCohort.Digital => "18-25 years",
            GenerationalCohort.NextGeneration => "Under 18 years",
            GenerationalCohort.MixedFamily => "Mixed ages",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Determines if the cohort prefers traditional cultural practices
    /// </summary>
    /// <param name="cohort">The generational cohort</param>
    /// <returns>True if prefers traditional practices</returns>
    public static bool PrefersTraditionalPractices(this GenerationalCohort cohort)
    {
        return cohort is GenerationalCohort.Traditional or GenerationalCohort.Bridge;
    }

    /// <summary>
    /// Determines if the cohort is digitally native
    /// </summary>
    /// <param name="cohort">The generational cohort</param>
    /// <returns>True if digitally native</returns>
    public static bool IsDigitallyNative(this GenerationalCohort cohort)
    {
        return cohort is GenerationalCohort.Digital or GenerationalCohort.NextGeneration;
    }
}