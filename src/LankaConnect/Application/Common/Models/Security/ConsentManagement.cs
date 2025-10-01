using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security
{
    public class ConsentRequest : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string DataCategory { get; set; } = string.Empty;
        public string ProcessingPurpose { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public bool IsGranted { get; set; }
        public DateTime? ConsentGrantedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string LegalBasis { get; set; } = string.Empty;
        public string CulturalContext { get; set; } = string.Empty;
        public bool CanWithdraw { get; set; } = true;
    }

    public class CulturalConsentPolicy : BaseEntity
    {
        public string PolicyName { get; set; } = string.Empty;
        public string CulturalGroup { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public List<string> RequiredConsents { get; set; } = new();
        public List<string> CulturalConsiderations { get; set; } = new();
        public bool RequiresExplicitConsent { get; set; } = true;
        public TimeSpan ConsentDuration { get; set; }
        public string RegulatoryFramework { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class ConsentManagementResult : BaseEntity
    {
        public string OperationId { get; set; } = string.Empty;
        public int ConsentRequestsProcessed { get; set; }
        public int ConsentGranted { get; set; }
        public int ConsentDenied { get; set; }
        public List<string> ComplianceViolations { get; set; } = new();
        public decimal ComplianceScore { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string RecommendedActions { get; set; } = string.Empty;
    }
}