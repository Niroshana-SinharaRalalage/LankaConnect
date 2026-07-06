using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.CulturalIntelligence.Domain.Enums;
namespace LankaConnect.Modules.CulturalIntelligence.Domain.ValueObjects;

public record RecoveryStep(
    string StepId,
    string Description,
    CulturalDataPriority Priority,
    TimeSpan EstimatedDuration,
    bool IsCompleted,
    DateTime? CompletedAt,
    IEnumerable<string> Dependencies
);