using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public class RetryMechanismConfiguration
    {
        public Guid Id { get; set; }
        public int MaxRetryAttempts { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public List<string> RetryableExceptions { get; set; } = new();
        public string BackoffStrategy { get; set; } = string.Empty; // Linear, Exponential, Fixed
        public bool IsEnabled { get; set; }
    }

    public class RetryMechanismResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public int AttemptsUsed { get; set; }
        public TimeSpan TotalRetryDuration { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public DateTime CompletionTimestamp { get; set; }
    }

    public class HealthCheckConfiguration
    {
        public Guid Id { get; set; }
        public string HealthCheckName { get; set; } = string.Empty;
        public TimeSpan CheckInterval { get; set; }
        public TimeSpan Timeout { get; set; }
        public List<string> HealthCheckEndpoints { get; set; } = new();
        public Dictionary<string, object> HealthCheckParameters { get; set; } = new();
        public bool IsEnabled { get; set; }
    }

    public class TimeoutHandlingConfiguration
    {
        public Guid Id { get; set; }
        public TimeSpan DefaultTimeout { get; set; }
        public Dictionary<string, TimeSpan> OperationTimeouts { get; set; } = new();
        public string TimeoutStrategy { get; set; } = string.Empty; // Cancel, Retry, Fallback
        public bool EnableGracefulTimeout { get; set; }
    }

    public class TimeoutHandlingResult
    {
        public Guid Id { get; set; }
        public bool TimedOut { get; set; }
        public TimeSpan ActualDuration { get; set; }
        public string TimeoutAction { get; set; } = string.Empty;
        public bool IsRecoverable { get; set; }
        public DateTime TimeoutTimestamp { get; set; }
    }

    public class RollbackConfiguration
    {
        public Guid Id { get; set; }
        public string RollbackStrategy { get; set; } = string.Empty;
        public List<string> RollbackTriggers { get; set; } = new();
        public Dictionary<string, object> RollbackParameters { get; set; } = new();
        public TimeSpan RollbackTimeout { get; set; }
        public bool AutoRollbackEnabled { get; set; }
    }

    public class RollbackResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public List<string> RolledBackOperations { get; set; } = new();
        public string RollbackReason { get; set; } = string.Empty;
        public TimeSpan RollbackDuration { get; set; }
        public DateTime RollbackTimestamp { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}