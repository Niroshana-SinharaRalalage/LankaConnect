using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Configuration
{
    /// <summary>
    /// Configuration for system shutdown operations
    /// </summary>
    public class ShutdownConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ConfigurationId { get; set; } = string.Empty;
        public TimeSpan GracePeriod { get; set; } = TimeSpan.FromMinutes(5);
        public bool ForceShutdown { get; set; } = false;
        public List<string> ShutdownOrder { get; set; } = new();
        public Dictionary<string, object> ShutdownParameters { get; set; } = new();
        public bool WaitForConnections { get; set; } = true;
        public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromMinutes(10);
        public bool IsEnabled { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Configuration for health validation operations
    /// </summary>
    public class HealthValidationConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ConfigurationId { get; set; } = string.Empty;
        public List<string> ValidationChecks { get; set; } = new();
        public TimeSpan ValidationInterval { get; set; } = TimeSpan.FromMinutes(1);
        public Dictionary<string, object> ValidationParameters { get; set; } = new();
        public List<string> CriticalChecks { get; set; } = new();
        public bool FailOnAnyFailure { get; set; } = false;
        public int MaxRetries { get; set; } = 3;
        public bool IsEnabled { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}