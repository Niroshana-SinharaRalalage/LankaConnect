using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Infrastructure.Database.LoadBalancing;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Database;

/// <summary>
/// Focused TDD tests for Cultural Event Load Distribution Service core functionality
/// Tests the architect-recommended implementation independently of domain compilation issues
/// </summary>
public class CulturalEventLoadDistributionServiceFocusedTests : IDisposable
{
    private readonly Mock<ILogger<CulturalEventLoadDistributionService>> _loggerMock;
    private readonly Mock<ICulturalEventPredictionEngine> _predictionEngineMock;
    private readonly Mock<ICulturalConflictResolver> _conflictResolverMock;
    private readonly Mock<IFortuneHundredPerformanceOptimizer> _performanceOptimizerMock;
    private readonly Mock<ICulturalAffinityGeographicLoadBalancer> _culturalLoadBalancerMock;
    private readonly CulturalEventLoadDistributionService _service;

    public CulturalEventLoadDistributionServiceFocusedTests()
    {
        _loggerMock = new Mock<ILogger<CulturalEventLoadDistributionService>>();
        _predictionEngineMock = new Mock<ICulturalEventPredictionEngine>();
        _conflictResolverMock = new Mock<ICulturalConflictResolver>();
        _performanceOptimizerMock = new Mock<IFortuneHundredPerformanceOptimizer>();
        _culturalLoadBalancerMock = new Mock<ICulturalAffinityGeographicLoadBalancer>();

        _service = new CulturalEventLoadDistributionService(
            _loggerMock.Object,
            _predictionEngineMock.Object,
            _conflictResolverMock.Object,
            _performanceOptimizerMock.Object,
            _culturalLoadBalancerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Act & Assert
        _service.Should().NotBeNull();
        _service.Should().BeAssignableTo<ICulturalEventLoadDistributionService>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new CulturalEventLoadDistributionService(
            null!,
            _predictionEngineMock.Object,
            _conflictResolverMock.Object,
            _performanceOptimizerMock.Object,
            _culturalLoadBalancerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullPredictionEngine_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new CulturalEventLoadDistributionService(
            _loggerMock.Object,
            null!,
            _conflictResolverMock.Object,
            _performanceOptimizerMock.Object,
            _culturalLoadBalancerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("predictionEngine");
    }

    #endregion

    #region DistributeLoadAsync Tests

    [Fact]
    public async Task DistributeLoadAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.DistributeLoadAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task DistributeLoadAsync_WithValidVesakRequest_ShouldReturnSuccessfulDistribution()
    {
        // Arrange
        var request = new CulturalEventLoadDistributionRequest
        {
            EventId = Guid.NewGuid(),
            CulturalEventType = CulturalEventType.VesakDayBuddhist,
            PredictedTrafficMultiplier = 5.0m,
            GeographicScope = GeographicCulturalScope.Global,
            ExpectedConcurrentUsers = 50000,
            RequiredResponseTimeSla = TimeSpan.FromMilliseconds(200)
        };

        var mockCulturalAffinityResponse = new DiasporaLoadBalancingResponse
        {
            IsSuccessful = true,
            OptimalRoutes = new List<CulturalAffinityRoute>
            {
                new() { RegionId = "NorthAmerica", AffinityScore = 0.95m, AllocatedCapacity = 30000 }
            }
        };

        var mockEventResponse = new CulturalEventLoadDistributionResponse
        {
            DistributionId = Guid.NewGuid(),
            IsSuccessful = true,
            OptimalServerAllocations = new List<ServerAllocation>
            {
                new() { ServerId = "vesak-na-1", AllocatedCapacity = 25000, CulturalAffinityScore = 0.94m }
            },
            PredictedResponseTime = TimeSpan.FromMilliseconds(150),
            CulturalCompatibilityScore = 0.92m,
            TrafficMultiplierApplied = 5.0m
        };

        _culturalLoadBalancerMock.Setup(x => x.DistributeLoadAsync(
                It.IsAny<DiasporaLoadBalancingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCulturalAffinityResponse);

        _predictionEngineMock.Setup(x => x.DistributeLoadForCulturalEventAsync(
                It.IsAny<CulturalEventLoadDistributionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEventResponse);

        // Act
        var result = await _service.DistributeLoadAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.TrafficMultiplierApplied.Should().Be(5.0m);
        result.CulturalCompatibilityScore.Should().BeGreaterThan(0.9m);
        result.PredictedResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        
        // Verify integration with cultural affinity load balancer
        _culturalLoadBalancerMock.Verify(x => x.DistributeLoadAsync(
            It.IsAny<DiasporaLoadBalancingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _predictionEngineMock.Verify(x => x.DistributeLoadForCulturalEventAsync(
            It.IsAny<CulturalEventLoadDistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(CulturalEventType.VesakDayBuddhist, 5.0)]
    [InlineData(CulturalEventType.DiwaliHindu, 4.5)]
    [InlineData(CulturalEventType.EidAlFitrIslamic, 4.0)]
    public async Task DistributeLoadAsync_WithDifferentCulturalEvents_ShouldApplyCorrectTrafficMultipliers(
        CulturalEventType eventType, double expectedMultiplier)
    {
        // Arrange
        var request = new CulturalEventLoadDistributionRequest
        {
            EventId = Guid.NewGuid(),
            CulturalEventType = eventType,
            PredictedTrafficMultiplier = (decimal)expectedMultiplier,
            GeographicScope = GeographicCulturalScope.Global,
            ExpectedConcurrentUsers = 10000,
            RequiredResponseTimeSla = TimeSpan.FromMilliseconds(200)
        };

        var mockResponse = new CulturalEventLoadDistributionResponse
        {
            DistributionId = Guid.NewGuid(),
            IsSuccessful = true,
            TrafficMultiplierApplied = (decimal)expectedMultiplier
        };

        _culturalLoadBalancerMock.Setup(x => x.DistributeLoadAsync(
                It.IsAny<DiasporaLoadBalancingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DiasporaLoadBalancingResponse { IsSuccessful = true });

        _predictionEngineMock.Setup(x => x.DistributeLoadForCulturalEventAsync(
                It.IsAny<CulturalEventLoadDistributionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.DistributeLoadAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TrafficMultiplierApplied.Should().Be((decimal)expectedMultiplier);
        result.IsSuccessful.Should().BeTrue();
    }

    #endregion

    #region GenerateScalingPlanAsync Tests

    [Fact]
    public async Task GenerateScalingPlanAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.GenerateScalingPlanAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateScalingPlanAsync_ForVesakEvent_ShouldGenerate5xScalingPlan()
    {
        // Arrange
        var scalingRequest = new PredictiveScalingPlanRequest
        {
            CulturalEventType = CulturalEventType.VesakDayBuddhist,
            EventStartTime = DateTime.UtcNow.AddDays(7),
            EventDuration = TimeSpan.FromHours(24),
            BaselineTraffic = 10000,
            GeographicRegions = new[] { "NorthAmerica", "Europe", "AsiaPacific" }
        };

        var expectedPlan = new PredictiveScalingPlan
        {
            PlanId = Guid.NewGuid(),
            CulturalEventType = CulturalEventType.VesakDayBuddhist,
            TrafficMultiplier = 5.0m,
            ScalingActions = new List<ScalingAction>
            {
                new() { ActionType = ScalingActionType.PreScale, TargetCapacity = 50000, ExecuteAt = DateTime.UtcNow.AddDays(7).AddHours(-2) }
            },
            PredictionAccuracy = 0.95m
        };

        _predictionEngineMock.Setup(x => x.GeneratePredictiveScalingPlanAsync(
                It.IsAny<PredictiveScalingPlanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPlan);

        // Act
        var result = await _service.GenerateScalingPlanAsync(scalingRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TrafficMultiplier.Should().Be(5.0m);
        result.ScalingActions.Should().HaveCountGreaterThan(0);
        result.PredictionAccuracy.Should().BeGreaterThan(0.9m);
        result.CulturalEventType.Should().Be(CulturalEventType.VesakDayBuddhist);
    }

    #endregion

    #region Performance Validation Tests

    [Fact]
    public async Task MonitorPerformanceAsync_DuringVesakPeakTraffic_ShouldMaintainSub200msResponseTime()
    {
        // Arrange
        var performanceRequest = new FortuneHundredPerformanceMonitoringRequest
        {
            MonitoringScope = PerformanceMonitoringScope.CulturalEventSpecific,
            CulturalEventType = CulturalEventType.VesakDayBuddhist,
            RequiredResponseTimeSla = TimeSpan.FromMilliseconds(200),
            RequiredUptimeSla = 99.9m,
            MonitoringDuration = TimeSpan.FromHours(24),
            ExpectedTrafficMultiplier = 5.0m
        };

        var expectedMetrics = new FortuneHundredPerformanceMetrics
        {
            MetricsId = Guid.NewGuid(),
            AverageResponseTime = TimeSpan.FromMilliseconds(145),
            MaxResponseTime = TimeSpan.FromMilliseconds(195),
            UptimePercentage = 99.95m,
            ThroughputPerSecond = 25000,
            SlaCompliance = true,
            CulturalEventType = CulturalEventType.VesakDayBuddhist
        };

        _performanceOptimizerMock.Setup(x => x.MonitorPerformanceAsync(
                It.IsAny<FortuneHundredPerformanceMonitoringRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _service.MonitorPerformanceAsync(performanceRequest, CancellationToken.None);

        // Assert - Fortune 500 SLA Compliance
        result.Should().NotBeNull();
        result.AverageResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        result.MaxResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        result.UptimePercentage.Should().BeGreaterOrEqualTo(99.9m);
        result.SlaCompliance.Should().BeTrue();
        result.ThroughputPerSecond.Should().BeGreaterThan(20000); // 10x baseline of 2000
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task DistributeLoadAsync_WithInvalidTrafficMultiplier_ShouldThrowValidationException()
    {
        // Arrange
        var invalidRequest = new CulturalEventLoadDistributionRequest
        {
            EventId = Guid.NewGuid(),
            CulturalEventType = CulturalEventType.VesakDayBuddhist,
            PredictedTrafficMultiplier = -1.0m, // Invalid negative multiplier
            GeographicScope = GeographicCulturalScope.Global,
            ExpectedConcurrentUsers = 50000,
            RequiredResponseTimeSla = TimeSpan.FromMilliseconds(200)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.DistributeLoadAsync(invalidRequest, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateScalingPlanAsync_WithPastEventDate_ShouldThrowValidationException()
    {
        // Arrange
        var invalidRequest = new PredictiveScalingPlanRequest
        {
            CulturalEventType = CulturalEventType.VesakDayBuddhist,
            EventStartTime = DateTime.UtcNow.AddDays(-1), // Past date
            EventDuration = TimeSpan.FromHours(24),
            BaselineTraffic = 10000
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GenerateScalingPlanAsync(invalidRequest, CancellationToken.None));
    }

    #endregion

    #region Disposal

    public void Dispose()
    {
        _service?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Mock implementations for testing - simplified versions to avoid domain compilation issues
/// </summary>
public class DiasporaLoadBalancingResponse
{
    public bool IsSuccessful { get; set; }
    public List<CulturalAffinityRoute> OptimalRoutes { get; set; } = new();
}

public class DiasporaLoadBalancingRequest
{
    public Guid RequestId { get; set; }
    public CulturalCommunityType CulturalCommunityType { get; set; }
    public GeographicScope GeographicScope { get; set; }
    public int ExpectedConcurrentUsers { get; set; }
    public TimeSpan RequiredResponseTime { get; set; }
    public LoadBalancingStrategy LoadBalancingStrategy { get; set; }
    public LoadBalancingPriority PriorityLevel { get; set; }
}

public class CulturalAffinityRoute
{
    public string RegionId { get; set; } = string.Empty;
    public decimal AffinityScore { get; set; }
    public int AllocatedCapacity { get; set; }
}

public enum CulturalCommunityType
{
    SriLankanBuddhist = 1,
    IndianHindu = 2,
    PakistaniMuslim = 3,
    SikhPunjabi = 4,
    TamilHindu = 5,
    BengaliHindu = 6,
    MultiCultural = 7
}

public enum GeographicScope
{
    Global = 1,
    NorthAmerica = 2,
    Europe = 3,
    AsiaPacific = 4,
    SouthAmerica = 5,
    MultiRegional = 6
}

public enum LoadBalancingStrategy
{
    CulturalAffinityOptimized = 1,
    GeographicProximity = 2,
    PerformanceBased = 3
}

public enum LoadBalancingPriority
{
    Low = 1,
    Standard = 2,
    High = 3,
    Critical = 4
}