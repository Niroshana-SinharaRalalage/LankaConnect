using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.CulturalIntelligence.Domain.Enums;
namespace LankaConnect.Modules.CulturalIntelligence.Domain.ValueObjects;

public record PriorityRecoveryPlan(
    string PlanId,
    string EventId,
    IEnumerable<RecoveryStep> RecoverySteps,
    CulturalDataPriority PriorityLevel,
    TimeSpan EstimatedRecoveryTime,
    DateTime CreatedAt
);