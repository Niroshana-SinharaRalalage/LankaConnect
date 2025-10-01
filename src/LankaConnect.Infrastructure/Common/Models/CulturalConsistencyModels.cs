using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Infrastructure.Common.Models;

/// <summary>
/// Cultural data synchronization request for cross-region consistency.
/// </summary>
public record CulturalDataSynchronizationRequest
{
    public string RequestId { get; init; } = string.Empty;
    public string SourceRegion { get; init; } = string.Empty;
    public IReadOnlyList<string> TargetRegions { get; init; } = Array.Empty<string>();
    public CulturalDataType DataType { get; init; }
    public DateTime RequestTimestamp { get; init; } = DateTime.UtcNow;
    public bool IsHighPriority { get; init; }
    public string CulturalContext { get; init; } = string.Empty;

    public static CulturalDataSynchronizationRequest Create(
        string sourceRegion,
        IEnumerable<string> targetRegions,
        CulturalDataType dataType,
        bool isHighPriority = false)
    {
        return new CulturalDataSynchronizationRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            SourceRegion = sourceRegion,
            TargetRegions = targetRegions.ToList().AsReadOnly(),
            DataType = dataType,
            IsHighPriority = isHighPriority
        };
    }
}

/// <summary>
/// Cross-region synchronization result.
/// </summary>
public record CrossRegionSynchronizationResult
{
    public string SynchronizationId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string SourceRegion { get; init; } = string.Empty;
    public IReadOnlyList<string> SynchronizedRegions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FailedRegions { get; init; } = Array.Empty<string>();
    public TimeSpan Duration { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    public static CrossRegionSynchronizationResult Success(
        string sourceRegion,
        IEnumerable<string> synchronizedRegions,
        TimeSpan duration)
    {
        return new CrossRegionSynchronizationResult
        {
            SynchronizationId = Guid.NewGuid().ToString(),
            IsSuccessful = true,
            SourceRegion = sourceRegion,
            SynchronizedRegions = synchronizedRegions.ToList().AsReadOnly(),
            Duration = duration
        };
    }
}

/// <summary>
/// Cultural data consistency check result.
/// </summary>
public record CulturalDataConsistencyCheck
{
    public string CheckId { get; init; } = string.Empty;
    public bool IsConsistent { get; init; }
    public CulturalDataType DataType { get; init; }
    public IReadOnlyList<string> ConsistentRegions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> InconsistentRegions { get; init; } = Array.Empty<string>();
    public double OverallConsistencyScore { get; init; }
    public DateTime CheckTimestamp { get; init; } = DateTime.UtcNow;

    public static CulturalDataConsistencyCheck Create(
        CulturalDataType dataType,
        IEnumerable<string> consistentRegions,
        IEnumerable<string> inconsistentRegions,
        double consistencyScore)
    {
        return new CulturalDataConsistencyCheck
        {
            CheckId = Guid.NewGuid().ToString(),
            IsConsistent = !inconsistentRegions.Any(),
            DataType = dataType,
            ConsistentRegions = consistentRegions.ToList().AsReadOnly(),
            InconsistentRegions = inconsistentRegions.ToList().AsReadOnly(),
            OverallConsistencyScore = consistencyScore
        };
    }
}

/// <summary>
/// Cultural data conflict information.
/// </summary>
public record CulturalDataConflict
{
    public string ConflictId { get; init; } = string.Empty;
    public CulturalDataType DataType { get; init; }
    public string ConflictingRegion1 { get; init; } = string.Empty;
    public string ConflictingRegion2 { get; init; } = string.Empty;
    public string ConflictDescription { get; init; } = string.Empty;
    public ConflictSeverity Severity { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;

    public static CulturalDataConflict Create(
        CulturalDataType dataType,
        string region1,
        string region2,
        string description,
        ConflictSeverity severity)
    {
        return new CulturalDataConflict
        {
            ConflictId = Guid.NewGuid().ToString(),
            DataType = dataType,
            ConflictingRegion1 = region1,
            ConflictingRegion2 = region2,
            ConflictDescription = description,
            Severity = severity
        };
    }
}

/// <summary>
/// Cultural conflict resolution result.
/// </summary>
public record CulturalConflictResolution
{
    public string ResolutionId { get; init; } = string.Empty;
    public string ConflictId { get; init; } = string.Empty;
    public bool IsResolved { get; init; }
    public ConflictResolutionStrategy Strategy { get; init; }
    public string ResolutionDescription { get; init; } = string.Empty;
    public DateTime ResolvedAt { get; init; } = DateTime.UtcNow;

    public static CulturalConflictResolution Success(
        string conflictId,
        ConflictResolutionStrategy strategy,
        string description)
    {
        return new CulturalConflictResolution
        {
            ResolutionId = Guid.NewGuid().ToString(),
            ConflictId = conflictId,
            IsResolved = true,
            Strategy = strategy,
            ResolutionDescription = description
        };
    }
}

/// <summary>
/// Cross-region failover request.
/// </summary>
public record CrossRegionFailoverRequest
{
    public string RequestId { get; init; } = string.Empty;
    public string FailedRegion { get; init; } = string.Empty;
    public string TargetRegion { get; init; } = string.Empty;
    public CulturalDataType DataType { get; init; }
    public bool IsEmergency { get; init; }
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;

    public static CrossRegionFailoverRequest Create(
        string failedRegion,
        string targetRegion,
        CulturalDataType dataType,
        bool isEmergency = false)
    {
        return new CrossRegionFailoverRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            FailedRegion = failedRegion,
            TargetRegion = targetRegion,
            DataType = dataType,
            IsEmergency = isEmergency
        };
    }
}

/// <summary>
/// Cross-region failover result.
/// </summary>
public record CrossRegionFailoverResult
{
    public string FailoverId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string FailedRegion { get; init; } = string.Empty;
    public string ActiveRegion { get; init; } = string.Empty;
    public TimeSpan FailoverDuration { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    public static CrossRegionFailoverResult Success(
        string failedRegion,
        string activeRegion,
        TimeSpan duration)
    {
        return new CrossRegionFailoverResult
        {
            FailoverId = Guid.NewGuid().ToString(),
            IsSuccessful = true,
            FailedRegion = failedRegion,
            ActiveRegion = activeRegion,
            FailoverDuration = duration
        };
    }
}

/// <summary>
/// Cultural consistency metrics.
/// </summary>
public record CulturalConsistencyMetrics
{
    public double OverallConsistencyScore { get; init; }
    public IReadOnlyDictionary<string, double> RegionalScores { get; init; } = new Dictionary<string, double>();
    public IReadOnlyDictionary<CulturalDataType, double> DataTypeScores { get; init; } = new Dictionary<CulturalDataType, double>();
    public int TotalConflicts { get; init; }
    public int ResolvedConflicts { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    public static CulturalConsistencyMetrics Create(
        double overallScore,
        IDictionary<string, double> regionalScores,
        int totalConflicts,
        int resolvedConflicts)
    {
        return new CulturalConsistencyMetrics
        {
            OverallConsistencyScore = overallScore,
            RegionalScores = regionalScores.AsReadOnly(),
            TotalConflicts = totalConflicts,
            ResolvedConflicts = resolvedConflicts
        };
    }
}

// CulturalDataType is now imported from Domain.Common.Enums

public enum ConflictSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum ConflictResolutionStrategy
{
    AutoResolve,
    ManualReview,
    CulturalAuthorityDecision,
    CommunityConsensus,
    TemporalResolution
}

public enum ConsistencyLevel
{
    Eventual,
    Strong,
    Cultural,
    Sacred
}