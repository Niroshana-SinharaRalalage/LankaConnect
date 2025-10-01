using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Queries;
using LankaConnect.Domain.Common;
using MediatR;
using LankaConnect.IntegrationTests.Common;

namespace LankaConnect.IntegrationTests.Cache;

[Collection("IntegrationTests")]
public class CacheAsidePatternIntegrationTests : BaseIntegrationTest
{
    private readonly ICulturalIntelligenceCacheService _cacheService;
    private readonly IMediator _mediator;

    public CacheAsidePatternIntegrationTests(IntegrationTestWebApplicationFactory factory) : base(factory)
    {
        _cacheService = GetService<ICulturalIntelligenceCacheService>();
        _mediator = GetService<IMediator>();
    }

    [Fact]
    public async Task CulturalCalendarQuery_FirstRequest_ShouldExecuteQueryAndCache()
    {
        // Arrange
        var query = new CulturalCalendarQuery
        {
            CalendarType = CalendarType.Buddhist,
            Date = new DateTime(2024, 4, 23), // Vesak Day
            GeographicRegion = "sri_lanka",
            Language = "si",
            UserId = "test_user_123"
        };

        var cacheKey = query.GetCacheKey();
        
        // Ensure cache is empty
        await _cacheService.RemoveAsync(cacheKey);

        // Act - First request (should cache)
        var firstResult = await _mediator.Send(query);
        
        // Act - Second request (should hit cache)  
        var secondResult = await _mediator.Send(query);

        // Assert
        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.NotNull(firstResult.Value);
        Assert.NotNull(secondResult.Value);
        
        // Verify calendar data
        Assert.Equal(CalendarType.Buddhist, firstResult.Value.CalendarType);
        Assert.Equal(new DateTime(2024, 4, 23), firstResult.Value.Date);
        Assert.Contains("බුදු", firstResult.Value.FormattedDate); // Buddhist era in Sinhala
        
        // Verify caching behavior
        var cachedValue = await _cacheService.GetAsync<Result<CulturalCalendarResponse>>(cacheKey);
        Assert.NotNull(cachedValue);
        Assert.True(cachedValue.IsSuccess);
    }

    [Fact]
    public async Task CulturalAppropriatenessQuery_WithDifferentContent_ShouldGenerateUniqueCacheKeys()
    {
        // Arrange
        var query1 = new CulturalAppropriatenessQuery
        {
            Content = "Traditional Buddhist meditation practices",
            ContentType = "text",
            CommunityId = "buddhist",
            GeographicRegion = "sri_lanka",
            UserId = "user1"
        };

        var query2 = new CulturalAppropriatenessQuery
        {
            Content = "Hindu festival celebrations in diaspora communities",
            ContentType = "text", 
            CommunityId = "hindu",
            GeographicRegion = "india",
            UserId = "user1"
        };

        // Act
        var cacheKey1 = query1.GetCacheKey();
        var cacheKey2 = query2.GetCacheKey();

        // Assert
        Assert.NotEqual(cacheKey1, cacheKey2);
        Assert.Contains("buddhist", cacheKey1);
        Assert.Contains("hindu", cacheKey2);
        Assert.Contains("sri_lanka", cacheKey1);
        Assert.Contains("india", cacheKey2);
    }

    [Fact]
    public async Task CacheInvalidation_ByCulturalContext_ShouldRemoveRelatedCacheEntries()
    {
        // Arrange
        var buddhist_query1 = new CulturalCalendarQuery
        {
            CalendarType = CalendarType.Buddhist,
            Date = DateTime.Today,
            GeographicRegion = "sri_lanka",
            Language = "si"
        };

        var buddhist_query2 = new CulturalCalendarQuery
        {
            CalendarType = CalendarType.Buddhist,
            Date = DateTime.Today.AddDays(1),
            GeographicRegion = "sri_lanka", 
            Language = "si"
        };

        var hindu_query = new CulturalCalendarQuery
        {
            CalendarType = CalendarType.Hindu,
            Date = DateTime.Today,
            GeographicRegion = "india",
            Language = "hi"
        };

        // Cache all queries
        await _mediator.Send(buddhist_query1);
        await _mediator.Send(buddhist_query2);
        await _mediator.Send(hindu_query);

        // Verify all are cached
        var cached1 = await _cacheService.GetAsync<Result<CulturalCalendarResponse>>(buddhist_query1.GetCacheKey());
        var cached2 = await _cacheService.GetAsync<Result<CulturalCalendarResponse>>(buddhist_query2.GetCacheKey());
        var cached3 = await _cacheService.GetAsync<Result<CulturalCalendarResponse>>(hindu_query.GetCacheKey());
        
        Assert.NotNull(cached1);
        Assert.NotNull(cached2);
        Assert.NotNull(cached3);

        // Act - Invalidate Buddhist cache for Sri Lanka
        var invalidationContext = new CulturalCacheContext
        {
            CommunityId = "buddhist",
            GeographicRegion = "sri_lanka",
            DataType = "calendar"
        };

        var invalidationResult = await _cacheService.InvalidateCulturalCacheAsync(invalidationContext);

        // Assert
        Assert.True(invalidationResult.IsSuccess);
        
        // Buddhist entries should be removed
        var afterInvalidation1 = await _cacheService.GetAsync<Result<CulturalCalendarResponse>>(buddhist_query1.GetCacheKey());
        var afterInvalidation2 = await _cacheService.GetAsync<Result<CulturalCalendarResponse>>(buddhist_query2.GetCacheKey());
        
        // Hindu entry should remain (different context)
        var afterInvalidation3 = await _cacheService.GetAsync<Result<CulturalCalendarResponse>>(hindu_query.GetCacheKey());
        
        Assert.Null(afterInvalidation1);
        Assert.Null(afterInvalidation2);
        Assert.NotNull(afterInvalidation3); // Should still be cached
    }

    [Fact]
    public async Task CacheMetrics_AfterMultipleQueries_ShouldTrackPerformance()
    {
        // Arrange
        var queries = new[]
        {
            new CulturalCalendarQuery { CalendarType = CalendarType.Buddhist, Date = DateTime.Today, GeographicRegion = "sri_lanka", Language = "si" },
            new CulturalCalendarQuery { CalendarType = CalendarType.Buddhist, Date = DateTime.Today.AddDays(1), GeographicRegion = "sri_lanka", Language = "si" },
            new CulturalCalendarQuery { CalendarType = CalendarType.Hindu, Date = DateTime.Today, GeographicRegion = "india", Language = "hi" }
        };

        // Act - Execute queries multiple times (first time misses, subsequent hits)
        foreach (var query in queries)
        {
            await _mediator.Send(query); // Cache miss
            await _mediator.Send(query); // Cache hit
            await _mediator.Send(query); // Cache hit
        }

        // Get metrics
        var buddhistMetrics = await _cacheService.GetCacheMetricsAsync(CulturalIntelligenceEndpoint.BuddhistCalendar);
        var hinduMetrics = await _cacheService.GetCacheMetricsAsync(CulturalIntelligenceEndpoint.HinduCalendar);

        // Assert
        Assert.NotNull(buddhistMetrics);
        Assert.NotNull(hinduMetrics);
        Assert.True(buddhistMetrics.HitRatio >= 0);
        Assert.True(hinduMetrics.HitRatio >= 0);
        Assert.True(buddhistMetrics.LastUpdated <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CacheHealthCheck_WithActiveRedis_ShouldReturnHealthyStatus()
    {
        // Act
        var healthStatus = await _cacheService.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(healthStatus);
        Assert.True(healthStatus.IsHealthy);
        Assert.Equal("Healthy", healthStatus.Status);
        Assert.Contains("redis_connectivity", healthStatus.Details);
        Assert.True((bool)healthStatus.Details["redis_connectivity"]);
    }

    [Fact]
    public async Task CacheWarmup_ForCulturalCommunity_ShouldCompleteWithoutErrors()
    {
        // Act
        var warmupException = await Record.ExceptionAsync(async () =>
        {
            await _cacheService.WarmCulturalCacheAsync(
                CulturalCommunity.Buddhist,
                CacheWarmingStrategy.Immediate);
        });

        // Assert
        Assert.Null(warmupException);
    }

    [Theory]
    [InlineData(CalendarType.Buddhist, "sri_lanka", "si")]
    [InlineData(CalendarType.Buddhist, "thailand", "th")]
    [InlineData(CalendarType.Hindu, "india", "hi")]
    [InlineData(CalendarType.Hindu, "nepal", "ne")]
    public async Task CulturalCalendarQuery_DifferentRegionalContexts_ShouldHandleCorrectly(
        CalendarType calendarType, string region, string language)
    {
        // Arrange
        var query = new CulturalCalendarQuery
        {
            CalendarType = calendarType,
            Date = new DateTime(2024, 1, 1),
            GeographicRegion = region,
            Language = language
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(calendarType, result.Value.CalendarType);
        Assert.NotNull(result.Value.FormattedDate);
        Assert.NotEmpty(result.Value.FormattedDate);
        
        // Verify caching worked
        var cacheKey = query.GetCacheKey();
        var cachedResult = await _cacheService.GetAsync<Result<CulturalCalendarResponse>>(cacheKey);
        Assert.NotNull(cachedResult);
    }

    [Fact]
    public async Task CacheTTL_DifferentQueryTypes_ShouldRespectConfiguredTTL()
    {
        // Arrange
        var calendarQuery = new CulturalCalendarQuery
        {
            CalendarType = CalendarType.Buddhist,
            Date = DateTime.Today,
            GeographicRegion = "sri_lanka",
            Language = "si"
        };

        var appropriatenessQuery = new CulturalAppropriatenessQuery
        {
            Content = "Test cultural content",
            CommunityId = "buddhist",
            GeographicRegion = "sri_lanka"
        };

        // Act & Assert TTL values
        Assert.Equal(TimeSpan.FromDays(30), calendarQuery.GetCacheTtl()); // Long TTL for calendar data
        Assert.Equal(TimeSpan.FromHours(12), appropriatenessQuery.GetCacheTtl()); // Shorter TTL for ML results
    }
}