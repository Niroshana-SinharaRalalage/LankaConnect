using System;
using System.Collections.Generic;
using Xunit;
using LankaConnect.Domain.Common.Performance;

namespace LankaConnect.Domain.Tests.Common.Performance
{
    public class CostAwareScalingTypesTests
    {
        [Fact]
        public void CostConstraints_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var constraints = new CostConstraints();

            // Assert
            Assert.NotEqual(Guid.Empty, constraints.Id);
            Assert.NotNull(constraints.ConstraintId);
            Assert.NotNull(constraints.CostModel);
            Assert.NotNull(constraints.ResourceCosts);
            Assert.True(constraints.IsEnforced);
            Assert.True(constraints.CreatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void CostAwareScalingDecision_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var decision = new CostAwareScalingDecision();

            // Assert
            Assert.NotEqual(Guid.Empty, decision.Id);
            Assert.NotNull(decision.DecisionId);
            Assert.NotNull(decision.PerformanceRequirement);
            Assert.NotNull(decision.CostConstraints);
            Assert.NotNull(decision.DecisionReason);
            Assert.NotNull(decision.AlternativeOptions);
            Assert.True(decision.DecisionTime <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ApplicationTier_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var tier = new ApplicationTier();

            // Assert
            Assert.NotEqual(Guid.Empty, tier.Id);
            Assert.NotNull(tier.TierId);
            Assert.NotNull(tier.TierName);
            Assert.NotNull(tier.TierType);
            Assert.NotNull(tier.Configuration);
            Assert.NotNull(tier.Dependencies);
            Assert.True(tier.IsActive);
            Assert.True(tier.CreatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ScalingCoordinationStrategy_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var strategy = new ScalingCoordinationStrategy();

            // Assert
            Assert.NotEqual(Guid.Empty, strategy.Id);
            Assert.NotNull(strategy.StrategyId);
            Assert.NotNull(strategy.StrategyName);
            Assert.NotNull(strategy.CoordinationType);
            Assert.NotNull(strategy.TierPriorities);
            Assert.NotNull(strategy.CoordinationRules);
            Assert.True(strategy.IsEnabled);
            Assert.True(strategy.CreatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void MultiTierScalingCoordination_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var coordination = new MultiTierScalingCoordination();

            // Assert
            Assert.NotEqual(Guid.Empty, coordination.Id);
            Assert.NotNull(coordination.CoordinationId);
            Assert.NotNull(coordination.ApplicationTiers);
            Assert.NotNull(coordination.Strategy);
            Assert.NotNull(coordination.TierStatuses);
            Assert.NotNull(coordination.CoordinationActions);
            Assert.NotNull(coordination.OverallStatus);
            Assert.True(coordination.CoordinatedAt <= DateTimeOffset.UtcNow);
        }

        [Theory]
        [InlineData(100.50)]
        [InlineData(1000.75)]
        [InlineData(5000.00)]
        public void CostConstraints_ShouldAcceptValidCostValues(decimal cost)
        {
            // Arrange
            var constraints = new CostConstraints();

            // Act
            constraints.MaxHourlyCost = cost;
            constraints.MaxDailyCost = cost * 24;
            constraints.MaxMonthlyCost = cost * 24 * 30;

            // Assert
            Assert.Equal(cost, constraints.MaxHourlyCost);
            Assert.Equal(cost * 24, constraints.MaxDailyCost);
            Assert.Equal(cost * 24 * 30, constraints.MaxMonthlyCost);
        }

        [Theory]
        [InlineData(true, 5)]
        [InlineData(false, 0)]
        public void CostAwareScalingDecision_ShouldReflectScalingApprovalStatus(bool approved, int instances)
        {
            // Arrange
            var decision = new CostAwareScalingDecision();

            // Act
            decision.ScalingApproved = approved;
            decision.RecommendedInstances = instances;

            // Assert
            Assert.Equal(approved, decision.ScalingApproved);
            Assert.Equal(instances, decision.RecommendedInstances);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void ApplicationTier_ShouldAcceptValidPriority(int priority)
        {
            // Arrange
            var tier = new ApplicationTier();

            // Act
            tier.Priority = priority;

            // Assert
            Assert.Equal(priority, tier.Priority);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ScalingCoordinationStrategy_ShouldAcceptParallelExecutionSetting(bool parallelExecution)
        {
            // Arrange
            var strategy = new ScalingCoordinationStrategy();

            // Act
            strategy.ParallelExecution = parallelExecution;

            // Assert
            Assert.Equal(parallelExecution, strategy.ParallelExecution);
        }
    }
}