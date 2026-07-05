using LankaConnect.Domain.Common;
namespace LankaConnect.Modules.CulturalIntelligence.Domain.ValueObjects;

public record SacredEventRecoveryResult(
    bool IsSuccessful,
    string EventId,
    string RecoveryMessage,
    DateTime RecoveryTimestamp,
    IEnumerable<string> RecoveredComponents,
    IEnumerable<string> FailedComponents
);