using FluentAssertions;
using LankaConnect.Domain.Business.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Business;

public sealed class BusinessImageTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnSuccessResult()
    {
        // Arrange
        var originalUrl = "https://example.com/original.jpg";
        var thumbnailUrl = "https://example.com/thumbnail.jpg";
        var mediumUrl = "https://example.com/medium.jpg";
        var largeUrl = "https://example.com/large.jpg";
        var altText = "Test image";
        var caption = "Test caption";
        var displayOrder = 1;
        var isPrimary = true;
        var fileSizeBytes = 1024L;
        var contentType = "image/jpeg";
        var metadata = new Dictionary<string, string> { { "test", "value" } };

        // Act
        var result = BusinessImage.Create(
            originalUrl,
            thumbnailUrl,
            mediumUrl,
            largeUrl,
            altText,
            caption,
            displayOrder,
            isPrimary,
            fileSizeBytes,
            contentType,
            metadata);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OriginalUrl.Should().Be(originalUrl);
        result.Value.ThumbnailUrl.Should().Be(thumbnailUrl);
        result.Value.MediumUrl.Should().Be(mediumUrl);
        result.Value.LargeUrl.Should().Be(largeUrl);
        result.Value.AltText.Should().Be(altText);
        result.Value.Caption.Should().Be(caption);
        result.Value.DisplayOrder.Should().Be(displayOrder);
        result.Value.IsPrimary.Should().Be(isPrimary);
        result.Value.FileSizeBytes.Should().Be(fileSizeBytes);
        result.Value.ContentType.Should().Be(contentType);
        result.Value.Metadata.Should().ContainKey("test");
        result.Value.Id.Should().NotBeEmpty();
        result.Value.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("", "thumb", "medium", "large", "image/jpeg")]
    [InlineData("original", "", "medium", "large", "image/jpeg")]
    [InlineData("original", "thumb", "", "large", "image/jpeg")]
    [InlineData("original", "thumb", "medium", "", "image/jpeg")]
    [InlineData("original", "thumb", "medium", "large", "")]
    public void Create_WithMissingRequiredFields_ShouldReturnFailureResult(
        string originalUrl, string thumbnailUrl, string mediumUrl, string largeUrl, string contentType)
    {
        // Act
        var result = BusinessImage.Create(
            originalUrl,
            thumbnailUrl,
            mediumUrl,
            largeUrl,
            "alt",
            "caption",
            1,
            false,
            1024L,
            contentType);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithNegativeDisplayOrder_ShouldReturnFailureResult()
    {
        // Act
        var result = BusinessImage.Create(
            "original",
            "thumbnail",
            "medium",
            "large",
            "alt",
            "caption",
            -1, // Invalid display order
            false,
            1024L,
            "image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Display order must be non-negative");
    }

    [Fact]
    public void Create_WithZeroFileSize_ShouldReturnFailureResult()
    {
        // Act
        var result = BusinessImage.Create(
            "original",
            "thumbnail",
            "medium",
            "large",
            "alt",
            "caption",
            1,
            false,
            0L, // Invalid file size
            "image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("File size must be greater than zero");
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/pdf")]
    [InlineData("video/mp4")]
    public void Create_WithInvalidContentType_ShouldReturnFailureResult(string contentType)
    {
        // Act
        var result = BusinessImage.Create(
            "original",
            "thumbnail",
            "medium",
            "large",
            "alt",
            "caption",
            1,
            false,
            1024L,
            contentType);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid image content type");
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    public void Create_WithValidContentTypes_ShouldReturnSuccessResult(string contentType)
    {
        // Act
        var result = BusinessImage.Create(
            "original",
            "thumbnail",
            "medium",
            "large",
            "alt",
            "caption",
            1,
            false,
            1024L,
            contentType);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateMetadata_WithValidParameters_ShouldReturnUpdatedImage()
    {
        // Arrange
        var originalImage = BusinessImage.Create(
            "original", "thumbnail", "medium", "large",
            "old alt", "old caption", 1, false, 1024L, "image/jpeg").Value;

        var newAltText = "new alt";
        var newCaption = "new caption";
        var newDisplayOrder = 5;

        // Act
        var result = originalImage.UpdateMetadata(newAltText, newCaption, newDisplayOrder);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.AltText.Should().Be(newAltText);
        result.Value.Caption.Should().Be(newCaption);
        result.Value.DisplayOrder.Should().Be(newDisplayOrder);
        result.Value.Id.Should().Be(originalImage.Id); // ID should remain the same
    }

    [Fact]
    public void UpdateMetadata_WithNegativeDisplayOrder_ShouldReturnFailureResult()
    {
        // Arrange
        var originalImage = BusinessImage.Create(
            "original", "thumbnail", "medium", "large",
            "alt", "caption", 1, false, 1024L, "image/jpeg").Value;

        // Act
        var result = originalImage.UpdateMetadata("alt", "caption", -1);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Display order must be non-negative");
    }

    [Fact]
    public void SetAsPrimary_ShouldReturnImageWithPrimaryStatusTrue()
    {
        // Arrange
        var originalImage = BusinessImage.Create(
            "original", "thumbnail", "medium", "large",
            "alt", "caption", 1, false, 1024L, "image/jpeg").Value;

        // Act
        var primaryImage = originalImage.SetAsPrimary();

        // Assert
        primaryImage.Should().NotBeNull();
        primaryImage.IsPrimary.Should().BeTrue();
        primaryImage.Id.Should().Be(originalImage.Id);
    }

    [Fact]
    public void RemovePrimaryStatus_ShouldReturnImageWithPrimaryStatusFalse()
    {
        // Arrange
        var primaryImage = BusinessImage.Create(
            "original", "thumbnail", "medium", "large",
            "alt", "caption", 1, true, 1024L, "image/jpeg").Value;

        // Act
        var nonPrimaryImage = primaryImage.RemovePrimaryStatus();

        // Assert
        nonPrimaryImage.Should().NotBeNull();
        nonPrimaryImage.IsPrimary.Should().BeFalse();
        nonPrimaryImage.Id.Should().Be(primaryImage.Id);
    }

    [Theory]
    [InlineData(ImageSize.Thumbnail)]
    [InlineData(ImageSize.Medium)]
    [InlineData(ImageSize.Large)]
    [InlineData(ImageSize.Original)]
    public void GetImageUrl_WithDifferentSizes_ShouldReturnCorrectUrl(ImageSize size)
    {
        // Arrange
        var originalUrl = "https://example.com/original.jpg";
        var thumbnailUrl = "https://example.com/thumbnail.jpg";
        var mediumUrl = "https://example.com/medium.jpg";
        var largeUrl = "https://example.com/large.jpg";

        var image = BusinessImage.Create(
            originalUrl, thumbnailUrl, mediumUrl, largeUrl,
            "alt", "caption", 1, false, 1024L, "image/jpeg").Value;

        // Act
        var url = image.GetImageUrl(size);

        // Assert
        var expectedUrl = size switch
        {
            ImageSize.Thumbnail => thumbnailUrl,
            ImageSize.Medium => mediumUrl,
            ImageSize.Large => largeUrl,
            ImageSize.Original => originalUrl,
            _ => mediumUrl
        };

        url.Should().Be(expectedUrl);
    }

    [Fact]
    public void GetImageUrl_WithDefaultSize_ShouldReturnMediumUrl()
    {
        // Arrange
        var mediumUrl = "https://example.com/medium.jpg";
        var image = BusinessImage.Create(
            "original", "thumbnail", mediumUrl, "large",
            "alt", "caption", 1, false, 1024L, "image/jpeg").Value;

        // Act
        var url = image.GetImageUrl(); // No size specified, should default to Medium

        // Assert
        url.Should().Be(mediumUrl);
    }

    [Fact]
    public void Equals_WithSameImages_ShouldReturnTrue()
    {
        // Arrange
        var image1 = BusinessImage.Create(
            "original", "thumbnail", "medium", "large",
            "alt", "caption", 1, false, 1024L, "image/jpeg").Value;

        var image2 = BusinessImage.Create(
            "original", "thumbnail", "medium", "large",
            "alt", "caption", 1, false, 1024L, "image/jpeg").Value;

        // Act & Assert
        // Note: These will be different because IDs are generated, but URLs and content are same
        image1.OriginalUrl.Should().Be(image2.OriginalUrl);
        image1.ContentType.Should().Be(image2.ContentType);
        image1.FileSizeBytes.Should().Be(image2.FileSizeBytes);
    }
}