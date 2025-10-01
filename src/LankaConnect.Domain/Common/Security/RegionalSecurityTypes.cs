using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Security
{
    /// <summary>
    /// Implementation details for regional security measures
    /// </summary>
    public class RegionalSecurityImplementation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ImplementationId { get; set; } = string.Empty;
        public string RegionId { get; set; } = string.Empty;
        public string SecurityLevel { get; set; } = string.Empty;
        public Dictionary<string, object> SecurityPolicies { get; set; } = new();
        public List<string> ComplianceFrameworks { get; set; } = new();
        public Dictionary<string, string> EncryptionSettings { get; set; } = new();
        public List<string> AccessControls { get; set; } = new();
        public bool IsImplemented { get; set; }
        public string ImplementationStatus { get; set; } = string.Empty;
        public DateTimeOffset ImplementedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Result of data sovereignty security implementation
    /// </summary>
    public class DataSovereigntySecurityResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ResultId { get; set; } = string.Empty;
        public string DataSovereigntyRequirements { get; set; } = string.Empty;
        public RegionalSecurityImplementation Implementation { get; set; } = new();
        public bool IsCompliant { get; set; }
        public List<string> ComplianceViolations { get; set; } = new();
        public Dictionary<string, object> SecurityMetrics { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        public string ComplianceStatus { get; set; } = string.Empty;
        public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}