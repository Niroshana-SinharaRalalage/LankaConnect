using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Database;

namespace LankaConnect.Domain.CulturalIntelligence.ValueObjects;

public record RecoveryStep(
    string StepId,
    string Description,
    CulturalDataPriority Priority,
    TimeSpan EstimatedDuration,
    bool IsCompleted,
    DateTime? CompletedAt,
    IEnumerable<string> Dependencies
);