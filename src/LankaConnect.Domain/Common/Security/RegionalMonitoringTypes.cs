using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Security
{
    /// <summary>
    /// Configuration for regional monitoring operations
    /// </summary>
    public class RegionalMonitoringConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ConfigurationId { get; set; } = string.Empty;
        public List<string> MonitoredRegions { get; set; } = new();
        public Dictionary<string, object> MonitoringParameters { get; set; } = new();
        public TimeSpan MonitoringInterval { get; set; }
        public List<string> MonitoredMetrics { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cross-region alerting system configuration
    /// </summary>
    public class CrossRegionAlertingSystem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SystemId { get; set; } = string.Empty;
        public List<string> AlertingRegions { get; set; } = new();
        public Dictionary<string, object> AlertingRules { get; set; } = new();
        public List<string> NotificationChannels { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Result of regional security monitoring
    /// </summary>
    public class RegionalSecurityMonitoringResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ResultId { get; set; } = string.Empty;
        public RegionalMonitoringConfiguration Configuration { get; set; } = new();
        public CrossRegionAlertingSystem AlertingSystem { get; set; } = new();
        public Dictionary<string, object> MonitoringResults { get; set; } = new();
        public List<string> DetectedIssues { get; set; } = new();
        public bool IsHealthy { get; set; } = true;
        public DateTimeOffset MonitoredAt { get; set; } = DateTimeOffset.UtcNow;
    }
}