using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public class CrossRegionFailoverContext
    {
        public Guid Id { get; set; }
        public string PrimaryRegion { get; set; } = string.Empty;
        public string FailoverRegion { get; set; } = string.Empty;
        public string FailoverTrigger { get; set; } = string.Empty;
        public DateTime FailoverTimestamp { get; set; }
        public Dictionary<string, object> ContextData { get; set; } = new();
        public List<string> AffectedServices { get; set; } = new();
        public string Priority { get; set; } = string.Empty;
    }

    public class CrossRegionFailoverResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public string FailoverStatus { get; set; } = string.Empty;
        public TimeSpan FailoverDuration { get; set; }
        public List<string> CompletedActions { get; set; } = new();
        public List<string> FailedActions { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CompletionTimestamp { get; set; }
        public Dictionary<string, object> FailoverMetrics { get; set; } = new();
    }

    public class ConflictCacheOptimizationRequest
    {
        public Guid Id { get; set; }
        public string CacheRegion { get; set; } = string.Empty;
        public List<string> OptimizationTargets { get; set; } = new();
        public Dictionary<string, object> OptimizationParameters { get; set; } = new();
        public string OptimizationStrategy { get; set; } = string.Empty;
        public DateTime RequestTimestamp { get; set; }
        public bool IsUrgent { get; set; }
    }

    public class ConflictCacheOptimizationResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public Dictionary<string, string> OptimizationResults { get; set; } = new();
        public double PerformanceImprovement { get; set; }
        public List<string> OptimizedComponents { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime OptimizationTimestamp { get; set; }
        public TimeSpan OptimizationDuration { get; set; }
    }

    public class ConflictCachePreWarmingResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public int PreWarmedEntries { get; set; }
        public List<string> PreWarmedCaches { get; set; } = new();
        public TimeSpan PreWarmingDuration { get; set; }
        public Dictionary<string, object> PreWarmingMetrics { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CompletionTimestamp { get; set; }
    }

    public class ConflictStateManagementRequest
    {
        public Guid Id { get; set; }
        public string ConflictId { get; set; } = string.Empty;
        public string StateOperation { get; set; } = string.Empty; // Create, Update, Delete, Sync
        public Dictionary<string, object> StateData { get; set; } = new();
        public string Priority { get; set; } = string.Empty;
        public DateTime RequestTimestamp { get; set; }
        public List<string> AffectedRegions { get; set; } = new();
    }

    public class ConflictStateManagementResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public string FinalState { get; set; } = string.Empty;
        public List<string> AppliedOperations { get; set; } = new();
        public Dictionary<string, object> StateMetrics { get; set; } = new();
        public List<string> SynchronizedRegions { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CompletionTimestamp { get; set; }
    }
}