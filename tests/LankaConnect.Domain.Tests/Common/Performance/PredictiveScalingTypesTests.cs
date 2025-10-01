using System;
using System.Collections.Generic;
using Xunit;
using LankaConnect.Domain.Common.Performance;

namespace LankaConnect.Domain.Tests.Common.Performance
{
    public class PredictiveScalingTypesTests
    {
        [Fact]
        public void PerformanceForecast_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var forecast = new PerformanceForecast();

            // Assert
            Assert.NotEqual(Guid.Empty, forecast.Id);
            Assert.NotNull(forecast.ForecastId);
            Assert.NotNull(forecast.PredictedMetrics);
            Assert.NotNull(forecast.InfluencingFactors);
            Assert.True(forecast.IsActive);
            Assert.True(forecast.CreatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void PredictiveScalingPolicy_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var policy = new PredictiveScalingPolicy();

            // Assert
            Assert.NotEqual(Guid.Empty, policy.Id);
            Assert.NotNull(policy.PolicyId);
            Assert.NotNull(policy.PolicyName);
            Assert.NotNull(policy.PolicyParameters);
            Assert.True(policy.IsEnabled);
            Assert.True(policy.CreatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void PredictiveScalingCoordination_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var coordination = new PredictiveScalingCoordination();

            // Assert
            Assert.NotEqual(Guid.Empty, coordination.Id);
            Assert.NotNull(coordination.CoordinationId);
            Assert.NotNull(coordination.Forecast);
            Assert.NotNull(coordination.Policy);
            Assert.NotNull(coordination.RecommendedActions);
            Assert.NotNull(coordination.CoordinationResults);
            Assert.True(coordination.CoordinatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ScalingMetrics_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var metrics = new ScalingMetrics();

            // Assert
            Assert.NotEqual(Guid.Empty, metrics.Id);
            Assert.NotNull(metrics.MetricsId);
            Assert.NotNull(metrics.CustomMetrics);
            Assert.True(metrics.IsHealthy);
            Assert.True(metrics.CollectionTime <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void AnomalyDetectionThreshold_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var threshold = new AnomalyDetectionThreshold();

            // Assert
            Assert.NotEqual(Guid.Empty, threshold.Id);
            Assert.NotNull(threshold.ThresholdId);
            Assert.NotNull(threshold.MetricName);
            Assert.NotNull(threshold.DetectionAlgorithm);
            Assert.True(threshold.IsEnabled);
            Assert.True(threshold.CreatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ScalingAnomalyDetectionResult_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var result = new ScalingAnomalyDetectionResult();

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.NotNull(result.ResultId);
            Assert.NotNull(result.ScalingMetrics);
            Assert.NotNull(result.Threshold);
            Assert.NotNull(result.DetectedAnomalies);
            Assert.NotNull(result.RecommendedActions);
            Assert.True(result.DetectedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ScalingEvent_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var scalingEvent = new ScalingEvent();

            // Assert
            Assert.NotEqual(Guid.Empty, scalingEvent.Id);
            Assert.NotNull(scalingEvent.EventId);
            Assert.NotNull(scalingEvent.EventType);
            Assert.NotNull(scalingEvent.TriggerReason);
            Assert.NotNull(scalingEvent.EventData);
            Assert.NotNull(scalingEvent.ErrorMessage);
            Assert.True(scalingEvent.EventTime <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ScalingEffectivenessValidation_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var validation = new ScalingEffectivenessValidation();

            // Assert
            Assert.NotEqual(Guid.Empty, validation.Id);
            Assert.NotNull(validation.ValidationId);
            Assert.NotNull(validation.EvaluatedEvents);
            Assert.NotNull(validation.ImprovementRecommendations);
            Assert.NotNull(validation.PerformanceMetrics);
            Assert.NotNull(validation.ValidationSummary);
            Assert.True(validation.ValidatedAt <= DateTimeOffset.UtcNow);
        }

        [Theory]
        [InlineData(0.5)]
        [InlineData(0.8)]
        [InlineData(0.95)]
        public void PerformanceForecast_ShouldAcceptValidConfidenceLevel(double confidenceLevel)
        {
            // Arrange
            var forecast = new PerformanceForecast();

            // Act
            forecast.ConfidenceLevel = confidenceLevel;

            // Assert
            Assert.Equal(confidenceLevel, forecast.ConfidenceLevel);
        }

        [Theory]
        [InlineData(1, 10)]
        [InlineData(5, 50)]
        [InlineData(10, 100)]
        public void PredictiveScalingPolicy_ShouldAcceptValidInstanceLimits(int minInstances, int maxInstances)
        {
            // Arrange
            var policy = new PredictiveScalingPolicy();

            // Act
            policy.MinInstances = minInstances;
            policy.MaxInstances = maxInstances;

            // Assert
            Assert.Equal(minInstances, policy.MinInstances);
            Assert.Equal(maxInstances, policy.MaxInstances);
            Assert.True(policy.MinInstances <= policy.MaxInstances);
        }
    }
}