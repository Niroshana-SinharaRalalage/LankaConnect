using LankaConnect.Domain.Common;
using LankaConnect.Domain.CulturalIntelligence.Enums;

namespace LankaConnect.Domain.CulturalIntelligence.ValueObjects;

public record PriorityRecoveryPlan(
    string PlanId,
    string EventId,
    IEnumerable<RecoveryStep> RecoverySteps,
    SacredPriorityLevel PriorityLevel,
    TimeSpan EstimatedRecoveryTime,
    DateTime CreatedAt
);