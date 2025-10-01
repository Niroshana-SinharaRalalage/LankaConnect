using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Security
{
    /// <summary>
    /// Strategy for security optimization operations
    /// </summary>
    public class SecurityOptimizationStrategy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string StrategyId { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public string OptimizationType { get; set; } = string.Empty;
        public Dictionary<string, object> OptimizationParameters { get; set; } = new();
        public List<string> OptimizationTargets { get; set; } = new();
        public int Priority { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Result of regional security performance optimization
    /// </summary>
    public class RegionalSecurityPerformanceResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ResultId { get; set; } = string.Empty;
        public string RegionId { get; set; } = string.Empty;
        public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
        public SecurityOptimizationStrategy Strategy { get; set; } = new();
        public bool IsOptimized { get; set; }
        public List<string> OptimizationActions { get; set; } = new();
        public double PerformanceImprovement { get; set; }
        public string OptimizationStatus { get; set; } = string.Empty;
        public DateTimeOffset OptimizedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}