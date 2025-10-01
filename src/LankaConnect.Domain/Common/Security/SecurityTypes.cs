using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Common.Security;

/// <summary>
/// Value object representing quarantine policy for database security incidents
/// Foundational type for security threat containment
/// </summary>
public sealed record QuarantinePolicy
{
    public string PolicyId { get; init; } = string.Empty;
    public QuarantineLevel Level { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<string> AffectedResources { get; init; } = Array.Empty<string>();
    public bool AutomatedResponse { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string Reason { get; init; } = string.Empty;
    
    public static QuarantinePolicy Create(QuarantineLevel level, TimeSpan duration, IEnumerable<string> resources, string reason)
    {
        return new QuarantinePolicy
        {
            PolicyId = Guid.NewGuid().ToString(),
            Level = level,
            Duration = duration,
            AffectedResources = resources.ToList().AsReadOnly(),
            Reason = reason,
            AutomatedResponse = level >= QuarantineLevel.High
        };
    }
}

/// <summary>
/// Result of quarantine operation execution
/// </summary>
public sealed record QuarantineResult
{
    public string PolicyId { get; init; } = string.Empty;
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<string> ProcessedResources { get; init; } = Array.Empty<string>();
    public string? ErrorMessage { get; init; }
    public TimeSpan ExecutionDuration { get; init; }
    
    public static QuarantineResult Success(string policyId, IEnumerable<string> resources, TimeSpan duration)
    {
        return new QuarantineResult
        {
            PolicyId = policyId,
            ProcessedResources = resources.ToList().AsReadOnly(),
            ExecutionDuration = duration
        };
    }
    
    public static QuarantineResult Failure(string policyId, string error)
    {
        return new QuarantineResult
        {
            PolicyId = policyId,
            ErrorMessage = error
        };
    }
}

/// <summary>
/// Strategy for containing security threats and incidents
/// </summary>
public sealed record ContainmentStrategy
{
    public string StrategyId { get; init; } = string.Empty;
    public ContainmentLevel Level { get; init; }
    public IReadOnlyList<ContainmentAction> Actions { get; init; } = Array.Empty<ContainmentAction>();
    public TimeSpan MaxExecutionTime { get; init; }
    public bool RequiresManualApproval { get; init; }
    public int Priority { get; init; }
    
    public static ContainmentStrategy Create(ContainmentLevel level, IEnumerable<ContainmentAction> actions)
    {
        return new ContainmentStrategy
        {
            StrategyId = Guid.NewGuid().ToString(),
            Level = level,
            Actions = actions.ToList().AsReadOnly(),
            MaxExecutionTime = TimeSpan.FromMinutes(level switch
            {
                ContainmentLevel.Low => 30,
                ContainmentLevel.Medium => 15,
                ContainmentLevel.High => 5,
                ContainmentLevel.Critical => 1,
                _ => 30
            }),
            RequiresManualApproval = level >= ContainmentLevel.High,
            Priority = (int)level
        };
    }
}

/// <summary>
/// Result of containment strategy execution
/// </summary>
public sealed record ContainmentResult
{
    public string StrategyId { get; init; } = string.Empty;
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<ContainmentActionResult> ActionResults { get; init; } = Array.Empty<ContainmentActionResult>();
    public string? ErrorMessage { get; init; }
    public TimeSpan TotalExecutionTime { get; init; }
    public bool RequiredManualIntervention { get; init; }
    
    public static ContainmentResult Success(string strategyId, IEnumerable<ContainmentActionResult> results, TimeSpan duration)
    {
        return new ContainmentResult
        {
            StrategyId = strategyId,
            ActionResults = results.ToList().AsReadOnly(),
            TotalExecutionTime = duration
        };
    }
}

/// <summary>
/// Enumeration for quarantine severity levels
/// </summary>
public enum QuarantineLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Enumeration for containment severity levels
/// </summary>
public enum ContainmentLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Containment action to be executed
/// </summary>
public sealed record ContainmentAction
{
    public string ActionId { get; init; } = string.Empty;
    public string ActionType { get; init; } = string.Empty;
    public Dictionary<string, object> Parameters { get; init; } = new();
    public int OrderIndex { get; init; }
    public TimeSpan Timeout { get; init; }
}

/// <summary>
/// Result of individual containment action
/// </summary>
public sealed record ContainmentActionResult
{
    public string ActionId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public Dictionary<string, object> ResultData { get; init; } = new();
}