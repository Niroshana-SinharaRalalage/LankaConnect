using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security
{
    public class DataSubjectRightsResult : BaseEntity
    {
        public string RequestId { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = "Processed";
        public List<string> ActionsCompleted { get; set; } = new();
        public string ResultSummary { get; set; } = string.Empty;
        public bool RequiresFollowUp { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string ProcessedBy { get; set; } = string.Empty;
        public string ComplianceVerification { get; set; } = string.Empty;
    }

    public class AnalyticsConfiguration : BaseEntity
    {
        public string ConfigurationName { get; set; } = string.Empty;
        public List<string> MetricsToCollect { get; set; } = new();
        public string DataRetentionPeriod { get; set; } = string.Empty;
        public bool EnableRealTimeProcessing { get; set; } = true;
        public string PrivacyLevel { get; set; } = string.Empty;
        public List<string> AllowedDataSources { get; set; } = new();
        public bool RequireConsent { get; set; } = true;
        public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
    }

    public class PrivacyPreservationTechniques : BaseEntity
    {
        public string TechniqueName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImplementationMethod { get; set; } = string.Empty;
        public decimal PrivacyLevel { get; set; }
        public List<string> ApplicableDataTypes { get; set; } = new();
        public bool IsReversible { get; set; }
        public string ComplianceFramework { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class PrivacyPreservingAnalyticsResult : BaseEntity
    {
        public string AnalysisId { get; set; } = string.Empty;
        public string DataSetId { get; set; } = string.Empty;
        public List<string> TechniquesApplied { get; set; } = new();
        public decimal PrivacyScore { get; set; }
        public string AnalyticsResults { get; set; } = string.Empty;
        public bool MaintainsUtility { get; set; } = true;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string QualityMetrics { get; set; } = string.Empty;
    }
}