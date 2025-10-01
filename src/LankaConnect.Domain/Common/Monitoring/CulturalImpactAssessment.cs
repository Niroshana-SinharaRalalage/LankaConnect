using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Monitoring
{
    /// <summary>
    /// Assessment of cultural impact for monitoring purposes
    /// </summary>
    public class CulturalImpactAssessment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string AssessmentId { get; set; } = string.Empty;
        public string CulturalContext { get; set; } = string.Empty;
        public string ImpactLevel { get; set; } = string.Empty;
        public List<string> AffectedCommunities { get; set; } = new();
        public Dictionary<string, object> ImpactMetrics { get; set; } = new();
        public List<string> MitigationActions { get; set; } = new();
        public bool IsSignificant { get; set; }
        public string AssessmentNotes { get; set; } = string.Empty;
        public DateTimeOffset AssessedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}