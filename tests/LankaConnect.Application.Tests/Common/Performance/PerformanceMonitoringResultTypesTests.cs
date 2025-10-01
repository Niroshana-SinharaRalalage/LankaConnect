using FluentAssertions;
using LankaConnect.Application.Common.Performance;
using LankaConnect.Domain.Common;
using Xunit;

namespace LankaConnect.Application.Tests.Common.Performance;

/// <summary>
/// TDD RED Phase: Performance Monitoring Result Types Tests
/// Testing comprehensive performance monitoring result patterns for Cultural Intelligence platform
/// </summary>
public class PerformanceMonitoringResultTypesTests
{
    #region MultiRegionPerformanceCoordination Tests (RED Phase)

    [Fact]
    public void MultiRegionPerformanceCoordination_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var coordinationMetrics = new PerformanceCoordinationMetrics
        {
            TotalRegions = 5,
            ActiveRegions = 5,
            AverageLatencyMs = 45.7m,
            ThroughputPerSecond = 10000,
            CoordinationSuccessRate = 98.5m,
            LastCoordinationTimestamp = DateTime.UtcNow
        };

        // Act
        var result = MultiRegionPerformanceCoordination.Success(coordinationMetrics);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.CoordinationMetrics.Should().Be(coordinationMetrics);
        result.IsHealthy.Should().BeTrue();
        result.TotalRegions.Should().Be(5);
        result.SuccessRate.Should().Be(98.5m);
    }

    [Fact]
    public void MultiRegionPerformanceCoordination_CreateFailure_ShouldReturnFailedResult()
    {
        // Arrange
        var error = "Cross-region coordination failed - network partition detected";

        // Act
        var result = MultiRegionPerformanceCoordination.Failure(error);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.CoordinationMetrics.Should().BeNull();
        result.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public void MultiRegionPerformanceCoordination_WithDegradedPerformance_ShouldIndicateIssues()
    {
        // Arrange
        var degradedMetrics = new PerformanceCoordinationMetrics
        {
            TotalRegions = 5,
            ActiveRegions = 3,
            AverageLatencyMs = 250.0m,
            ThroughputPerSecond = 2000,
            CoordinationSuccessRate = 75.0m,
            LastCoordinationTimestamp = DateTime.UtcNow
        };

        // Act
        var result = MultiRegionPerformanceCoordination.Success(degradedMetrics);

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.CoordinationMetrics.ActiveRegions.Should().Be(3);
        result.SuccessRate.Should().Be(75.0m);
    }

    #endregion

    #region RegionSyncResult Tests (RED Phase)

    [Fact]
    public void RegionSyncResult_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var syncSummary = new RegionSyncSummary
        {
            SourceRegion = "US-East",
            TargetRegion = "EU-West",
            RecordsSynced = 50000,
            SyncDurationMs = 12000,
            DataTransferredGB = 2.5m,
            SyncSuccessRate = 100.0m,
            LastSyncTimestamp = DateTime.UtcNow
        };

        // Act
        var result = RegionSyncResult.Success(syncSummary);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.SyncSummary.Should().Be(syncSummary);
        result.IsSynced.Should().BeTrue();
        result.RecordsSynced.Should().Be(50000);
        result.SyncDurationSeconds.Should().Be(12);
    }

    [Fact]
    public void RegionSyncResult_WithPartialSync_ShouldIndicateIncomplete()
    {
        // Arrange
        var partialSyncSummary = new RegionSyncSummary
        {
            SourceRegion = "Asia-Pacific",
            TargetRegion = "US-West",
            RecordsSynced = 25000,
            SyncDurationMs = 30000,
            DataTransferredGB = 1.8m,
            SyncSuccessRate = 85.0m,
            LastSyncTimestamp = DateTime.UtcNow.AddMinutes(-5)
        };

        // Act
        var result = RegionSyncResult.Success(partialSyncSummary);

        // Assert
        result.IsSynced.Should().BeFalse(); // Based on success rate < 95%
        result.SyncSummary.SyncSuccessRate.Should().Be(85.0m);
    }

    #endregion

    #region RegionalPerformanceAnalysis Tests (RED Phase)

    [Fact]
    public void RegionalPerformanceAnalysis_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var analysisData = new PerformanceAnalysisData
        {
            RegionName = "Europe-Central",
            AnalysisPeriodHours = 24,
            TotalRequests = 1000000,
            AverageResponseTimeMs = 89.5m,
            P95ResponseTimeMs = 150.0m,
            ErrorRate = 0.02m,
            CulturalEventProcessingRate = 500,
            DiasporaEngagementScore = 94.2m
        };

        // Act
        var result = RegionalPerformanceAnalysis.Success(analysisData);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.AnalysisData.Should().Be(analysisData);
        result.IsPerformant.Should().BeTrue();
        result.RegionName.Should().Be("Europe-Central");
        result.EngagementScore.Should().Be(94.2m);
    }

    [Fact]
    public void RegionalPerformanceAnalysis_WithPoorPerformance_ShouldIndicateIssues()
    {
        // Arrange
        var poorPerformanceData = new PerformanceAnalysisData
        {
            RegionName = "Test-Region",
            AnalysisPeriodHours = 1,
            TotalRequests = 10000,
            AverageResponseTimeMs = 500.0m,
            P95ResponseTimeMs = 2000.0m,
            ErrorRate = 5.0m,
            CulturalEventProcessingRate = 50,
            DiasporaEngagementScore = 60.0m
        };

        // Act
        var result = RegionalPerformanceAnalysis.Success(poorPerformanceData);

        // Assert
        result.IsPerformant.Should().BeFalse();
        result.AnalysisData.ErrorRate.Should().Be(5.0m);
        result.EngagementScore.Should().Be(60.0m);
    }

    #endregion

    #region PerformanceComparisonMetrics Tests (RED Phase)

    [Fact]
    public void PerformanceComparisonMetrics_Create_ShouldHaveValidProperties()
    {
        // Arrange & Act
        var metrics = new PerformanceComparisonMetrics
        {
            BaselineRegion = "US-East",
            ComparisonRegion = "EU-West",
            LatencyDifferenceMs = 25.5m,
            ThroughputDifferencePercent = -12.3m,
            ErrorRateDifferencePercent = 0.5m,
            CulturalAccuracyDifferencePercent = 2.1m,
            OverallPerformanceScore = 87.5m
        };

        // Assert
        metrics.Should().NotBeNull();
        metrics.BaselineRegion.Should().Be("US-East");
        metrics.ComparisonRegion.Should().Be("EU-West");
        metrics.OverallPerformanceScore.Should().Be(87.5m);
        metrics.HasSignificantDifference.Should().BeTrue(); // Based on thresholds
    }

    [Fact]
    public void PerformanceComparisonMetrics_WithMinimalDifferences_ShouldNotBeSignificant()
    {
        // Arrange & Act
        var metrics = new PerformanceComparisonMetrics
        {
            BaselineRegion = "Region-A",
            ComparisonRegion = "Region-B",
            LatencyDifferenceMs = 2.0m,
            ThroughputDifferencePercent = 1.0m,
            ErrorRateDifferencePercent = 0.1m,
            CulturalAccuracyDifferencePercent = 0.5m,
            OverallPerformanceScore = 95.0m
        };

        // Assert
        metrics.HasSignificantDifference.Should().BeFalse();
        metrics.OverallPerformanceScore.Should().Be(95.0m);
    }

    #endregion

    #region Integration Tests (RED Phase)

    [Fact]
    public void PerformanceResults_CanChainTogether_ShouldWork()
    {
        // Arrange
        var coordinationResult = MultiRegionPerformanceCoordination.Success(new PerformanceCoordinationMetrics
        {
            TotalRegions = 3,
            ActiveRegions = 3,
            AverageLatencyMs = 50.0m,
            CoordinationSuccessRate = 99.0m
        });

        var syncResult = RegionSyncResult.Success(new RegionSyncSummary
        {
            SourceRegion = "US-East",
            TargetRegion = "EU-West",
            RecordsSynced = 10000,
            SyncSuccessRate = 98.0m
        });

        // Act
        var combinedHealthy = CombinePerformanceResults(coordinationResult, syncResult);

        // Assert
        combinedHealthy.Should().BeTrue();
    }

    #endregion

    private bool CombinePerformanceResults(MultiRegionPerformanceCoordination coordination, RegionSyncResult sync)
    {
        return coordination.IsSuccess && sync.IsSuccess && 
               coordination.IsHealthy && sync.IsSynced;
    }
}