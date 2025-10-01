using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Performance
{
    /// <summary>
    /// Configuration for Application Performance Monitoring integration
    /// </summary>
    public class APMConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ConfigurationId { get; set; } = string.Empty;
        public string APMProvider { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public Dictionary<string, object> ProviderSettings { get; set; } = new();
        public List<string> MonitoredServices { get; set; } = new();
        public TimeSpan SamplingInterval { get; set; }
        public bool IsEnabled { get; set; } = true;
        public Dictionary<string, string> Tags { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Status of APM integration
    /// </summary>
    public class APMIntegrationStatus
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string StatusId { get; set; } = string.Empty;
        public APMConfiguration Configuration { get; set; } = new();
        public bool IsIntegrated { get; set; }
        public string IntegrationStatus { get; set; } = string.Empty;
        public List<string> AvailableMetrics { get; set; } = new();
        public Dictionary<string, object> HealthChecks { get; set; } = new();
        public DateTime LastSyncTime { get; set; } = DateTime.UtcNow;
        public List<string> IntegrationIssues { get; set; } = new();
        public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Configuration for tracing operations
    /// </summary>
    public class TracingConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ConfigurationId { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public double SamplingRate { get; set; } = 0.1;
        public List<string> TracedOperations { get; set; } = new();
        public Dictionary<string, object> TracingParameters { get; set; } = new();
        public TimeSpan MaxTraceLength { get; set; }
        public bool IncludeHeaders { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}