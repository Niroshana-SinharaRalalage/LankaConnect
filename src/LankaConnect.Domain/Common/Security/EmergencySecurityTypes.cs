using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Security;

/// <summary>
/// Emergency security types to resolve CS0246 compilation errors
/// Phase 1 of 3-hour emergency architecture recovery - minimal implementation for compilation
/// </summary>

// Audit and Access Security Types
public class AuditConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public List<string> AuditedEvents { get; set; } = new();
    public TimeSpan RetentionPeriod { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AccessPatternAnalysis
{
    public string AnalysisId { get; set; } = string.Empty;
    public Dictionary<string, object> AccessPatterns { get; set; } = new();
    public List<string> AnomalousPatterns { get; set; } = new();
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
}

public class AccessAuditResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool IsCompliant { get; set; }
    public List<string> ViolationDetails { get; set; } = new();
    public DateTime AuditTimestamp { get; set; } = DateTime.UtcNow;
}

// Cross-Region Security Types
public class CrossRegionSecurityPolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public List<string> ApplicableRegions { get; set; } = new();
    public Dictionary<string, object> SecurityRules { get; set; } = new();
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}

public class RegionalDataCenter
{
    public string DataCenterId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> SecurityCapabilities { get; set; } = new();
}

public class SecurityConfigurationSync
{
    public string SyncId { get; set; } = string.Empty;
    public Dictionary<string, object> SyncParameters { get; set; } = new();
    public bool IsSuccessful { get; set; }
    public DateTime SyncTimestamp { get; set; } = DateTime.UtcNow;
}

public class SecuritySynchronizationResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public List<string> SynchronizedItems { get; set; } = new();
    public Dictionary<string, object> SyncMetrics { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

// Data Transfer Security Types
public class DataTransferRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string SourceRegion { get; set; } = string.Empty;
    public string DestinationRegion { get; set; } = string.Empty;
    public List<string> DataCategories { get; set; } = new();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

public class CrossBorderComplianceRequirements
{
    public string RequirementId { get; set; } = string.Empty;
    public Dictionary<string, List<string>> RegionCompliance { get; set; } = new();
    public bool RequiresLegalReview { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
}

public class CrossBorderSecurityResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool IsCompliant { get; set; }
    public List<string> ComplianceIssues { get; set; } = new();
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
}

// Additional Critical Types
public class RegionalSecurityMaintenance
{
    public string MaintenanceId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public Dictionary<string, object> MaintenanceTasks { get; set; } = new();
    public DateTime ScheduledTime { get; set; }
}

public class RegionalFailoverSecurityResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool FailoverSuccessful { get; set; }
    public Dictionary<string, object> SecurityStatus { get; set; } = new();
    public DateTime FailoverTimestamp { get; set; } = DateTime.UtcNow;
}

public class CrossBorderPrivacyResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool PrivacyCompliant { get; set; }
    public List<string> PrivacyViolations { get; set; } = new();
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
}

// Auto-scaling and Monitoring Integration
public class AutoScalingConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public Dictionary<string, object> ScalingRules { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
}

public class SecurityMonitoringIntegration
{
    public string IntegrationId { get; set; } = string.Empty;
    public Dictionary<string, object> MonitoringConfiguration { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
}

public class CulturalEventSecurityMonitoringResult
{
    public string ResultId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public Dictionary<string, object> SecurityMetrics { get; set; } = new();
    public List<string> SecurityAlerts { get; set; } = new();
    public DateTime MonitoredAt { get; set; } = DateTime.UtcNow;
}

// Backup and Disaster Recovery Security
public class BackupConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public Dictionary<string, object> BackupParameters { get; set; } = new();
    public TimeSpan BackupFrequency { get; set; }
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
}

public class SecurityIntegrationPolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public Dictionary<string, object> IntegrationRules { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}

public class SecurityBackupIntegrationResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool IntegrationSuccessful { get; set; }
    public Dictionary<string, object> BackupStatus { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

public class BackupOperation
{
    public string OperationId { get; set; } = string.Empty;
    public string BackupType { get; set; } = string.Empty;
    public Dictionary<string, object> OperationParameters { get; set; } = new();
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}

// DisasterRecoveryProcedure moved to canonical location: LankaConnect.Application.Common.Security.SecurityFoundationTypes
// Use Application type for rich domain model with Result pattern and business logic validation

public class SecurityMaintenanceProtocol
{
    public string ProtocolId { get; set; } = string.Empty;
    public Dictionary<string, object> MaintenanceRules { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}

public class ScalingOperation
{
    public string OperationId { get; set; } = string.Empty;
    public string ScalingDirection { get; set; } = string.Empty; // "up" or "down"
    public Dictionary<string, object> ScalingParameters { get; set; } = new();
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
}

public class MLThreatDetectionConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public Dictionary<string, object> MLModelParameters { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
}

// Additional Cross-Border and Privacy Types
public class CrossBorderDataTransfer
{
    public string TransferId { get; set; } = string.Empty;
    public string SourceCountry { get; set; } = string.Empty;
    public string DestinationCountry { get; set; } = string.Empty;
    public List<string> DataCategories { get; set; } = new();
    public Dictionary<string, object> ComplianceData { get; set; } = new();
    public DateTime TransferTimestamp { get; set; } = DateTime.UtcNow;
}

public class InternationalPrivacyFramework
{
    public string FrameworkId { get; set; } = string.Empty;
    public string FrameworkName { get; set; } = string.Empty;
    public Dictionary<string, List<string>> CountryRequirements { get; set; } = new();
    public List<string> ApplicableRegulations { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}