using System;
using System.Collections.Generic;
using Xunit;
using LankaConnect.Domain.Common.Performance;

namespace LankaConnect.Domain.Tests.Common.Performance
{
    public class DistributedTracingTypesTests
    {
        [Fact]
        public void DistributedTracingMetrics_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var metrics = new DistributedTracingMetrics();

            // Assert
            Assert.NotEqual(Guid.Empty, metrics.Id);
            Assert.NotNull(metrics.MetricsId);
            Assert.NotNull(metrics.TraceId);
            Assert.NotNull(metrics.SpanId);
            Assert.NotNull(metrics.ServiceName);
            Assert.NotNull(metrics.OperationName);
            Assert.NotNull(metrics.Tags);
            Assert.NotNull(metrics.Logs);
            Assert.NotNull(metrics.Status);
            Assert.NotNull(metrics.CustomMetrics);
            Assert.NotNull(metrics.ChildSpanIds);
            Assert.True(metrics.StartTime <= DateTimeOffset.UtcNow);
            Assert.True(metrics.EndTime <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void SyntheticTransaction_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var transaction = new SyntheticTransaction();

            // Assert
            Assert.NotEqual(Guid.Empty, transaction.Id);
            Assert.NotNull(transaction.TransactionId);
            Assert.NotNull(transaction.TransactionName);
            Assert.NotNull(transaction.TransactionType);
            Assert.NotNull(transaction.Steps);
            Assert.NotNull(transaction.Parameters);
            Assert.True(transaction.IsEnabled);
            Assert.NotNull(transaction.Endpoints);
            Assert.NotNull(transaction.Headers);
            Assert.True(transaction.CreatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void SyntheticTransactionResults_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var results = new SyntheticTransactionResults();

            // Assert
            Assert.NotEqual(Guid.Empty, results.Id);
            Assert.NotNull(results.ResultsId);
            Assert.NotNull(results.ExecutedTransactions);
            Assert.NotNull(results.TransactionResults);
            Assert.NotNull(results.ExecutionTimes);
            Assert.NotNull(results.ErrorMessages);
            Assert.NotNull(results.FailedTransactions);
            Assert.NotNull(results.ExecutionSummary);
            Assert.True(results.ExecutedAt <= DateTimeOffset.UtcNow);
        }

        [Theory]
        [InlineData("trace-123", "span-456")]
        [InlineData("trace-789", "span-101")]
        public void DistributedTracingMetrics_ShouldAcceptValidTraceAndSpanIds(string traceId, string spanId)
        {
            // Arrange
            var metrics = new DistributedTracingMetrics();

            // Act
            metrics.TraceId = traceId;
            metrics.SpanId = spanId;

            // Assert
            Assert.Equal(traceId, metrics.TraceId);
            Assert.Equal(spanId, metrics.SpanId);
        }

        [Theory]
        [InlineData("UserService", "GetUser")]
        [InlineData("OrderService", "CreateOrder")]
        [InlineData("PaymentService", "ProcessPayment")]
        public void DistributedTracingMetrics_ShouldAcceptValidServiceAndOperationNames(string serviceName, string operationName)
        {
            // Arrange
            var metrics = new DistributedTracingMetrics();

            // Act
            metrics.ServiceName = serviceName;
            metrics.OperationName = operationName;

            // Assert
            Assert.Equal(serviceName, metrics.ServiceName);
            Assert.Equal(operationName, metrics.OperationName);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DistributedTracingMetrics_ShouldReflectErrorStatus(bool hasErrors)
        {
            // Arrange
            var metrics = new DistributedTracingMetrics();

            // Act
            metrics.HasErrors = hasErrors;

            // Assert
            Assert.Equal(hasErrors, metrics.HasErrors);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(15)]
        public void SyntheticTransaction_ShouldAcceptValidFrequency(int frequencyMinutes)
        {
            // Arrange
            var transaction = new SyntheticTransaction();

            // Act
            transaction.FrequencyMinutes = frequencyMinutes;

            // Assert
            Assert.Equal(frequencyMinutes, transaction.FrequencyMinutes);
        }

        [Theory]
        [InlineData(100, 500)]
        [InlineData(250, 1000)]
        [InlineData(1000, 5000)]
        public void SyntheticTransaction_ShouldAcceptValidExpectedDuration(int milliseconds, int maxMilliseconds)
        {
            // Arrange
            var transaction = new SyntheticTransaction();
            var expectedDuration = TimeSpan.FromMilliseconds(milliseconds);

            // Act
            transaction.ExpectedDuration = expectedDuration;

            // Assert
            Assert.Equal(expectedDuration, transaction.ExpectedDuration);
            Assert.True(transaction.ExpectedDuration.TotalMilliseconds < maxMilliseconds);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        public void SyntheticTransactionResults_ShouldAcceptValidSuccessRate(double successRate)
        {
            // Arrange
            var results = new SyntheticTransactionResults();

            // Act
            results.OverallSuccessRate = successRate;

            // Assert
            Assert.Equal(successRate, results.OverallSuccessRate);
            Assert.True(results.OverallSuccessRate >= 0.0 && results.OverallSuccessRate <= 1.0);
        }

        [Fact]
        public void DistributedTracingMetrics_ShouldCalculateDurationCorrectly()
        {
            // Arrange
            var metrics = new DistributedTracingMetrics();
            var startTime = DateTimeOffset.UtcNow.AddSeconds(-10);
            var endTime = DateTimeOffset.UtcNow;

            // Act
            metrics.StartTime = startTime;
            metrics.EndTime = endTime;
            metrics.Duration = endTime - startTime;

            // Assert
            Assert.Equal(endTime - startTime, metrics.Duration);
            Assert.True(metrics.Duration.TotalSeconds >= 0);
        }

        [Fact]
        public void SyntheticTransaction_ShouldMaintainStepsOrder()
        {
            // Arrange
            var transaction = new SyntheticTransaction();
            var steps = new List<string> { "Step1", "Step2", "Step3" };

            // Act
            transaction.Steps = steps;

            // Assert
            Assert.Equal(steps.Count, transaction.Steps.Count);
            for (int i = 0; i < steps.Count; i++)
            {
                Assert.Equal(steps[i], transaction.Steps[i]);
            }
        }
    }
}