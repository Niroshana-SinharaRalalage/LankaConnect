using FluentAssertions;
using Xunit;
using LankaConnect.Infrastructure.Database.Consistency;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LankaConnect.Infrastructure.Tests.Database;

public class CulturalIntelligenceConsistencyServiceTests
{
    [Fact]
    public void CulturalIntelligenceConsistencyService_CanBeConstructed_WithValidParameters()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceConsistencyService>>();
        var options = Options.Create(new CrossRegionConsistencyOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var calendarService = Substitute.For<ICulturalCalendarSynchronizationService>();

        // Act
        var service = new CulturalIntelligenceConsistencyService(
            logger, options, shardingService, calendarService);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void CrossRegionConsistencyOptions_HasDefaultValues_WhenCreated()
    {
        // Arrange & Act
        var options = new CrossRegionConsistencyOptions();

        // Assert
        options.EnableStrongConsistencyForSacredEvents.Should().BeTrue();
        options.MaxSacredEventSyncTimeMs.Should().Be(500);
        options.MaxCommunityContentSyncTimeMs.Should().Be(50);
        options.DefaultConsistencyLevel.Should().Be(ConsistencyLevel.BoundedStaleness);
        options.SacredEventConsistencyLevel.Should().Be(ConsistencyLevel.LinearizableStrong);
        options.CrossRegionFailoverTimeoutSeconds.Should().Be(60);
        options.ConsistencyScoreThreshold.Should().Be(0.95);
        options.EnableCulturalConflictResolution.Should().BeTrue();
    }

    [Theory]
    [InlineData(CulturalDataType.BuddhistCalendar, ConsistencyLevel.LinearizableStrong)]
    [InlineData(CulturalDataType.HinduCalendar, ConsistencyLevel.LinearizableStrong)]
    [InlineData(CulturalDataType.IslamicCalendar, ConsistencyLevel.LinearizableStrong)]
    [InlineData(CulturalDataType.CommunityInsights, ConsistencyLevel.BoundedStaleness)]
    [InlineData(CulturalDataType.DiasporaAnalytics, ConsistencyLevel.Session)]
    public void GetRequiredConsistencyLevel_ReturnsCorrectLevel_ForDataType(
        CulturalDataType dataType, ConsistencyLevel expectedLevel)
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceConsistencyService>>();
        var options = Options.Create(new CrossRegionConsistencyOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var calendarService = Substitute.For<ICulturalCalendarSynchronizationService>();
        var service = new CulturalIntelligenceConsistencyService(
            logger, options, shardingService, calendarService);

        // Act
        var result = service.GetRequiredConsistencyLevel(dataType);

        // Assert
        result.Should().Be(expectedLevel);
    }

    [Theory]
    [InlineData(CulturalEventSyncType.SacredEvent, "SacredEvent")]
    [InlineData(CulturalEventSyncType.CommunityContent, "CommunityContent")]
    [InlineData(CulturalEventSyncType.DiasporaNews, "DiasporaNews")]
    [InlineData(CulturalEventSyncType.BusinessListing, "BusinessListing")]
    [InlineData(CulturalEventSyncType.UserProfile, "UserProfile")]
    public void CulturalEventSyncType_EnumValues_ShouldHaveCorrectNames(
        CulturalEventSyncType syncType, string expectedName)
    {
        // Act & Assert
        syncType.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalDataConsistencyCheck_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var consistencyCheck = new CulturalDataConsistencyCheck();

        // Assert
        consistencyCheck.CheckId.Should().NotBeNull();
        consistencyCheck.DataType.Should().Be(CulturalDataType.CommunityInsights);
        consistencyCheck.SourceRegion.Should().NotBeNull();
        consistencyCheck.TargetRegions.Should().NotBeNull();
        consistencyCheck.ConsistencyLevel.Should().Be(ConsistencyLevel.BoundedStaleness);
        consistencyCheck.CheckTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        consistencyCheck.ConsistencyScore.Should().Be(0.0);
        consistencyCheck.IsConsistent.Should().BeFalse();
    }

    [Theory]
    [InlineData(ConsistencyCheckStatus.Pending, "Pending")]
    [InlineData(ConsistencyCheckStatus.InProgress, "InProgress")]
    [InlineData(ConsistencyCheckStatus.Completed, "Completed")]
    [InlineData(ConsistencyCheckStatus.Failed, "Failed")]
    [InlineData(ConsistencyCheckStatus.PartiallyConsistent, "PartiallyConsistent")]
    public void ConsistencyCheckStatus_EnumValues_ShouldHaveCorrectNames(
        ConsistencyCheckStatus status, string expectedName)
    {
        // Act & Assert
        status.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalConflictResolution_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var resolution = new CulturalConflictResolution();

        // Assert
        resolution.ConflictId.Should().NotBeNull();
        resolution.ConflictType.Should().Be(CulturalConflictType.CalendarDiscrepancy);
        resolution.ConflictingRegions.Should().NotBeNull();
        resolution.ResolutionStrategy.Should().Be(ConflictResolutionStrategy.CulturalSignificancePriority);
        resolution.ResolutionTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        resolution.ResolutionDetails.Should().NotBeNull();
        resolution.AuthoritySource.Should().NotBeNull();
    }

    [Theory]
    [InlineData(CulturalConflictType.CalendarDiscrepancy, "CalendarDiscrepancy")]
    [InlineData(CulturalConflictType.EventTimingConflict, "EventTimingConflict")]
    [InlineData(CulturalConflictType.CulturalAppropriateness, "CulturalAppropriateness")]
    [InlineData(CulturalConflictType.LanguageTranslation, "LanguageTranslation")]
    [InlineData(CulturalConflictType.CommunityPriority, "CommunityPriority")]
    public void CulturalConflictType_EnumValues_ShouldHaveCorrectNames(
        CulturalConflictType conflictType, string expectedName)
    {
        // Act & Assert
        conflictType.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CrossRegionSynchronizationResult_DefaultProperties_ShouldBeValid()
    {
        // Act
        var result = new CrossRegionSynchronizationResult
        {
            SynchronizationId = "vesak_2025_sync",
            SourceRegion = "asia_pacific",
            TargetRegions = new List<string> { "north_america", "europe" },
            DataType = CulturalDataType.BuddhistCalendar,
            SynchronizationDuration = TimeSpan.FromMilliseconds(350),
            ConsistencyAchieved = true,
            ConsistencyScore = 0.98
        };

        // Assert
        result.SynchronizationId.Should().Be("vesak_2025_sync");
        result.SourceRegion.Should().Be("asia_pacific");
        result.TargetRegions.Should().Contain("north_america");
        result.TargetRegions.Should().Contain("europe");
        result.DataType.Should().Be(CulturalDataType.BuddhistCalendar);
        result.SynchronizationDuration.Should().Be(TimeSpan.FromMilliseconds(350));
        result.ConsistencyAchieved.Should().BeTrue();
        result.ConsistencyScore.Should().Be(0.98);
    }

    [Theory]
    [InlineData(ConflictResolutionStrategy.CulturalSignificancePriority, "CulturalSignificancePriority")]
    [InlineData(ConflictResolutionStrategy.SourceRegionAuthority, "SourceRegionAuthority")]
    [InlineData(ConflictResolutionStrategy.MajorityConsensus, "MajorityConsensus")]
    [InlineData(ConflictResolutionStrategy.TimestampBased, "TimestampBased")]
    [InlineData(ConflictResolutionStrategy.CulturalExpertReview, "CulturalExpertReview")]
    public void ConflictResolutionStrategy_EnumValues_ShouldHaveCorrectNames(
        ConflictResolutionStrategy strategy, string expectedName)
    {
        // Act & Assert
        strategy.ToString().Should().Be(expectedName);
    }

    [Fact]
    public async Task SynchronizeCulturalDataAcrossRegionsAsync_ShouldReturnResult_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceConsistencyService>>();
        var options = Options.Create(new CrossRegionConsistencyOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var calendarService = Substitute.For<ICulturalCalendarSynchronizationService>();
        var service = new CulturalIntelligenceConsistencyService(
            logger, options, shardingService, calendarService);

        var syncRequest = new CulturalDataSynchronizationRequest
        {
            DataType = CulturalDataType.BuddhistCalendar,
            SourceRegion = "asia_pacific",
            TargetRegions = new List<string> { "north_america", "europe" },
            ConsistencyLevel = ConsistencyLevel.LinearizableStrong
        };

        // Act
        var result = await service.SynchronizeCulturalDataAcrossRegionsAsync(
            syncRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateCrossCulturalConsistencyAsync_ShouldReturnCheck_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceConsistencyService>>();
        var options = Options.Create(new CrossRegionConsistencyOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var calendarService = Substitute.For<ICulturalCalendarSynchronizationService>();
        var service = new CulturalIntelligenceConsistencyService(
            logger, options, shardingService, calendarService);

        var regions = new List<string> { "north_america", "europe", "asia_pacific" };

        // Act
        var result = await service.ValidateCrossCulturalConsistencyAsync(
            CulturalDataType.BuddhistCalendar, regions, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void CulturalDataSynchronizationRequest_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var request = new CulturalDataSynchronizationRequest();

        // Assert
        request.RequestId.Should().NotBeNull();
        request.DataType.Should().Be(CulturalDataType.CommunityInsights);
        request.SourceRegion.Should().NotBeNull();
        request.TargetRegions.Should().NotBeNull();
        request.ConsistencyLevel.Should().Be(ConsistencyLevel.BoundedStaleness);
        request.RequestTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        request.Priority.Should().Be(SynchronizationPriority.Medium);
        request.CulturalContext.Should().BeNull();
    }

    [Theory]
    [InlineData(SynchronizationPriority.Low, "Low")]
    [InlineData(SynchronizationPriority.Medium, "Medium")]
    [InlineData(SynchronizationPriority.High, "High")]
    [InlineData(SynchronizationPriority.Critical, "Critical")]
    [InlineData(SynchronizationPriority.Sacred, "Sacred")]
    public void SynchronizationPriority_EnumValues_ShouldHaveCorrectNames(
        SynchronizationPriority priority, string expectedName)
    {
        // Act & Assert
        priority.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalAuthoritySource_DefaultProperties_ShouldBeValid()
    {
        // Act
        var authority = new CulturalAuthoritySource
        {
            AuthorityId = "sri_lanka_buddhist_council",
            AuthorityName = "Sri Lanka Buddhist Council",
            AuthorityType = CulturalAuthorityType.ReligiousCouncil,
            GeographicRegion = "asia_pacific",
            CulturalDomain = CulturalDataType.BuddhistCalendar,
            AuthorityWeight = 0.95,
            IsVerified = true
        };

        // Assert
        authority.AuthorityId.Should().Be("sri_lanka_buddhist_council");
        authority.AuthorityName.Should().Be("Sri Lanka Buddhist Council");
        authority.AuthorityType.Should().Be(CulturalAuthorityType.ReligiousCouncil);
        authority.GeographicRegion.Should().Be("asia_pacific");
        authority.CulturalDomain.Should().Be(CulturalDataType.BuddhistCalendar);
        authority.AuthorityWeight.Should().Be(0.95);
        authority.IsVerified.Should().BeTrue();
    }

    [Theory]
    [InlineData(CulturalAuthorityType.ReligiousCouncil, "ReligiousCouncil")]
    [InlineData(CulturalAuthorityType.CommunityLeaders, "CommunityLeaders")]
    [InlineData(CulturalAuthorityType.AcademicInstitution, "AcademicInstitution")]
    [InlineData(CulturalAuthorityType.GovernmentAgency, "GovernmentAgency")]
    [InlineData(CulturalAuthorityType.CulturalOrganization, "CulturalOrganization")]
    public void CulturalAuthorityType_EnumValues_ShouldHaveCorrectNames(
        CulturalAuthorityType authorityType, string expectedName)
    {
        // Act & Assert
        authorityType.ToString().Should().Be(expectedName);
    }

    [Fact]
    public async Task ResolveCulturalConflictAsync_ShouldReturnResolution_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceConsistencyService>>();
        var options = Options.Create(new CrossRegionConsistencyOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var calendarService = Substitute.For<ICulturalCalendarSynchronizationService>();
        var service = new CulturalIntelligenceConsistencyService(
            logger, options, shardingService, calendarService);

        var conflict = new CulturalDataConflict
        {
            ConflictId = "vesak_date_2025",
            ConflictType = CulturalConflictType.CalendarDiscrepancy,
            ConflictingRegions = new List<string> { "north_america", "asia_pacific" },
            CulturalSignificance = CulturalSignificance.Sacred
        };

        // Act
        var result = await service.ResolveCulturalConflictAsync(conflict, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void CulturalDataConflict_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var conflict = new CulturalDataConflict();

        // Assert
        conflict.ConflictId.Should().NotBeNull();
        conflict.ConflictType.Should().Be(CulturalConflictType.CalendarDiscrepancy);
        conflict.ConflictingRegions.Should().NotBeNull();
        conflict.DataType.Should().Be(CulturalDataType.CommunityInsights);
        conflict.CulturalSignificance.Should().Be(CulturalSignificance.Medium);
        conflict.ConflictDetectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        conflict.ConflictDescription.Should().NotBeNull();
        conflict.ConflictingValues.Should().NotBeNull();
    }

    [Fact]
    public void CrossRegionFailoverResult_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var result = new CrossRegionFailoverResult();

        // Assert
        result.FailoverId.Should().NotBeNull();
        result.SourceRegion.Should().NotBeNull();
        result.TargetRegion.Should().NotBeNull();
        result.FailoverReason.Should().NotBeNull();
        result.FailoverStartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.FailoverDuration.Should().Be(TimeSpan.Zero);
        result.Success.Should().BeTrue();
        result.DataConsistencyMaintained.Should().BeTrue();
        result.AffectedServices.Should().NotBeNull();
    }

    [Theory]
    [InlineData(FailoverTriggerType.RegionOutage, "RegionOutage")]
    [InlineData(FailoverTriggerType.NetworkPartition, "NetworkPartition")]
    [InlineData(FailoverTriggerType.PerformanceDegradation, "PerformanceDegradation")]
    [InlineData(FailoverTriggerType.MaintenanceWindow, "MaintenanceWindow")]
    [InlineData(FailoverTriggerType.DisasterRecovery, "DisasterRecovery")]
    public void FailoverTriggerType_EnumValues_ShouldHaveCorrectNames(
        FailoverTriggerType triggerType, string expectedName)
    {
        // Act & Assert
        triggerType.ToString().Should().Be(expectedName);
    }

    [Fact]
    public async Task ExecuteCrossRegionFailoverAsync_ShouldReturnResult_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceConsistencyService>>();
        var options = Options.Create(new CrossRegionConsistencyOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var calendarService = Substitute.For<ICulturalCalendarSynchronizationService>();
        var service = new CulturalIntelligenceConsistencyService(
            logger, options, shardingService, calendarService);

        var failoverRequest = new CrossRegionFailoverRequest
        {
            SourceRegion = "north_america",
            TargetRegion = "europe",
            TriggerType = FailoverTriggerType.RegionOutage,
            PreserveCulturalConsistency = true
        };

        // Act
        var result = await service.ExecuteCrossRegionFailoverAsync(failoverRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void CulturalConsistencyMetrics_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var metrics = new CulturalConsistencyMetrics();

        // Assert
        metrics.MetricsId.Should().NotBeNull();
        metrics.OverallConsistencyScore.Should().Be(0.0);
        metrics.RegionConsistencyScores.Should().NotBeNull();
        metrics.DataTypeConsistencyScores.Should().NotBeNull();
        metrics.ConflictResolutionSuccessRate.Should().Be(0.0);
        metrics.AverageSynchronizationTime.Should().Be(TimeSpan.Zero);
        metrics.MetricsCollectionTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        metrics.SacredEventConsistencyScore.Should().Be(0.0);
    }

    [Fact]
    public async Task GetCulturalConsistencyMetricsAsync_ShouldReturnMetrics_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceConsistencyService>>();
        var options = Options.Create(new CrossRegionConsistencyOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var calendarService = Substitute.For<ICulturalCalendarSynchronizationService>();
        var service = new CulturalIntelligenceConsistencyService(
            logger, options, shardingService, calendarService);

        // Act
        var result = await service.GetCulturalConsistencyMetricsAsync(
            TimeSpan.FromHours(24), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("sri_lankan_buddhist", "north_america", CulturalDataType.BuddhistCalendar)]
    [InlineData("indian_hindu", "europe", CulturalDataType.HinduCalendar)]
    [InlineData("pakistani_muslim", "australia", CulturalDataType.IslamicCalendar)]
    [InlineData("sikh_punjabi", "canada", CulturalDataType.CalendarEvents)]
    public void CulturalDataSynchronizationRequest_WithDifferentCombinations_ShouldBeValid(
        string communityId, string region, CulturalDataType dataType)
    {
        // Act
        var request = new CulturalDataSynchronizationRequest
        {
            DataType = dataType,
            SourceRegion = region,
            TargetRegions = new List<string> { "north_america", "europe" },
            ConsistencyLevel = ConsistencyLevel.LinearizableStrong,
            Priority = SynchronizationPriority.Sacred,
            CulturalContext = new CulturalContext
            {
                CommunityId = communityId,
                GeographicRegion = region
            }
        };

        // Assert
        request.DataType.Should().Be(dataType);
        request.SourceRegion.Should().Be(region);
        request.TargetRegions.Should().Contain("north_america");
        request.TargetRegions.Should().Contain("europe");
        request.ConsistencyLevel.Should().Be(ConsistencyLevel.LinearizableStrong);
        request.Priority.Should().Be(SynchronizationPriority.Sacred);
        request.CulturalContext.Should().NotBeNull();
        request.CulturalContext.CommunityId.Should().Be(communityId);
        request.CulturalContext.GeographicRegion.Should().Be(region);
    }

    [Fact]
    public void CrossRegionFailoverRequest_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var request = new CrossRegionFailoverRequest();

        // Assert
        request.RequestId.Should().NotBeNull();
        request.SourceRegion.Should().NotBeNull();
        request.TargetRegion.Should().NotBeNull();
        request.TriggerType.Should().Be(FailoverTriggerType.RegionOutage);
        request.RequestTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        request.PreserveCulturalConsistency.Should().BeTrue();
        request.MaxFailoverDuration.Should().Be(TimeSpan.FromSeconds(60));
        request.AffectedDataTypes.Should().NotBeNull();
    }

    [Fact]
    public async Task MonitorCrossRegionConsistencyAsync_ShouldReturnStatus_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceConsistencyService>>();
        var options = Options.Create(new CrossRegionConsistencyOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var calendarService = Substitute.For<ICulturalCalendarSynchronizationService>();
        var service = new CulturalIntelligenceConsistencyService(
            logger, options, shardingService, calendarService);

        var regions = new List<string> { "north_america", "europe", "asia_pacific" };

        // Act
        var result = await service.MonitorCrossRegionConsistencyAsync(regions, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}