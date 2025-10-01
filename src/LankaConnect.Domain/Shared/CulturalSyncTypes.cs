using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Shared;

/// <summary>
/// Cultural data synchronization request for cross-region consistency
/// </summary>
public class CulturalDataSyncRequest
{
    public required string RequestId { get; set; }
    public required string SourceRegion { get; set; }
    public required string TargetRegion { get; set; }
    public required List<CulturalDataType> DataTypes { get; set; } = new();
    public required DateTime RequestTimestamp { get; set; }
    public required SouthAsianLanguage PrimaryLanguage { get; set; }
    public bool IncludeSacredContent { get; set; }
    public required string RequestingService { get; set; }
    public Dictionary<string, object> SyncParameters { get; set; } = new();
}

/// <summary>
/// Cultural data synchronization result
/// </summary>
public class CulturalDataSynchronizationResult
{
    public required string RequestId { get; set; }
    public bool Success { get; set; }
    public required DateTime SyncTimestamp { get; set; }
    public required List<string> SynchronizedDataTypes { get; set; } = new();
    public required int RecordsSynchronized { get; set; }
    public required TimeSpan SyncDuration { get; set; }
    public string? ErrorMessage { get; set; }
    public required decimal CulturalIntegrityScore { get; set; }
    public List<string> SyncWarnings { get; set; } = new();
}

/// <summary>
/// Cultural conflict resolution result
/// </summary>
public class CulturalConflictResolutionResult
{
    public required string ConflictId { get; set; }
    public bool Resolved { get; set; }
    public required string ResolutionStrategy { get; set; }
    public required DateTime ResolutionTimestamp { get; set; }
    public required List<string> ConflictingDataSources { get; set; } = new();
    public required string SelectedAuthoritySource { get; set; }
    public string? ResolutionNotes { get; set; }
    public required decimal CulturalAccuracyScore { get; set; }
    public List<string> CulturalValidatorComments { get; set; } = new();
}

/// <summary>
/// Cultural calendar synchronization request
/// </summary>
public class CulturalCalendarSyncRequest
{
    public required string RequestId { get; set; }
    public required string CalendarType { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required List<SouthAsianLanguage> TargetLanguages { get; set; } = new();
    public bool IncludeReligiousEvents { get; set; }
    public required string SourceRegion { get; set; }
    public Dictionary<string, object> CalendarParameters { get; set; } = new();
}

/// <summary>
/// Cultural calendar synchronization result
/// </summary>
public class CulturalCalendarSyncResult
{
    public required string RequestId { get; set; }
    public bool Success { get; set; }
    public required DateTime SyncTimestamp { get; set; }
    public required int EventsSynchronized { get; set; }
    public required List<string> SynchronizedEventTypes { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public required decimal ReligiousAccuracyScore { get; set; }
    public List<string> ValidationWarnings { get; set; } = new();
}

/// <summary>
/// Diaspora community synchronization request
/// </summary>
public class DiasporaCommunitySyncRequest
{
    public required string RequestId { get; set; }
    public required string CommunityId { get; set; }
    public required List<string> TargetRegions { get; set; } = new();
    public required SouthAsianLanguage PrimaryLanguage { get; set; }
    public bool IncludeCulturalPreferences { get; set; }
    public required DateTime RequestTimestamp { get; set; }
    public Dictionary<string, object> CommunityMetadata { get; set; } = new();
}

/// <summary>
/// Diaspora community synchronization result
/// </summary>
public class DiasporaCommunitySyncResult
{
    public required string RequestId { get; set; }
    public bool Success { get; set; }
    public required string CommunityId { get; set; }
    public required int MembersSynchronized { get; set; }
    public required DateTime SyncTimestamp { get; set; }
    public string? ErrorMessage { get; set; }
    public required List<string> SynchronizedRegions { get; set; } = new();
    public List<string> SyncIssues { get; set; } = new();
}

// CulturalDataType is now imported from Domain.Common.Enums