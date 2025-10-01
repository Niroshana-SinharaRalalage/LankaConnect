using LankaConnect.Application.Common.Models;
using LankaConnect.Domain.CulturalIntelligence.ValueObjects;

namespace LankaConnect.Application.CulturalIntelligence.Models;

public record MultiCulturalRecoveryResult(
    bool IsSuccessful,
    IEnumerable<SacredEventRecoveryResult> EventResults,
    IEnumerable<PriorityRecoveryPlan> RecoveryPlans,
    string OverallStatus,
    DateTime CompletedAt,
    IEnumerable<string> Errors
);