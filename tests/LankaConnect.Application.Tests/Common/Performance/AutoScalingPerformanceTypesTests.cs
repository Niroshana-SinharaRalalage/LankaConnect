using FluentAssertions;
using LankaConnect.Application.Common.Performance;
using LankaConnect.Domain.Common;
using Xunit;

namespace LankaConnect.Application.Tests.Common.Performance;

/// <summary>
/// TDD RED Phase: Auto-Scaling Performance & Infrastructure Types Tests
/// Testing comprehensive auto-scaling patterns for 6M+ user Cultural Intelligence platform
/// Expected Error Reduction: 40-50 errors (-25% of remaining 194 errors)
/// </summary>
public class AutoScalingPerformanceTypesTests
{
    #region Auto-Scaling Performance Impact Tests (RED Phase)

    [Fact]
    public void AutoScalingPerformanceImpact_CreateSuccess_ShouldReturnValidImpact()
    {
        // Arrange
        var scalingEvent = new ScalingEvent
        {
            EventType = ScalingEventType.CulturalEventSpike,
            UserLoadIncrease = 150000, // 150K new users during cultural event
            ResourceScalingFactor = 2.5m,
            ExpectedPerformanceImpact = PerformanceImpactLevel.Moderate
        };

        var performanceMetrics = new ScalingPerformanceMetrics
        {
            ResponseTimeBeforeScaling = TimeSpan.FromMilliseconds(450),
            ResponseTimeAfterScaling = TimeSpan.FromMilliseconds(280),
            ThroughputIncrease = 85.5m,
            ResourceUtilizationOptimal = true
        };
        
        // Act
        var result = AutoScalingPerformanceImpact.Analyze(scalingEvent, performanceMetrics);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ScalingEfficiencyScore.Should().BeGreaterThan(70m);
        result.Value.PerformanceImprovement.Should().BeGreaterThan(0m);
        result.Value.IsOptimalScaling.Should().BeTrue();
        result.Value.CulturalEventReadiness.Should().BeTrue(); // Ready for cultural events
        result.Value.DiasporaEngagementSupport.Should().BeTrue(); // Supports diaspora communities
    }

    [Fact]
    public void AutoScalingPerformanceImpact_SuboptimalScaling_ShouldIndicateIssues()
    {
        // Arrange
        var poorScalingEvent = new ScalingEvent
        {
            EventType = ScalingEventType.DiasporaEngagementSpike,
            UserLoadIncrease = 200000, // Massive spike
            ResourceScalingFactor = 1.2m, // Insufficient scaling
            ExpectedPerformanceImpact = PerformanceImpactLevel.Severe
        };

        var poorMetrics = new ScalingPerformanceMetrics
        {
            ResponseTimeBeforeScaling = TimeSpan.FromMilliseconds(500),
            ResponseTimeAfterScaling = TimeSpan.FromMilliseconds(1200), // Worse performance
            ThroughputIncrease = -15.0m, // Negative throughput
            ResourceUtilizationOptimal = false
        };
        
        // Act
        var result = AutoScalingPerformanceImpact.Analyze(poorScalingEvent, poorMetrics);
        
        // Assert
        result.Value.IsOptimalScaling.Should().BeFalse();
        result.Value.ScalingEfficiencyScore.Should().BeLessThan(50m);
        result.Value.RequiresImmediateOptimization.Should().BeTrue();
        result.Value.PerformanceRecommendations.Should().NotBeEmpty();
    }

    #endregion

    #region Scaling Threshold Optimization Tests (RED Phase)

    [Fact]
    public void ScalingThresholdOptimization_CreateSuccess_ShouldReturnValidOptimization()
    {
        // Arrange
        var currentThresholds = new ScalingThresholds
        {
            CPUThreshold = 75.0m,
            MemoryThreshold = 80.0m,
            CulturalEventLoadThreshold = 85.0m, // Cultural intelligence specific
            DiasporaEngagementThreshold = 70.0m, // Diaspora community specific
            NetworkThroughputThreshold = 90.0m
        };

        var performanceData = new ThresholdPerformanceData
        {
            HistoricalCPUUsage = new[] { 65m, 78m, 82m, 76m, 70m },
            HistoricalMemoryUsage = new[] { 72m, 85m, 88m, 79m, 74m },
            CulturalEventTrafficSpikes = new[] { 120m, 150m, 95m, 180m, 110m },
            DiasporaEngagementPatterns = new[] { 60m, 85m, 90m, 75m, 68m }
        };
        
        // Act
        var result = ScalingThresholdOptimization.Optimize(currentThresholds, performanceData);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.OptimizedThresholds.CPUThreshold.Should().BeInRange(65m, 85m);
        result.Value.OptimizedThresholds.CulturalEventLoadThreshold.Should().BeLessThan(85m); // Should be optimized lower
        result.Value.ExpectedPerformanceImprovement.Should().BeGreaterThan(10m);
        result.Value.IsCulturalIntelligenceOptimized.Should().BeTrue();
        result.Value.ThresholdRecommendations.Should().NotBeEmpty();
    }

    [Fact]
    public void ScalingThresholdOptimization_CulturalEventOptimization_ShouldPrioritizeCulturalMetrics()
    {
        // Arrange
        var culturalEventFocusedThresholds = new ScalingThresholds
        {
            CPUThreshold = 70.0m,
            MemoryThreshold = 75.0m,
            CulturalEventLoadThreshold = 60.0m, // Very sensitive to cultural events
            DiasporaEngagementThreshold = 65.0m,
            NetworkThroughputThreshold = 80.0m
        };

        var culturalEventData = new ThresholdPerformanceData
        {
            HistoricalCPUUsage = new[] { 60m, 65m, 70m, 68m, 62m },
            CulturalEventTrafficSpikes = new[] { 200m, 300m, 180m, 250m, 220m }, // Massive cultural spikes
            DiasporaEngagementPatterns = new[] { 80m, 120m, 95m, 110m, 88m }
        };
        
        // Act
        var result = ScalingThresholdOptimization.Optimize(culturalEventFocusedThresholds, culturalEventData);
        
        // Assert
        result.Value.IsCulturalEventOptimized.Should().BeTrue();
        result.Value.OptimizedThresholds.CulturalEventLoadThreshold.Should().BeLessThan(60m);
        result.Value.CulturalIntelligenceRecommendations.Should().Contain(r => r.Contains("cultural event"));
    }

    #endregion

    #region Predictive Scaling Coordination Tests (RED Phase)

    [Fact]
    public void PredictiveScalingCoordination_CreateSuccess_ShouldReturnValidCoordination()
    {
        // Arrange
        var predictionConfiguration = new PredictiveScalingConfiguration
        {
            PredictionHorizon = TimeSpan.FromHours(4),
            CulturalEventPredictionEnabled = true,
            DiasporaEngagementForecastEnabled = true,
            MachineLearningModelAccuracy = 87.5m,
            ScalingPreparationTime = TimeSpan.FromMinutes(15)
        };

        var historicalPatterns = new ScalingHistoricalPatterns
        {
            CulturalEventPatterns = new[]
            {
                new CulturalEventPattern { EventType = "Vesak Day", TrafficMultiplier = 4.2m, Duration = TimeSpan.FromHours(6) },
                new CulturalEventPattern { EventType = "Sinhala New Year", TrafficMultiplier = 3.8m, Duration = TimeSpan.FromHours(8) },
                new CulturalEventPattern { EventType = "Poson", TrafficMultiplier = 2.9m, Duration = TimeSpan.FromHours(4) }
            },
            DiasporaEngagementCycles = new[]
            {
                new DiasporaEngagementCycle { Region = "North America", PeakHours = "18:00-22:00", EngagementMultiplier = 2.1m },
                new DiasporaEngagementCycle { Region = "Europe", PeakHours = "19:00-23:00", EngagementMultiplier = 1.8m },
                new DiasporaEngagementCycle { Region = "Australia", PeakHours = "20:00-24:00", EngagementMultiplier = 1.6m }
            }
        };
        
        // Act
        var result = PredictiveScalingCoordination.Plan(predictionConfiguration, historicalPatterns);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.PredictedScalingEvents.Should().NotBeEmpty();
        result.Value.CulturalEventPredictions.Should().HaveCountGreaterThan(0);
        result.Value.DiasporaEngagementForecasts.Should().HaveCountGreaterThan(0);
        result.Value.ScalingRecommendations.Should().NotBeEmpty();
        result.Value.PredictionConfidence.Should().BeGreaterThan(80m);
        result.Value.IsReadyForCulturalEvents.Should().BeTrue();
    }

    #endregion

    #region Performance Forecast Tests (RED Phase)

    [Fact]
    public void PerformanceForecast_CreateSuccess_ShouldReturnValidForecast()
    {
        // Arrange
        var forecastConfiguration = new PerformanceForecastConfiguration
        {
            ForecastPeriod = TimeSpan.FromDays(30),
            UserGrowthRate = 15.8m, // 15.8% monthly growth
            CulturalEventImpactFactor = 1.4m,
            DiasporaEngagementGrowth = 12.2m,
            SeasonalityEnabled = true
        };

        var currentMetrics = new CurrentPerformanceMetrics
        {
            ActiveUsers = 2800000, // 2.8M current users
            PeakConcurrentUsers = 450000,
            AverageResponseTime = TimeSpan.FromMilliseconds(320),
            ThroughputPerSecond = 15000,
            CulturalEventParticipation = 68.5m // 68.5% participation rate
        };
        
        // Act
        var result = PerformanceForecast.Generate(forecastConfiguration, currentMetrics);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectedUserCount.Should().BeGreaterThan(2800000);
        result.Value.ProjectedPeakConcurrency.Should().BeGreaterThan(450000);
        result.Value.ExpectedPerformanceImpact.Should().BeDefined();
        result.Value.ScalingRecommendations.Should().NotBeEmpty();
        result.Value.CulturalEventCapacityRecommendations.Should().NotBeEmpty();
        result.Value.IsSustainableGrowth.Should().BeDefined();
    }

    [Fact]
    public void PerformanceForecast_HighGrowthScenario_ShouldRecommendAggressiveScaling()
    {
        // Arrange
        var highGrowthConfig = new PerformanceForecastConfiguration
        {
            ForecastPeriod = TimeSpan.FromDays(60),
            UserGrowthRate = 45.0m, // Explosive growth
            CulturalEventImpactFactor = 2.0m, // High cultural impact
            DiasporaEngagementGrowth = 35.0m,
            SeasonalityEnabled = true
        };

        var currentMetrics = new CurrentPerformanceMetrics
        {
            ActiveUsers = 5500000, // Already high user base
            PeakConcurrentUsers = 850000,
            AverageResponseTime = TimeSpan.FromMilliseconds(380),
            ThroughputPerSecond = 25000
        };
        
        // Act
        var result = PerformanceForecast.Generate(highGrowthConfig, currentMetrics);
        
        // Assert
        result.Value.RequiresAggressiveScaling.Should().BeTrue();
        result.Value.ProjectedUserCount.Should().BeGreaterThan(7000000); // Aggressive growth projection
        result.Value.ScalingUrgency.Should().Be(ScalingUrgency.Immediate);
        result.Value.InfrastructureExpansionRequired.Should().BeTrue();
    }

    #endregion

    #region Scaling Anomaly Detection Tests (RED Phase)

    [Fact]
    public void ScalingAnomalyDetectionResult_CreateSuccess_ShouldReturnValidDetection()
    {
        // Arrange
        var detectionConfiguration = new AnomalyDetectionConfiguration
        {
            SensitivityLevel = AnomalySensitivity.CulturalIntelligenceOptimized,
            DetectionWindowMinutes = 30,
            CulturalEventAnomalyDetection = true,
            DiasporaEngagementAnomalyDetection = true,
            MachineLearningModelEnabled = true
        };

        var performanceData = new AnomalyPerformanceData
        {
            RecentMetrics = new[]
            {
                new PerformanceDataPoint { Timestamp = DateTime.UtcNow.AddMinutes(-5), CPUUsage = 95m, ResponseTime = 800 },
                new PerformanceDataPoint { Timestamp = DateTime.UtcNow.AddMinutes(-10), CPUUsage = 45m, ResponseTime = 250 },
                new PerformanceDataPoint { Timestamp = DateTime.UtcNow.AddMinutes(-15), CPUUsage = 48m, ResponseTime = 280 }
            },
            BaselineMetrics = new PerformanceBaseline
            {
                NormalCPURange = (40m, 60m),
                NormalResponseTimeRange = (200, 400),
                CulturalEventNormalLoad = 75m
            }
        };
        
        // Act
        var result = ScalingAnomalyDetectionResult.Detect(detectionConfiguration, performanceData);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.AnomaliesDetected.Should().NotBeEmpty();
        result.Value.AnomalyType.Should().Be(AnomalyType.PerformanceDegradation);
        result.Value.SeverityLevel.Should().BeDefined();
        result.Value.RecommendedActions.Should().NotBeEmpty();
        result.Value.RequiresImmediateScaling.Should().BeTrue();
    }

    #endregion

    #region Cost-Aware Scaling Decision Tests (RED Phase)

    [Fact]
    public void CostAwareScalingDecision_CreateSuccess_ShouldReturnValidDecision()
    {
        // Arrange
        var costConfiguration = new CostAwareScalingConfiguration
        {
            MaximumMonthlyCostBudget = 750000m, // $750K monthly budget
            CostPerScalingUnit = 125m,
            CulturalEventCostMultiplier = 1.3m, // Cultural events cost more
            DiasporaEngagementCostFactor = 1.1m,
            PerformanceVsCostPriority = CostPriority.BalancedOptimization
        };

        var scalingRequest = new CostAwareScalingRequest
        {
            RequestedScalingFactor = 2.2m,
            ExpectedUserLoadIncrease = 180000,
            EstimatedDurationHours = 6,
            IsCulturalEventDriven = true,
            PerformanceRequirement = PerformanceRequirement.High
        };
        
        // Act
        var result = CostAwareScalingDecision.Evaluate(costConfiguration, scalingRequest);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.IsScalingApproved.Should().BeDefined();
        result.Value.OptimizedScalingFactor.Should().BeGreaterThan(0m);
        result.Value.EstimatedCost.Should().BeGreaterThan(0m);
        result.Value.CostEfficiencyScore.Should().BeInRange(0m, 100m);
        result.Value.AlternativeScalingOptions.Should().NotBeEmpty();
        result.Value.CulturalEventCostImpact.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void CostAwareScalingDecision_BudgetExceeded_ShouldProposeAlternatives()
    {
        // Arrange
        var tightBudgetConfig = new CostAwareScalingConfiguration
        {
            MaximumMonthlyCostBudget = 50000m, // Very tight budget
            CostPerScalingUnit = 200m,
            PerformanceVsCostPriority = CostPriority.CostOptimized
        };

        var expensiveRequest = new CostAwareScalingRequest
        {
            RequestedScalingFactor = 5.0m, // Very expensive scaling
            ExpectedUserLoadIncrease = 500000,
            EstimatedDurationHours = 12,
            PerformanceRequirement = PerformanceRequirement.Maximum
        };
        
        // Act
        var result = CostAwareScalingDecision.Evaluate(tightBudgetConfig, expensiveRequest);
        
        // Assert
        result.Value.IsScalingApproved.Should().BeFalse();
        result.Value.BudgetExceeded.Should().BeTrue();
        result.Value.AlternativeScalingOptions.Should().NotBeEmpty();
        result.Value.CostReductionRecommendations.Should().NotBeEmpty();
    }

    #endregion

    #region Integration Tests (RED Phase)

    [Fact]
    public void AutoScalingSystem_IntegratedWorkflow_ShouldProvideComprehensiveScaling()
    {
        // Arrange
        var performanceImpact = AutoScalingPerformanceImpact.Analyze(
            new ScalingEvent { EventType = ScalingEventType.CulturalEventSpike, UserLoadIncrease = 200000 },
            new ScalingPerformanceMetrics { ResourceUtilizationOptimal = true });

        var thresholdOptimization = ScalingThresholdOptimization.Optimize(
            new ScalingThresholds { CulturalEventLoadThreshold = 80m },
            new ThresholdPerformanceData { CulturalEventTrafficSpikes = new[] { 150m, 200m } });

        var predictiveCoordination = PredictiveScalingCoordination.Plan(
            new PredictiveScalingConfiguration { CulturalEventPredictionEnabled = true },
            new ScalingHistoricalPatterns());
        
        // Act
        var systemEfficiency = CalculateAutoScalingSystemEfficiency(
            performanceImpact.Value,
            thresholdOptimization.Value,
            predictiveCoordination.Value);
        
        // Assert
        systemEfficiency.Should().BeGreaterThan(80.0m);
        performanceImpact.Value.CulturalEventReadiness.Should().BeTrue();
        thresholdOptimization.Value.IsCulturalIntelligenceOptimized.Should().BeTrue();
        predictiveCoordination.Value.IsReadyForCulturalEvents.Should().BeTrue();
    }

    #endregion

    private decimal CalculateAutoScalingSystemEfficiency(
        AutoScalingPerformanceImpact performanceImpact,
        ScalingThresholdOptimization thresholdOpt,
        PredictiveScalingCoordination predictive)
    {
        var performanceScore = performanceImpact.IsOptimalScaling ? 90m : 60m;
        var thresholdScore = thresholdOpt.IsCulturalIntelligenceOptimized ? 85m : 70m;
        var predictiveScore = predictive.PredictionConfidence;

        return (performanceScore + thresholdScore + predictiveScore) / 3;
    }
}