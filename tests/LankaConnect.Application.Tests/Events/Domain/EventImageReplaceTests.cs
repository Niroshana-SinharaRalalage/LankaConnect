using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// TDD Tests for ReplaceImage functionality (Epic 2 - Image Replace Feature)
/// Tests the domain behavior for replacing an image in event gallery
/// </summary>
public class EventImageReplaceTests
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
            Guid.NewGuid(), // organizerId
            100 // capacity
        ).Value;
    }

    #region ReplaceImage - Success Cases

    [Fact]
    public void ReplaceImage_WithValidImageAndNewUrl_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var originalImage = @event.AddImage("https://blob.azure.com/original.jpg", "original.jpg").Value;
        var imageId = originalImage.Id;

        // Act
        var result = @event.ReplaceImage(imageId, "https://blob.azure.com/new.jpg", "new.jpg");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImageUrl.Should().Be("https://blob.azure.com/new.jpg");
        result.Value.BlobName.Should().Be("new.jpg");
        result.Value.Id.Should().Be(imageId, "should maintain same ID");
        result.Value.DisplayOrder.Should().Be(originalImage.DisplayOrder, "should maintain display order");
    }

    [Fact]
    public void ReplaceImage_ShouldMaintainDisplayOrder()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.AddImage("https://blob.azure.com/image1.jpg", "image1.jpg");
        var image2 = @event.AddImage("https://blob.azure.com/image2.jpg", "image2.jpg").Value;
        @event.AddImage("https://blob.azure.com/image3.jpg", "image3.jpg");

        var originalDisplayOrder = image2.DisplayOrder; // Should be 2

        // Act
        var result = @event.ReplaceImage(image2.Id, "https://blob.azure.com/new-image2.jpg", "new-image2.jpg");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayOrder.Should().Be(originalDisplayOrder);
    }

    [Fact]
    public void ReplaceImage_ShouldNotAffectOtherImages()
    {
        // Arrange
        var @event = CreateTestEvent();
        var image1 = @event.AddImage("https://blob.azure.com/image1.jpg", "image1.jpg").Value;
        var image2 = @event.AddImage("https://blob.azure.com/image2.jpg", "image2.jpg").Value;
        var image3 = @event.AddImage("https://blob.azure.com/image3.jpg", "image3.jpg").Value;

        // Act
        @event.ReplaceImage(image2.Id, "https://blob.azure.com/new.jpg", "new.jpg");

        // Assert
        var images = @event.Images;
        images.Should().HaveCount(3);

        var unchangedImage1 = images.First(i => i.Id == image1.Id);
        unchangedImage1.ImageUrl.Should().Be("https://blob.azure.com/image1.jpg");
        unchangedImage1.BlobName.Should().Be("image1.jpg");

        var unchangedImage3 = images.First(i => i.Id == image3.Id);
        unchangedImage3.ImageUrl.Should().Be("https://blob.azure.com/image3.jpg");
        unchangedImage3.BlobName.Should().Be("image3.jpg");
    }

    #endregion

    #region ReplaceImage - Validation Failures

    [Fact]
    public void ReplaceImage_WithNonExistentImageId_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.AddImage("https://blob.azure.com/image1.jpg", "image1.jpg");
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = @event.ReplaceImage(nonExistentId, "https://blob.azure.com/new.jpg", "new.jpg");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Image not found");
    }

    [Fact]
    public void ReplaceImage_WithEmptyImageUrl_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();
        var image = @event.AddImage("https://blob.azure.com/original.jpg", "original.jpg").Value;

        // Act
        var result = @event.ReplaceImage(image.Id, "", "new.jpg");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Image URL");
    }

    [Fact]
    public void ReplaceImage_WithNullImageUrl_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();
        var image = @event.AddImage("https://blob.azure.com/original.jpg", "original.jpg").Value;

        // Act
        var result = @event.ReplaceImage(image.Id, null!, "new.jpg");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Image URL");
    }

    [Fact]
    public void ReplaceImage_WithEmptyBlobName_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();
        var image = @event.AddImage("https://blob.azure.com/original.jpg", "original.jpg").Value;

        // Act
        var result = @event.ReplaceImage(image.Id, "https://blob.azure.com/new.jpg", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Blob name");
    }

    [Fact]
    public void ReplaceImage_WithNullBlobName_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();
        var image = @event.AddImage("https://blob.azure.com/original.jpg", "original.jpg").Value;

        // Act
        var result = @event.ReplaceImage(image.Id, "https://blob.azure.com/new.jpg", null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Blob name");
    }

    [Fact]
    public void ReplaceImage_WhenEventHasNoImages_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();
        var randomId = Guid.NewGuid();

        // Act
        var result = @event.ReplaceImage(randomId, "https://blob.azure.com/new.jpg", "new.jpg");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Image not found");
    }

    #endregion

    #region ReplaceImage - Domain Events

    [Fact]
    public void ReplaceImage_ShouldRaiseDomainEvent()
    {
        // Arrange
        var @event = CreateTestEvent();
        var image = @event.AddImage("https://blob.azure.com/original.jpg", "original.jpg").Value;

        // Clear any existing domain events from AddImage
        @event.ClearDomainEvents();

        // Act
        @event.ReplaceImage(image.Id, "https://blob.azure.com/new.jpg", "new.jpg");

        // Assert
        @event.DomainEvents.Should().NotBeEmpty();
        @event.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "ImageReplacedInEventDomainEvent");
    }

    #endregion
}
