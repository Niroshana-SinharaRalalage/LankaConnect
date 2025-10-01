using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Common.ValueObjects;

/// <summary>
/// Value object representing a user segment for cultural intelligence and targeting
/// </summary>
public class UserSegment : ValueObject
{
    /// <summary>
    /// Segment identifier
    /// </summary>
    public string SegmentId { get; }

    /// <summary>
    /// Human-readable segment name
    /// </summary>
    public string SegmentName { get; }

    /// <summary>
    /// Primary cultural affiliation
    /// </summary>
    public string CulturalAffiliation { get; }

    /// <summary>
    /// Geographic region
    /// </summary>
    public string GeographicRegion { get; }

    /// <summary>
    /// Preferred language
    /// </summary>
    public string PreferredLanguage { get; }

    private UserSegment(string segmentId, string segmentName, string culturalAffiliation, string geographicRegion, string preferredLanguage)
    {
        SegmentId = segmentId;
        SegmentName = segmentName;
        CulturalAffiliation = culturalAffiliation;
        GeographicRegion = geographicRegion;
        PreferredLanguage = preferredLanguage;
    }

    public static UserSegment Create(string segmentId, string segmentName, string culturalAffiliation, string geographicRegion, string preferredLanguage)
    {
        if (string.IsNullOrWhiteSpace(segmentId))
            throw new ArgumentException("Segment ID cannot be empty", nameof(segmentId));
        if (string.IsNullOrWhiteSpace(segmentName))
            throw new ArgumentException("Segment name cannot be empty", nameof(segmentName));

        return new UserSegment(segmentId, segmentName, culturalAffiliation, geographicRegion, preferredLanguage);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return SegmentId;
        yield return CulturalAffiliation;
        yield return GeographicRegion;
        yield return PreferredLanguage;
    }
}