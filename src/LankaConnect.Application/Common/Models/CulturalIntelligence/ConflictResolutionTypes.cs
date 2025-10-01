namespace LankaConnect.Application.Common.Models.CulturalIntelligence;

public class CulturalConflictScenario
{
    public required string ScenarioId { get; set; }
    public required string ConflictType { get; set; }
    public required List<string> InvolvedCommunities { get; set; }
    public required Dictionary<string, object> CulturalContext { get; set; }
    public required string ConflictDescription { get; set; }
    public required int SeverityLevel { get; set; }
    public DateTime ConflictTimestamp { get; set; }
    public Dictionary<string, string> StakeholderPositions { get; set; } = new();
}

public class ConflictResolutionStrategy
{
    public required string StrategyId { get; set; }
    public required string StrategyName { get; set; }
    public required List<string> ResolutionSteps { get; set; }
    public required Dictionary<string, object> CulturalAdaptations { get; set; }
    public required string MediationApproach { get; set; }
    public required TimeSpan EstimatedResolutionTime { get; set; }
    public Dictionary<string, double> SuccessProbabilities { get; set; } = new();
    public List<string> RequiredResources { get; set; } = new();
}

public class CulturalConflictResolutionResult
{
    public required bool ResolutionSuccessful { get; set; }
    public required string ResolutionOutcome { get; set; }
    public required List<string> AppliedStrategies { get; set; }
    public required DateTime ResolutionTimestamp { get; set; }
    public required Dictionary<string, object> CommunityFeedback { get; set; }
    public required double SatisfactionScore { get; set; }
    public Dictionary<string, string> LessonsLearned { get; set; } = new();
    public string? FollowUpRequired { get; set; }
}

public class CulturalMediationContext
{
    public required string MediationId { get; set; }
    public required List<string> MediatorProfiles { get; set; }
    public required Dictionary<string, object> CulturalProtocols { get; set; }
    public required string MediationFormat { get; set; }
    public required List<string> ParticipatingParties { get; set; }
    public DateTime ScheduledTime { get; set; }
    public Dictionary<string, string> CommunicationPreferences { get; set; } = new();
}

public class ConflictAnalysisParameters
{
    public required string ConflictId { get; set; }
    public required List<string> AnalysisDimensions { get; set; }
    public required Dictionary<string, double> CulturalFactors { get; set; }
    public required string AnalysisDepth { get; set; }
    public required List<string> DataSources { get; set; }
    public DateTime AnalysisTimestamp { get; set; }
    public Dictionary<string, object> ContextualInformation { get; set; } = new();
}

public class ConflictAnalysisResult
{
    public required string AnalysisId { get; set; }
    public required Dictionary<string, double> ConflictFactors { get; set; }
    public required List<string> RootCauses { get; set; }
    public required string ConflictClassification { get; set; }
    public required double EscalationRisk { get; set; }
    public Dictionary<string, object> RecommendedActions { get; set; } = new();
    public List<string> PotentialSolutions { get; set; } = new();
}

public class CulturalSensitivityAssessment
{
    public required string AssessmentId { get; set; }
    public required Dictionary<string, double> SensitivityMetrics { get; set; }
    public required List<string> CulturalConsiderations { get; set; }
    public required string AssessmentLevel { get; set; }
    public required DateTime AssessmentDate { get; set; }
    public Dictionary<string, string> RecommendedApproaches { get; set; } = new();
    public List<string> CulturalTaboos { get; set; } = new();
}

public class EscalationPreventionStrategy
{
    public required string StrategyId { get; set; }
    public required List<string> EarlyWarningIndicators { get; set; }
    public required Dictionary<string, object> PreventionMeasures { get; set; }
    public required string InterventionTiming { get; set; }
    public required List<string> StakeholderEngagement { get; set; }
    public Dictionary<string, double> EffectivenessMetrics { get; set; } = new();
    public TimeSpan MonitoringInterval { get; set; }
}

public class EscalationPreventionResult
{
    public required bool PreventionSuccessful { get; set; }
    public required List<string> TriggeredIndicators { get; set; }
    public required Dictionary<string, object> InterventionResults { get; set; }
    public required DateTime PreventionTimestamp { get; set; }
    public required string PreventionStrategy { get; set; }
    public Dictionary<string, double> RiskReduction { get; set; } = new();
    public string? EscalationStatus { get; set; }
}

public class CommunityEngagementPlan
{
    public required string PlanId { get; set; }
    public required List<string> TargetCommunities { get; set; }
    public required Dictionary<string, object> EngagementStrategies { get; set; }
    public required List<string> CommunicationChannels { get; set; }
    public required string EngagementTimeline { get; set; }
    public Dictionary<string, string> CulturalAdaptations { get; set; } = new();
    public List<string> ExpectedOutcomes { get; set; } = new();
}

public class CommunityEngagementResult
{
    public required bool EngagementSuccessful { get; set; }
    public required Dictionary<string, object> CommunityResponses { get; set; }
    public required List<string> ParticipatingCommunities { get; set; }
    public required double EngagementLevel { get; set; }
    public required DateTime EngagementDate { get; set; }
    public Dictionary<string, string> Feedback { get; set; } = new();
    public List<string> FollowUpActions { get; set; } = new();
}
