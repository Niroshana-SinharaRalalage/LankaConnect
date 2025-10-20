namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Extension methods for CulturalDataPriority enum to provide processing weight and validation logic
/// Migrated from Domain.Shared.CulturalPriorityTypes during enum consolidation (2025-10-10)
/// </summary>
public static class CulturalDataPriorityExtensions
{
    /// <summary>
    /// Gets the processing priority weight for cultural data
    /// Higher weight = higher priority in backup/recovery operations
    /// </summary>
    /// <param name="priority">The cultural data priority level</param>
    /// <returns>Processing weight (100-1000, higher = more priority)</returns>
    public static int GetProcessingWeight(this CulturalDataPriority priority)
    {
        return priority switch
        {
            CulturalDataPriority.Level10Sacred => 1000,      // Highest: Sacred events/religious data
            CulturalDataPriority.Level9Religious => 900,     // Religious ceremonies
            CulturalDataPriority.Level8Traditional => 800,   // Traditional celebrations
            CulturalDataPriority.Level7Cultural => 700,      // Cultural festivals
            CulturalDataPriority.Level6Community => 600,     // Community events
            CulturalDataPriority.Level5General => 500,       // General cultural content
            CulturalDataPriority.Level4Social => 400,        // Social gatherings
            CulturalDataPriority.Level3Commercial => 300,    // Commercial events
            CulturalDataPriority.Level2Administrative => 200, // Administrative data
            CulturalDataPriority.Level1System => 100,        // System logs/metadata
            _ => 0
        };
    }

    /// <summary>
    /// Determines if the cultural data requires special validation
    /// Level 8+ (Traditional and above) requires cultural sensitivity validation
    /// </summary>
    /// <param name="priority">The cultural data priority level</param>
    /// <returns>True if special cultural validation is required</returns>
    public static bool RequiresSpecialValidation(this CulturalDataPriority priority)
    {
        return priority >= CulturalDataPriority.Level8Traditional;
    }

    /// <summary>
    /// Determines if the cultural data is considered high priority
    /// Level 7+ (Cultural and above) is high priority
    /// </summary>
    /// <param name="priority">The cultural data priority level</param>
    /// <returns>True if high priority</returns>
    public static bool IsHighPriority(this CulturalDataPriority priority)
    {
        return priority >= CulturalDataPriority.Level7Cultural;
    }

    /// <summary>
    /// Determines if the cultural data is sacred/religious content
    /// Level 9+ (Religious and above) is sacred content
    /// </summary>
    /// <param name="priority">The cultural data priority level</param>
    /// <returns>True if sacred/religious content</returns>
    public static bool IsSacredContent(this CulturalDataPriority priority)
    {
        return priority >= CulturalDataPriority.Level9Religious;
    }

    /// <summary>
    /// Gets a human-readable description of the priority level
    /// </summary>
    /// <param name="priority">The cultural data priority level</param>
    /// <returns>Description string</returns>
    public static string GetDescription(this CulturalDataPriority priority)
    {
        return priority switch
        {
            CulturalDataPriority.Level10Sacred => "Highest priority - Sacred events and religious data",
            CulturalDataPriority.Level9Religious => "Religious ceremonies and worship content",
            CulturalDataPriority.Level8Traditional => "Traditional cultural celebrations",
            CulturalDataPriority.Level7Cultural => "Cultural festivals and community events",
            CulturalDataPriority.Level6Community => "Community gatherings and interactions",
            CulturalDataPriority.Level5General => "General cultural content and information",
            CulturalDataPriority.Level4Social => "Social events and casual gatherings",
            CulturalDataPriority.Level3Commercial => "Commercial and business events",
            CulturalDataPriority.Level2Administrative => "Administrative data and records",
            CulturalDataPriority.Level1System => "System logs and technical metadata",
            _ => "Unknown priority level"
        };
    }
}
