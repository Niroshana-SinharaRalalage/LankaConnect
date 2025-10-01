using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LankaConnect.Infrastructure.Monitoring
{
    /// <summary>
    /// Security metrics collector for monitoring and analytics
    /// </summary>
    public interface ISecurityMetricsCollector
    {
        Task<SecurityMetrics> CollectSecurityOptimizationMetricsAsync(string culturalIdentifier, DateTime optimizationStart, CancellationToken cancellationToken);
        Task<PerformanceMetrics> CollectPerformanceMetricsAsync(string operationId, CancellationToken cancellationToken);
        Task<ComplianceMetrics> CollectComplianceMetricsAsync(string frameworkId, CancellationToken cancellationToken);
    }

    public record SecurityMetrics(
        string MetricsId,
        DateTime Timestamp,
        Dictionary<string, object> Metrics,
        TimeSpan CollectionDuration);

    public record PerformanceMetrics(
        string MetricsId,
        DateTime Timestamp,
        double LatencyMs,
        double ThroughputOps,
        double CpuUsagePercent,
        double MemoryUsageMb);

    public record ComplianceMetrics(
        string MetricsId,
        DateTime Timestamp,
        double ComplianceScore,
        int ViolationCount,
        int ControlsImplemented,
        int ControlsTotal);
}