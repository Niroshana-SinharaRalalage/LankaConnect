using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// TDD Tests for Event.AddVideo functionality (Epic 2 - Video Support Feature)
/// Tests the domain behavior for adding videos to events
/// </summary>
public class EventAddVideoTests
{
    private Event CreateTestEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);

        return Event.Create(
            title,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            100
        ).Value;
    }

    #region AddVideo - Success Cases

    [Fact]
    public void AddVideo_WithValidVideo_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act
        var result = @event.AddVideo(
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.VideoUrl.Should().Be("https://blob.azure.com/video.mp4");
        result.Value.BlobName.Should().Be("video.mp4");
        result.Value.ThumbnailUrl.Should().Be("https://blob.azure.com/thumb.jpg");
        result.Value.DisplayOrder.Should().Be(1, "first video should have display order 1");

        @event.Videos.Should().HaveCount(1);
        @event.Videos.First().Should().Be(result.Value);
    }

    [Fact]
    public void AddVideo_ShouldAssignSequentialDisplayOrders()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act
        var video1 = @event.AddVideo("https://blob.azure.com/video1.mp4", "video1.mp4",
            "https://blob.azure.com/thumb1.jpg", "thumb1.jpg", TimeSpan.FromMinutes(3), "mp4", 1024000L).Value;
        var video2 = @event.AddVideo("https://blob.azure.com/video2.mp4", "video2.mp4",
            "https://blob.azure.com/thumb2.jpg", "thumb2.jpg", TimeSpan.FromMinutes(4), "mp4", 2048000L).Value;
        var video3 = @event.AddVideo("https://blob.azure.com/video3.mp4", "video3.mp4",
            "https://blob.azure.com/thumb3.jpg", "thumb3.jpg", TimeSpan.FromMinutes(5), "mp4", 3072000L).Value;

        // Assert
        video1.DisplayOrder.Should().Be(1);
        video2.DisplayOrder.Should().Be(2);
        video3.DisplayOrder.Should().Be(3);
        @event.Videos.Should().HaveCount(3);
    }

    [Fact]
    public void AddVideo_WithNullDuration_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act
        var result = @event.AddVideo(
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            null, // Duration can be null for live streams or unknown duration
            "mp4",
            1024000L);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Duration.Should().BeNull();
    }

    #endregion

    #region AddVideo - Validation Failures

    [Fact]
    public void AddVideo_WithEmptyVideoUrl_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act
        var result = @event.AddVideo(
            "", // Empty URL
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Video URL");
    }

    [Fact]
    public void AddVideo_WithEmptyBlobName_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act
        var result = @event.AddVideo(
            "https://blob.azure.com/video.mp4",
            "", // Empty blob name
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Blob name");
    }

    [Fact]
    public void AddVideo_WhenMaxVideosReached_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Add 3 videos (maximum allowed)
        @event.AddVideo("https://blob.azure.com/video1.mp4", "video1.mp4",
            "https://blob.azure.com/thumb1.jpg", "thumb1.jpg", TimeSpan.FromMinutes(3), "mp4", 1024000L);
        @event.AddVideo("https://blob.azure.com/video2.mp4", "video2.mp4",
            "https://blob.azure.com/thumb2.jpg", "thumb2.jpg", TimeSpan.FromMinutes(4), "mp4", 2048000L);
        @event.AddVideo("https://blob.azure.com/video3.mp4", "video3.mp4",
            "https://blob.azure.com/thumb3.jpg", "thumb3.jpg", TimeSpan.FromMinutes(5), "mp4", 3072000L);

        // Act - Try to add a 4th video
        var result = @event.AddVideo(
            "https://blob.azure.com/video4.mp4",
            "video4.mp4",
            "https://blob.azure.com/thumb4.jpg",
            "thumb4.jpg",
            TimeSpan.FromMinutes(6),
            "mp4",
            4096000L);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("3");
        result.Error.Should().Contain("maximum");
        @event.Videos.Should().HaveCount(3);
    }

    #endregion

    #region AddVideo - Domain Events

    [Fact]
    public void AddVideo_ShouldRaiseDomainEvent()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.ClearDomainEvents(); // Clear any events from creation

        // Act
        @event.AddVideo(
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L);

        // Assert
        @event.DomainEvents.Should().NotBeEmpty();
        @event.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "VideoAddedToEventDomainEvent");
    }

    #endregion
}
