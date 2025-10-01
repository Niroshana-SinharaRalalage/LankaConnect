using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects.Recommendations;

/// <summary>
/// Geographic coordinates for precise location calculation
/// </summary>
public class Coordinates : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }

    public Coordinates(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90 degrees");
        
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180 degrees");

        Latitude = latitude;
        Longitude = longitude;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Math.Round(Latitude, 6);
        yield return Math.Round(Longitude, 6);
    }
}

/// <summary>
/// Distance measurement with unit specification
/// </summary>
public class Distance : ValueObject
{
    public double Value { get; }
    public DistanceUnit Unit { get; }

    public Distance(double value, DistanceUnit unit)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Distance cannot be negative");

        Value = value;
        Unit = unit;
    }

    public Distance ConvertTo(DistanceUnit targetUnit)
    {
        if (Unit == targetUnit)
            return this;

        double convertedValue = (Unit, targetUnit) switch
        {
            (DistanceUnit.Miles, DistanceUnit.Kilometers) => Value * 1.60934,
            (DistanceUnit.Kilometers, DistanceUnit.Miles) => Value * 0.621371,
            _ => Value
        };

        return new Distance(convertedValue, targetUnit);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Unit;
    }
}

/// <summary>
/// Distance unit enumeration
/// </summary>
public enum DistanceUnit
{
    Miles,
    Kilometers
}

/// <summary>
/// Community cluster for geographic analysis
/// </summary>
public class CommunityCluster : ValueObject
{
    public string Name { get; }
    public string[] Locations { get; }
    public string Location => Locations.FirstOrDefault() ?? string.Empty; // Primary location
    public double CommunityDensity { get; }
    public int EstimatedPopulation { get; }
    public int Size => EstimatedPopulation; // Alias for compatibility

    public CommunityCluster(string name, string[] locations, double communityDensity, int estimatedPopulation = 0)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Locations = locations ?? throw new ArgumentNullException(nameof(locations));
        CommunityDensity = Math.Max(0.0, Math.Min(1.0, communityDensity));
        EstimatedPopulation = Math.Max(0, estimatedPopulation);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return string.Join(",", Locations.OrderBy(x => x));
        yield return CommunityDensity;
        yield return EstimatedPopulation;
    }
}

/// <summary>
/// Regional preferences for community-based recommendations
/// </summary>
public class RegionalPreferences : ValueObject
{
    public string Region { get; }
    public string[] PopularCategories { get; }
    public string[] LessPopularCategories { get; }
    public int CommunitySize { get; }
    public double EngagementLevel { get; }

    public RegionalPreferences(
        string region, 
        string[] popularCategories, 
        string[] lessPopularCategories, 
        int communitySize,
        double engagementLevel = 0.7)
    {
        Region = region ?? throw new ArgumentNullException(nameof(region));
        PopularCategories = popularCategories ?? Array.Empty<string>();
        LessPopularCategories = lessPopularCategories ?? Array.Empty<string>();
        CommunitySize = Math.Max(0, communitySize);
        EngagementLevel = Math.Max(0.0, Math.Min(1.0, engagementLevel));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Region;
        yield return string.Join(",", PopularCategories.OrderBy(x => x));
        yield return string.Join(",", LessPopularCategories.OrderBy(x => x));
        yield return CommunitySize;
        yield return EngagementLevel;
    }
}

/// <summary>
/// Regional match score
/// </summary>
public class RegionalMatchScore : ValueObject
{
    public double Value { get; }
    public double MatchScore => Value; // Alias for compatibility
    public string MatchReason { get; }

    public RegionalMatchScore(double value, string matchReason = "")
    {
        Value = Math.Max(0.0, Math.Min(1.0, value));
        MatchReason = matchReason ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return MatchReason;
    }
}

/// <summary>
/// Transportation preferences for accessibility
/// </summary>
public class TransportationPreferences : ValueObject
{
    public string[] PreferredModes { get; }
    public string[] AvoidedModes { get; }
    public bool AccessibilityNeeds { get; }
    public double MaxWalkingDistance { get; }

    public TransportationPreferences(
        string[] preferredModes, 
        string[] avoidedModes, 
        bool accessibilityNeeds = false,
        double maxWalkingDistance = 0.5)
    {
        PreferredModes = preferredModes ?? Array.Empty<string>();
        AvoidedModes = avoidedModes ?? Array.Empty<string>();
        AccessibilityNeeds = accessibilityNeeds;
        MaxWalkingDistance = Math.Max(0.0, maxWalkingDistance);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return string.Join(",", PreferredModes.OrderBy(x => x));
        yield return string.Join(",", AvoidedModes.OrderBy(x => x));
        yield return AccessibilityNeeds;
        yield return MaxWalkingDistance;
    }
}

/// <summary>
/// Accessibility score for transportation
/// </summary>
public class AccessibilityScore : ValueObject
{
    public double Value { get; }
    public string[] AccessibleModes { get; }
    public string[] Barriers { get; }

    public AccessibilityScore(double value, string[]? accessibleModes = null, string[]? barriers = null)
    {
        Value = Math.Max(0.0, Math.Min(1.0, value));
        AccessibleModes = accessibleModes ?? Array.Empty<string>();
        Barriers = barriers ?? Array.Empty<string>();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return string.Join(",", AccessibleModes.OrderBy(x => x));
        yield return string.Join(",", Barriers.OrderBy(x => x));
    }
}

/// <summary>
/// Multi-location proximity analysis
/// </summary>
public class MultiLocationProximity : ValueObject
{
    public double ClosestDistance { get; }
    public double AverageDistance { get; }
    public double MaxDistance { get; }
    public int LocationCount { get; }
    public double[] AllDistances { get; }

    public MultiLocationProximity(double closestDistance, double averageDistance, double maxDistance, int locationCount, double[]? allDistances = null)
    {
        ClosestDistance = Math.Max(0, closestDistance);
        AverageDistance = Math.Max(0, averageDistance);
        MaxDistance = Math.Max(0, maxDistance);
        LocationCount = Math.Max(1, locationCount);
        AllDistances = allDistances ?? Array.Empty<double>();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ClosestDistance;
        yield return AverageDistance;
        yield return MaxDistance;
        yield return LocationCount;
    }
}

/// <summary>
/// Proximity score for multi-location events
/// </summary>
public class ProximityScore : ValueObject
{
    public double Value { get; }
    public double VarietyBonus { get; }
    public string ProximityReason { get; }

    public ProximityScore(double value, double varietyBonus = 0.0, string proximityReason = "")
    {
        Value = Math.Max(0.0, Math.Min(1.0, value));
        VarietyBonus = Math.Max(0.0, Math.Min(0.2, varietyBonus));
        ProximityReason = proximityReason ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return VarietyBonus;
        yield return ProximityReason;
    }
}

/// <summary>
/// Location handling result for edge cases
/// </summary>
public class LocationHandlingResult : ValueObject
{
    public bool CanRecommend { get; }
    public string Reason { get; }
    public string SuggestedAction { get; }
    public double ProximityScore { get; }

    public LocationHandlingResult(bool canRecommend, string reason, string suggestedAction = "", double proximityScore = 0.0)
    {
        CanRecommend = canRecommend;
        Reason = reason ?? string.Empty;
        SuggestedAction = suggestedAction ?? string.Empty;
        ProximityScore = Math.Max(0.0, Math.Min(1.0, proximityScore));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CanRecommend;
        yield return Reason;
        yield return SuggestedAction;
        yield return ProximityScore;
    }
}