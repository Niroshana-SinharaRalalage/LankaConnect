using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// TDD Tests for Event.RemoveVideo functionality (Epic 2 - Video Support Feature)
/// Tests the domain behavior for removing videos from events
/// </summary>
public class EventRemoveVideoTests
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

    #region RemoveVideo - Success Cases

    [Fact]
    public void RemoveVideo_WithValidVideoId_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var video = @event.AddVideo(
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L).Value;

        // Act
        var result = @event.RemoveVideo(video.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Videos.Should().BeEmpty();
    }

    [Fact]
    public void RemoveVideo_ShouldResequenceRemainingVideos()
    {
        // Arrange
        var @event = CreateTestEvent();
        var video1 = @event.AddVideo("https://blob.azure.com/video1.mp4", "video1.mp4",
            "https://blob.azure.com/thumb1.jpg", "thumb1.jpg", TimeSpan.FromMinutes(3), "mp4", 1024000L).Value;
        var video2 = @event.AddVideo("https://blob.azure.com/video2.mp4", "video2.mp4",
            "https://blob.azure.com/thumb2.jpg", "thumb2.jpg", TimeSpan.FromMinutes(4), "mp4", 2048000L).Value;
        var video3 = @event.AddVideo("https://blob.azure.com/video3.mp4", "video3.mp4",
            "https://blob.azure.com/thumb3.jpg", "thumb3.jpg", TimeSpan.FromMinutes(5), "mp4", 3072000L).Value;

        // Act - Remove middle video
        @event.RemoveVideo(video2.Id);

        // Assert
        @event.Videos.Should().HaveCount(2);
        var remainingVideos = @event.Videos.OrderBy(v => v.DisplayOrder).ToList();
        remainingVideos[0].Id.Should().Be(video1.Id);
        remainingVideos[0].DisplayOrder.Should().Be(1);
        remainingVideos[1].Id.Should().Be(video3.Id);
        remainingVideos[1].DisplayOrder.Should().Be(2, "should be resequenced from 3 to 2");
    }

    [Fact]
    public void RemoveVideo_LastVideo_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.AddVideo("https://blob.azure.com/video1.mp4", "video1.mp4",
            "https://blob.azure.com/thumb1.jpg", "thumb1.jpg", TimeSpan.FromMinutes(3), "mp4", 1024000L);
        @event.AddVideo("https://blob.azure.com/video2.mp4", "video2.mp4",
            "https://blob.azure.com/thumb2.jpg", "thumb2.jpg", TimeSpan.FromMinutes(4), "mp4", 2048000L);
        var video3 = @event.AddVideo("https://blob.azure.com/video3.mp4", "video3.mp4",
            "https://blob.azure.com/thumb3.jpg", "thumb3.jpg", TimeSpan.FromMinutes(5), "mp4", 3072000L).Value;

        // Act
        var result = @event.RemoveVideo(video3.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Videos.Should().HaveCount(2);
        @event.Videos.Max(v => v.DisplayOrder).Should().Be(2);
    }

    #endregion

    #region RemoveVideo - Validation Failures

    [Fact]
    public void RemoveVideo_WithNonExistentVideoId_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.AddVideo("https://blob.azure.com/video.mp4", "video.mp4",
            "https://blob.azure.com/thumb.jpg", "thumb.jpg", TimeSpan.FromMinutes(5), "mp4", 1024000L);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = @event.RemoveVideo(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Video");
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void RemoveVideo_WhenNoVideos_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();
        var randomId = Guid.NewGuid();

        // Act
        var result = @event.RemoveVideo(randomId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region RemoveVideo - Domain Events

    [Fact]
    public void RemoveVideo_ShouldRaiseDomainEvent()
    {
        // Arrange
        var @event = CreateTestEvent();
        var video = @event.AddVideo(
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L).Value;

        @event.ClearDomainEvents(); // Clear events from AddVideo

        // Act
        @event.RemoveVideo(video.Id);

        // Assert
        @event.DomainEvents.Should().NotBeEmpty();
        @event.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "VideoRemovedFromEventDomainEvent");
    }

    [Fact]
    public void RemoveVideo_DomainEventShouldIncludeBlobNames()
    {
        // Arrange
        var @event = CreateTestEvent();
        var video = @event.AddVideo(
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L).Value;

        @event.ClearDomainEvents();

        // Act
        @event.RemoveVideo(video.Id);

        // Assert - Event should include blob names for cleanup
        var domainEvent = @event.DomainEvents.FirstOrDefault(e => e.GetType().Name == "VideoRemovedFromEventDomainEvent");
        domainEvent.Should().NotBeNull();
        // The event should have properties for VideoBlobName and ThumbnailBlobName for cleanup
    }

    #endregion
}
