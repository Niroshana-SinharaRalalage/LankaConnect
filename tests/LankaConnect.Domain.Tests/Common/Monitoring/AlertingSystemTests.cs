using FluentAssertions;
using LankaConnect.Domain.Common.Monitoring;

namespace LankaConnect.Domain.Tests.Common.Monitoring;

/// <summary>
/// TDD RED Phase: Tests for Comprehensive Alerting & Notification System
/// These tests should FAIL until we implement the complete alerting infrastructure
/// Target: 45-60+ error reduction through foundational alerting/monitoring system
/// </summary>
public class AlertingSystemTests
{
    #region Alert Processing Tests

    [Fact]
    public void AlertProcessingResult_Create_ShouldCreateValidResult()
    {
        // Arrange
        var alertId = "alert-001";
        var isProcessed = true;
        var processingDuration = TimeSpan.FromSeconds(2);
        var culturalContext = "Vesak Day Traffic Spike";
        
        // Act
        var result = AlertProcessingResult.Create(alertId, isProcessed, processingDuration, culturalContext);
        
        // Assert
        result.Should().NotBeNull();
        result.AlertId.Should().Be(alertId);
        result.IsProcessed.Should().BeTrue();
        result.ProcessingDuration.Should().Be(processingDuration);
        result.CulturalContext.Should().Be(culturalContext);
        result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AlertProcessingContext_WithCulturalIntelligence_ShouldProvideRichContext()
    {
        // Arrange
        var contextId = "context-001";
        var eventType = CulturalEventType.ReligiousFestival;
        var priority = AlertPriority.CulturalEvent;
        var affectedRegions = new List<string> { "Colombo", "Kandy", "Galle" };
        
        // Act
        var context = AlertProcessingContext.CreateCultural(contextId, eventType, priority, affectedRegions);
        
        // Assert
        context.ContextId.Should().Be(contextId);
        context.EventType.Should().Be(eventType);
        context.Priority.Should().Be(priority);
        context.AffectedRegions.Should().BeEquivalentTo(affectedRegions);
        context.IsCulturallySignificant.Should().BeTrue();
        context.RequiresSpecialHandling.Should().BeTrue();
    }

    [Fact]
    public void EscalationResult_WithMultiLevelEscalation_ShouldTrackEscalationPath()
    {
        // Arrange
        var escalationId = "esc-001";
        var escalationLevels = new List<string> { "L1-Support", "L2-Engineering", "L3-Management" };
        var finalLevel = "L3-Management";
        var resolutionTime = TimeSpan.FromMinutes(45);
        
        // Act
        var result = EscalationResult.Create(escalationId, escalationLevels, finalLevel, resolutionTime);
        
        // Assert
        result.EscalationId.Should().Be(escalationId);
        result.EscalationPath.Should().BeEquivalentTo(escalationLevels);
        result.FinalEscalationLevel.Should().Be(finalLevel);
        result.TotalEscalationTime.Should().Be(resolutionTime);
        result.EscalationCount.Should().Be(3);
        result.RequiredManagementIntervention.Should().BeTrue();
    }

    [Fact]
    public void AlertEscalationResult_ForCulturalEvents_ShouldHaveEnhancedTracking()
    {
        // Arrange
        var alertId = "cultural-alert-001";
        var culturalEventId = "diwali-2024";
        var escalationReason = "Cultural event load exceeded threshold";
        var stakeholdersNotified = new List<string> { "Cultural-Team", "Engineering-Lead", "Business-Owner" };
        
        // Act
        var result = AlertEscalationResult.CreateCultural(alertId, culturalEventId, escalationReason, stakeholdersNotified);
        
        // Assert
        result.AlertId.Should().Be(alertId);
        result.CulturalEventId.Should().Be(culturalEventId);
        result.EscalationReason.Should().Be(escalationReason);
        result.NotifiedStakeholders.Should().BeEquivalentTo(stakeholdersNotified);
        result.IsCulturalEvent.Should().BeTrue();
        result.RequiresCulturalExpertise.Should().BeTrue();
    }

    #endregion

    #region Notification and Acknowledgment Tests

    [Fact]
    public void NotificationPreferences_WithMultiChannel_ShouldConfigureCorrectly()
    {
        // Arrange
        var userId = "user-001";
        var channels = new Dictionary<NotificationChannel, bool>
        {
            [NotificationChannel.Email] = true,
            [NotificationChannel.SMS] = false,
            [NotificationChannel.Teams] = true,
            [NotificationChannel.Slack] = false
        };
        var culturalSettings = new CulturalNotificationSettings("si-LK", "Buddhism", true);
        
        // Act
        var preferences = NotificationPreferences.Create(userId, channels, culturalSettings);
        
        // Assert
        preferences.UserId.Should().Be(userId);
        preferences.IsChannelEnabled(NotificationChannel.Email).Should().BeTrue();
        preferences.IsChannelEnabled(NotificationChannel.SMS).Should().BeFalse();
        preferences.CulturalSettings.Should().NotBeNull();
        preferences.CulturalSettings!.Language.Should().Be("si-LK");
        preferences.SupportsCulturalNotifications.Should().BeTrue();
    }

    [Fact]
    public void AlertAcknowledgment_WithUserDetails_ShouldTrackAcknowledgment()
    {
        // Arrange
        var alertId = "alert-ack-001";
        var userId = "engineer-001";
        var acknowledgmentMessage = "Investigating cultural event load impact";
        var estimatedResolutionTime = TimeSpan.FromMinutes(30);
        
        // Act
        var acknowledgment = AlertAcknowledgment.Create(alertId, userId, acknowledgmentMessage, estimatedResolutionTime);
        
        // Assert
        acknowledgment.AlertId.Should().Be(alertId);
        acknowledgment.AcknowledgedBy.Should().Be(userId);
        acknowledgment.AcknowledgmentMessage.Should().Be(acknowledgmentMessage);
        acknowledgment.EstimatedResolutionTime.Should().Be(estimatedResolutionTime);
        acknowledgment.AcknowledgedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        acknowledgment.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AlertResolutionResult_WithCulturalContext_ShouldCaptureResolutionDetails()
    {
        // Arrange
        var alertId = "resolved-001";
        var resolvedBy = "senior-engineer-001";
        var resolutionSummary = "Scaled infrastructure for Sinhala New Year traffic";
        var actionsTaken = new List<string> { "Increased server capacity", "Enabled cultural load balancing", "Notified stakeholders" };
        var preventiveMeasures = new List<string> { "Implement cultural event prediction", "Set up auto-scaling rules" };
        
        // Act
        var result = AlertResolutionResult.Create(alertId, resolvedBy, resolutionSummary, actionsTaken, preventiveMeasures);
        
        // Assert
        result.AlertId.Should().Be(alertId);
        result.ResolvedBy.Should().Be(resolvedBy);
        result.ResolutionSummary.Should().Be(resolutionSummary);
        result.ActionsTaken.Should().BeEquivalentTo(actionsTaken);
        result.PreventiveMeasures.Should().BeEquivalentTo(preventiveMeasures);
        result.ResolutionDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.HasPreventiveMeasures.Should().BeTrue();
    }

    #endregion

    #region Health Monitoring Tests

    [Fact]
    public void HealthIssue_CreateCritical_ShouldClassifyCorrectly()
    {
        // Arrange
        var issueId = "health-001";
        var component = "CulturalIntelligenceEngine";
        var severity = HealthIssueSeverity.Critical;
        var description = "Cultural model accuracy degraded below threshold";
        var affectedMetrics = new List<string> { "ModelAccuracy", "ResponseTime", "ThroughputRate" };
        
        // Act
        var issue = HealthIssue.Create(issueId, component, severity, description, affectedMetrics);
        
        // Assert
        issue.IssueId.Should().Be(issueId);
        issue.Component.Should().Be(component);
        issue.Severity.Should().Be(severity);
        issue.Description.Should().Be(description);
        issue.AffectedMetrics.Should().BeEquivalentTo(affectedMetrics);
        issue.IsCritical.Should().BeTrue();
        issue.RequiresImmediateAction.Should().BeTrue();
        issue.IsCulturalIntelligenceRelated.Should().BeTrue();
    }

    [Fact]
    public void HealthRecommendation_WithActionPriority_ShouldProvideGuidance()
    {
        // Arrange
        var recommendationId = "rec-001";
        var issueId = "health-001";
        var title = "Optimize Cultural Model Training";
        var description = "Retrain cultural intelligence models with latest diaspora data";
        var priority = RecommendationPriority.High;
        var estimatedImpact = "15% improvement in cultural prediction accuracy";
        var actionSteps = new List<string> { "Collect latest training data", "Retrain models", "Validate accuracy", "Deploy updates" };
        
        // Act
        var recommendation = HealthRecommendation.Create(recommendationId, issueId, title, description, 
            priority, estimatedImpact, actionSteps);
        
        // Assert
        recommendation.RecommendationId.Should().Be(recommendationId);
        recommendation.RelatedIssueId.Should().Be(issueId);
        recommendation.Title.Should().Be(title);
        recommendation.Priority.Should().Be(priority);
        recommendation.EstimatedImpact.Should().Be(estimatedImpact);
        recommendation.ActionSteps.Should().BeEquivalentTo(actionSteps);
        recommendation.IsHighPriority.Should().BeTrue();
        recommendation.IsActionable.Should().BeTrue();
    }

    [Fact]
    public void HealthTrend_AnalyzeCulturalMetrics_ShouldIdentifyTrends()
    {
        // Arrange
        var trendId = "trend-001";
        var metricName = "CulturalEngagementRate";
        var dataPoints = new List<HealthTrendDataPoint>
        {
            new("2024-01-01", 85.2),
            new("2024-01-02", 87.1),
            new("2024-01-03", 89.5),
            new("2024-01-04", 91.2)
        };
        var analysisWindow = TimeSpan.FromDays(30);
        
        // Act
        var trend = HealthTrend.Analyze(trendId, metricName, dataPoints, analysisWindow);
        
        // Assert
        trend.TrendId.Should().Be(trendId);
        trend.MetricName.Should().Be(metricName);
        trend.DataPoints.Should().HaveCount(4);
        trend.TrendDirection.Should().Be(TrendDirection.Improving);
        trend.ConfidenceScore.Should().BeGreaterThan(0.8);
        trend.IsSignificantTrend.Should().BeTrue();
        trend.PredictedNextValue.Should().BeGreaterThan(91.2);
    }

    #endregion

    #region Performance Analytics Tests

    [Fact]
    public void AnalyticsWidget_CreateCulturalDashboard_ShouldConfigureCorrectly()
    {
        // Arrange
        var widgetId = "widget-001";
        var widgetType = AnalyticsWidgetType.CulturalEngagementChart;
        var title = "Diaspora Community Engagement";
        var dataSource = "CulturalIntelligenceEngine";
        var refreshInterval = TimeSpan.FromMinutes(5);
        var culturalFilters = new Dictionary<string, object> { ["Region"] = "North America", ["Language"] = "Sinhala" };
        
        // Act
        var widget = AnalyticsWidget.Create(widgetId, widgetType, title, dataSource, refreshInterval, culturalFilters);
        
        // Assert
        widget.WidgetId.Should().Be(widgetId);
        widget.WidgetType.Should().Be(widgetType);
        widget.Title.Should().Be(title);
        widget.DataSource.Should().Be(dataSource);
        widget.RefreshInterval.Should().Be(refreshInterval);
        widget.CulturalFilters.Should().BeEquivalentTo(culturalFilters);
        widget.IsCulturalWidget.Should().BeTrue();
        widget.IsRealTime.Should().BeTrue();
    }

    [Fact]
    public void PerformanceInsight_GenerateCulturalInsight_ShouldProvideActionableIntel()
    {
        // Arrange
        var insightId = "insight-001";
        var category = InsightCategory.CulturalEngagement;
        var title = "Tamil Community Engagement Peak Detected";
        var description = "Tamil community shows 40% higher engagement during Deepavali season";
        var confidence = 0.92;
        var recommendations = new List<string> { "Scale Tamil language services", "Prepare cultural content", "Alert community managers" };
        
        // Act
        var insight = PerformanceInsight.Create(insightId, category, title, description, confidence, recommendations);
        
        // Assert
        insight.InsightId.Should().Be(insightId);
        insight.Category.Should().Be(category);
        insight.Title.Should().Be(title);
        insight.Confidence.Should().Be(confidence);
        insight.Recommendations.Should().BeEquivalentTo(recommendations);
        insight.IsHighConfidence.Should().BeTrue();
        insight.IsCulturalInsight.Should().BeTrue();
        insight.IsActionable.Should().BeTrue();
    }

    [Fact]
    public void PerformanceSummary_ExecutiveDashboard_ShouldSummarizeKeyMetrics()
    {
        // Arrange
        var summaryId = "summary-001";
        var period = "Q1-2024";
        var keyMetrics = new Dictionary<string, double>
        {
            ["CulturalEngagementRate"] = 87.5,
            ["DiasporaReachPercentage"] = 92.3,
            ["MultilingualCoverage"] = 95.1,
            ["CulturalAccuracyScore"] = 89.7
        };
        var culturalHighlights = new List<string> { "Vesak Day engagement +150%", "Tamil New Year reach record", "Multilingual support expansion" };
        
        // Act
        var summary = PerformanceSummary.Create(summaryId, period, keyMetrics, culturalHighlights);
        
        // Assert
        summary.SummaryId.Should().Be(summaryId);
        summary.Period.Should().Be(period);
        summary.KeyMetrics.Should().BeEquivalentTo(keyMetrics);
        summary.CulturalHighlights.Should().BeEquivalentTo(culturalHighlights);
        summary.OverallHealthScore.Should().BeGreaterThan(85);
        summary.HasCulturalHighlights.Should().BeTrue();
        summary.IsPositivePerformance.Should().BeTrue();
    }

    [Fact]
    public void ActionableRecommendation_CulturalOptimization_ShouldPrioritizeActions()
    {
        // Arrange
        var recommendationId = "action-001";
        var title = "Optimize Cultural Event Load Distribution";
        var category = RecommendationCategory.Performance;
        var priority = ActionPriority.High;
        var estimatedImpact = "25% reduction in cultural event latency";
        var implementation = new ImplementationGuidance(
            effort: "3 weeks",
            resources: new List<string> { "2 Engineers", "1 Cultural Expert", "1 DevOps" },
            risks: new List<string> { "Temporary service disruption", "Data migration complexity" }
        );
        
        // Act
        var recommendation = ActionableRecommendation.Create(recommendationId, title, category, 
            priority, estimatedImpact, implementation);
        
        // Assert
        recommendation.RecommendationId.Should().Be(recommendationId);
        recommendation.Title.Should().Be(title);
        recommendation.Category.Should().Be(category);
        recommendation.Priority.Should().Be(priority);
        recommendation.EstimatedImpact.Should().Be(estimatedImpact);
        recommendation.Implementation.Should().NotBeNull();
        recommendation.IsHighPriority.Should().BeTrue();
        recommendation.IsImplementable.Should().BeTrue();
        recommendation.HasCulturalContext.Should().BeTrue();
    }

    #endregion

    #region Compliance and SLA Tests

    [Fact]
    public void SLAComplianceStatus_FortuneCompliance_ShouldMeetEnterpriseStandards()
    {
        // Arrange
        var slaId = "sla-001";
        var serviceName = "CulturalIntelligenceAPI";
        var targetUptime = 99.9;
        var actualUptime = 99.95;
        var culturalEventCompliance = true;
        var compliancePeriod = "2024-Q1";
        
        // Act
        var status = SLAComplianceStatus.Create(slaId, serviceName, targetUptime, actualUptime, 
            culturalEventCompliance, compliancePeriod);
        
        // Assert
        status.SLAId.Should().Be(slaId);
        status.ServiceName.Should().Be(serviceName);
        status.TargetUptime.Should().Be(targetUptime);
        status.ActualUptime.Should().Be(actualUptime);
        status.CompliancePercentage.Should().BeGreaterThan(100);
        status.IsCompliant.Should().BeTrue();
        status.IsFortune500Compliant.Should().BeTrue();
        status.SupportsCulturalEvents.Should().BeTrue();
    }

    [Fact]
    public void OverallComplianceScore_EnterpriseAggregate_ShouldCalculateCorrectly()
    {
        // Arrange
        var scoreId = "score-001";
        var period = "2024-Q1";
        var serviceScores = new Dictionary<string, double>
        {
            ["CulturalIntelligenceAPI"] = 99.95,
            ["DiasporaCommunityService"] = 99.87,
            ["MultilingualSearchAPI"] = 99.92,
            ["CulturalEventPrediction"] = 99.89
        };
        var culturalEventBonus = 0.5; // Bonus for cultural event handling
        
        // Act
        var score = OverallComplianceScore.Calculate(scoreId, period, serviceScores, culturalEventBonus);
        
        // Assert
        score.ScoreId.Should().Be(scoreId);
        score.Period.Should().Be(period);
        score.ServiceScores.Should().BeEquivalentTo(serviceScores);
        score.OverallScore.Should().BeGreaterThan(99.9);
        score.CulturalEventBonus.Should().Be(culturalEventBonus);
        score.IsFortune500Grade.Should().BeTrue();
        score.HasCulturalEventSupport.Should().BeTrue();
    }

    [Fact]
    public void ComplianceViolation_CulturalEventImpact_ShouldTrackViolationDetails()
    {
        // Arrange
        var violationId = "violation-001";
        var slaId = "sla-001";
        var violationType = ComplianceViolationType.AvailabilityBreach;
        var severity = ViolationSeverity.High;
        var description = "Service unavailable during Vesak Day traffic spike";
        var culturalImpact = "Affected 50,000+ Buddhist community members";
        var duration = TimeSpan.FromMinutes(12);
        
        // Act
        var violation = ComplianceViolation.Create(violationId, slaId, violationType, severity, 
            description, culturalImpact, duration);
        
        // Assert
        violation.ViolationId.Should().Be(violationId);
        violation.SLAId.Should().Be(slaId);
        violation.ViolationType.Should().Be(violationType);
        violation.Severity.Should().Be(severity);
        violation.Description.Should().Be(description);
        violation.CulturalImpact.Should().Be(culturalImpact);
        violation.ViolationDuration.Should().Be(duration);
        violation.HasCulturalImpact.Should().BeTrue();
        violation.RequiresExecutiveAttention.Should().BeTrue();
    }

    [Fact]
    public void ComplianceTrend_CulturalEventSeason_ShouldAnalyzeTrends()
    {
        // Arrange
        var trendId = "compliance-trend-001";
        var period = "Cultural-Event-Season-2024";
        var trendData = new List<ComplianceTrendPoint>
        {
            new(DateTime.Parse("2024-04-01"), 99.95, "Vesak Preparation"),
            new(DateTime.Parse("2024-04-13"), 99.87, "Sinhala New Year"),
            new(DateTime.Parse("2024-04-14"), 99.91, "Post New Year"),
            new(DateTime.Parse("2024-04-20"), 99.96, "Normal Operations")
        };
        
        // Act
        var trend = ComplianceTrend.Analyze(trendId, period, trendData);
        
        // Assert
        trend.TrendId.Should().Be(trendId);
        trend.AnalysisPeriod.Should().Be(period);
        trend.TrendData.Should().HaveCount(4);
        trend.AverageCompliance.Should().BeGreaterThan(99.9);
        trend.TrendDirection.Should().Be(TrendDirection.Stable);
        trend.HasCulturalEventCorrelation.Should().BeTrue();
        trend.MinimumCompliance.Should().Be(99.87);
    }

    #endregion

    #region Revenue Protection Tests

    [Fact]
    public void RevenueRiskFactor_CulturalEventRisk_ShouldIdentifyRisks()
    {
        // Arrange
        var riskId = "risk-001";
        var riskType = RevenueRiskType.CulturalEventCapacity;
        var severity = RiskSeverity.High;
        var description = "Insufficient capacity for Tamil New Year celebration traffic";
        var estimatedImpact = 250000m; // $250K potential revenue impact
        var probability = 0.75;
        var mitigationStrategies = new List<string> { "Pre-scale infrastructure", "Enable emergency capacity", "Prepare fallback systems" };
        
        // Act
        var risk = RevenueRiskFactor.Create(riskId, riskType, severity, description, estimatedImpact, probability, mitigationStrategies);
        
        // Assert
        risk.RiskId.Should().Be(riskId);
        risk.RiskType.Should().Be(riskType);
        risk.Severity.Should().Be(severity);
        risk.Description.Should().Be(description);
        risk.EstimatedRevenueImpact.Should().Be(estimatedImpact);
        risk.Probability.Should().Be(probability);
        risk.MitigationStrategies.Should().BeEquivalentTo(mitigationStrategies);
        risk.ExpectedValue.Should().Be(187500m); // Impact * Probability
        risk.IsCulturalEventRelated.Should().BeTrue();
        risk.IsHighImpact.Should().BeTrue();
    }

    [Fact]
    public void RevenueProtectionStatus_CulturalEventProtection_ShouldMonitorEffectiveness()
    {
        // Arrange
        var statusId = "protection-001";
        var protectionType = RevenueProtectionType.CulturalEventLoadBalancing;
        var isActive = true;
        var protectedRevenue = 500000m; // $500K protected
        var protectionEffectiveness = 0.94;
        var culturalEventsSupported = new List<string> { "VesakDay", "Diwali", "SinhalaNewYear" };
        var lastActivation = DateTime.UtcNow.AddDays(-2);
        
        // Act
        var status = RevenueProtectionStatus.Create(statusId, protectionType, isActive, protectedRevenue, 
            protectionEffectiveness, culturalEventsSupported, lastActivation);
        
        // Assert
        status.StatusId.Should().Be(statusId);
        status.ProtectionType.Should().Be(protectionType);
        status.IsActive.Should().BeTrue();
        status.ProtectedRevenue.Should().Be(protectedRevenue);
        status.ProtectionEffectiveness.Should().Be(protectionEffectiveness);
        status.CulturalEventsSupported.Should().BeEquivalentTo(culturalEventsSupported);
        status.LastActivation.Should().Be(lastActivation);
        status.IsHighlyEffective.Should().BeTrue();
        status.SupportsCulturalEvents.Should().BeTrue();
        status.RecentlyActivated.Should().BeTrue();
    }

    #endregion
}