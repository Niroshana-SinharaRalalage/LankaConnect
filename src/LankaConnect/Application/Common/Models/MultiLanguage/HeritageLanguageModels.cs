using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.MultiLanguage
{
    public class HeritageLanguagePreservationRequest : BaseEntity
    {
        public string LanguageName { get; set; } = string.Empty;
        public string CommunityId { get; set; } = string.Empty;
        public string PreservationGoal { get; set; } = string.Empty;
        public List<string> ContentTypes { get; set; } = new();
        public string CulturalSignificance { get; set; } = string.Empty;
        public int EstimatedSpeakers { get; set; }
        public string ThreatLevel { get; set; } = string.Empty;
        public List<string> PreservationMethods { get; set; } = new();
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string RequestedBy { get; set; } = string.Empty;
    }

    public class HeritageLanguagePreservationResult : BaseEntity
    {
        public string RequestId { get; set; } = string.Empty;
        public string PreservationPlan { get; set; } = string.Empty;
        public List<string> RecommendedActions { get; set; } = new();
        public decimal FundingRequired { get; set; }
        public TimeSpan EstimatedTimeframe { get; set; }
        public List<string> StakeholdersIdentified { get; set; } = new();
        public string PriorityLevel { get; set; } = string.Empty;
        public bool CommunityApproval { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";
    }

    public class IntergenerationalContentRequest : BaseEntity
    {
        public string ContentType { get; set; } = string.Empty;
        public string SourceLanguage { get; set; } = string.Empty;
        public string TargetAgeGroup { get; set; } = string.Empty;
        public string CulturalContext { get; set; } = string.Empty;
        public List<string> LearningObjectives { get; set; } = new();
        public string AdaptationLevel { get; set; } = string.Empty;
        public bool RequiresElderReview { get; set; } = true;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string RequestedBy { get; set; } = string.Empty;
    }

    public class IntergenerationalContentResult : BaseEntity
    {
        public string RequestId { get; set; } = string.Empty;
        public string AdaptedContent { get; set; } = string.Empty;
        public List<string> CulturalNotes { get; set; } = new();
        public string AgeAppropriatenessScore { get; set; } = string.Empty;
        public List<string> ElderFeedback { get; set; } = new();
        public bool RequiresRevision { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string QualityScore { get; set; } = string.Empty;
        public string Status { get; set; } = "Completed";
    }

    public class LanguageRevenueAnalysisRequest : BaseEntity
    {
        public string LanguagePair { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public DateTime AnalysisPeriodStart { get; set; }
        public DateTime AnalysisPeriodEnd { get; set; }
        public List<string> MetricsToAnalyze { get; set; } = new();
        public string Region { get; set; } = string.Empty;
        public bool IncludeCommunityImpact { get; set; } = true;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string RequestedBy { get; set; } = string.Empty;
    }
}