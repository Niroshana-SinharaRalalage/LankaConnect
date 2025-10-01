using FluentAssertions;
using LankaConnect.Domain.Common.DisasterRecovery;

namespace LankaConnect.Domain.Tests.Common.DisasterRecovery;

/// <summary>
/// TDD RED Phase: Tests for Disaster Recovery & Synchronization Type System
/// These tests should FAIL until we implement the comprehensive synchronization framework
/// Target: 35-45 error reduction through foundational disaster recovery infrastructure
/// </summary>
public class DisasterRecoveryTypesTests
{
    #region SynchronizationResult Core Tests
    
    [Fact]
    public void SynchronizationResult_Create_ShouldCreateValidResult()
    {
        // Arrange
        var syncId = "sync-001";
        var isSuccessful = true;
        var itemsProcessed = 1500;
        var duration = TimeSpan.FromMinutes(2);
        var conflictsResolved = 5;
        
        // Act
        var result = SynchronizationResult.Create(syncId, isSuccessful, itemsProcessed, duration, conflictsResolved);
        
        // Assert
        result.Should().NotBeNull();
        result.SynchronizationId.Should().Be(syncId);
        result.IsSuccessful.Should().BeTrue();
        result.ItemsProcessed.Should().Be(itemsProcessed);
        result.SynchronizationDuration.Should().Be(duration);
        result.ConflictsResolved.Should().Be(conflictsResolved);
    }

    [Fact]
    public void SynchronizationResult_WithFailure_ShouldCaptureErrorDetails()
    {
        // Arrange
        var syncId = "sync-failed-001";
        var errorMessage = "Network timeout during cultural event synchronization";
        var retryCount = 3;
        
        // Act
        var result = SynchronizationResult.CreateFailed(syncId, errorMessage, retryCount);
        
        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
        result.RetryCount.Should().Be(retryCount);
        result.RequiresManualIntervention.Should().BeTrue();
    }

    #endregion

    #region Specialized Synchronization Results
    
    [Fact]
    public void EventSynchronizationResult_WithCulturalEvents_ShouldTrackEventSpecificMetrics()
    {
        // Arrange
        var culturalEventId = "vesak-2024";
        var eventsProcessed = 1200;
        var culturalConflicts = 8;
        
        // Act
        var result = EventSynchronizationResult.Create("event-sync-001", true, eventsProcessed, 
            TimeSpan.FromMinutes(5), culturalEventId, culturalConflicts);
        
        // Assert
        result.CulturalEventId.Should().Be(culturalEventId);
        result.CulturalConflictsResolved.Should().Be(culturalConflicts);
        result.EventsProcessed.Should().Be(eventsProcessed);
        result.HasCulturalSignificance.Should().BeTrue();
    }

    [Fact]
    public void ModelSynchronizationResult_WithIntelligenceModels_ShouldTrackModelMetrics()
    {
        // Arrange
        var modelType = "CulturalIntelligenceModel";
        var modelsUpdated = 15;
        var accuracyImprovement = 0.05;
        
        // Act
        var result = ModelSynchronizationResult.Create("model-sync-001", true, modelsUpdated,
            TimeSpan.FromMinutes(10), modelType, accuracyImprovement);
        
        // Assert
        result.ModelType.Should().Be(modelType);
        result.ModelsUpdated.Should().Be(modelsUpdated);
        result.AccuracyImprovement.Should().Be(accuracyImprovement);
        result.IsIntelligenceModelUpdate.Should().BeTrue();
    }

    [Fact]
    public void SecuritySynchronizationResult_WithSecurityPolicies_ShouldTrackSecurityMetrics()
    {
        // Arrange
        var securityDomain = "CulturalContentProtection";
        var policiesSynced = 25;
        var vulnerabilitiesFixed = 3;
        
        // Act
        var result = SecuritySynchronizationResult.Create("security-sync-001", true, policiesSynced,
            TimeSpan.FromMinutes(3), securityDomain, vulnerabilitiesFixed);
        
        // Assert
        result.SecurityDomain.Should().Be(securityDomain);
        result.PoliciesSynchronized.Should().Be(policiesSynced);
        result.VulnerabilitiesAddressed.Should().Be(vulnerabilitiesFixed);
        result.IsSecurityCritical.Should().BeTrue();
    }

    #endregion

    #region Enumeration Tests
    
    [Theory]
    [InlineData(MetricAggregationLevel.Individual)]
    [InlineData(MetricAggregationLevel.Community)]
    [InlineData(MetricAggregationLevel.Regional)]
    [InlineData(MetricAggregationLevel.Global)]
    [InlineData(MetricAggregationLevel.Enterprise)]
    public void MetricAggregationLevel_AllValues_ShouldBeValid(MetricAggregationLevel level)
    {
        // Act & Assert
        Enum.IsDefined(typeof(MetricAggregationLevel), level).Should().BeTrue();
        level.GetAggregationScope().Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(ConflictResolutionScope.Automatic)]
    [InlineData(ConflictResolutionScope.SemiAutomatic)]
    [InlineData(ConflictResolutionScope.Manual)]
    [InlineData(ConflictResolutionScope.CulturallyAware)]
    [InlineData(ConflictResolutionScope.EnterpriseManaged)]
    public void ConflictResolutionScope_AllValues_ShouldBeValid(ConflictResolutionScope scope)
    {
        // Act & Assert
        Enum.IsDefined(typeof(ConflictResolutionScope), scope).Should().BeTrue();
        scope.RequiresHumanIntervention().Should().Be(scope == ConflictResolutionScope.Manual || 
                                                     scope == ConflictResolutionScope.EnterpriseManaged);
    }

    [Theory]
    [InlineData(SynchronizationPriority.Low)]
    [InlineData(SynchronizationPriority.Medium)]
    [InlineData(SynchronizationPriority.High)]
    [InlineData(SynchronizationPriority.Critical)]
    [InlineData(SynchronizationPriority.CulturalEvent)]
    public void SynchronizationPriority_AllValues_ShouldHaveCorrectPriorityLevel(SynchronizationPriority priority)
    {
        // Act & Assert
        Enum.IsDefined(typeof(SynchronizationPriority), priority).Should().BeTrue();
        var priorityLevel = priority.GetPriorityLevel();
        priorityLevel.Should().BeGreaterThan(0);
        
        if (priority == SynchronizationPriority.CulturalEvent)
        {
            priorityLevel.Should().BeGreaterOrEqualTo(9); // Highest priority for cultural events
        }
    }

    #endregion

    #region Recovery and Operational Types Tests
    
    [Fact]
    public void RecoveryScenario_CreateCulturalEventRecovery_ShouldConfigureCorrectly()
    {
        // Arrange
        var scenarioName = "VesakDayTrafficSpike";
        var expectedDuration = TimeSpan.FromHours(4);
        var maxDataLoss = TimeSpan.FromMinutes(1);
        var culturalContext = "Sacred Buddhist celebration requiring zero data loss";
        
        // Act
        var scenario = RecoveryScenario.CreateCulturalEventRecovery(scenarioName, expectedDuration, 
            maxDataLoss, culturalContext);
        
        // Assert
        scenario.ScenarioName.Should().Be(scenarioName);
        scenario.ExpectedRecoveryTime.Should().Be(expectedDuration);
        scenario.MaxAcceptableDataLoss.Should().Be(maxDataLoss);
        scenario.CulturalContext.Should().Be(culturalContext);
        scenario.IsCulturallySignificant.Should().BeTrue();
        scenario.Priority.Should().Be(SynchronizationPriority.CulturalEvent);
    }

    [Theory]
    [InlineData(FailbackStrategy.Immediate)]
    [InlineData(FailbackStrategy.Gradual)]
    [InlineData(FailbackStrategy.Scheduled)]
    [InlineData(FailbackStrategy.CulturallyAware)]
    public void FailbackStrategy_AllStrategies_ShouldHaveValidConfiguration(FailbackStrategy strategy)
    {
        // Act
        var config = strategy.GetFailbackConfiguration();
        
        // Assert
        config.Should().NotBeNull();
        config.Strategy.Should().Be(strategy);
        config.EstimatedDuration.Should().BeGreaterThan(TimeSpan.Zero);
        config.RequiresApproval.Should().Be(strategy == FailbackStrategy.Scheduled || 
                                           strategy == FailbackStrategy.CulturallyAware);
    }

    [Theory]
    [InlineData(ReplicationStatus.Healthy)]
    [InlineData(ReplicationStatus.Warning)]
    [InlineData(ReplicationStatus.Critical)]
    [InlineData(ReplicationStatus.Failed)]
    [InlineData(ReplicationStatus.CulturalEventMode)]
    public void ReplicationStatus_AllStatuses_ShouldHaveCorrectHealthIndicators(ReplicationStatus status)
    {
        // Act
        var healthMetrics = status.GetHealthMetrics();
        
        // Assert
        healthMetrics.Should().NotBeNull();
        healthMetrics.Status.Should().Be(status);
        healthMetrics.IsHealthy.Should().Be(status == ReplicationStatus.Healthy || 
                                           status == ReplicationStatus.CulturalEventMode);
        healthMetrics.RequiresImmedateAction.Should().Be(status == ReplicationStatus.Critical || 
                                                        status == ReplicationStatus.Failed);
    }

    #endregion

    #region Integration and Validation Tests
    
    [Fact]
    public void DisasterRecoveryConfiguration_WithCulturalIntelligence_ShouldIntegrateCorrectly()
    {
        // Arrange
        var config = DisasterRecoveryConfiguration.CreateEnterpriseGrade("cultural-dr-config");
        var culturalEvent = RecoveryScenario.CreateCulturalEventRecovery("DiwaliCelebration", 
            TimeSpan.FromHours(2), TimeSpan.FromSeconds(30), "Hindu festival celebration");
        
        // Act
        config = config.WithCulturalEventSupport(culturalEvent);
        var validation = config.ValidateConfiguration();
        
        // Assert
        config.SupportsCulturalEvents.Should().BeTrue();
        config.CulturalRecoveryScenarios.Should().Contain(culturalEvent);
        validation.IsValid.Should().BeTrue();
        validation.IsEnterpriseGrade.Should().BeTrue();
        validation.CulturalComplianceLevel.Should().BeGreaterOrEqualTo(95);
    }

    [Fact]
    public void SynchronizationMetrics_AggregateResults_ShouldProvideComprehensiveMetrics()
    {
        // Arrange
        var results = new List<SynchronizationResult>
        {
            SynchronizationResult.Create("sync-1", true, 1000, TimeSpan.FromMinutes(1), 2),
            SynchronizationResult.Create("sync-2", true, 800, TimeSpan.FromMinutes(1.5), 1),
            SynchronizationResult.CreateFailed("sync-3", "Network error", 2)
        };
        
        // Act
        var metrics = SynchronizationMetrics.AggregateResults(results, MetricAggregationLevel.Enterprise);
        
        // Assert
        metrics.TotalSynchronizations.Should().Be(3);
        metrics.SuccessfulSynchronizations.Should().Be(2);
        metrics.FailedSynchronizations.Should().Be(1);
        metrics.SuccessRate.Should().Be(66.67, "Success rate should be calculated correctly");
        metrics.TotalItemsProcessed.Should().Be(1800);
        metrics.AverageProcessingTime.Should().Be(TimeSpan.FromMinutes(1.25));
        metrics.AggregationLevel.Should().Be(MetricAggregationLevel.Enterprise);
    }

    [Fact]
    public void ReadinessValidationResult_ForCulturalEventReadiness_ShouldValidateComprehensively()
    {
        // Arrange
        var scenario = RecoveryScenario.CreateCulturalEventRecovery("SinhalaNewYear", 
            TimeSpan.FromHours(6), TimeSpan.FromMinutes(5), "Traditional New Year celebration");
        var validationCriteria = new ValidationCriteria
        {
            RequireCulturalCompliance = true,
            MaxAcceptableDowntime = TimeSpan.FromMinutes(2),
            MinimumDataProtectionLevel = 99.9
        };
        
        // Act
        var validation = ReadinessValidationResult.ValidateScenarioReadiness(scenario, validationCriteria);
        
        // Assert
        validation.IsReady.Should().BeTrue("Cultural event scenarios should meet readiness requirements");
        validation.ValidationErrors.Should().BeEmpty();
        validation.CulturalComplianceScore.Should().BeGreaterOrEqualTo(95);
        validation.RecommendedActions.Should().NotBeEmpty();
        validation.EstimatedReadinessDate.Should().BeBefore(DateTime.UtcNow.AddDays(7));
    }

    #endregion
}