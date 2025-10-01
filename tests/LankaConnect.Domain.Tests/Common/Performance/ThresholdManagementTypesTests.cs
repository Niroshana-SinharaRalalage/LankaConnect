using System;
using System.Collections.Generic;
using Xunit;
using LankaConnect.Domain.Common.Performance;

namespace LankaConnect.Domain.Tests.Common.Performance
{
    public class ThresholdManagementTypesTests
    {
        [Fact]
        public void DynamicThresholdConfiguration_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var config = new DynamicThresholdConfiguration();

            // Assert
            Assert.NotEqual(Guid.Empty, config.Id);
            Assert.NotNull(config.ConfigurationId);
            Assert.NotNull(config.MetricName);
            Assert.True(config.IsDynamic);
            Assert.NotNull(config.AdjustmentAlgorithm);
            Assert.NotNull(config.Parameters);
            Assert.True(config.IsEnabled);
            Assert.True(config.CreatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ThresholdValidationResult_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var result = new ThresholdValidationResult();

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.NotNull(result.ValidationId);
            Assert.NotNull(result.ValidatedThresholds);
            Assert.NotNull(result.ValidationScores);
            Assert.NotNull(result.ValidationIssues);
            Assert.NotNull(result.RecommendedAdjustments);
            Assert.NotNull(result.ValidationSummary);
            Assert.True(result.ValidatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ThresholdOptimizationObjective_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var objective = new ThresholdOptimizationObjective();

            // Assert
            Assert.NotEqual(Guid.Empty, objective.Id);
            Assert.NotNull(objective.ObjectiveId);
            Assert.NotNull(objective.ObjectiveName);
            Assert.NotNull(objective.OptimizationGoal);
            Assert.NotNull(objective.TargetMetrics);
            Assert.NotNull(objective.OptimizationConstraints);
            Assert.True(objective.IsActive);
            Assert.True(objective.CreatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ThresholdRecommendation_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var recommendation = new ThresholdRecommendation();

            // Assert
            Assert.NotEqual(Guid.Empty, recommendation.Id);
            Assert.NotNull(recommendation.RecommendationId);
            Assert.NotNull(recommendation.MetricName);
            Assert.NotNull(recommendation.RecommendationReason);
            Assert.NotNull(recommendation.RiskFactors);
            Assert.NotNull(recommendation.ImplementationGuidance);
            Assert.True(recommendation.GeneratedAt <= DateTimeOffset.UtcNow);
        }

        [Theory]
        [InlineData(0.1, 0.9)]
        [InlineData(0.3, 0.7)]
        [InlineData(0.5, 0.8)]
        public void DynamicThresholdConfiguration_ShouldAcceptValidThresholdRange(double minThreshold, double maxThreshold)
        {
            // Arrange
            var config = new DynamicThresholdConfiguration();

            // Act
            config.MinThreshold = minThreshold;
            config.MaxThreshold = maxThreshold;

            // Assert
            Assert.Equal(minThreshold, config.MinThreshold);
            Assert.Equal(maxThreshold, config.MaxThreshold);
            Assert.True(config.MinThreshold < config.MaxThreshold);
        }

        [Theory]
        [InlineData(0.1)]
        [InlineData(0.5)]
        [InlineData(0.9)]
        public void DynamicThresholdConfiguration_ShouldAcceptValidVariabilityFactor(double variabilityFactor)
        {
            // Arrange
            var config = new DynamicThresholdConfiguration();

            // Act
            config.VariabilityFactor = variabilityFactor;

            // Assert
            Assert.Equal(variabilityFactor, config.VariabilityFactor);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ThresholdValidationResult_ShouldReflectValidationStatus(bool isValid)
        {
            // Arrange
            var result = new ThresholdValidationResult();

            // Act
            result.IsValid = isValid;

            // Assert
            Assert.Equal(isValid, result.IsValid);
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(5.0)]
        [InlineData(10.0)]
        public void ThresholdOptimizationObjective_ShouldAcceptValidPriority(double priority)
        {
            // Arrange
            var objective = new ThresholdOptimizationObjective();

            // Act
            objective.Priority = priority;

            // Assert
            Assert.Equal(priority, objective.Priority);
        }

        [Theory]
        [InlineData(50.0, 75.0)]
        [InlineData(100.0, 120.0)]
        [InlineData(200.0, 250.0)]
        public void ThresholdRecommendation_ShouldAcceptValidThresholdValues(double current, double recommended)
        {
            // Arrange
            var recommendation = new ThresholdRecommendation();

            // Act
            recommendation.CurrentThreshold = current;
            recommendation.RecommendedThreshold = recommended;

            // Assert
            Assert.Equal(current, recommendation.CurrentThreshold);
            Assert.Equal(recommended, recommendation.RecommendedThreshold);
        }

        [Theory]
        [InlineData(0.1)]
        [InlineData(0.5)]
        [InlineData(0.9)]
        public void ThresholdRecommendation_ShouldAcceptValidConfidenceLevel(double confidenceLevel)
        {
            // Arrange
            var recommendation = new ThresholdRecommendation();

            // Act
            recommendation.ConfidenceLevel = confidenceLevel;

            // Assert
            Assert.Equal(confidenceLevel, recommendation.ConfidenceLevel);
        }
    }
}