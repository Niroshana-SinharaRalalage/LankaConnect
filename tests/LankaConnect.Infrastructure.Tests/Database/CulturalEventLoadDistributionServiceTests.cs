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
/// TDD RED phase comprehensive test suite for Cultural Event Load Distribution Service
/// Implements architect-recommended testing strategy for cultural event scenarios:
/// - Vesak Traffic Spike Testing (5x load simulation with SLA validation)
/// - Multi-Cultural Overlap Testing (Diwali-Eid conflict resolution validation)
/// - Fortune 500 SLA Testing (continuous performance monitoring under extreme load)
/// </summary>
public class CulturalEventLoadDistributionServiceTests : IDisposable
{
    private readonly Mock<ILogger<CulturalEventLoadDistributionService>> _loggerMock;
    private readonly Mock<ICulturalEventPredictionEngine> _predictionEngineMock;
    private readonly Mock<ICulturalConflictResolver> _conflictResolverMock;
    private readonly Mock<IFortuneHundredPerformanceOptimizer> _performanceOptimizerMock;
    private readonly Mock<ICulturalAffinityGeographicLoadBalancer> _culturalLoadBalancerMock;
    private readonly CulturalEventLoadDistributionService _service;

    public CulturalEventLoadDistributionServiceTests()
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

    #region Core Service Construction Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
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
        // Arrange & Act & Assert
        Action act = () => new CulturalEventLoadDistributionService(
            _loggerMock.Object,
            null!,
            _conflictResolverMock.Object,
            _performanceOptimizerMock.Object,
            _culturalLoadBalancerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("predictionEngine");
    }

    [Fact]
    public void Constructor_WithAllValidDependencies_ShouldCreateInstance()
    {
        // Arrange & Act & Assert
        _service.Should().NotBeNull();
        _service.Should().BeAssignableTo<ICulturalEventLoadDistributionService>();
    }

    #endregion

    #region Cultural Event Load Distribution Tests (Core Functionality)

    [Fact]
    public async Task DistributeLoadAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
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

        var expectedDistribution = new CulturalEventLoadDistributionResponse
        {
            DistributionId = Guid.NewGuid(),
            IsSuccessful = true,
            OptimalServerAllocations = new List<ServerAllocation>
            {
                new() { ServerId = "vesak-na-1", AllocatedCapacity = 15000, CulturalAffinityScore = 0.95m },
                new() { ServerId = "vesak-eu-1", AllocatedCapacity = 20000, CulturalAffinityScore = 0.92m }
            },
            PredictedResponseTime = TimeSpan.FromMilliseconds(150),
            CulturalCompatibilityScore = 0.94m
        };

        _predictionEngineMock.Setup(x => x.DistributeLoadForCulturalEventAsync(
                It.IsAny<CulturalEventLoadDistributionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDistribution);

        // Act
        var result = await _service.DistributeLoadAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.OptimalServerAllocations.Should().HaveCount(2);
        result.PredictedResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        result.CulturalCompatibilityScore.Should().BeGreaterThan(0.9m);
    }

    [Theory]
    [InlineData(CulturalEventType.VesakDayBuddhist, 5.0)]
    [InlineData(CulturalEventType.DiwaliHindu, 4.5)]
    [InlineData(CulturalEventType.EidAlFitrIslamic, 4.0)]
    [InlineData(CulturalEventType.GuruNanakJayanti, 3.5)]
    [InlineData(CulturalEventType.ThaipusamTamil, 3.0)]
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

    #region Predictive Scaling Tests (Festival-Specific Traffic Multipliers)

    [Fact]
    public async Task GenerateScalingPlanAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
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
                new() { ActionType = ScalingActionType.PreScale, TargetCapacity = 50000, ExecuteAt = DateTime.UtcNow.AddDays(7).AddHours(-2) },
                new() { ActionType = ScalingActionType.PeakScale, TargetCapacity = 75000, ExecuteAt = DateTime.UtcNow.AddDays(7).AddHours(6) }
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

    [Fact]
    public async Task GenerateScalingPlanAsync_ForDiwaliEvent_ShouldGenerate4Point5xScalingPlan()
    {
        // Arrange
        var scalingRequest = new PredictiveScalingPlanRequest
        {
            CulturalEventType = CulturalEventType.DiwaliHindu,
            EventStartTime = DateTime.UtcNow.AddDays(14),
            EventDuration = TimeSpan.FromHours(72), // 3-day celebration
            BaselineTraffic = 20000,
            GeographicRegions = new[] { "NorthAmerica", "Europe", "India", "AsiaPacific" }
        };

        var expectedPlan = new PredictiveScalingPlan
        {
            PlanId = Guid.NewGuid(),
            CulturalEventType = CulturalEventType.DiwaliHindu,
            TrafficMultiplier = 4.5m,
            PredictionAccuracy = 0.90m
        };

        _predictionEngineMock.Setup(x => x.GeneratePredictiveScalingPlanAsync(
                It.IsAny<PredictiveScalingPlanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPlan);

        // Act
        var result = await _service.GenerateScalingPlanAsync(scalingRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TrafficMultiplier.Should().Be(4.5m);
        result.PredictionAccuracy.Should().BeGreaterOrEqualTo(0.90m);
        result.CulturalEventType.Should().Be(CulturalEventType.DiwaliHindu);
    }

    [Fact]
    public async Task GenerateScalingPlanAsync_ForEidEvent_ShouldGenerate4xScalingPlan()
    {
        // Arrange
        var scalingRequest = new PredictiveScalingPlanRequest
        {
            CulturalEventType = CulturalEventType.EidAlFitrIslamic,
            EventStartTime = DateTime.UtcNow.AddDays(21),
            EventDuration = TimeSpan.FromHours(48), // 2-day celebration
            BaselineTraffic = 15000,
            GeographicRegions = new[] { "NorthAmerica", "Europe", "MiddleEast", "AsiaPacific" }
        };

        var expectedPlan = new PredictiveScalingPlan
        {
            PlanId = Guid.NewGuid(),
            CulturalEventType = CulturalEventType.EidAlFitrIslamic,
            TrafficMultiplier = 4.0m,
            PredictionAccuracy = 0.88m // Lunar variation affects prediction accuracy
        };

        _predictionEngineMock.Setup(x => x.GeneratePredictiveScalingPlanAsync(
                It.IsAny<PredictiveScalingPlanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPlan);

        // Act
        var result = await _service.GenerateScalingPlanAsync(scalingRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TrafficMultiplier.Should().Be(4.0m);
        result.PredictionAccuracy.Should().BeGreaterOrEqualTo(0.88m);
        result.CulturalEventType.Should().Be(CulturalEventType.EidAlFitrIslamic);
    }

    #endregion

    #region Multi-Cultural Conflict Resolution Tests

    [Fact]
    public async Task ResolveEventConflictsAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.ResolveEventConflictsAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ResolveEventConflictsAsync_WithOverlappingVesakAndDiwali_ShouldPrioritizeVesak()
    {
        // Arrange
        var conflictRequest = new CulturalEventConflictResolutionRequest
        {
            ConflictingEvents = new List<CulturalEventSchedule>
            {
                new()
                {
                    EventId = Guid.NewGuid(),
                    CulturalEventType = CulturalEventType.VesakDayBuddhist,
                    StartTime = DateTime.UtcNow.AddDays(10),
                    EndTime = DateTime.UtcNow.AddDays(10).AddHours(24),
                    PriorityLevel = SacredEventPriority.Level10Sacred,
                    ExpectedAttendees = 25000
                },
                new()
                {
                    EventId = Guid.NewGuid(),
                    CulturalEventType = CulturalEventType.DiwaliHindu,
                    StartTime = DateTime.UtcNow.AddDays(10).AddHours(12), // Overlapping
                    EndTime = DateTime.UtcNow.AddDays(12),
                    PriorityLevel = SacredEventPriority.Level9MajorFestival,
                    ExpectedAttendees = 30000
                }
            },
            ResolutionStrategy = ConflictResolutionStrategy.SacredEventPriority
        };

        var expectedResolution = new CulturalEventConflictResolution
        {
            ResolutionId = Guid.NewGuid(),
            IsResolved = true,
            PrimaryEvent = conflictRequest.ConflictingEvents.First(e => e.CulturalEventType == CulturalEventType.VesakDayBuddhist),
            ResourceAllocations = new List<ResourceAllocation>
            {
                new() { EventId = conflictRequest.ConflictingEvents[0].EventId, AllocatedCapacity = 35000, Priority = 1 },
                new() { EventId = conflictRequest.ConflictingEvents[1].EventId, AllocatedCapacity = 20000, Priority = 2 }
            },
            ConflictResolutionStrategy = ConflictResolutionStrategy.SacredEventPriority
        };

        _conflictResolverMock.Setup(x => x.ResolveMultiCulturalConflictsAsync(
                It.IsAny<CulturalEventConflictResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResolution);

        // Act
        var result = await _service.ResolveEventConflictsAsync(conflictRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsResolved.Should().BeTrue();
        result.PrimaryEvent.CulturalEventType.Should().Be(CulturalEventType.VesakDayBuddhist);
        result.ResourceAllocations.Should().HaveCount(2);
        result.ResourceAllocations.First().Priority.Should().Be(1);
    }

    [Fact]
    public async Task ResolveEventConflictsAsync_WithMultipleEidAndDiwaliEvents_ShouldUseResourceAllocationMatrix()
    {
        // Arrange
        var conflictRequest = new CulturalEventConflictResolutionRequest
        {
            ConflictingEvents = new List<CulturalEventSchedule>
            {
                new()
                {
                    EventId = Guid.NewGuid(),
                    CulturalEventType = CulturalEventType.EidAlFitrIslamic,
                    PriorityLevel = SacredEventPriority.Level10Sacred,
                    ExpectedAttendees = 22000
                },
                new()
                {
                    EventId = Guid.NewGuid(),
                    CulturalEventType = CulturalEventType.DiwaliHindu,
                    PriorityLevel = SacredEventPriority.Level9MajorFestival,
                    ExpectedAttendees = 28000
                }
            },
            ResolutionStrategy = ConflictResolutionStrategy.ResourceAllocationMatrix
        };

        var expectedResolution = new CulturalEventConflictResolution
        {
            ResolutionId = Guid.NewGuid(),
            IsResolved = true,
            ConflictResolutionStrategy = ConflictResolutionStrategy.ResourceAllocationMatrix,
            ResourceAllocations = new List<ResourceAllocation>
            {
                new() { EventId = conflictRequest.ConflictingEvents[0].EventId, AllocatedCapacity = 26000, Priority = 1 },
                new() { EventId = conflictRequest.ConflictingEvents[1].EventId, AllocatedCapacity = 24000, Priority = 2 }
            }
        };

        _conflictResolverMock.Setup(x => x.ResolveMultiCulturalConflictsAsync(
                It.IsAny<CulturalEventConflictResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResolution);

        // Act
        var result = await _service.ResolveEventConflictsAsync(conflictRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsResolved.Should().BeTrue();
        result.ConflictResolutionStrategy.Should().Be(ConflictResolutionStrategy.ResourceAllocationMatrix);
        result.ResourceAllocations.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(SacredEventPriority.Level10Sacred, SacredEventPriority.Level9MajorFestival, true)]
    [InlineData(SacredEventPriority.Level9MajorFestival, SacredEventPriority.Level8MonthlyObservance, true)]
    [InlineData(SacredEventPriority.Level8MonthlyObservance, SacredEventPriority.Level7RegionalFestival, true)]
    [InlineData(SacredEventPriority.Level7RegionalFestival, SacredEventPriority.Level5CommunityEvent, true)]
    public async Task ResolveEventConflictsAsync_WithDifferentPriorityLevels_ShouldPrioritizeHigherLevel(
        SacredEventPriority higherPriority, SacredEventPriority lowerPriority, bool expectedResolution)
    {
        // Arrange
        var conflictRequest = new CulturalEventConflictResolutionRequest
        {
            ConflictingEvents = new List<CulturalEventSchedule>
            {
                new() { EventId = Guid.NewGuid(), PriorityLevel = higherPriority, ExpectedAttendees = 15000 },
                new() { EventId = Guid.NewGuid(), PriorityLevel = lowerPriority, ExpectedAttendees = 18000 }
            },
            ResolutionStrategy = ConflictResolutionStrategy.SacredEventPriority
        };

        var expectedResolution = new CulturalEventConflictResolution
        {
            ResolutionId = Guid.NewGuid(),
            IsResolved = expectedResolution,
            PrimaryEvent = conflictRequest.ConflictingEvents.First()
        };

        _conflictResolverMock.Setup(x => x.ResolveMultiCulturalConflictsAsync(
                It.IsAny<CulturalEventConflictResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResolution);

        // Act
        var result = await _service.ResolveEventConflictsAsync(conflictRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsResolved.Should().Be(expectedResolution);
        result.PrimaryEvent.PriorityLevel.Should().Be(higherPriority);
    }

    #endregion

    #region Fortune 500 SLA Performance Tests

    [Fact]
    public async Task MonitorPerformanceAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.MonitorPerformanceAsync(null!, CancellationToken.None));
    }

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

        // Assert
        result.Should().NotBeNull();
        result.AverageResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        result.MaxResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        result.UptimePercentage.Should().BeGreaterOrEqualTo(99.9m);
        result.SlaCompliance.Should().BeTrue();
        result.ThroughputPerSecond.Should().BeGreaterThan(20000);
    }

    [Theory]
    [InlineData(CulturalEventType.VesakDayBuddhist, 5.0, 25000)]
    [InlineData(CulturalEventType.DiwaliHindu, 4.5, 22500)]
    [InlineData(CulturalEventType.EidAlFitrIslamic, 4.0, 20000)]
    [InlineData(CulturalEventType.GuruNanakJayanti, 3.5, 17500)]
    public async Task MonitorPerformanceAsync_WithDifferentTrafficMultipliers_ShouldScaleThroughputProperly(
        CulturalEventType eventType, double trafficMultiplier, int expectedMinThroughput)
    {
        // Arrange
        var performanceRequest = new FortuneHundredPerformanceMonitoringRequest
        {
            MonitoringScope = PerformanceMonitoringScope.CulturalEventSpecific,
            CulturalEventType = eventType,
            RequiredResponseTimeSla = TimeSpan.FromMilliseconds(200),
            ExpectedTrafficMultiplier = (decimal)trafficMultiplier
        };

        var expectedMetrics = new FortuneHundredPerformanceMetrics
        {
            MetricsId = Guid.NewGuid(),
            AverageResponseTime = TimeSpan.FromMilliseconds(160),
            ThroughputPerSecond = expectedMinThroughput + 1000,
            SlaCompliance = true,
            CulturalEventType = eventType
        };

        _performanceOptimizerMock.Setup(x => x.MonitorPerformanceAsync(
                It.IsAny<FortuneHundredPerformanceMonitoringRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _service.MonitorPerformanceAsync(performanceRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ThroughputPerSecond.Should().BeGreaterOrEqualTo(expectedMinThroughput);
        result.SlaCompliance.Should().BeTrue();
        result.CulturalEventType.Should().Be(eventType);
    }

    [Fact]
    public async Task MonitorPerformanceAsync_DuringMultiCulturalOverlap_ShouldMaintainSlaCompliance()
    {
        // Arrange
        var performanceRequest = new FortuneHundredPerformanceMonitoringRequest
        {
            MonitoringScope = PerformanceMonitoringScope.MultiCulturalEventOverlap,
            RequiredResponseTimeSla = TimeSpan.FromMilliseconds(200),
            RequiredUptimeSla = 99.9m,
            ExpectedTrafficMultiplier = 7.5m, // Combined Diwali + Eid traffic
            MonitoringDuration = TimeSpan.FromHours(48)
        };

        var expectedMetrics = new FortuneHundredPerformanceMetrics
        {
            MetricsId = Guid.NewGuid(),
            AverageResponseTime = TimeSpan.FromMilliseconds(180),
            MaxResponseTime = TimeSpan.FromMilliseconds(195),
            UptimePercentage = 99.92m,
            ThroughputPerSecond = 37500,
            SlaCompliance = true
        };

        _performanceOptimizerMock.Setup(x => x.MonitorPerformanceAsync(
                It.IsAny<FortuneHundredPerformanceMonitoringRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _service.MonitorPerformanceAsync(performanceRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AverageResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        result.UptimePercentage.Should().BeGreaterOrEqualTo(99.9m);
        result.SlaCompliance.Should().BeTrue();
        result.ThroughputPerSecond.Should().BeGreaterThan(35000); // 7.5x baseline of 5000
    }

    #endregion

    #region Integration with Cultural Affinity Load Balancer Tests

    [Fact]
    public async Task DistributeLoadAsync_ShouldIntegrateWithCulturalAffinityLoadBalancer()
    {
        // Arrange
        var request = new CulturalEventLoadDistributionRequest
        {
            EventId = Guid.NewGuid(),
            CulturalEventType = CulturalEventType.DiwaliHindu,
            PredictedTrafficMultiplier = 4.5m,
            GeographicScope = GeographicCulturalScope.Global,
            ExpectedConcurrentUsers = 45000
        };

        var culturalAffinityResponse = new DiasporaLoadBalancingResponse
        {
            IsSuccessful = true,
            OptimalRoutes = new List<CulturalAffinityRoute>
            {
                new() { RegionId = "NorthAmerica", AffinityScore = 0.94m, AllocatedCapacity = 15000 },
                new() { RegionId = "Europe", AffinityScore = 0.91m, AllocatedCapacity = 18000 },
                new() { RegionId = "AsiaPacific", AffinityScore = 0.96m, AllocatedCapacity = 12000 }
            }
        };

        _culturalLoadBalancerMock.Setup(x => x.DistributeLoadAsync(
                It.IsAny<DiasporaLoadBalancingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(culturalAffinityResponse);

        var eventDistribution = new CulturalEventLoadDistributionResponse
        {
            DistributionId = Guid.NewGuid(),
            IsSuccessful = true,
            CulturalCompatibilityScore = 0.93m
        };

        _predictionEngineMock.Setup(x => x.DistributeLoadForCulturalEventAsync(
                It.IsAny<CulturalEventLoadDistributionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventDistribution);

        // Act
        var result = await _service.DistributeLoadAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.CulturalCompatibilityScore.Should().BeGreaterThan(0.9m);

        // Verify integration occurred
        _culturalLoadBalancerMock.Verify(x => x.DistributeLoadAsync(
            It.IsAny<DiasporaLoadBalancingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Error Handling and Edge Cases

    [Fact]
    public async Task DistributeLoadAsync_WhenPredictionEngineFails_ShouldHandleGracefully()
    {
        // Arrange
        var request = new CulturalEventLoadDistributionRequest
        {
            EventId = Guid.NewGuid(),
            CulturalEventType = CulturalEventType.VesakDayBuddhist,
            PredictedTrafficMultiplier = 5.0m
        };

        _predictionEngineMock.Setup(x => x.DistributeLoadForCulturalEventAsync(
                It.IsAny<CulturalEventLoadDistributionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Prediction engine unavailable"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DistributeLoadAsync(request, CancellationToken.None));

        exception.Message.Should().Contain("Prediction engine unavailable");
    }

    [Fact]
    public async Task GenerateScalingPlanAsync_WithInvalidEventType_ShouldHandleGracefully()
    {
        // Arrange
        var request = new PredictiveScalingPlanRequest
        {
            CulturalEventType = (CulturalEventType)999, // Invalid enum value
            EventStartTime = DateTime.UtcNow.AddDays(1),
            BaselineTraffic = 1000
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GenerateScalingPlanAsync(request, CancellationToken.None));
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