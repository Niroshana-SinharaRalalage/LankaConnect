using FluentAssertions;
using LankaConnect.Application.Common.Revenue;
using LankaConnect.Domain.Common;
using Xunit;

namespace LankaConnect.Application.Tests.Common.Revenue;

/// <summary>
/// TDD RED Phase: Revenue Optimization & Business Intelligence Types Tests
/// Testing comprehensive revenue optimization patterns for Cultural Intelligence platform
/// Expected Error Reduction: 60-70 errors (-35% of remaining 192 errors)
/// </summary>
public class RevenueOptimizationTypesTests
{
    #region Revenue Metrics Configuration Tests (RED Phase)

    [Fact]
    public void RevenueMetricsConfiguration_CreateSuccess_ShouldReturnValidConfiguration()
    {
        // Arrange
        var metricsScope = RevenueMetricsScope.CulturalEventSubscriptions;
        var trackingInterval = TimeSpan.FromMinutes(5);
        var complianceStandards = new[] { "SOC2", "Fortune500", "GDPR" };
        var culturalIntelligenceTracking = true;
        
        // Act
        var result = RevenueMetricsConfiguration.Create(metricsScope, trackingInterval, complianceStandards, culturalIntelligenceTracking);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.MetricsScope.Should().Be(metricsScope);
        result.Value.TrackingInterval.Should().Be(trackingInterval);
        result.Value.ComplianceStandards.Should().Contain("Fortune500");
        result.Value.CulturalIntelligenceTracking.Should().BeTrue();
        result.Value.IsOptimalConfiguration.Should().BeTrue();
    }

    [Fact]
    public void RevenueMetricsConfiguration_InvalidInterval_ShouldReturnFailure()
    {
        // Arrange
        var invalidInterval = TimeSpan.FromSeconds(30); // Too frequent for Fortune 500
        
        // Act
        var result = RevenueMetricsConfiguration.Create(
            RevenueMetricsScope.DiasporaEngagement, 
            invalidInterval, 
            new[] { "SOC2" }, 
            true);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("minimum 1 minute"));
    }

    #endregion

    #region Revenue Risk Calculation Tests (RED Phase)

    [Fact]
    public void RevenueRiskCalculation_CreateSuccess_ShouldReturnValidCalculation()
    {
        // Arrange
        var riskParameters = new RevenueRiskParameters
        {
            ChurnRiskThreshold = 15.0m,
            RevenueDeclineThreshold = 10.0m,
            CulturalEventImpactWeight = 0.4m,
            DiasporaEngagementWeight = 0.6m,
            MarketVolatilityFactor = 1.2m
        };

        var historicalData = new RevenueHistoricalData
        {
            MonthlyRevenueStream = new[] { 100000m, 105000m, 98000m, 110000m },
            CulturalEventRevenue = new[] { 15000m, 18000m, 12000m, 20000m },
            DiasporaSubscriptionRevenue = new[] { 85000m, 87000m, 86000m, 90000m }
        };
        
        // Act
        var result = RevenueRiskCalculation.Calculate(riskParameters, historicalData);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.CalculatedRiskScore.Should().BeInRange(0m, 100m);
        result.Value.PredictedRevenueDecline.Should().BeGreaterOrEqualTo(0m);
        result.Value.RecommendedActions.Should().NotBeEmpty();
        result.Value.IsHighRisk.Should().BeDefined();
    }

    [Fact]
    public void RevenueRiskCalculation_HighRiskScenario_ShouldTriggerAlerts()
    {
        // Arrange
        var highRiskParameters = new RevenueRiskParameters
        {
            ChurnRiskThreshold = 5.0m, // Very low threshold
            RevenueDeclineThreshold = 3.0m, // Very sensitive
            CulturalEventImpactWeight = 0.7m,
            DiasporaEngagementWeight = 0.3m,
            MarketVolatilityFactor = 2.0m // High volatility
        };

        var decliningData = new RevenueHistoricalData
        {
            MonthlyRevenueStream = new[] { 100000m, 85000m, 70000m, 55000m }, // Declining trend
            CulturalEventRevenue = new[] { 15000m, 10000m, 8000m, 5000m },
            DiasporaSubscriptionRevenue = new[] { 85000m, 75000m, 62000m, 50000m }
        };
        
        // Act
        var result = RevenueRiskCalculation.Calculate(highRiskParameters, decliningData);
        
        // Assert
        result.Value.IsHighRisk.Should().BeTrue();
        result.Value.CalculatedRiskScore.Should().BeGreaterThan(70m);
        result.Value.RequiresImmediateIntervention.Should().BeTrue();
        result.Value.RecommendedActions.Should().Contain(action => action.Contains("intervention"));
    }

    #endregion

    #region Revenue Calculation Model Tests (RED Phase)

    [Fact]
    public void RevenueCalculationModel_CreateSuccess_ShouldReturnValidModel()
    {
        // Arrange
        var modelConfiguration = new RevenueModelConfiguration
        {
            CalculationMethod = RevenueCalculationMethod.CulturalIntelligenceWeighted,
            BaseSubscriptionRate = 29.99m,
            CulturalEventMultiplier = 1.5m,
            DiasporaEngagementBonus = 0.25m,
            Fortune500Compliance = true
        };

        var inputData = new RevenueCalculationInput
        {
            ActiveSubscribers = 50000,
            CulturalEventsParticipation = 25000,
            DiasporaEngagementScore = 85.5m,
            MonthlyActivityLevel = ActivityLevel.High
        };
        
        // Act
        var result = RevenueCalculationModel.Calculate(modelConfiguration, inputData);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectedMonthlyRevenue.Should().BeGreaterThan(1000000m);
        result.Value.CulturalEventContribution.Should().BeGreaterThan(0m);
        result.Value.DiasporaEngagementImpact.Should().BeGreaterThan(0m);
        result.Value.IsRealistic.Should().BeTrue();
    }

    #endregion

    #region Revenue Protection Policy Tests (RED Phase)

    [Fact]
    public void RevenueProtectionPolicy_CreateSuccess_ShouldReturnValidPolicy()
    {
        // Arrange
        var protectionScope = RevenueProtectionScope.ComprehensiveCulturalIntelligence;
        var protectionLevel = ProtectionLevel.Enterprise;
        var failoverStrategies = new[]
        {
            "Cultural Event Revenue Diversification",
            "Diaspora Engagement Backup Channels",
            "Fortune 500 Compliance Revenue Streams"
        };

        var protectionThresholds = new RevenueProtectionThresholds
        {
            MinimumRevenueProtection = 95.0m,
            FailoverTriggerThreshold = 15.0m,
            RecoveryTimeObjective = TimeSpan.FromMinutes(30),
            MaximumRevenueAtRisk = 5.0m
        };
        
        // Act
        var result = RevenueProtectionPolicy.Create(protectionScope, protectionLevel, failoverStrategies, protectionThresholds);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ProtectionScope.Should().Be(protectionScope);
        result.Value.ProtectionLevel.Should().Be(protectionLevel);
        result.Value.ProtectionThresholds.MinimumRevenueProtection.Should().Be(95.0m);
        result.Value.IsEnterprisePolicyCompliant.Should().BeTrue();
        result.Value.FailoverStrategies.Should().HaveCount(3);
    }

    #endregion

    #region Revenue Recovery Metrics Tests (RED Phase)

    [Fact]
    public void RevenueRecoveryMetrics_CreateSuccess_ShouldReturnValidMetrics()
    {
        // Arrange
        var recoveryConfiguration = new RevenueRecoveryConfiguration
        {
            RecoveryScope = RecoveryScope.CulturalIntelligencePlatform,
            TargetRecoveryTime = TimeSpan.FromMinutes(45),
            MinimumRecoveryPercentage = 90.0m,
            FailoverChannelsRequired = 3
        };

        var actualRecovery = new RevenueRecoveryData
        {
            ActualRecoveryTime = TimeSpan.FromMinutes(38),
            RecoveredRevenuePercentage = 94.5m,
            FailoverChannelsActivated = 2,
            CulturalEventRevenueLoss = 2500m,
            DiasporaEngagementImpact = 1200m
        };
        
        // Act
        var result = RevenueRecoveryMetrics.Analyze(recoveryConfiguration, actualRecovery);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.MeetsRecoveryTargets.Should().BeTrue(); // 94.5% > 90%
        result.Value.RecoveryTimeSuccess.Should().BeTrue(); // 38 min < 45 min
        result.Value.TotalRevenueLoss.Should().Be(3700m);
        result.Value.RecoveryEfficiencyScore.Should().BeGreaterThan(85m);
    }

    #endregion

    #region Competitive Benchmark Data Tests (RED Phase)

    [Fact]
    public void CompetitiveBenchmarkData_CreateSuccess_ShouldReturnValidBenchmark()
    {
        // Arrange
        var benchmarkScope = CompetitiveBenchmarkScope.CulturalIntelligencePlatforms;
        var marketSegment = MarketSegment.DiasporaCommunityServices;
        
        var competitorData = new CompetitorAnalysisData
        {
            CompetitorCount = 8,
            AverageSubscriptionPrice = 24.99m,
            MarketLeaderPrice = 39.99m,
            AverageCulturalEventPremium = 8.99m,
            MarketGrowthRate = 15.5m
        };

        var ourMetrics = new OurPlatformMetrics
        {
            CurrentSubscriptionPrice = 29.99m,
            CulturalEventPremium = 12.99m,
            MarketShare = 18.5m,
            CustomerSatisfactionScore = 92.0m
        };
        
        // Act
        var result = CompetitiveBenchmarkData.Analyze(benchmarkScope, marketSegment, competitorData, ourMetrics);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.CompetitivePosition.Should().Be(CompetitivePosition.StrongPosition);
        result.Value.PricingAdvantage.Should().BeDefined();
        result.Value.RecommendedPricing.Should().BeGreaterThan(0m);
        result.Value.MarketOpportunityScore.Should().BeInRange(0m, 100m);
    }

    #endregion

    #region Market Position Analysis Tests (RED Phase)

    [Fact]
    public void MarketPositionAnalysis_CreateSuccess_ShouldReturnValidAnalysis()
    {
        // Arrange
        var analysisScope = MarketAnalysisScope.GlobalDiasporaServices;
        var analysisParameters = new MarketAnalysisParameters
        {
            GeographicScope = new[] { "North America", "Europe", "Australia", "Asia-Pacific" },
            CulturalSegments = new[] { "Sri Lankan", "South Asian", "Buddhist Communities" },
            TimeHorizon = TimeSpan.FromDays(365),
            CompetitorTrackingEnabled = true
        };

        var marketData = new MarketPositionData
        {
            TotalAddressableMarket = 2500000,
            ServiceableAddressableMarket = 850000,
            CurrentUserBase = 156000,
            MarketPenetration = 18.5m,
            GrowthVelocity = 24.8m
        };
        
        // Act
        var result = MarketPositionAnalysis.Analyze(analysisScope, analysisParameters, marketData);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.MarketPosition.Should().Be(MarketPosition.MarketLeader);
        result.Value.GrowthOpportunity.Should().BeGreaterThan(50m);
        result.Value.RevenueGrowthPotential.Should().BeGreaterThan(1000000m);
        result.Value.StrategicRecommendations.Should().NotBeEmpty();
    }

    #endregion

    #region Churn Risk Analysis Tests (RED Phase)

    [Fact]
    public void ChurnRiskAnalysis_CreateSuccess_ShouldReturnValidAnalysis()
    {
        // Arrange
        var riskConfiguration = new ChurnRiskConfiguration
        {
            AnalysisScope = ChurnAnalysisScope.CulturalIntelligenceSubscribers,
            RiskFactors = new[]
            {
                "Decreased Cultural Event Participation",
                "Reduced Community Engagement",
                "Competitive Pricing Pressure"
            },
            PredictionHorizon = TimeSpan.FromDays(90),
            RiskThreshold = 25.0m
        };

        var subscriberData = new ChurnSubscriberData
        {
            TotalSubscribers = 156000,
            HighRiskSubscribers = 12500,
            MediumRiskSubscribers = 23000,
            RecentCancellations = 1200,
            CulturalEventParticipationDecline = 8.5m
        };
        
        // Act
        var result = ChurnRiskAnalysis.Analyze(riskConfiguration, subscriberData);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.OverallChurnRisk.Should().BeInRange(0m, 100m);
        result.Value.PredictedMonthlyChurn.Should().BeGreaterThan(0);
        result.Value.RevenueAtRisk.Should().BeGreaterThan(0m);
        result.Value.RetentionRecommendations.Should().NotBeEmpty();
        result.Value.RequiresImmediateAction.Should().BeDefined();
    }

    #endregion

    #region Integration Tests (RED Phase)

    [Fact]
    public void RevenueOptimizationSystem_IntegratedWorkflow_ShouldProvideComprehensiveInsights()
    {
        // Arrange
        var metricsConfig = RevenueMetricsConfiguration.Create(
            RevenueMetricsScope.ComprehensiveRevenueIntelligence,
            TimeSpan.FromMinutes(15),
            new[] { "SOC2", "Fortune500" },
            true);

        var riskAnalysis = RevenueRiskCalculation.Calculate(
            new RevenueRiskParameters { ChurnRiskThreshold = 20.0m },
            new RevenueHistoricalData { MonthlyRevenueStream = new[] { 100000m, 105000m } });

        var protectionPolicy = RevenueProtectionPolicy.Create(
            RevenueProtectionScope.CriticalRevenueFunctions,
            ProtectionLevel.Enterprise,
            new[] { "Cultural Event Backup", "Diaspora Failover" },
            new RevenueProtectionThresholds { MinimumRevenueProtection = 95.0m });
        
        // Act
        var systemReliability = CalculateRevenueSystemReliability(metricsConfig.Value, riskAnalysis.Value, protectionPolicy.Value);
        
        // Assert
        systemReliability.Should().BeGreaterThan(90.0m);
        metricsConfig.Value.IsOptimalConfiguration.Should().BeTrue();
        riskAnalysis.Value.IsHighRisk.Should().BeFalse();
        protectionPolicy.Value.IsEnterprisePolicyCompliant.Should().BeTrue();
    }

    #endregion

    private decimal CalculateRevenueSystemReliability(
        RevenueMetricsConfiguration metrics,
        RevenueRiskCalculation risk,
        RevenueProtectionPolicy protection)
    {
        var metricsScore = metrics.IsOptimalConfiguration ? 95m : 70m;
        var riskScore = risk.IsHighRisk ? 60m : 90m;
        var protectionScore = protection.ProtectionThresholds.MinimumRevenueProtection;

        return (metricsScore + riskScore + protectionScore) / 3;
    }
}