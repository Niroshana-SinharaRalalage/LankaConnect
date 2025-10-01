using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;
using LankaConnect.Infrastructure.Cache;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Infrastructure.Tests.Cache;

public class CulturalIntelligenceCacheServiceTests : IDisposable
{
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<IServer> _mockServer;
    private readonly Mock<ILogger<CulturalIntelligenceCacheService>> _mockLogger;
    private readonly CulturalIntelligenceCacheService _cacheService;

    public CulturalIntelligenceCacheServiceTests()
    {
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockServer = new Mock<IServer>();
        _mockLogger = new Mock<ILogger<CulturalIntelligenceCacheService>>();

        // Setup Redis mocks
        _mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);
        
        _mockRedis.Setup(x => x.GetEndPoints(It.IsAny<bool>()))
            .Returns(new EndPoint[] { new IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 6379) });
        
        _mockRedis.Setup(x => x.GetServer(It.IsAny<EndPoint>(), It.IsAny<object>()))
            .Returns(_mockServer.Object);

        _cacheService = new CulturalIntelligenceCacheService(
            _mockDistributedCache.Object,
            _mockRedis.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetOrSetAsync_CacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var key = "test_key";
        var cachedValue = new TestCacheObject { Id = 1, Name = "Cached" };
        var serializedValue = System.Text.Json.JsonSerializer.Serialize(cachedValue);
        
        _mockDistributedCache
            .Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _cacheService.GetOrSetAsync(key, 
            () => Task.FromResult(new TestCacheObject { Id = 2, Name = "Fresh" }));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Cached", result.Name);
        
        _mockDistributedCache.Verify(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCache.Verify(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrSetAsync_CacheMiss_ShouldExecuteFactoryAndCache()
    {
        // Arrange
        var key = "test_key";
        var freshValue = new TestCacheObject { Id = 2, Name = "Fresh" };
        
        _mockDistributedCache
            .Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null!);

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () => Task.FromResult(freshValue));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("Fresh", result.Name);
        
        _mockDistributedCache.Verify(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCache.Verify(x => x.SetStringAsync(
            key, 
            It.IsAny<string>(), 
            It.IsAny<DistributedCacheEntryOptions>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ExistingKey_ShouldReturnValue()
    {
        // Arrange
        var key = "existing_key";
        var testObject = new TestCacheObject { Id = 3, Name = "Existing" };
        var serializedValue = System.Text.Json.JsonSerializer.Serialize(testObject);
        
        _mockDistributedCache
            .Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
        Assert.Equal("Existing", result.Name);
    }

    [Fact]
    public async Task GetAsync_NonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var key = "non_existing_key";
        
        _mockDistributedCache
            .Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null!);

        // Act
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ValidKeyValue_ShouldCacheValue()
    {
        // Arrange
        var key = "set_key";
        var value = new TestCacheObject { Id = 4, Name = "Set" };
        var expiry = TimeSpan.FromMinutes(30);

        // Act
        await _cacheService.SetAsync(key, value, expiry);

        // Assert
        _mockDistributedCache.Verify(x => x.SetStringAsync(
            key,
            It.IsAny<string>(),
            It.Is<DistributedCacheEntryOptions>(o => 
                o.AbsoluteExpirationRelativeToNow == expiry),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_ShouldRemoveFromCache()
    {
        // Arrange
        var key = "remove_key";

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _mockDistributedCache.Verify(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemovePatternAsync_ValidPattern_ShouldRemoveMatchingKeys()
    {
        // Arrange
        var pattern = "cultural:buddhist:*";
        var matchingKeys = new RedisKey[] { "cultural:buddhist:sri_lanka", "cultural:buddhist:thailand" };
        
        _mockServer
            .Setup(x => x.Keys(It.IsAny<int>(), pattern, It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(matchingKeys);
        
        _mockDatabase
            .Setup(x => x.KeyDeleteAsync(matchingKeys, It.IsAny<CommandFlags>()))
            .ReturnsAsync(matchingKeys.Length);

        // Act
        var result = await _cacheService.RemovePatternAsync(pattern);

        // Assert
        Assert.Equal(2, result);
        _mockDatabase.Verify(x => x.KeyDeleteAsync(matchingKeys, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateCulturalCacheAsync_ValidContext_ShouldInvalidateRelatedCache()
    {
        // Arrange
        var context = new CulturalCacheContext
        {
            CommunityId = "buddhist",
            GeographicRegion = "sri_lanka",
            Language = "si",
            DataType = "calendar"
        };
        
        _mockServer
            .Setup(x => x.Keys(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(new RedisKey[] { "cal_buddhist:buddhist:sri_lanka:si:calendar:user123" });
        
        _mockDatabase
            .Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        var result = await _cacheService.InvalidateCulturalCacheAsync(context);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetCacheMetricsAsync_ValidEndpoint_ShouldReturnMetrics()
    {
        // Arrange
        var endpoint = CulturalIntelligenceEndpoint.BuddhistCalendar;

        // Act
        var result = await _cacheService.GetCacheMetricsAsync(endpoint);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HitRatio >= 0);
        Assert.True(result.MissRatio >= 0);
        Assert.True(result.TotalRequests >= 0);
    }

    [Fact]
    public async Task GetHealthStatusAsync_HealthyRedis_ShouldReturnHealthyStatus()
    {
        // Arrange
        var testKey = "health_check_test";
        var testValue = "health_test";
        
        _mockDatabase
            .Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        
        _mockDatabase
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(testValue);
        
        _mockDatabase
            .Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        
        _mockDatabase
            .Setup(x => x.ExecuteAsync("INFO", "memory"))
            .ReturnsAsync("used_memory:1048576");

        // Act
        var result = await _cacheService.GetHealthStatusAsync();

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("Healthy", result.Status);
        Assert.Contains("redis_connectivity", result.Details);
        Assert.Contains("test_operation_success", result.Details);
    }

    [Theory]
    [InlineData(CacheWarmingStrategy.Immediate)]
    [InlineData(CacheWarmingStrategy.Background)]
    [InlineData(CacheWarmingStrategy.Scheduled)]
    [InlineData(CacheWarmingStrategy.Predictive)]
    public async Task WarmCulturalCacheAsync_DifferentStrategies_ShouldCompleteSuccessfully(CacheWarmingStrategy strategy)
    {
        // Arrange
        var community = CulturalCommunity.Buddhist;

        // Act
        var exception = await Record.ExceptionAsync(() => 
            _cacheService.WarmCulturalCacheAsync(community, strategy));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task GetOrSetAsync_NullKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cacheService.GetOrSetAsync<TestCacheObject>(null!, () => Task.FromResult(new TestCacheObject())));
    }

    [Fact]
    public async Task GetOrSetAsync_NullFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _cacheService.GetOrSetAsync<TestCacheObject>("test", null!));
    }

    [Fact]
    public async Task GetOrSetAsync_CacheExceptionWithSuccessfulFallback_ShouldReturnFallbackValue()
    {
        // Arrange
        var key = "exception_key";
        var fallbackValue = new TestCacheObject { Id = 99, Name = "Fallback" };
        
        _mockDistributedCache
            .Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Redis timeout"));

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () => Task.FromResult(fallbackValue));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(99, result.Id);
        Assert.Equal("Fallback", result.Name);
    }

    public void Dispose()
    {
        // Cleanup resources if needed
    }
}

// Test helper class
public class TestCacheObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}