using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Cultural event importance matrix for priority assessment
/// </summary>
public class CulturalEventImportanceMatrix
{
    public string MatrixId { get; set; } = string.Empty;
    public Dictionary<CulturalEventType, int> ImportanceScores { get; set; } = new();
    public Dictionary<string, double> RegionalWeights { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public List<string> ConsideredFactors { get; set; } = new();

    public int GetImportanceScore(CulturalEventType eventType)
    {
        return ImportanceScores.TryGetValue(eventType, out var score) ? score : 1;
    }

    public double GetRegionalWeight(string region)
    {
        return RegionalWeights.TryGetValue(region, out var weight) ? weight : 1.0;
    }
}

/// <summary>
/// Language boost configuration for cultural events
/// </summary>
public class CulturalEventLanguageBoost
{
    public string BoostId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public Dictionary<string, double> LanguageMultipliers { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Region { get; set; } = string.Empty;
    public List<string> TargetLanguages { get; set; } = new();

    public double GetLanguageMultiplier(string language)
    {
        return LanguageMultipliers.TryGetValue(language, out var multiplier) ? multiplier : 1.0;
    }
}

/// <summary>
/// Cultural event prediction model for forecasting
/// </summary>
public class CulturalEventPredictionModel
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public Dictionary<string, double> Parameters { get; set; } = new();
    public double AccuracyScore { get; set; }
    public DateTime TrainedAt { get; set; }
    public List<string> TrainingDataSources { get; set; } = new();
    public string Version { get; set; } = "1.0";
    public bool IsProduction { get; set; }

    public class PredictionResult
    {
        public string EventId { get; set; } = string.Empty;
        public double ConfidenceLevel { get; set; }
        public Dictionary<string, object> Predictions { get; set; } = new();
        public DateTime PredictedAt { get; set; } = DateTime.UtcNow;
    }

    public PredictionResult Predict(Dictionary<string, object> inputData)
    {
        return new PredictionResult
        {
            EventId = inputData.ContainsKey("eventId") ? inputData["eventId"]?.ToString() ?? "" : "",
            ConfidenceLevel = 0.75, // Default confidence
            Predictions = new Dictionary<string, object> { ["attendance"] = 1000, ["impact"] = 1.5 }
        };
    }
}

/// <summary>
/// Monetization strategy for cultural events
/// </summary>
public class CulturalEventMonetizationStrategy
{
    public string StrategyId { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public Dictionary<string, decimal> RevenueTargets { get; set; } = new();
    public List<string> MonetizationChannels { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string TargetAudience { get; set; } = string.Empty;

    public decimal GetRevenueTarget(string channel)
    {
        return RevenueTargets.TryGetValue(channel, out var target) ? target : 0m;
    }
}

/// <summary>
/// Cultural event scenario for testing and planning
/// </summary>
public class CulturalEventScenario
{
    public string ScenarioId { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> ExpectedOutcomes { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsTestScenario { get; set; }

    public class ScenarioResult
    {
        public string ScenarioId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public Dictionary<string, object> ActualOutcomes { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }

    public ScenarioResult Execute()
    {
        return new ScenarioResult
        {
            ScenarioId = ScenarioId,
            Success = true,
            ActualOutcomes = new Dictionary<string, object> { ["executed"] = true }
        };
    }
}

/// <summary>
/// Cultural event load pattern for performance prediction
/// </summary>
public class CulturalEventLoadPattern
{
    public string PatternId { get; set; } = string.Empty;
    public string PatternName { get; set; } = string.Empty;
    public CulturalEventType EventType { get; set; }
    public Dictionary<int, double> HourlyMultipliers { get; set; } = new(); // 24-hour pattern
    public List<int> PeakHours { get; set; } = new();
    public double BaseLoadMultiplier { get; set; } = 1.0;
    public string Region { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public double GetHourlyMultiplier(int hour)
    {
        return HourlyMultipliers.TryGetValue(hour, out var multiplier) ? multiplier : BaseLoadMultiplier;
    }

    public bool IsPeakHour(int hour)
    {
        return PeakHours.Contains(hour);
    }
}

/// <summary>
/// Cultural event security monitoring result
/// </summary>
public class CulturalEventSecurityMonitoringResult
{
    public string ResultId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public DateTime MonitoredAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> SecurityMetrics { get; set; } = new();
    public List<string> SecurityAlerts { get; set; } = new();
    public bool IsSecure { get; set; } = true;
    public string RiskLevel { get; set; } = "Low";
    public List<string> RecommendedActions { get; set; } = new();

    public void AddSecurityAlert(string alert)
    {
        SecurityAlerts.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: {alert}");
    }
}