using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Database;

namespace LankaConnect.Domain.CulturalIntelligence.ValueObjects;

public record PriorityRecoveryPlan(
    string PlanId,
    string EventId,
    IEnumerable<RecoveryStep> RecoverySteps,
    CulturalDataPriority PriorityLevel,
    TimeSpan EstimatedRecoveryTime,
    DateTime CreatedAt
);