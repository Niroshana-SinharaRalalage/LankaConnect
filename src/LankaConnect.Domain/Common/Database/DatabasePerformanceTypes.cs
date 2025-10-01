using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public class DiasporaActivityMetrics
    {
        public Guid Id { get; set; }
        public string Region { get; set; } = string.Empty;
        public int ActiveUsers { get; set; }
        public int TotalConnections { get; set; }
        public double AverageSessionDuration { get; set; }
        public Dictionary<string, int> ActivityBreakdown { get; set; } = new();
        public DateTime MetricsTimestamp { get; set; }
        public List<string> PopularFeatures { get; set; } = new();
    }

    public class CulturalContentType
    {
        public Guid Id { get; set; }
        public string ContentTypeName { get; set; } = string.Empty;
        public string CulturalContext { get; set; } = string.Empty;
        public List<string> AssociatedTraditions { get; set; } = new();
        public string ContentCategory { get; set; } = string.Empty;
        public bool RequiresSpecialHandling { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ContentEngagementPerformanceMetrics
    {
        public Guid Id { get; set; }
        public Guid ContentId { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int ShareCount { get; set; }
        public int CommentCount { get; set; }
        public double EngagementRate { get; set; }
        public TimeSpan AverageViewTime { get; set; }
        public DateTime MetricsDate { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }

    public class ValidationScope
    {
        public Guid Id { get; set; }
        public string ScopeName { get; set; } = string.Empty;
        public List<string> IncludedComponents { get; set; } = new();
        public List<string> ExcludedComponents { get; set; } = new();
        public Dictionary<string, object> ValidationCriteria { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class PerformanceAnomaly
    {
        public Guid Id { get; set; }
        public string AnomalyType { get; set; } = string.Empty;
        public string AffectedComponent { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public Dictionary<string, object> AnomalyData { get; set; } = new();
        public bool IsResolved { get; set; }
        public string ResolutionAction { get; set; } = string.Empty;
    }
}