using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Domain.Common.Database
{
    // Security Level Classifications
    public enum SecurityLevel
    {
        Public = 1,
        Internal = 2,
        Confidential = 3,
        Secret = 4,
        TopSecret = 5,
        CulturalSacred = 10
    }

    public enum ComplianceStandard
    {
        GDPR = 1,
        CCPA = 2,
        HIPAA = 3,
        SOX = 4,
        PCI_DSS = 5,
        ISO27001 = 6,
        NIST = 7,
        FedRAMP = 8,
        SOC2 = 9,
        FISMA = 10,
        CulturalHeritage = 100,
        SacredContentProtection = 101,
        DiasporaPrivacy = 102
    }

    public enum CulturalContentSensitivity
    {
        Public = 1,
        CommunityOnly = 2,
        RegionalSensitive = 3,
        ReligiousSacred = 4,
        FamilyPrivate = 5,
        CeremoniaBound = 6,
        AncestralProtected = 7,
        SpirituallySignificant = 8,
        CulturallyTaboo = 9,
        SacredSecret = 10
    }

    public enum SecurityEventType
    {
        UnauthorizedAccess = 1,
        DataBreach = 2,
        ComplianceViolation = 3,
        SuspiciousActivity = 4,
        CulturalContentViolation = 5,
        PermissionEscalation = 6,
        AuditFailure = 7,
        IntegrityViolation = 8,
        AvailabilityIssue = 9,
        EncryptionFailure = 10,
        SacredContentExposure = 11,
        CulturalProtocolBreach = 12
    }

    public enum SecurityAction
    {
        Allow = 1,
        Deny = 2,
        Audit = 3,
        Block = 4,
        Quarantine = 5,
        Encrypt = 6,
        Anonymize = 7,
        CulturalValidation = 8,
        EscalateToElder = 9,
        RequireCeremonyApproval = 10
    }

    public enum AccessControlType
    {
        RoleBased = 1,
        AttributeBased = 2,
        DiscretionaryBased = 3,
        MandatoryBased = 4,
        RiskBased = 5,
        CulturalHierarchy = 6,
        CommunityConsensus = 7,
        SacredGuardianship = 8,
        ElderApproval = 9,
        CeremonyBased = 10
    }

    // Value Objects
    public class SecurityClassification : ValueObject
    {
        public SecurityLevel Level { get; }
        public CulturalContentSensitivity CulturalSensitivity { get; }
        public string[] Tags { get; }
        public DateTime ClassifiedAt { get; }
        public string ClassifiedBy { get; }

        public SecurityClassification(
            SecurityLevel level,
            CulturalContentSensitivity culturalSensitivity,
            string[] tags,
            string classifiedBy)
        {
            Level = level;
            CulturalSensitivity = culturalSensitivity;
            Tags = tags ?? Array.Empty<string>();
            ClassifiedAt = DateTime.UtcNow;
            ClassifiedBy = classifiedBy;
        }

        public bool RequiresCulturalValidation => CulturalSensitivity >= CulturalContentSensitivity.ReligiousSacred;
        public bool IsSacredContent => CulturalSensitivity >= CulturalContentSensitivity.SpirituallySignificant;
        public bool RequiresElderApproval => Level == SecurityLevel.CulturalSacred;

        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return Level;
            yield return CulturalSensitivity;
            yield return string.Join(",", Tags);
            yield return ClassifiedBy;
        }
    }

    public class ComplianceConfiguration : ValueObject
    {
        public ComplianceStandard[] Standards { get; }
        public Dictionary<string, object> Requirements { get; }
        public bool IsActive { get; }
        public DateTime EffectiveFrom { get; }
        public DateTime? EffectiveUntil { get; }

        public ComplianceConfiguration(
            ComplianceStandard[] standards,
            Dictionary<string, object> requirements,
            DateTime effectiveFrom,
            DateTime? effectiveUntil = null)
        {
            Standards = standards ?? throw new ArgumentNullException(nameof(standards));
            Requirements = requirements ?? new Dictionary<string, object>();
            IsActive = true;
            EffectiveFrom = effectiveFrom;
            EffectiveUntil = effectiveUntil;
        }

        public bool MeetsStandard(ComplianceStandard standard) => Array.Exists(Standards, s => s == standard);
        public bool HasCulturalCompliance => MeetsStandard(ComplianceStandard.CulturalHeritage);

        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return string.Join(",", Standards);
            yield return EffectiveFrom;
            yield return EffectiveUntil?.ToString() ?? string.Empty;
        }
    }

    public class SecurityMetrics : ValueObject
    {
        public int ThreatScore { get; }
        public double RiskRating { get; }
        public int VulnerabilityCount { get; }
        public double ComplianceScore { get; }
        public double CulturalSensitivityScore { get; }
        public DateTime CalculatedAt { get; }

        public SecurityMetrics(
            int threatScore,
            double riskRating,
            int vulnerabilityCount,
            double complianceScore,
            double culturalSensitivityScore)
        {
            ThreatScore = Math.Max(0, Math.Min(100, threatScore));
            RiskRating = Math.Max(0.0, Math.Min(10.0, riskRating));
            VulnerabilityCount = Math.Max(0, vulnerabilityCount);
            ComplianceScore = Math.Max(0.0, Math.Min(100.0, complianceScore));
            CulturalSensitivityScore = Math.Max(0.0, Math.Min(10.0, culturalSensitivityScore));
            CalculatedAt = DateTime.UtcNow;
        }

        public bool IsHighRisk => RiskRating >= 7.0 || ThreatScore >= 80 || VulnerabilityCount >= 10;
        public bool RequiresCulturalReview => CulturalSensitivityScore >= 7.0;
        public bool IsCompliant => ComplianceScore >= 80.0;

        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return ThreatScore;
            yield return RiskRating;
            yield return VulnerabilityCount;
            yield return ComplianceScore;
            yield return CulturalSensitivityScore;
        }
    }

    // Domain Models
    public class DatabaseSecurityRequest
    {
        public Guid Id { get; }
        public string DatabaseName { get; }
        public string Operation { get; }
        public SecurityClassification Classification { get; }
        public string UserId { get; }
        public string UserRole { get; }
        public string[] CulturalAffiliations { get; }
        public Dictionary<string, object> Context { get; }
        public DateTime RequestedAt { get; }
        public required string IPAddress { get; init; }
        public required string UserAgent { get; init; }

        public DatabaseSecurityRequest(
            string databaseName,
            string operation,
            SecurityClassification classification,
            string userId,
            string userRole,
            string ipAddress,
            string userAgent,
            string[]? culturalAffiliations = null,
            Dictionary<string, object>? context = null)
        {
            Id = Guid.NewGuid();
            DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Classification = classification ?? throw new ArgumentNullException(nameof(classification));
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            UserRole = userRole ?? throw new ArgumentNullException(nameof(userRole));
            CulturalAffiliations = culturalAffiliations ?? Array.Empty<string>();
            Context = context ?? new Dictionary<string, object>();
            RequestedAt = DateTime.UtcNow;
            IPAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            UserAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
        }

        public bool HasCulturalContext => CulturalAffiliations.Length > 0;
        public bool RequiresCulturalValidation => Classification.RequiresCulturalValidation;
    }

    public class DatabaseSecurityResponse
    {
        public Guid RequestId { get; }
        public SecurityAction Action { get; }
        public bool IsAllowed { get; }
        public string Reason { get; }
        public string[] Violations { get; }
        public required SecurityMetrics Metrics { get; init; }
        public Dictionary<string, object> AdditionalData { get; }
        public DateTime ProcessedAt { get; }
        public TimeSpan ProcessingTime { get; }

        public DatabaseSecurityResponse(
            Guid requestId,
            SecurityAction action,
            bool isAllowed,
            string reason,
            SecurityMetrics metrics,
            string[]? violations = null,
            Dictionary<string, object>? additionalData = null,
            TimeSpan? processingTime = null)
        {
            RequestId = requestId;
            Action = action;
            IsAllowed = isAllowed;
            Reason = reason ?? string.Empty;
            Violations = violations ?? Array.Empty<string>();
            Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            AdditionalData = additionalData ?? new Dictionary<string, object>();
            ProcessedAt = DateTime.UtcNow;
            ProcessingTime = processingTime ?? TimeSpan.Zero;
        }

        public bool HasViolations => Violations.Length > 0;
        public bool RequiresAudit => Action == SecurityAction.Audit || !IsAllowed;
    }

    public class CulturalSecurityAnalysis
    {
        public Guid Id { get; }
        public string ContentType { get; }
        public CulturalContentSensitivity Sensitivity { get; }
        public string[] CulturalTags { get; }
        public string[] RequiredPermissions { get; }
        public string[] ApprovalRequired { get; }
        public Dictionary<string, double> CulturalScores { get; }
        public bool RequiresElderReview { get; }
        public bool RequiresCommunityConsent { get; }
        public DateTime AnalyzedAt { get; }

        public CulturalSecurityAnalysis(
            string contentType,
            CulturalContentSensitivity sensitivity,
            string[] culturalTags,
            string[] requiredPermissions,
            string[] approvalRequired,
            Dictionary<string, double> culturalScores,
            bool requiresElderReview = false,
            bool requiresCommunityConsent = false)
        {
            Id = Guid.NewGuid();
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
            Sensitivity = sensitivity;
            CulturalTags = culturalTags ?? Array.Empty<string>();
            RequiredPermissions = requiredPermissions ?? Array.Empty<string>();
            ApprovalRequired = approvalRequired ?? Array.Empty<string>();
            CulturalScores = culturalScores ?? new Dictionary<string, double>();
            RequiresElderReview = requiresElderReview;
            RequiresCommunityConsent = requiresCommunityConsent;
            AnalyzedAt = DateTime.UtcNow;
        }

        public bool IsSacredContent => Sensitivity >= CulturalContentSensitivity.SpirituallySignificant;
        public bool HasHighCulturalImpact => CulturalScores.Values.Any(score => score >= 8.0);
        public bool RequiresSpecialHandling => RequiresElderReview || RequiresCommunityConsent || IsSacredContent;
    }

    public class SecurityCoordination
    {
        public Guid Id { get; }
        public string[] Regions { get; }
        public Dictionary<string, SecurityConfiguration> RegionalConfigurations { get; }
        public SecurityLevel GlobalSecurityLevel { get; }
        public ComplianceStandard[] GlobalCompliance { get; }
        public Dictionary<string, object> SynchronizationSettings { get; }
        public DateTime LastSynchronized { get; private set; }
        public bool IsActive { get; private set; }

        public SecurityCoordination(
            string[] regions,
            Dictionary<string, SecurityConfiguration> regionalConfigurations,
            SecurityLevel globalSecurityLevel,
            ComplianceStandard[] globalCompliance)
        {
            Id = Guid.NewGuid();
            Regions = regions ?? throw new ArgumentNullException(nameof(regions));
            RegionalConfigurations = regionalConfigurations ?? new Dictionary<string, SecurityConfiguration>();
            GlobalSecurityLevel = globalSecurityLevel;
            GlobalCompliance = globalCompliance ?? Array.Empty<ComplianceStandard>();
            SynchronizationSettings = new Dictionary<string, object>();
            LastSynchronized = DateTime.UtcNow;
            IsActive = true;
        }

        public void Synchronize()
        {
            LastSynchronized = DateTime.UtcNow;
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        public bool HasRegion(string region) => Array.Exists(Regions, r => r.Equals(region, StringComparison.OrdinalIgnoreCase));
        public SecurityConfiguration? GetRegionalConfiguration(string region) => 
            RegionalConfigurations.TryGetValue(region, out var config) ? config : null;
    }

    public class SecurityConfiguration
    {
        public Guid Id { get; }
        public string Name { get; }
        public SecurityLevel MinimumSecurityLevel { get; }
        public ComplianceConfiguration ComplianceConfig { get; }
        public Dictionary<string, object> EncryptionSettings { get; }
        public Dictionary<string, object> AccessControlSettings { get; }
        public Dictionary<string, object> AuditSettings { get; }
        public Dictionary<string, object> CulturalSettings { get; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; }
        public DateTime LastUpdated { get; private set; }

        public SecurityConfiguration(
            string name,
            SecurityLevel minimumSecurityLevel,
            ComplianceConfiguration complianceConfig,
            Dictionary<string, object>? encryptionSettings = null,
            Dictionary<string, object>? accessControlSettings = null,
            Dictionary<string, object>? auditSettings = null,
            Dictionary<string, object>? culturalSettings = null)
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MinimumSecurityLevel = minimumSecurityLevel;
            ComplianceConfig = complianceConfig ?? throw new ArgumentNullException(nameof(complianceConfig));
            EncryptionSettings = encryptionSettings ?? new Dictionary<string, object>();
            AccessControlSettings = accessControlSettings ?? new Dictionary<string, object>();
            AuditSettings = auditSettings ?? new Dictionary<string, object>();
            CulturalSettings = culturalSettings ?? new Dictionary<string, object>();
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
        }

        public void UpdateSettings(Dictionary<string, object> settings)
        {
            if (settings?.Count > 0)
            {
                foreach (var setting in settings)
                {
                    // Update appropriate settings based on key prefix
                    if (setting.Key.StartsWith("encryption."))
                        EncryptionSettings[setting.Key] = setting.Value;
                    else if (setting.Key.StartsWith("access."))
                        AccessControlSettings[setting.Key] = setting.Value;
                    else if (setting.Key.StartsWith("audit."))
                        AuditSettings[setting.Key] = setting.Value;
                    else if (setting.Key.StartsWith("cultural."))
                        CulturalSettings[setting.Key] = setting.Value;
                }
                LastUpdated = DateTime.UtcNow;
            }
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        public bool HasCulturalProtections => CulturalSettings.Count > 0;
        public bool MeetsComplianceStandard(ComplianceStandard standard) => ComplianceConfig.MeetsStandard(standard);
    }

    public class IncidentResponseMetrics
    {
        public Guid Id { get; }
        public SecurityEventType EventType { get; }
        public SecurityLevel SeverityLevel { get; }
        public DateTime DetectedAt { get; }
        public DateTime? ResponseStartedAt { get; private set; }
        public DateTime? ResolvedAt { get; private set; }
        public TimeSpan? ResponseTime => ResponseStartedAt?.Subtract(DetectedAt);
        public TimeSpan? ResolutionTime => ResolvedAt?.Subtract(DetectedAt);
        public string[] AffectedSystems { get; }
        public string[] AffectedCulturalContent { get; }
        public Dictionary<string, object> ImpactMetrics { get; }
        public string[] ResponseActions { get; private set; }
        public bool IsResolved => ResolvedAt.HasValue;

        public IncidentResponseMetrics(
            SecurityEventType eventType,
            SecurityLevel severityLevel,
            string[] affectedSystems,
            string[]? affectedCulturalContent = null,
            Dictionary<string, object>? impactMetrics = null)
        {
            Id = Guid.NewGuid();
            EventType = eventType;
            SeverityLevel = severityLevel;
            DetectedAt = DateTime.UtcNow;
            AffectedSystems = affectedSystems ?? Array.Empty<string>();
            AffectedCulturalContent = affectedCulturalContent ?? Array.Empty<string>();
            ImpactMetrics = impactMetrics ?? new Dictionary<string, object>();
            ResponseActions = Array.Empty<string>();
        }

        public void StartResponse()
        {
            if (!ResponseStartedAt.HasValue)
                ResponseStartedAt = DateTime.UtcNow;
        }

        public void AddResponseAction(string action)
        {
            var actions = ResponseActions.ToList();
            actions.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {action}");
            ResponseActions = actions.ToArray();
        }

        public void Resolve()
        {
            if (!ResolvedAt.HasValue)
                ResolvedAt = DateTime.UtcNow;
        }

        public bool IsCritical => SeverityLevel >= SecurityLevel.Secret;
        public bool AffectsCulturalContent => AffectedCulturalContent.Length > 0;
        public bool RequiresImmediateResponse => IsCritical || AffectsCulturalContent;
    }

    public class AccessControlProcedure
    {
        public Guid Id { get; }
        public string Name { get; }
        public AccessControlType Type { get; }
        public string[] RequiredRoles { get; }
        public string[] RequiredAttributes { get; }
        public string[] CulturalRequirements { get; }
        public Dictionary<string, object> ValidationRules { get; }
        public SecurityLevel MinimumSecurityLevel { get; }
        public bool RequiresCulturalValidation { get; }
        public bool RequiresElderApproval { get; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; }

        public AccessControlProcedure(
            string name,
            AccessControlType type,
            string[] requiredRoles,
            string[]? requiredAttributes = null,
            string[]? culturalRequirements = null,
            Dictionary<string, object>? validationRules = null,
            SecurityLevel minimumSecurityLevel = SecurityLevel.Internal,
            bool requiresCulturalValidation = false,
            bool requiresElderApproval = false)
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            RequiredRoles = requiredRoles ?? Array.Empty<string>();
            RequiredAttributes = requiredAttributes ?? Array.Empty<string>();
            CulturalRequirements = culturalRequirements ?? Array.Empty<string>();
            ValidationRules = validationRules ?? new Dictionary<string, object>();
            MinimumSecurityLevel = minimumSecurityLevel;
            RequiresCulturalValidation = requiresCulturalValidation;
            RequiresElderApproval = requiresElderApproval;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public bool ValidateAccess(string[] userRoles, string[] userAttributes, string[] userCulturalAffiliations)
        {
            // Role validation
            if (RequiredRoles.Length > 0 && !RequiredRoles.Any(role => userRoles.Contains(role)))
                return false;

            // Attribute validation
            if (RequiredAttributes.Length > 0 && !RequiredAttributes.Any(attr => userAttributes.Contains(attr)))
                return false;

            // Cultural validation
            if (CulturalRequirements.Length > 0 && !CulturalRequirements.Any(req => userCulturalAffiliations.Contains(req)))
                return false;

            return true;
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        public bool HasCulturalContext => CulturalRequirements.Length > 0 || RequiresCulturalValidation;
        public bool IsHighSecurity => MinimumSecurityLevel >= SecurityLevel.Confidential || RequiresElderApproval;
    }

    public class DataPrivacyProtection
    {
        public Guid Id { get; }
        public string DataType { get; }
        public ComplianceStandard[] ApplicableStandards { get; }
        public CulturalContentSensitivity CulturalSensitivity { get; }
        public Dictionary<string, object> EncryptionRequirements { get; }
        public Dictionary<string, object> RetentionPolicies { get; }
        public Dictionary<string, object> AccessRestrictions { get; }
        public Dictionary<string, object> CulturalProtections { get; }
        public bool RequiresConsent { get; }
        public bool AllowsCrossRegionTransfer { get; }
        public DateTime CreatedAt { get; }

        public DataPrivacyProtection(
            string dataType,
            ComplianceStandard[] applicableStandards,
            CulturalContentSensitivity culturalSensitivity,
            Dictionary<string, object> encryptionRequirements,
            Dictionary<string, object> retentionPolicies,
            Dictionary<string, object> accessRestrictions,
            Dictionary<string, object>? culturalProtections = null,
            bool requiresConsent = false,
            bool allowsCrossRegionTransfer = true)
        {
            Id = Guid.NewGuid();
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            ApplicableStandards = applicableStandards ?? Array.Empty<ComplianceStandard>();
            CulturalSensitivity = culturalSensitivity;
            EncryptionRequirements = encryptionRequirements ?? new Dictionary<string, object>();
            RetentionPolicies = retentionPolicies ?? new Dictionary<string, object>();
            AccessRestrictions = accessRestrictions ?? new Dictionary<string, object>();
            CulturalProtections = culturalProtections ?? new Dictionary<string, object>();
            RequiresConsent = requiresConsent;
            AllowsCrossRegionTransfer = allowsCrossRegionTransfer;
            CreatedAt = DateTime.UtcNow;
        }

        public bool IsGDPRCompliant => Array.Exists(ApplicableStandards, s => s == ComplianceStandard.GDPR);
        public bool IsCCPACompliant => Array.Exists(ApplicableStandards, s => s == ComplianceStandard.CCPA);
        public bool HasCulturalProtections => CulturalProtections.Count > 0;
        public bool IsSacredContent => CulturalSensitivity >= CulturalContentSensitivity.SpirituallySignificant;
        public bool RequiresSpecialHandling => IsSacredContent || RequiresConsent;
    }

    public class MultiRegionSecurityCoordinator
    {
        public Guid Id { get; }
        public string[] Regions { get; }
        public Dictionary<string, SecurityLevel> RegionalSecurityLevels { get; }
        public Dictionary<string, ComplianceStandard[]> RegionalCompliance { get; }
        public Dictionary<string, object> CrossRegionPolicies { get; }
        public Dictionary<string, object> DataResidencyRules { get; }
        public DateTime LastSynchronized { get; private set; }
        public bool IsActive { get; private set; }

        public MultiRegionSecurityCoordinator(
            string[] regions,
            Dictionary<string, SecurityLevel> regionalSecurityLevels,
            Dictionary<string, ComplianceStandard[]> regionalCompliance,
            Dictionary<string, object>? crossRegionPolicies = null,
            Dictionary<string, object>? dataResidencyRules = null)
        {
            Id = Guid.NewGuid();
            Regions = regions ?? throw new ArgumentNullException(nameof(regions));
            RegionalSecurityLevels = regionalSecurityLevels ?? new Dictionary<string, SecurityLevel>();
            RegionalCompliance = regionalCompliance ?? new Dictionary<string, ComplianceStandard[]>();
            CrossRegionPolicies = crossRegionPolicies ?? new Dictionary<string, object>();
            DataResidencyRules = dataResidencyRules ?? new Dictionary<string, object>();
            LastSynchronized = DateTime.UtcNow;
            IsActive = true;
        }

        public SecurityLevel GetRegionalSecurityLevel(string region)
        {
            return RegionalSecurityLevels.TryGetValue(region, out var level) ? level : SecurityLevel.Internal;
        }

        public ComplianceStandard[] GetRegionalCompliance(string region)
        {
            return RegionalCompliance.TryGetValue(region, out var standards) ? standards : Array.Empty<ComplianceStandard>();
        }

        public bool CanTransferData(string fromRegion, string toRegion, CulturalContentSensitivity sensitivity)
        {
            if (!HasRegion(fromRegion) || !HasRegion(toRegion))
                return false;

            // Sacred content typically stays within cultural regions
            if (sensitivity >= CulturalContentSensitivity.SpirituallySignificant)
                return fromRegion.Equals(toRegion, StringComparison.OrdinalIgnoreCase);

            return CrossRegionPolicies.TryGetValue($"{fromRegion}-{toRegion}", out var policy) && 
                   policy is bool allowed && allowed;
        }

        public void Synchronize() => LastSynchronized = DateTime.UtcNow;
        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        private bool HasRegion(string region) => Array.Exists(Regions, r => r.Equals(region, StringComparison.OrdinalIgnoreCase));
    }
}