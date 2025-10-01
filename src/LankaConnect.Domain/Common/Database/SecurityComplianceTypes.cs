using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public class CulturalComplianceRequirements
    {
        public Guid Id { get; set; }
        public string CulturalRegion { get; set; } = string.Empty;
        public List<string> ComplianceStandards { get; set; } = new();
        public Dictionary<string, object> SpecificRequirements { get; set; } = new();
        public List<string> RestrictedContentTypes { get; set; } = new();
        public List<string> MandatoryProtections { get; set; } = new();
        public DateTime EffectiveDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class RegionalComplianceResult
    {
        public Guid Id { get; set; }
        public string Region { get; set; } = string.Empty;
        public bool IsCompliant { get; set; }
        public List<string> PassedRequirements { get; set; } = new();
        public List<string> FailedRequirements { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        public DateTime AssessmentDate { get; set; }
        public string ComplianceScore { get; set; } = string.Empty;
    }

    public class ISO27001ValidationCriteria
    {
        public Guid Id { get; set; }
        public string ControlCategory { get; set; } = string.Empty;
        public List<string> ValidationPoints { get; set; } = new();
        public Dictionary<string, object> AssessmentCriteria { get; set; } = new();
        public string ComplianceLevel { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public bool IsActive { get; set; }
    }

    public class SecurityManagementScope
    {
        public Guid Id { get; set; }
        public string ScopeName { get; set; } = string.Empty;
        public List<string> IncludedSystems { get; set; } = new();
        public List<string> IncludedProcesses { get; set; } = new();
        public List<string> SecurityControls { get; set; } = new();
        public Dictionary<string, object> ScopeParameters { get; set; } = new();
        public DateTime DefinedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class ISO27001ComplianceResult
    {
        public Guid Id { get; set; }
        public string AssessmentId { get; set; } = string.Empty;
        public bool IsCompliant { get; set; }
        public Dictionary<string, bool> ControlCompliance { get; set; } = new();
        public List<string> NonCompliantAreas { get; set; } = new();
        public List<string> ImprovementRecommendations { get; set; } = new();
        public string OverallComplianceScore { get; set; } = string.Empty;
        public DateTime AssessmentDate { get; set; }
        public DateTime NextAssessmentDue { get; set; }
    }

    public class AuditScope
    {
        public Guid Id { get; set; }
        public string AuditName { get; set; } = string.Empty;
        public List<string> AuditAreas { get; set; } = new();
        public Dictionary<string, object> AuditCriteria { get; set; } = new();
        public DateTime AuditPeriodStart { get; set; }
        public DateTime AuditPeriodEnd { get; set; }
        public List<string> Auditors { get; set; } = new();
        public bool IsActive { get; set; }
    }

    public class ComplianceAuditReport
    {
        public Guid Id { get; set; }
        public string AuditId { get; set; } = string.Empty;
        public string ReportTitle { get; set; } = string.Empty;
        public Dictionary<string, string> Findings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public string OverallAssessment { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string AuditorName { get; set; } = string.Empty;
        public bool RequiresFollowUp { get; set; }
    }
}