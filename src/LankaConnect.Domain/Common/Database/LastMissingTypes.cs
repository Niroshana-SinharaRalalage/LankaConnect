using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public class SystemStatusReportConfiguration
    {
        public Guid Id { get; set; }
        public List<string> ReportComponents { get; set; } = new();
        public string ReportFormat { get; set; } = string.Empty;
        public TimeSpan ReportingInterval { get; set; }
        public Dictionary<string, object> ReportParameters { get; set; } = new();
        public bool IsEnabled { get; set; }
    }

    public class SystemStatusReport
    {
        public Guid Id { get; set; }
        public Dictionary<string, string> ComponentStatuses { get; set; } = new();
        public string OverallSystemStatus { get; set; } = string.Empty;
        public List<string> Issues { get; set; } = new();
        public List<string> Alerts { get; set; } = new();
        public DateTime ReportTimestamp { get; set; }
    }

    public class ResourceMonitoringConfiguration
    {
        public Guid Id { get; set; }
        public List<string> MonitoredResources { get; set; } = new();
        public Dictionary<string, double> ResourceThresholds { get; set; } = new();
        public TimeSpan MonitoringInterval { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class ResourceUtilizationMetrics
    {
        public Guid Id { get; set; }
        public Dictionary<string, double> ResourceUtilization { get; set; } = new();
        public List<string> ResourceBottlenecks { get; set; } = new();
        public DateTime MetricsTimestamp { get; set; }
    }

    public class AutoScalingEngineConfiguration
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> ScalingRules { get; set; } = new();
        public List<string> ScalingTriggers { get; set; } = new();
        public TimeSpan ScalingCooldown { get; set; }
        public int MinInstances { get; set; }
        public int MaxInstances { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class ConfigurationResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public string ConfigurationStatus { get; set; } = string.Empty;
        public List<string> AppliedSettings { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ConfigurationTimestamp { get; set; }
    }

    public class CulturalScalingPolicy
    {
        public Guid Id { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public string CulturalContext { get; set; } = string.Empty;
        public Dictionary<string, object> ScalingRules { get; set; } = new();
        public List<string> ApplicableEvents { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PolicyUpdateResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public string UpdateStatus { get; set; } = string.Empty;
        public List<string> UpdatedPolicies { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime UpdateTimestamp { get; set; }
    }
}