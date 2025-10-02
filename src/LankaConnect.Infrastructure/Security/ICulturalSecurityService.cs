using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Notifications;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Security;
using LankaConnect.Domain.Infrastructure;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Shared;
using LankaConnect.Infrastructure.Monitoring;
using LankaConnect.Infrastructure.Database.LoadBalancing;
using LankaConnect.Domain.Common.Models;

// Alias ambiguous types to their Domain layer sources (Clean Architecture preference)
using GeographicRegion = LankaConnect.Domain.Common.Enums.GeographicRegion;
using ResponseAction = LankaConnect.Domain.Shared.ResponseAction;
using RegionalSecurityStatus = LankaConnect.Domain.Common.Models.RegionalSecurityStatus;
using CulturalContext = LankaConnect.Domain.Common.Database.CulturalContext;

namespace LankaConnect.Infrastructure.Security
{
    /// <summary>
    /// Cultural security service interface for cultural intelligence-aware security operations
    /// </summary>
    public interface ICulturalSecurityService
    {
        Task<bool> AnalyzeSensitivityAsync(CulturalContext context, CancellationToken cancellationToken);
        Task ApplyBuddhistSecurityProtocolsAsync(CulturalEventType eventType, GeographicRegion region, CancellationToken cancellationToken);
        Task ApplyIslamicSecurityProtocolsAsync(CulturalEventType eventType, GeographicRegion region, CancellationToken cancellationToken);
        Task ApplyHinduSecurityProtocolsAsync(CulturalEventType eventType, GeographicRegion region, CancellationToken cancellationToken);
        Task ApplySikhSecurityProtocolsAsync(CulturalEventType eventType, GeographicRegion region, CancellationToken cancellationToken);
        Task ApplyGenericCulturalSecurityAsync(CulturalEventType eventType, SensitivityLevel sensitivityLevel, CancellationToken cancellationToken);
        Task<SecurityProtocol> CreateUnifiedProtocolAsync(CulturalProfile[] profiles, SecurityPolicySet policySet, CancellationToken cancellationToken);
        Task<bool> ValidateCrossReligiousCompatibilityAsync(CulturalProfile[] profiles, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Encryption service interface for cultural content encryption
    /// </summary>
    public interface IEncryptionService
    {
        Task<string> EncryptWithCulturalContextAsync(string content, string algorithm, int rounds, CulturalContext culturalContext, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Compliance validator interface for Fortune 500 compliance validation
    /// </summary>
    public interface IComplianceValidator
    {
        Task<ComplianceValidationResult> ValidateSOXComplianceAsync(ValidationScope scope, CancellationToken cancellationToken);
        Task<ComplianceValidationResult> ValidateGDPRComplianceAsync(ValidationScope scope, CancellationToken cancellationToken);
        Task<ComplianceValidationResult> ValidateHIPAAComplianceAsync(ValidationScope scope, CancellationToken cancellationToken);
        Task<ComplianceValidationResult> ValidatePCIDSSComplianceAsync(ValidationScope scope, CancellationToken cancellationToken);
        Task<ComplianceValidationResult> ValidateISO27001ComplianceAsync(ValidationScope scope, CancellationToken cancellationToken);
        Task<RegionalComplianceValidationResult> ValidateRegionalComplianceAsync(GeographicRegion region, CulturalEventType eventType, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Security incident handler interface
    /// </summary>
    public interface ISecurityIncidentHandler
    {
        Task<ResponseAction> ExecuteImmediateContainmentAsync(SecurityIncident incident, CancellationToken cancellationToken);
        Task<ResponseAction> NotifyReligiousAuthoritiesAsync(SecurityIncident incident, CulturalIncidentContext context, CancellationToken cancellationToken);
        Task<ResponseAction> InitiateCulturalDamageAssessmentAsync(SecurityIncident incident, CulturalIncidentContext context, CancellationToken cancellationToken);
        Task<ResponseAction> InitiateCulturalMediationAsync(SecurityIncident incident, CulturalIncidentContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Multi-region security coordinator interface
    /// </summary>
    public interface IMultiRegionSecurityCoordinator
    {
        Task<RegionalSecurityStatus> ApplyRegionalSecurityPolicyAsync(GeographicRegion region, CrossRegionSecurityPolicy policy, CancellationToken cancellationToken);
        Task<TimeSpan> CalculateAverageRegionalLatencyAsync(GeographicRegion[] regions, CancellationToken cancellationToken);
        Task<SyncResult> SynchronizeDataCenterSecurityAsync(RegionalDataCenter dataCenter, SecurityConfigurationSync syncConfig, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Access control service interface
    /// </summary>
    public interface IAccessControlService
    {
        Task<AuthorityLevel> ValidateCulturalAuthorityAsync(CulturalUserProfile userProfile, CulturalRoleDefinition roleDefinition, CancellationToken cancellationToken);
        Task<bool> ValidateCulturalRoleCompatibilityAsync(CulturalUserProfile userProfile, CulturalRoleDefinition roleDefinition, CancellationToken cancellationToken);
        Task<bool> ValidateCommunityVerificationAsync(CulturalUserProfile userProfile, CancellationToken cancellationToken);
        Task<AccessAuditTrail> CreateAccessAuditTrailAsync(CulturalUserProfile userProfile, CulturalRoleDefinition roleDefinition, List<string> grantedRoles, List<string> deniedPermissions, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Security audit logger interface
    /// </summary>
    public interface ISecurityAuditLogger
    {
        Task LogSecurityOptimizationAsync(CulturalContext culturalContext, SecurityProfile securityProfile, List<OptimizationRecommendation> recommendations, CancellationToken cancellationToken);
        Task LogSacredEventAccessAsync(SacredEvent sacredEvent, EnhancedSecurityConfig enhancedConfig, CancellationToken cancellationToken);
        Task LogIncidentResponseAsync(SecurityIncident incident, List<ResponseAction> responseActions, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Data classification service interface
    /// </summary>
    public interface IDataClassificationService
    {
        Task<IEnumerable<ClassifiedCulturalDataElement>> ClassifyCulturalDataElementsAsync(IEnumerable<CulturalDataElement> elements, CancellationToken cancellationToken);
    }


    // Supporting types for the interfaces
    public record SecurityProtocol(string ProtocolId, string Name, IEnumerable<SecurityRule> Rules);
    public record SecurityRule(string RuleId, string Description, SecurityLevel Level);
    public record SecurityPolicySet(string PolicySetId, IEnumerable<SecurityPolicy> Policies);
    public record SecurityPolicy(string PolicyId, string Name, SecurityLevel Level);
    public record ValidationScope(string ScopeId, string Description);
    public record RegionalComplianceValidationResult(bool IsCompliant, IEnumerable<ComplianceViolation> Violations);
    public record ComplianceViolation(string Code, string Description);
    public record CulturalIncidentContext(string CulturalIdentifier, bool InvolvesSacredContent, int SacredLevel, bool AffectsMultipleCultures);
    public record SecurityIncidentTrigger(string Type, IncidentSeverity BaseSeverity);
    public record CulturalImpactAssessment(string AssessmentId, ImpactLevel Level, string Description);
    public record CulturalRoleDefinition(string RoleName, AuthorityLevel RequiredAuthorityLevel, bool RequiresCommunityVerification, IEnumerable<string> InheritedRoles);
    public record CrossRegionSecurityPolicy(string PolicyId, string Name, IEnumerable<SecurityRule> Rules);
    public record RegionalDataCenter(string DataCenterId, string Region, string Status);
    public record SecurityConfigurationSync(string SyncId, IEnumerable<SecurityConfiguration> Configurations);
    public record SecurityConfiguration(string ConfigId, string Name, SecurityLevel Level);
    public record SacredEvent(string EventId, string EventName, SacredSignificanceLevel SignificanceLevel, IEnumerable<CulturalGroup> InvolvedGroups);
    public record CulturalGroup(string GroupId, string Name, bool RequiresSpecialHandling);
    public record EnhancedSecurityConfig(string ConfigId, SecurityLevel Level, bool DoubleEncryption);
    public record SensitiveData(string DataType, string Content, SensitivityLevel SensitivityLevel, CulturalContext CulturalContext, Dictionary<string, object> Metadata);
    public record CulturalEncryptionPolicy(string PolicyId, string Algorithm, int Rounds);
    public record CulturalDataElement(string ElementId, string Content, CulturalContext CulturalContext);
    public record ClassifiedCulturalDataElement(string ElementId, string Content, CulturalContext CulturalContext, DataClassification Classification);
    public record DataClassification(string ClassificationId, SecurityLevel Level, PrivacyLevel PrivacyLevel);
    public record SecurityMetrics(string MetricsId, Dictionary<string, object> Metrics);

    public enum AuthorityLevel { Unknown, Basic, Verified, Expert, Religious }
    public enum SacredSignificanceLevel { Ceremonial, Ritual, Sacred, MostSacred, Taboo }
    public enum ImpactLevel { Low, Medium, High, Critical }
    public enum PrivacyLevel { Public, Internal, Confidential, Secret }

    // Additional supporting types for tests and implementation
    public record PrivacyPreferences(SensitivityLevel SensitivityLevel);
    public record RegionalComplianceRequirements(string RequirementId, IEnumerable<ComplianceRequirement> Requirements);
    public record ComplianceRequirement(string RequirementId, string Description);
    public record EncryptionResult(bool IsEncrypted, string EncryptedData, Dictionary<string, object> PreservedMetadata, string Algorithm, DateTime EncryptedAt);
    public record SacredEventSecurityResult(bool DoubleEncryptionApplied, bool SpecialHandlingApplied, string[] AccessRestrictions, bool AuditTrailCreated, DateTime ProcessedAt);
    
    // Extended result types for comprehensive testing
    public record MultiCulturalSecurityResult(bool HighestSecurityLevelApplied, SecurityLevel SecurityLevelApplied, bool CrossReligiousValidationPassed, bool UnifiedSecurityProtocolUsed, CrossCulturalSecurityMetrics Metrics);
    public record CulturalContentSecurityResult(bool IsValid, IEnumerable<SecurityViolation> Violations, DateTime ValidatedAt);
}