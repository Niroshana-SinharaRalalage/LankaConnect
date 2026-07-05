using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Database;
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