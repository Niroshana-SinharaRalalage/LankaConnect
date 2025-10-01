using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using LankaConnect.Application.Common.Behaviors;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Tests.Common.Behaviors;

public class CulturalIntelligenceCachingBehaviorTests
{
    private readonly Mock<ICulturalIntelligenceCacheService> _mockCacheService;
    private readonly Mock<ILogger<CulturalIntelligenceCachingBehavior<TestCacheableQuery, Result<TestResponse>>>> _mockLogger;
    private readonly CulturalIntelligenceCachingBehavior<TestCacheableQuery, Result<TestResponse>> _behavior;
    private readonly Mock<RequestHandlerDelegate<Result<TestResponse>>> _mockNext;

    public CulturalIntelligenceCachingBehaviorTests()
    {
        _mockCacheService = new Mock<ICulturalIntelligenceCacheService>();
        _mockLogger = new Mock<ILogger<CulturalIntelligenceCachingBehavior<TestCacheableQuery, Result<TestResponse>>>>();
        _behavior = new CulturalIntelligenceCachingBehavior<TestCacheableQuery, Result<TestResponse>>(
            _mockCacheService.Object, _mockLogger.Object);
        _mockNext = new Mock<RequestHandlerDelegate<Result<TestResponse>>>();
    }

    [Fact]
    public async Task Handle_NonCacheableQuery_ShouldCallNextDirectly()
    {
        // Arrange
        var request = new TestNonCacheableQuery();
        var expectedResponse = Result.Success(new TestResponse { Id = 1, Name = "Test" });
        _mockNext.Setup(x => x()).ReturnsAsync(expectedResponse);
        
        var behavior = new CulturalIntelligenceCachingBehavior<TestNonCacheableQuery, Result<TestResponse>>(
            _mockCacheService.Object,
            Mock.Of<ILogger<CulturalIntelligenceCachingBehavior<TestNonCacheableQuery, Result<TestResponse>>>>());

        // Act
        var result = await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockNext.Verify(x => x(), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<Result<TestResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CacheableQueryWithCacheHit_ShouldReturnCachedResult()
    {
        // Arrange
        var request = new TestCacheableQuery { CommunityId = "buddhist", Region = "sri_lanka" };
        var cachedResponse = Result.Success(new TestResponse { Id = 1, Name = "Cached" });
        
        _mockCacheService
            .Setup(x => x.GetAsync<Result<TestResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        // Act
        var result = await _behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        Assert.Equal(cachedResponse, result);
        _mockNext.Verify(x => x(), Times.Never);
        _mockCacheService.Verify(x => x.GetAsync<Result<TestResponse>>(
            "test_cache_key_buddhist_sri_lanka", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_CacheableQueryWithCacheMiss_ShouldExecuteQueryAndCache()
    {
        // Arrange
        var request = new TestCacheableQuery { CommunityId = "hindu", Region = "india" };
        var queryResponse = Result.Success(new TestResponse { Id = 2, Name = "Fresh" });
        
        _mockCacheService
            .Setup(x => x.GetAsync<Result<TestResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result<TestResponse>)null!);
        
        _mockNext.Setup(x => x()).ReturnsAsync(queryResponse);

        // Act
        var result = await _behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        Assert.Equal(queryResponse, result);
        _mockNext.Verify(x => x(), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<Result<TestResponse>>(
            "test_cache_key_hindu_india", CancellationToken.None), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(
            "test_cache_key_hindu_india", 
            queryResponse, 
            TimeSpan.FromHours(1), 
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_CacheableQueryWithFailedResult_ShouldNotCache()
    {
        // Arrange
        var request = new TestCacheableQuery { CommunityId = "tamil", Region = "sri_lanka" };
        var failedResponse = Result.Failure<TestResponse>("Test error");
        
        _mockCacheService
            .Setup(x => x.GetAsync<Result<TestResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result<TestResponse>)null!);
        
        _mockNext.Setup(x => x()).ReturnsAsync(failedResponse);

        // Act
        var result = await _behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        Assert.Equal(failedResponse, result);
        _mockNext.Verify(x => x(), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<Result<TestResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Result<TestResponse>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CachingDisabled_ShouldSkipCache()
    {
        // Arrange
        var request = new TestCacheableQuery { CommunityId = "muslim", Region = "malaysia", EnableCaching = false };
        var queryResponse = Result.Success(new TestResponse { Id = 3, Name = "No Cache" });
        
        _mockNext.Setup(x => x()).ReturnsAsync(queryResponse);

        // Act
        var result = await _behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        Assert.Equal(queryResponse, result);
        _mockNext.Verify(x => x(), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<Result<TestResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CacheServiceThrows_ShouldFallbackToQuery()
    {
        // Arrange
        var request = new TestCacheableQuery { CommunityId = "christian", Region = "philippines" };
        var queryResponse = Result.Success(new TestResponse { Id = 4, Name = "Fallback" });
        
        _mockCacheService
            .Setup(x => x.GetAsync<Result<TestResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis connection failed"));
        
        _mockNext.Setup(x => x()).ReturnsAsync(queryResponse);

        // Act
        var result = await _behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        Assert.Equal(queryResponse, result);
        _mockNext.Verify(x => x(), Times.Once);
    }

    [Theory]
    [InlineData("buddhist", "sri_lanka")]
    [InlineData("hindu", "india")]
    [InlineData("tamil", "singapore")]
    [InlineData("muslim", "malaysia")]
    public async Task Handle_DifferentCulturalContexts_ShouldGenerateUniqueCacheKeys(string community, string region)
    {
        // Arrange
        var request = new TestCacheableQuery { CommunityId = community, Region = region };
        var expectedCacheKey = $"test_cache_key_{community}_{region}";
        var queryResponse = Result.Success(new TestResponse { Id = 5, Name = "Cultural Test" });
        
        _mockCacheService
            .Setup(x => x.GetAsync<Result<TestResponse>>(expectedCacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result<TestResponse>)null!);
        
        _mockNext.Setup(x => x()).ReturnsAsync(queryResponse);

        // Act
        await _behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockCacheService.Verify(x => x.GetAsync<Result<TestResponse>>(expectedCacheKey, CancellationToken.None), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(expectedCacheKey, queryResponse, TimeSpan.FromHours(1), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_CancellationRequested_ShouldRespectCancellation()
    {
        // Arrange
        var request = new TestCacheableQuery { CommunityId = "buddhist", Region = "sri_lanka" };
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        _mockCacheService
            .Setup(x => x.GetAsync<Result<TestResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _behavior.Handle(request, _mockNext.Object, cts.Token));
    }
}

// Test classes
public class TestCacheableQuery : IRequest<Result<TestResponse>>, ICacheableQuery
{
    public string CommunityId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public bool EnableCaching { get; set; } = true;

    public string GetCacheKey()
    {
        return $"test_cache_key_{CommunityId}_{Region}";
    }

    public TimeSpan GetCacheTtl()
    {
        return TimeSpan.FromHours(1);
    }

    public bool ShouldCache()
    {
        return EnableCaching;
    }
}

public class TestNonCacheableQuery : IRequest<Result<TestResponse>>
{
    public string Name { get; set; } = string.Empty;
}

public class TestResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}