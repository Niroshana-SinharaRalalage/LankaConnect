using FluentAssertions;
using LankaConnect.Application.ReferenceData.DTOs;
using LankaConnect.Application.ReferenceData.Services;
using LankaConnect.Domain.ReferenceData.Entities;
using LankaConnect.Domain.ReferenceData.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.ReferenceData;

/// <summary>
/// Unit tests for ReferenceDataService
/// Phase 6A.47: Test unified reference data service with caching
/// </summary>
public class ReferenceDataServiceTests
{
    private readonly Mock<IReferenceDataRepository> _mockRepository;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<ReferenceDataService>> _mockLogger;
    private readonly ReferenceDataService _sut;

    public ReferenceDataServiceTests()
    {
        _mockRepository = new Mock<IReferenceDataRepository>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<ReferenceDataService>>();
        _sut = new ReferenceDataService(_mockRepository.Object, _cache, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByTypesAsync_ShouldReturnReferenceValues_WhenTypesExist()
    {
        // Arrange
        var enumTypes = new[] { "EventCategory", "EventStatus" };
        var mockData = new List<ReferenceValue>
        {
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Religious", 0, "Religious", 1, "Religious events"),
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Cultural", 1, "Cultural", 2, "Cultural events"),
            ReferenceValue.Create(Guid.NewGuid(), "EventStatus", "Draft", 0, "Draft", 1, "Draft status"),
            ReferenceValue.Create(Guid.NewGuid(), "EventStatus", "Published", 1, "Published", 2, "Published status")
        };

        _mockRepository
            .Setup(r => r.GetByTypesAsync(It.IsAny<IEnumerable<string>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        // Act
        var result = await _sut.GetByTypesAsync(enumTypes);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result.Should().Contain(dto => dto.EnumType == "EventCategory");
        result.Should().Contain(dto => dto.EnumType == "EventStatus");

        _mockRepository.Verify(
            r => r.GetByTypesAsync(It.IsAny<IEnumerable<string>>(), true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByTypesAsync_ShouldUseCachedData_OnSecondCall()
    {
        // Arrange
        var enumTypes = new[] { "EventCategory" };
        var mockData = new List<ReferenceValue>
        {
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Religious", 0, "Religious", 1, "Religious events")
        };

        _mockRepository
            .Setup(r => r.GetByTypesAsync(It.IsAny<IEnumerable<string>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        // Act - First call (cache miss)
        var result1 = await _sut.GetByTypesAsync(enumTypes);

        // Act - Second call (cache hit)
        var result2 = await _sut.GetByTypesAsync(enumTypes);

        // Assert
        result1.Should().HaveCount(1);
        result2.Should().HaveCount(1);
        result1.Should().BeEquivalentTo(result2);

        // Repository should only be called once (second call uses cache)
        _mockRepository.Verify(
            r => r.GetByTypesAsync(It.IsAny<IEnumerable<string>>(), true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByTypesAsync_ShouldRespectActiveOnlyFilter()
    {
        // Arrange
        var enumTypes = new[] { "EventCategory" };
        var activeOnly = false;

        var mockData = new List<ReferenceValue>
        {
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Religious", 0, "Religious", 1, "Religious events"),
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Archived", 99, "Archived", 99, "Archived category")
        };

        _mockRepository
            .Setup(r => r.GetByTypesAsync(It.IsAny<IEnumerable<string>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        // Act
        var result = await _sut.GetByTypesAsync(enumTypes, activeOnly);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(dto => dto.Code == "Religious");
        result.Should().Contain(dto => dto.Code == "Archived");

        _mockRepository.Verify(
            r => r.GetByTypesAsync(It.IsAny<IEnumerable<string>>(), false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByTypesAsync_ShouldMapMetadataCorrectly()
    {
        // Arrange
        var enumTypes = new[] { "EventCategory" };
        var mockData = new List<ReferenceValue>
        {
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Religious", 0, "Religious", 1, "Religious events",
                new Dictionary<string, object> { { "iconUrl", "icon.png" } })
        };

        _mockRepository
            .Setup(r => r.GetByTypesAsync(It.IsAny<IEnumerable<string>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        // Act
        var result = await _sut.GetByTypesAsync(enumTypes);

        // Assert
        var dto = result.First();
        dto.Metadata.Should().NotBeNull();
        dto.Metadata.Should().ContainKey("iconUrl");
    }

    [Fact]
    public async Task GetByTypesAsync_ShouldReturnEmptyList_WhenNoDataExists()
    {
        // Arrange
        var enumTypes = new[] { "NonExistentType" };
        _mockRepository
            .Setup(r => r.GetByTypesAsync(It.IsAny<IEnumerable<string>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReferenceValue>());

        // Act
        var result = await _sut.GetByTypesAsync(enumTypes);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidateCacheAsync_ShouldClearCache_ForSpecificType()
    {
        // Arrange
        var enumTypes = new[] { "EventCategory" };
        var mockData = new List<ReferenceValue>
        {
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Religious", 0, "Religious", 1, "Religious events")
        };

        _mockRepository
            .Setup(r => r.GetByTypesAsync(It.IsAny<IEnumerable<string>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        // Populate cache
        await _sut.GetByTypesAsync(enumTypes);

        // Act - Invalidate cache
        await _sut.InvalidateCacheAsync("eventcategory");

        // Act - Fetch again (should hit repository)
        await _sut.GetByTypesAsync(enumTypes);

        // Assert - Repository should be called twice (once before invalidation, once after)
        _mockRepository.Verify(
            r => r.GetByTypesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task InvalidateAllCachesAsync_ShouldClearAllCaches()
    {
        // Arrange
        var mockEventCategories = new List<ReferenceValue>
        {
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Religious", 0, "Religious", 1, "Religious events")
        };
        var mockEventStatuses = new List<ReferenceValue>
        {
            ReferenceValue.Create(Guid.NewGuid(), "EventStatus", "Draft", 0, "Draft", 1, "Draft status")
        };

        _mockRepository
            .Setup(r => r.GetByTypesAsync(It.Is<IEnumerable<string>>(types => types.Contains("EventCategory")), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEventCategories);

        _mockRepository
            .Setup(r => r.GetByTypesAsync(It.Is<IEnumerable<string>>(types => types.Contains("EventStatus")), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEventStatuses);

        // Populate both caches
        await _sut.GetByTypesAsync(new[] { "EventCategory" });
        await _sut.GetByTypesAsync(new[] { "EventStatus" });

        // Act - Invalidate all caches
        await _sut.InvalidateAllCachesAsync();

        // Act - Fetch again (should hit repository for both)
        await _sut.GetByTypesAsync(new[] { "EventCategory" });
        await _sut.GetByTypesAsync(new[] { "EventStatus" });

        // Assert - Repository should be called 4 times total (2 before, 2 after invalidation)
        _mockRepository.Verify(
            r => r.GetByTypesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }
}
