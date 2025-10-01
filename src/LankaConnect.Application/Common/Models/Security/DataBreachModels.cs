using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security
{
    public class DataBreachIncident : BaseEntity
    {
        public string IncidentId { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public string AffectedSystems { get; set; } = string.Empty;
        public List<string> AffectedDataTypes { get; set; } = new();
        public int EstimatedRecordsAffected { get; set; }
        public string AttackVector { get; set; } = string.Empty;
        public bool ContainmentAchieved { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class BreachResponseProtocol : BaseEntity
    {
        public string ProtocolName { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public List<string> ResponseSteps { get; set; } = new();
        public List<string> NotificationRequirements { get; set; } = new();
        public TimeSpan MaxResponseTime { get; set; }
        public List<string> StakeholdersToNotify { get; set; } = new();
        public string EscalationCriteria { get; set; } = string.Empty;
        public bool RequiresLegalReview { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class DataBreachResponseResult : BaseEntity
    {
        public string IncidentId { get; set; } = string.Empty;
        public string ResponseId { get; set; } = string.Empty;
        public List<string> ActionsCompleted { get; set; } = new();
        public TimeSpan ResponseTime { get; set; }
        public bool ContainmentSuccessful { get; set; }
        public List<string> NotificationsSent { get; set; } = new();
        public string ImpactAssessment { get; set; } = string.Empty;
        public List<string> LessonsLearned { get; set; } = new();
        public DateTime ResponseCompletedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Resolved";
    }

    public class CulturalFeatureImplementation : BaseEntity
    {
        public string FeatureName { get; set; } = string.Empty;
        public string CulturalContext { get; set; } = string.Empty;
        public string ImplementationApproach { get; set; } = string.Empty;
        public List<string> CulturalConsiderations { get; set; } = new();
        public bool RequiresCommunityReview { get; set; } = true;
        public string ComplianceRequirements { get; set; } = string.Empty;
        public DateTime ImplementedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class PIAValidationCriteria : BaseEntity
    {
        public string CriteriaName { get; set; } = string.Empty;
        public string DataCategory { get; set; } = string.Empty;
        public List<string> ValidationChecks { get; set; } = new();
        public string ComplianceFramework { get; set; } = string.Empty;
        public bool RequiresThirdPartyValidation { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public List<string> MitigationMeasures { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class PrivacyImpactAssessmentResult : BaseEntity
    {
        public string AssessmentId { get; set; } = string.Empty;
        public string FeatureId { get; set; } = string.Empty;
        public decimal PrivacyRiskScore { get; set; }
        public List<string> IdentifiedRisks { get; set; } = new();
        public List<string> MitigationRecommendations { get; set; } = new();
        public bool ApprovalRequired { get; set; }
        public string CulturalCompliance { get; set; } = string.Empty;
        public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Completed";
    }

    public class CulturalEventLoadPattern : BaseEntity
    {
        public string PatternName { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string CulturalContext { get; set; } = string.Empty;
        public decimal PeakLoadMultiplier { get; set; }
        public TimeSpan LoadDuration { get; set; }
        public List<string> AffectedSystems { get; set; } = new();
        public string LoadCharacteristics { get; set; } = string.Empty;
        public bool RequiresPreScaling { get; set; } = true;
        public DateTime IdentifiedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class SecurityPerformanceMonitoring : BaseEntity
    {
        public string MonitoringId { get; set; } = string.Empty;
        public string MonitoredComponent { get; set; } = string.Empty;
        public List<string> SecurityMetrics { get; set; } = new();
        public decimal PerformanceThreshold { get; set; }
        public TimeSpan MonitoringInterval { get; set; }
        public bool AlertingEnabled { get; set; } = true;
        public List<string> EscalationContacts { get; set; } = new();
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Active";
    }
}