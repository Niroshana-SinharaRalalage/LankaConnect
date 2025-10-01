using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Privacy
{
    /// <summary>
    /// Policy for privacy protection operations
    /// </summary>
    public class PrivacyProtectionPolicy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string PolicyId { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public string ProtectionLevel { get; set; } = string.Empty;
        public List<string> ProtectionMethods { get; set; } = new();
        public Dictionary<string, object> PolicyParameters { get; set; } = new();
        public List<string> ApplicableDataTypes { get; set; } = new();
        public bool IsEnforced { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Identifier for personal data elements
    /// </summary>
    public class PersonalDataIdentifier
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string IdentifierId { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string DataCategory { get; set; } = string.Empty;
        public string SensitivityLevel { get; set; } = string.Empty;
        public List<string> DataElements { get; set; } = new();
        public Dictionary<string, object> IdentificationCriteria { get; set; } = new();
        public bool RequiresSpecialHandling { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Strategy for data anonymization operations
    /// </summary>
    public class AnonymizationStrategy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string StrategyId { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public string AnonymizationMethod { get; set; } = string.Empty;
        public List<string> AnonymizationTechniques { get; set; } = new();
        public Dictionary<string, object> StrategyParameters { get; set; } = new();
        public double AnonymizationLevel { get; set; }
        public bool PreservesUtility { get; set; } = true;
        public bool IsReversible { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}