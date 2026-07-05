using LankaConnect.BuildingBlocks.Application.Common.Models;
using LankaConnect.Modules.CulturalIntelligence.Domain.ValueObjects;
namespace LankaConnect.Modules.CulturalIntelligence.Application.Models;

public record MultiCulturalRecoveryResult(
    bool IsSuccessful,
    IEnumerable<SacredEventRecoveryResult> EventResults,
    IEnumerable<PriorityRecoveryPlan> RecoveryPlans,
    string OverallStatus,
    DateTime CompletedAt,
    IEnumerable<string> Errors
);