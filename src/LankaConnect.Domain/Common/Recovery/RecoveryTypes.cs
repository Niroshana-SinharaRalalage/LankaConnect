using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Recovery;

/// <summary>
/// Comprehensive recovery plan for disaster recovery scenarios
/// Foundational type for business continuity and disaster recovery
/// </summary>
public sealed record RecoveryPlan
{
    public string PlanId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public RecoveryPriority Priority { get; init; }
    public TimeSpan EstimatedRecoveryTime { get; init; }
    public TimeSpan RecoveryPointObjective { get; init; }
    public IReadOnlyList<RecoveryStep> Steps { get; init; } = Array.Empty<RecoveryStep>();
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();
    public bool RequiresManualActivation { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string CreatedBy { get; init; } = string.Empty;
    public RecoveryScope Scope { get; init; }
    
    public static RecoveryPlan Create(string name, RecoveryPriority priority, RecoveryScope scope, IEnumerable<RecoveryStep> steps)
    {
        return new RecoveryPlan
        {
            PlanId = Guid.NewGuid().ToString(),
            Name = name,
            Priority = priority,
            Scope = scope,
            Steps = steps.ToList().AsReadOnly(),
            EstimatedRecoveryTime = CalculateEstimatedTime(steps),
            RequiresManualActivation = priority >= RecoveryPriority.Critical
        };
    }
    
    private static TimeSpan CalculateEstimatedTime(IEnumerable<RecoveryStep> steps)
    {
        return steps.Aggregate(TimeSpan.Zero, (total, step) => total.Add(step.EstimatedDuration));
    }
}

/// <summary>
/// Result of recovery plan execution
/// </summary>
public sealed record RecoveryResult
{
    public string PlanId { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; init; }
    public IReadOnlyList<RecoveryStepResult> StepResults { get; init; } = Array.Empty<RecoveryStepResult>();
    public string? ErrorMessage { get; init; }
    public TimeSpan ActualRecoveryTime { get; init; }
    public RecoveryStatus Status { get; init; }
    public double SuccessPercentage { get; init; }
    
    public static RecoveryResult InProgress(string planId, DateTime startedAt)
    {
        return new RecoveryResult
        {
            PlanId = planId,
            StartedAt = startedAt,
            Status = RecoveryStatus.InProgress
        };
    }
    
    public static RecoveryResult Success(string planId, DateTime startedAt, IEnumerable<RecoveryStepResult> results)
    {
        var completedAt = DateTime.UtcNow;
        var stepResults = results.ToList();
        var successCount = stepResults.Count(r => r.Success);
        
        return new RecoveryResult
        {
            PlanId = planId,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            StepResults = stepResults.AsReadOnly(),
            ActualRecoveryTime = completedAt - startedAt,
            Status = RecoveryStatus.Completed,
            SuccessPercentage = stepResults.Count > 0 ? (double)successCount / stepResults.Count * 100 : 0
        };
    }
    
    public static RecoveryResult Failure(string planId, DateTime startedAt, string error, IEnumerable<RecoveryStepResult> results)
    {
        return new RecoveryResult
        {
            PlanId = planId,
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow,
            StepResults = results.ToList().AsReadOnly(),
            ErrorMessage = error,
            Status = RecoveryStatus.Failed,
            ActualRecoveryTime = DateTime.UtcNow - startedAt
        };
    }
}

/// <summary>
/// Individual step in recovery plan
/// </summary>
public sealed record RecoveryStep
{
    public string StepId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TimeSpan EstimatedDuration { get; init; }
    public int OrderIndex { get; init; }
    public bool IsParallel { get; init; }
    public IReadOnlyList<string> Prerequisites { get; init; } = Array.Empty<string>();
    public RecoveryStepType Type { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Result of individual recovery step execution
/// </summary>
public sealed record RecoveryStepResult
{
    public string StepId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ActualDuration { get; init; }
    public Dictionary<string, object> ResultData { get; init; } = new();
}

/// <summary>
/// Recovery plan priority levels
/// </summary>
public enum RecoveryPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Recovery plan scope
/// </summary>
public enum RecoveryScope
{
    Service,
    Database,
    Infrastructure,
    Application,
    FullSystem
}

/// <summary>
/// Recovery execution status
/// </summary>
public enum RecoveryStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted
}

/// <summary>
/// Recovery step types
/// </summary>
public enum RecoveryStepType
{
    Backup,
    Restore,
    Verification,
    Configuration,
    Notification,
    Cleanup
}