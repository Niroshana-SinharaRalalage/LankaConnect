using FluentAssertions;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Common;
using Xunit;

namespace LankaConnect.Domain.Tests.Business;

public sealed class BusinessImageManagementTests
{
    private readonly Domain.Business.Business _business;

    public BusinessImageManagementTests()
    {
        var businessProfileResult = BusinessProfile.Create("Test Business", "Test Description", null, null, new List<string> { "food" }, new List<string> { "restaurant" });
        if (!businessProfileResult.IsSuccess) throw new InvalidOperationException($"BusinessProfile create failed: {string.Join(", ", businessProfileResult.Errors)}");
        
        var businessLocationResult = BusinessLocation.Create("123 Test St", "Test City", "Test Province", "12345", "US", 40.7128m, -74.0060m);
        if (!businessLocationResult.IsSuccess) throw new InvalidOperationException($"BusinessLocation create failed: {string.Join(", ", businessLocationResult.Errors)}");
        
        var contactInfoResult = ContactInformation.Create("+1-555-0123", "test@test.com", "https://test.com");
        if (!contactInfoResult.IsSuccess) throw new InvalidOperationException($"ContactInformation create failed: {string.Join(", ", contactInfoResult.Errors)}");
        
        var businessHoursResult = BusinessHours.Create(new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Tuesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Wednesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Thursday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Friday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Saturday, (null, null) },
            { DayOfWeek.Sunday, (null, null) }
        });
        if (!businessHoursResult.IsSuccess) throw new InvalidOperationException($"BusinessHours create failed: {string.Join(", ", businessHoursResult.Errors)}");

        _business = Domain.Business.Business.Create(
            businessProfileResult.Value,
            businessLocationResult.Value,
            contactInfoResult.Value,
            businessHoursResult.Value,
            BusinessCategory.Restaurant,
            Guid.NewGuid()).Value;
    }

    [Fact]
    public void AddImage_WithValidImage_ShouldAddImageToBusiness()
    {
        // Arrange
        var image = CreateTestBusinessImage(displayOrder: 1, isPrimary: false);

        // Act
        var result = _business.AddImage(image);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _business.Images.Should().HaveCount(1);
        _business.Images.Should().Contain(image);
    }

    [Fact]
    public void AddImage_WithNullImage_ShouldReturnFailure()
    {
        // Act
        var result = _business.AddImage(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Business image is required");
        _business.Images.Should().BeEmpty();
    }

    [Fact]
    public void AddImage_WithDuplicateUrl_ShouldReturnFailure()
    {
        // Arrange - Create images with the exact same URL (no unique ID)
        const string sameUrl = "https://test.com/image1.jpg";
        var image1 = BusinessImage.Create(
            sameUrl,
            "https://test.com/thumb1.jpg",
            "https://test.com/med1.jpg", 
            "https://test.com/large1.jpg",
            "Alt text 1",
            "Caption 1",
            0,
            false,
            1024L,
            "image/jpeg").Value;
            
        var image2 = BusinessImage.Create(
            sameUrl, // Same URL as image1
            "https://test.com/thumb2.jpg",
            "https://test.com/med2.jpg", 
            "https://test.com/large2.jpg",
            "Alt text 2",
            "Caption 2",
            1,
            false,
            1024L,
            "image/jpeg").Value;

        _business.AddImage(image1);

        // Act
        var result = _business.AddImage(image2);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("An image with this URL already exists");
        _business.Images.Should().HaveCount(1);
    }

    [Fact]
    public void AddImage_WithPrimaryImage_ShouldSetAsPrimaryAndRemoveOtherPrimary()
    {
        // Arrange
        var image1 = CreateTestBusinessImage(displayOrder: 1, isPrimary: true);
        var image2 = CreateTestBusinessImage(displayOrder: 2, isPrimary: true);

        // Act
        _business.AddImage(image1);
        _business.AddImage(image2);

        // Assert
        _business.Images.Should().HaveCount(2);
        _business.Images.Count(img => img.IsPrimary).Should().Be(1);
        _business.Images.Single(img => img.IsPrimary).Should().Be(image2);
        _business.Images.Single(img => !img.IsPrimary).Should().Be(image1);
    }

    [Fact]
    public void RemoveImage_WithExistingImage_ShouldRemoveImage()
    {
        // Arrange
        var image = CreateTestBusinessImage();
        _business.AddImage(image);

        // Act
        var result = _business.RemoveImage(image.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _business.Images.Should().BeEmpty();
    }

    [Fact]
    public void RemoveImage_WithNonExistentImage_ShouldReturnFailure()
    {
        // Act
        var result = _business.RemoveImage("non-existent-id");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Image not found");
    }

    [Fact]
    public void RemoveImage_WithPrimaryImage_ShouldSetAnotherImageAsPrimary()
    {
        // Arrange
        var image1 = CreateTestBusinessImage(displayOrder: 1, isPrimary: true);
        var image2 = CreateTestBusinessImage(displayOrder: 2, isPrimary: false);

        _business.AddImage(image1);
        _business.AddImage(image2);

        // Act
        var result = _business.RemoveImage(image1.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _business.Images.Should().HaveCount(1);
        _business.Images.Single().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void SetPrimaryImage_WithExistingImage_ShouldSetAsPrimary()
    {
        // Arrange
        var image1 = CreateTestBusinessImage(displayOrder: 1, isPrimary: true);
        var image2 = CreateTestBusinessImage(displayOrder: 2, isPrimary: false);

        _business.AddImage(image1);
        _business.AddImage(image2);

        // Act
        var result = _business.SetPrimaryImage(image2.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _business.Images.Count(img => img.IsPrimary).Should().Be(1);
        _business.Images.Single(img => img.IsPrimary).Id.Should().Be(image2.Id);
    }

    [Fact]
    public void SetPrimaryImage_WithNonExistentImage_ShouldReturnFailure()
    {
        // Act
        var result = _business.SetPrimaryImage("non-existent-id");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Image not found");
    }

    [Fact]
    public void SetPrimaryImage_WithAlreadyPrimaryImage_ShouldReturnFailure()
    {
        // Arrange
        var image = CreateTestBusinessImage(isPrimary: true);
        _business.AddImage(image);

        // Act
        var result = _business.SetPrimaryImage(image.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Image is already set as primary");
    }

    [Fact]
    public void UpdateImageMetadata_WithExistingImage_ShouldUpdateMetadata()
    {
        // Arrange
        var image = CreateTestBusinessImage();
        _business.AddImage(image);

        var newAltText = "Updated alt text";
        var newCaption = "Updated caption";
        var newDisplayOrder = 5;

        // Act
        var result = _business.UpdateImageMetadata(image.Id, newAltText, newCaption, newDisplayOrder);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        var updatedImage = _business.Images.Single();
        updatedImage.AltText.Should().Be(newAltText);
        updatedImage.Caption.Should().Be(newCaption);
        updatedImage.DisplayOrder.Should().Be(newDisplayOrder);
    }

    [Fact]
    public void UpdateImageMetadata_WithNonExistentImage_ShouldReturnFailure()
    {
        // Act
        var result = _business.UpdateImageMetadata("non-existent-id", "alt", "caption", 1);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Image not found");
    }

    [Fact]
    public void ReorderImages_WithValidOrder_ShouldReorderImages()
    {
        // Arrange
        var image1 = CreateTestBusinessImage(displayOrder: 0);
        var image2 = CreateTestBusinessImage(displayOrder: 1);
        var image3 = CreateTestBusinessImage(displayOrder: 2);

        _business.AddImage(image1);
        _business.AddImage(image2);
        _business.AddImage(image3);

        var newOrder = new List<string> { image3.Id, image1.Id, image2.Id };

        // Act
        var result = _business.ReorderImages(newOrder);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        var reorderedImages = _business.GetImagesSortedByDisplayOrder();
        reorderedImages[0].Id.Should().Be(image3.Id);
        reorderedImages[0].DisplayOrder.Should().Be(0);
        reorderedImages[1].Id.Should().Be(image1.Id);
        reorderedImages[1].DisplayOrder.Should().Be(1);
        reorderedImages[2].Id.Should().Be(image2.Id);
        reorderedImages[2].DisplayOrder.Should().Be(2);
    }

    [Fact]
    public void ReorderImages_WithIncompleteImageIds_ShouldReturnFailure()
    {
        // Arrange
        var image1 = CreateTestBusinessImage();
        var image2 = CreateTestBusinessImage();

        _business.AddImage(image1);
        _business.AddImage(image2);

        var incompleteOrder = new List<string> { image1.Id }; // Missing image2.Id

        // Act
        var result = _business.ReorderImages(incompleteOrder);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("All image IDs must be provided for reordering");
    }

    [Fact]
    public void ReorderImages_WithDuplicateImageIds_ShouldReturnFailure()
    {
        // Arrange
        var image = CreateTestBusinessImage();
        _business.AddImage(image);

        var duplicateOrder = new List<string> { image.Id, image.Id };

        // Act
        var result = _business.ReorderImages(duplicateOrder);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Duplicate image IDs are not allowed");
    }

    [Fact]
    public void ReorderImages_WithNonExistentImageId_ShouldReturnFailure()
    {
        // Arrange
        var image = CreateTestBusinessImage();
        _business.AddImage(image);

        var invalidOrder = new List<string> { image.Id, "non-existent-id" };

        // Act
        var result = _business.ReorderImages(invalidOrder);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("One or more image IDs do not exist");
    }

    [Fact]
    public void GetPrimaryImage_WithPrimaryImagePresent_ShouldReturnPrimaryImage()
    {
        // Arrange
        var primaryImage = CreateTestBusinessImage(isPrimary: true);
        var regularImage = CreateTestBusinessImage(isPrimary: false);

        _business.AddImage(regularImage);
        _business.AddImage(primaryImage);

        // Act
        var result = _business.GetPrimaryImage();

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(primaryImage);
    }

    [Fact]
    public void GetPrimaryImage_WithNoPrimaryImage_ShouldReturnNull()
    {
        // Arrange
        var image = CreateTestBusinessImage(isPrimary: false);
        _business.AddImage(image);

        // Act
        var result = _business.GetPrimaryImage();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetImagesSortedByDisplayOrder_ShouldReturnImagesInCorrectOrder()
    {
        // Arrange
        var image3 = CreateTestBusinessImage(displayOrder: 3);
        var image1 = CreateTestBusinessImage(displayOrder: 1);
        var image2 = CreateTestBusinessImage(displayOrder: 2);

        // Add in random order
        _business.AddImage(image3);
        _business.AddImage(image1);
        _business.AddImage(image2);

        // Act
        var sortedImages = _business.GetImagesSortedByDisplayOrder();

        // Assert
        sortedImages.Should().HaveCount(3);
        sortedImages[0].Should().Be(image1);
        sortedImages[1].Should().Be(image2);
        sortedImages[2].Should().Be(image3);
    }

    #region Strategic Edge Cases - Architect Guidance Phase 2

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateImageMetadata_WithInvalidAltText_ShouldValidateAppropriately(string? invalidAltText)
    {
        // Arrange
        var image = CreateTestBusinessImage();
        _business.AddImage(image);

        // Act
        var result = _business.UpdateImageMetadata(image.Id, invalidAltText ?? string.Empty, "Valid caption", 1);

        // Assert - Should handle invalid alt text gracefully based on business rules
        if (result.IsSuccess)
        {
            // Business rules allow empty/null alt text
            var updatedImage = _business.Images.First();
            updatedImage.AltText.Should().Be(invalidAltText ?? string.Empty);
        }
        else
        {
            // Business rules require alt text
            result.Errors.Should().NotBeEmpty();
        }
    }

    [Fact]
    public void AddImage_WithExtremeDisplayOrder_ShouldHandleGracefully()
    {
        // Arrange - Test boundary conditions
        var imageMaxResult = TryCreateTestBusinessImage(displayOrder: int.MaxValue);
        var imageMinResult = TryCreateTestBusinessImage(displayOrder: 0);
        var imageNegativeResult = TryCreateTestBusinessImage(displayOrder: -1);

        // Act & Assert for valid images
        if (imageMaxResult.IsSuccess)
        {
            var resultMax = _business.AddImage(imageMaxResult.Value);
            resultMax.IsSuccess.Should().BeTrue("Max display order should be acceptable");
        }

        if (imageMinResult.IsSuccess)
        {
            var resultMin = _business.AddImage(imageMinResult.Value);
            resultMin.IsSuccess.Should().BeTrue("Zero display order should be acceptable");
        }

        // Negative display order should fail at BusinessImage creation level
        imageNegativeResult.IsSuccess.Should().BeFalse("Negative display order should be rejected");
        if (imageNegativeResult.IsFailure)
        {
            imageNegativeResult.Errors.Should().NotBeEmpty("Should have validation error for negative display order");
        }
    }

    [Fact]
    public void ImageOperations_WithUnicodeContent_ShouldHandleInternationalization()
    {
        // Arrange - Test unicode support
        var unicodeAltText = "商品图片"; // Chinese
        var unicodeCaption = "පින්තූර විස්තරය"; // Sinhala
        
        var image = CreateTestBusinessImageWithUnicode(unicodeAltText, unicodeCaption);
        
        // Act
        var result = _business.AddImage(image);

        // Assert
        result.IsSuccess.Should().BeTrue("Unicode content should be supported");
        if (result.IsSuccess)
        {
            var addedImage = _business.Images.First();
            addedImage.AltText.Should().Be(unicodeAltText);
            addedImage.Caption.Should().Be(unicodeCaption);
        }
    }

    [Fact]
    public void ReorderImages_WithLargeImageSet_ShouldMaintainPerformance()
    {
        // Arrange - Performance test with reasonable size
        const int imageCount = 10; // Reasonable for unit test
        var images = new List<BusinessImage>();
        
        for (int i = 0; i < imageCount; i++)
        {
            var image = CreateTestBusinessImage($"https://test.com/perf_{i}.jpg", i);
            images.Add(image);
            _business.AddImage(image);
        }

        var reverseOrder = images.Select(img => img.Id).Reverse().ToList();

        // Act - Performance measurement
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = _business.ReorderImages(reverseOrder);
        stopwatch.Stop();

        // Assert - Performance and correctness
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Reordering should be efficient");
        
        var reorderedImages = _business.GetImagesSortedByDisplayOrder();
        reorderedImages.Should().HaveCount(imageCount);
    }

    [Fact]
    public void PrimaryImageInvariants_MultiplePrimaryOperations_ShouldMaintainConsistency()
    {
        // Arrange - Test invariant enforcement
        var image1 = CreateTestBusinessImage(isPrimary: true);
        var image2 = CreateTestBusinessImage(isPrimary: true);
        var image3 = CreateTestBusinessImage(isPrimary: true);

        // Act - Sequential primary image operations
        _business.AddImage(image1);
        _business.AddImage(image2);
        _business.AddImage(image3);

        // Assert - Only one primary image should exist (invariant)
        _business.Images.Count(img => img.IsPrimary).Should().Be(1, 
            "Business should maintain invariant of exactly one primary image");
        
        // Last added should be primary (or first, depending on business rules)
        var primaryCount = _business.Images.Count(img => img.IsPrimary);
        primaryCount.Should().Be(1);
    }

    [Fact]
    public void BusinessImageCollection_ReadOnlyEnforcement_ShouldPreventDirectModification()
    {
        // Arrange
        var image = CreateTestBusinessImage();
        _business.AddImage(image);

        // Act & Assert - Collection should be read-only
        var imagesCollection = _business.Images;
        imagesCollection.Should().BeOfType<System.Collections.ObjectModel.ReadOnlyCollection<BusinessImage>>(
            "Images collection should be read-only to maintain aggregate boundaries");
        
        // Verify we cannot modify the collection directly
        imagesCollection.Should().HaveCount(1);
        // Any attempt to cast and modify should fail at compile time
    }

    #endregion

    private static BusinessImage CreateTestBusinessImage(
        string originalUrl = "https://test.com/original.jpg",
        int displayOrder = 0,
        bool isPrimary = false)
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        return BusinessImage.Create(
            $"{originalUrl}?id={uniqueId}",
            $"https://test.com/thumbnail_{uniqueId}.jpg",
            $"https://test.com/medium_{uniqueId}.jpg",
            $"https://test.com/large_{uniqueId}.jpg",
            $"Alt text {uniqueId}",
            $"Caption {uniqueId}",
            displayOrder,
            isPrimary,
            1024L,
            "image/jpeg").Value;
    }

    private static BusinessImage CreateTestBusinessImageWithUnicode(string altText, string caption)
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        return BusinessImage.Create(
            $"https://test.com/unicode_{uniqueId}.jpg",
            $"https://test.com/thumb_{uniqueId}.jpg",
            $"https://test.com/medium_{uniqueId}.jpg",
            $"https://test.com/large_{uniqueId}.jpg",
            altText,
            caption,
            0,
            false,
            1024L,
            "image/jpeg").Value;
    }

    private static Result<BusinessImage> TryCreateTestBusinessImage(
        string originalUrl = "https://test.com/original.jpg",
        int displayOrder = 0,
        bool isPrimary = false)
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        return BusinessImage.Create(
            $"{originalUrl}?id={uniqueId}",
            $"https://test.com/thumbnail_{uniqueId}.jpg",
            $"https://test.com/medium_{uniqueId}.jpg",
            $"https://test.com/large_{uniqueId}.jpg",
            $"Alt text {uniqueId}",
            $"Caption {uniqueId}",
            displayOrder,
            isPrimary,
            1024L,
            "image/jpeg");
    }
}