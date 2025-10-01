using LankaConnect.Domain.Common;
using DomainAccessPatternAnalysis = LankaConnect.Domain.Common.Security.AccessPatternAnalysis;

namespace LankaConnect.Application.Common.Security;

/// <summary>
/// TDD GREEN Phase: Audit & Access Types Implementation
/// Comprehensive audit and access pattern types for Cultural Intelligence platform security
/// </summary>

#region Audit Configuration

/// <summary>
/// Configuration for audit logging and compliance monitoring
/// </summary>
public class AuditConfiguration
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public AuditScope AuditScope { get; private set; }
    public TimeSpan RetentionPeriod { get; private set; }
    public List<string> ComplianceStandards { get; private set; } = new();
    public bool IsActive { get; private set; } = true;
    public bool RealTimeMonitoringEnabled { get; private set; } = false;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public Dictionary<string, object> ConfigurationMetadata { get; private set; } = new();

    private AuditConfiguration() { }

    private AuditConfiguration(AuditScope auditScope, TimeSpan retentionPeriod, IEnumerable<string> complianceStandards)
    {
        AuditScope = auditScope;
        RetentionPeriod = retentionPeriod;
        ComplianceStandards = complianceStandards.ToList();
        IsActive = true;
        RealTimeMonitoringEnabled = false;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<AuditConfiguration> Create(AuditScope auditScope, TimeSpan retentionPeriod, IEnumerable<string> complianceStandards)
    {
        if (retentionPeriod.TotalDays < 365) // Minimum 1 year for compliance
            return Result<AuditConfiguration>.Failure("Retention period must be at least 365 days for compliance requirements");

        if (!complianceStandards.Any())
            return Result<AuditConfiguration>.Failure("At least one compliance standard must be specified");

        var config = new AuditConfiguration(auditScope, retentionPeriod, complianceStandards);
        return Result<AuditConfiguration>.Success(config);
    }

    public Result EnableRealTimeMonitoring(bool enabled)
    {
        RealTimeMonitoringEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Scope of audit operations
/// </summary>
public enum AuditScope
{
    SystemAccess = 1,
    CulturalData = 2,
    UserActivity = 3,
    AdminOperations = 4,
    SecurityEvents = 5,
    ComplianceOperations = 6
}

#endregion

#region Access Pattern Analysis

/// <summary>
/// Analysis of user access patterns for anomaly detection
/// </summary>
public class AccessPatternAnalysis
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public TimeSpan AnalysisWindow { get; private set; }
    public List<AccessPattern> AccessPatterns { get; private set; } = new();
    public int TotalAccesses => AccessPatterns.Count;
    public bool IsAnomalous { get; private set; }
    public decimal AnomalyScore { get; private set; }
    public DateTime AnalysisTimestamp { get; private set; } = DateTime.UtcNow;
    public Dictionary<string, object> AnalysisMetadata { get; private set; } = new();

    private AccessPatternAnalysis() { }

    private AccessPatternAnalysis(Guid userId, TimeSpan analysisWindow, IEnumerable<AccessPattern> accessPatterns)
    {
        UserId = userId;
        AnalysisWindow = analysisWindow;
        AccessPatterns = accessPatterns.ToList();
        AnalysisTimestamp = DateTime.UtcNow;
        
        // Calculate anomaly detection
        CalculateAnomalyScore();
    }

    public static Result<AccessPatternAnalysis> Create(Guid userId, TimeSpan analysisWindow, IEnumerable<AccessPattern> accessPatterns)
    {
        if (userId == Guid.Empty)
            return Result<AccessPatternAnalysis>.Failure("User ID cannot be empty");

        if (analysisWindow.TotalMinutes < 1)
            return Result<AccessPatternAnalysis>.Failure("Analysis window must be at least 1 minute");

        var analysis = new AccessPatternAnalysis(userId, analysisWindow, accessPatterns ?? Enumerable.Empty<AccessPattern>());
        return Result<AccessPatternAnalysis>.Success(analysis);
    }

    private void CalculateAnomalyScore()
    {
        if (!AccessPatterns.Any())
        {
            AnomalyScore = 0;
            IsAnomalous = false;
            return;
        }

        // Calculate anomaly based on access frequency and patterns
        var accessesPerMinute = TotalAccesses / Math.Max(1, AnalysisWindow.TotalMinutes);
        var uniqueResources = AccessPatterns.Select(p => p.Resource).Distinct().Count();
        var resourceVariety = uniqueResources / Math.Max(1, TotalAccesses);

        // High frequency access (>10 per minute) or very low resource variety (<0.1) indicates potential anomaly
        var frequencyScore = accessesPerMinute > 10 ? Math.Min(100, accessesPerMinute * 2) : 0;
        var varietyScore = resourceVariety < 0.1 ? (1 - resourceVariety) * 100 : 0;

        AnomalyScore = (decimal)Math.Max(frequencyScore, varietyScore);
        IsAnomalous = AnomalyScore > 70; // Threshold for anomaly detection
    }
}

/// <summary>
/// Individual access pattern record
/// </summary>
public class AccessPattern
{
    public string Resource { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string AccessType { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

#endregion

#region Access Audit Result

/// <summary>
/// Result type for access audit operations
/// </summary>
public class AccessAuditResult : Result<AccessAuditSummary>
{
    public AccessAuditSummary? AuditSummary => IsSuccess ? Value : null;
    public bool IsCompliant => AuditSummary?.ComplianceScore >= 95m;
    public int ViolationCount => AuditSummary?.ViolationsFound ?? 0;
    public decimal CompliancePercentage => AuditSummary?.ComplianceScore ?? 0m;
    public bool RequiresImmediateAction => CompliancePercentage < 90m;

    private AccessAuditResult(bool isSuccess, IEnumerable<string> errors, AccessAuditSummary? value = null)
        : base(isSuccess, errors, value) { }

    public static new AccessAuditResult Success(AccessAuditSummary summary)
        => new(true, Array.Empty<string>(), summary);

    public static new AccessAuditResult Failure(string error)
        => new(false, new[] { error });

    public static new AccessAuditResult Failure(IEnumerable<string> errors)
        => new(false, errors);
}

/// <summary>
/// Summary of access audit results
/// </summary>
public class AccessAuditSummary
{
    public Guid AuditId { get; init; } = Guid.NewGuid();
    public long TotalAccessesAudited { get; init; }
    public int ViolationsFound { get; init; }
    public decimal ComplianceScore { get; init; }
    public long AuditDurationMs { get; init; }
    public long CulturalDataAccesses { get; init; }
    public DateTime AuditTimestamp { get; init; } = DateTime.UtcNow;
    public List<string> ViolationDetails { get; init; } = new();
    public Dictionary<string, object> AuditMetadata { get; init; } = new();
    
    /// <summary>
    /// Percentage of cultural data accesses that were properly authorized
    /// </summary>
    public decimal CulturalDataComplianceRate => 
        CulturalDataAccesses > 0 ? (decimal)(CulturalDataAccesses - ViolationsFound) / CulturalDataAccesses * 100 : 100m;
}

#endregion

#region Cross-Region Security Types (Supporting Types)

/// <summary>
/// Cross-region security policy for cultural intelligence data
/// </summary>
public class CrossRegionSecurityPolicy
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string PolicyName { get; private set; } = string.Empty;
    public List<string> AllowedRegions { get; private set; } = new();
    public List<string> RestrictedRegions { get; private set; } = new();
    public bool RequiresCulturalApproval { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private CrossRegionSecurityPolicy() { }

    public static Result<CrossRegionSecurityPolicy> Create(string policyName, IEnumerable<string> allowedRegions, bool requiresCulturalApproval = true)
    {
        if (string.IsNullOrWhiteSpace(policyName))
            return Result<CrossRegionSecurityPolicy>.Failure("Policy name is required");

        var policy = new CrossRegionSecurityPolicy
        {
            PolicyName = policyName,
            AllowedRegions = allowedRegions.ToList(),
            RequiresCulturalApproval = requiresCulturalApproval,
            CreatedAt = DateTime.UtcNow
        };

        return Result<CrossRegionSecurityPolicy>.Success(policy);
    }
}

/// <summary>
/// Regional data center information for security operations
/// </summary>
public class RegionalDataCenter
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Region { get; private set; } = string.Empty;
    public string DataCenterCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public List<string> ComplianceStandards { get; private set; } = new();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private RegionalDataCenter() { }

    public static Result<RegionalDataCenter> Create(string region, string dataCenterCode, IEnumerable<string> complianceStandards)
    {
        if (string.IsNullOrWhiteSpace(region))
            return Result<RegionalDataCenter>.Failure("Region is required");

        if (string.IsNullOrWhiteSpace(dataCenterCode))
            return Result<RegionalDataCenter>.Failure("Data center code is required");

        var dataCenter = new RegionalDataCenter
        {
            Region = region,
            DataCenterCode = dataCenterCode,
            ComplianceStandards = complianceStandards.ToList(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result<RegionalDataCenter>.Success(dataCenter);
    }
}

/// <summary>
/// Security configuration synchronization across regions
/// </summary>
public class SecurityConfigurationSync
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string SourceRegion { get; private set; } = string.Empty;
    public List<string> TargetRegions { get; private set; } = new();
    public DateTime LastSyncTimestamp { get; private set; }
    public bool IsActive { get; private set; } = true;

    private SecurityConfigurationSync() { }

    public static Result<SecurityConfigurationSync> Create(string sourceRegion, IEnumerable<string> targetRegions)
    {
        if (string.IsNullOrWhiteSpace(sourceRegion))
            return Result<SecurityConfigurationSync>.Failure("Source region is required");

        if (!targetRegions.Any())
            return Result<SecurityConfigurationSync>.Failure("At least one target region is required");

        var sync = new SecurityConfigurationSync
        {
            SourceRegion = sourceRegion,
            TargetRegions = targetRegions.ToList(),
            LastSyncTimestamp = DateTime.UtcNow,
            IsActive = true
        };

        return Result<SecurityConfigurationSync>.Success(sync);
    }
}

#endregion