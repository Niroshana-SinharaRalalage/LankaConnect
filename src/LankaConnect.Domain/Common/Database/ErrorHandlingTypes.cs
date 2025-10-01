using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public class ErrorHandlingConfiguration
    {
        public Guid Id { get; set; }
        public string ErrorHandlingStrategy { get; set; } = string.Empty;
        public int MaxRetryAttempts { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public List<string> HandledExceptionTypes { get; set; } = new();
        public Dictionary<string, object> CustomParameters { get; set; } = new();
        public bool EnableCircuitBreaker { get; set; }
    }

    public class ErrorHandlingResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public int AttemptsUsed { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ErrorTimestamp { get; set; }
        public string ResolutionAction { get; set; } = string.Empty;
        public bool RequiresManualIntervention { get; set; }
    }

    public class FallbackMechanismConfiguration
    {
        public Guid Id { get; set; }
        public string FallbackStrategy { get; set; } = string.Empty;
        public List<string> FallbackOptions { get; set; } = new();
        public Dictionary<string, object> FallbackParameters { get; set; } = new();
        public TimeSpan ActivationThreshold { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class FallbackMechanismResult
    {
        public Guid Id { get; set; }
        public bool FallbackActivated { get; set; }
        public string ActivatedFallback { get; set; } = string.Empty;
        public string ActivationReason { get; set; } = string.Empty;
        public DateTime ActivationTimestamp { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class CircuitBreakerConfiguration
    {
        public Guid Id { get; set; }
        public int FailureThreshold { get; set; }
        public TimeSpan OpenCircuitDuration { get; set; }
        public TimeSpan HalfOpenTimeout { get; set; }
        public List<string> MonitoredOperations { get; set; } = new();
        public bool IsEnabled { get; set; }
    }

    public class CircuitBreakerResult
    {
        public Guid Id { get; set; }
        public string CircuitState { get; set; } = string.Empty; // Open, Closed, HalfOpen
        public int CurrentFailureCount { get; set; }
        public DateTime LastFailureTimestamp { get; set; }
        public DateTime LastSuccessTimestamp { get; set; }
        public bool OperationAllowed { get; set; }
        public string StateChangeReason { get; set; } = string.Empty;
    }

    public class GracefulDegradationConfiguration
    {
        public Guid Id { get; set; }
        public Dictionary<string, string> DegradationLevels { get; set; } = new();
        public List<string> EssentialFeatures { get; set; } = new();
        public List<string> OptionalFeatures { get; set; } = new();
        public Dictionary<string, object> DegradationRules { get; set; } = new();
        public bool IsEnabled { get; set; }
    }

    public class GracefulDegradationResult
    {
        public Guid Id { get; set; }
        public string CurrentDegradationLevel { get; set; } = string.Empty;
        public List<string> DisabledFeatures { get; set; } = new();
        public List<string> ActiveFeatures { get; set; } = new();
        public string DegradationReason { get; set; } = string.Empty;
        public DateTime DegradationTimestamp { get; set; }
        public bool RequiresRecovery { get; set; }
    }
}