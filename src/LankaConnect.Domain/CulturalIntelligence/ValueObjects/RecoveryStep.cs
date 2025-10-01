using LankaConnect.Domain.Common;
using LankaConnect.Domain.CulturalIntelligence.Enums;

namespace LankaConnect.Domain.CulturalIntelligence.ValueObjects;

public record RecoveryStep(
    string StepId,
    string Description,
    SacredPriorityLevel Priority,
    TimeSpan EstimatedDuration,
    bool IsCompleted,
    DateTime? CompletedAt,
    IEnumerable<string> Dependencies
);