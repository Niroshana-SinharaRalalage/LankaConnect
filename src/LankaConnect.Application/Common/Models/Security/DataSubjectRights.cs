using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security
{
    public class DataSubjectRequest : BaseEntity
    {
        public string RequestId { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty; // Access, Rectification, Erasure, Portability
        public string Description { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";
        public string LegalBasis { get; set; } = string.Empty;
        public DateTime? ResponseDeadline { get; set; }
        public string CulturalContext { get; set; } = string.Empty;
        public bool IsUrgent { get; set; }
        public string ContactPreference { get; set; } = string.Empty;
    }

    public class RightsFulfillmentProcess : BaseEntity
    {
        public string ProcessId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public List<string> ProcessingSteps { get; set; } = new();
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public List<string> DataSourcesAccessed { get; set; } = new();
        public string ResultSummary { get; set; } = string.Empty;
        public bool RequiresManualReview { get; set; }
        public decimal ComplianceScore { get; set; }
    }
}