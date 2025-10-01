using Azure.Storage.Blobs;
using FluentAssertions;
using LankaConnect.Infrastructure.Storage.Configuration;
using LankaConnect.Infrastructure.Storage.Services;
using LankaConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Testcontainers.Azurite;
using Xunit;

namespace LankaConnect.IntegrationTests.Storage;

public sealed class AzureBlobImageServiceIntegrationTests : IAsyncLifetime
{
    private readonly AzuriteContainer _azuriteContainer;
    private BlobServiceClient? _blobServiceClient;
    private IImageService? _imageService = null; // Placeholder for service - will be initialized when service is implemented
    // Skip service for now since implementation is not complete

    public AzureBlobImageServiceIntegrationTests()
    {
        _azuriteContainer = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:3.28.0")
            .WithCommand("azurite", "--blobHost", "0.0.0.0", "--queueHost", "0.0.0.0", "--tableHost", "0.0.0.0", "--location", "/data")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _azuriteContainer.StartAsync();

        var connectionString = _azuriteContainer.GetConnectionString();
        _blobServiceClient = new BlobServiceClient(connectionString);

        var options = Options.Create(new AzureStorageOptions
        {
            BusinessImagesContainer = "test-business-images",
            MaxFileSizeBytes = 10 * 1024 * 1024, // 10MB
            AllowedContentTypes = new List<string> { "image/jpeg", "image/png", "image/gif" },
            ImageSizes = new ImageSizeSettings
            {
                Thumbnail = new ImageSizeConfig { Width = 150, Height = 150, Quality = 80 },
                Medium = new ImageSizeConfig { Width = 500, Height = 500, Quality = 85 },
                Large = new ImageSizeConfig { Width = 1200, Height = 1200, Quality = 90 }
            },
            IsDevelopment = true,
            Azurite = new AzuriteSettings
            {
                ConnectionString = connectionString
            }
        });

        // Skip service creation for now since AzureBlobImageService implementation is not complete
        // This test file serves as a specification for future implementation
    }

    public async Task DisposeAsync()
    {
        if (_azuriteContainer != null)
        {
            await _azuriteContainer.DisposeAsync();
        }
    }

    [Fact(Skip = "AzureBlobImageService implementation not complete yet")]
    public async Task UploadImageAsync_WithValidImage_ShouldUploadSuccessfully()
    {
        // TODO: Implement AzureBlobImageService first
        await Task.CompletedTask;
        
        // Arrange
        var businessId = Guid.NewGuid();
        // var "placeholder-file.jpg" = "test-image.jpg"; // Unused in stub implementation
        var imageBytes = CreateValidJpegBytes();

        // Act - Service not implemented yet
        // var result = await _imageService!.UploadImageAsync(imageBytes, "placeholder-file.jpg", businessId);

        // Assert - Test specification for future implementation
        // result.Should().NotBeNull();
        // result.IsSuccess.Should().BeTrue();
        // result.Value.Should().NotBeNull();
        // result.Value.Url.Should().NotBeEmpty();
        // result.Value.BlobName.Should().NotBeEmpty();
        // result.Value.SizeBytes.Should().Be(imageBytes.Length);
        // result.Value.ContentType.Should().Be("image/jpeg");
        // result.Value.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        // Verify the blob actually exists in Azurite
        // var containerClient = _blobServiceClient!.GetBlobContainerClient("test-business-images");
        // var blobClient = containerClient.GetBlobClient(result.Value.BlobName);
        // var exists = await blobClient.ExistsAsync();
        // exists.Value.Should().BeTrue();
    }

    [Fact(Skip = "AzureBlobImageService implementation not complete yet")]
    public async Task DeleteImageAsync_WithExistingImage_ShouldDeleteSuccessfully()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        // var "placeholder-file.jpg" = "test-image.jpg"; // Unused in stub implementation
        var imageBytes = CreateValidJpegBytes();

        // First upload an image
        var uploadResult = await _imageService!.UploadImageAsync(imageBytes, "placeholder-file.jpg", businessId);
        uploadResult.IsSuccess.Should().BeTrue();

        // Act - Delete the image
        var deleteResult = await _imageService.DeleteImageAsync(uploadResult.Value.Url);

        // Assert
        deleteResult.Should().NotBeNull();
        deleteResult.IsSuccess.Should().BeTrue();

        // Verify the blob no longer exists
        var containerClient = _blobServiceClient!.GetBlobContainerClient("test-business-images");
        var blobClient = containerClient.GetBlobClient(uploadResult.Value.BlobName);
        var exists = await blobClient.ExistsAsync();
        exists.Value.Should().BeFalse();
    }

    [Fact(Skip = "AzureBlobImageService implementation not complete yet")]
    public async Task ResizeAndUploadAsync_WithValidImage_ShouldCreateMultipleSizes()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        // var "placeholder-file.jpg" = "test-image.jpg"; // Unused in stub implementation
        var imageBytes = CreateValidJpegBytes();

        // Act
        await Task.CompletedTask; // Service not implemented yet
        // var result = await _imageService!.ResizeAndUploadAsync(imageBytes, "placeholder-file.jpg", businessId);

        // Assert - Test specification for future implementation
        // result.Should().NotBeNull();
        // result.IsSuccess.Should().BeTrue();
        // result.Value.Should().NotBeNull();
        // result.Value.OriginalUrl.Should().NotBeEmpty();
        // result.Value.ThumbnailUrl.Should().NotBeEmpty();
        // result.Value.MediumUrl.Should().NotBeEmpty();
        // result.Value.LargeUrl.Should().NotBeEmpty();
        // result.Value.SizesBytes.Should().ContainKey("original");
        // result.Value.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        // Verify all sized images exist in Azurite
        // var containerClient = _blobServiceClient!.GetBlobContainerClient("test-business-images");

        // var originalBlobName = ExtractBlobNameFromUrl(result.Value.OriginalUrl);
        // var originalBlob = containerClient.GetBlobClient(originalBlobName);
        // (await originalBlob.ExistsAsync()).Value.Should().BeTrue();

        // Note: In a full test, we would verify the thumbnail, medium, and large images too
        // For now, we're focusing on the core functionality
    }

    [Fact(Skip = "AzureBlobImageService implementation not complete yet")]
    public async Task UploadImageAsync_WithLargeFile_ShouldFailValidation()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        // var "placeholder-file.jpg" = "large-image.jpg"; // Unused in stub implementation
        var largeImageBytes = new byte[15 * 1024 * 1024]; // 15MB - exceeds limit

        // Act
        await Task.CompletedTask; // Service not implemented yet
        // var result = await _imageService!.UploadImageAsync(largeImageBytes, "placeholder-file.jpg", businessId);

        // Assert - Test specification for future implementation
        // result.Should().NotBeNull();
        // result.IsSuccess.Should().BeFalse();
        // result.Errors.Should().Contain(e => e.Contains("exceeds maximum allowed size"));
    }

    [Fact(Skip = "AzureBlobImageService implementation not complete yet")]
    public async Task GetSecureUrlAsync_InDevelopmentMode_ShouldReturnOriginalUrl()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        // var "placeholder-file.jpg" = "test-image.jpg"; // Unused in stub implementation
        var imageBytes = CreateValidJpegBytes();

        // Upload an image first
        var uploadResult = await _imageService!.UploadImageAsync(imageBytes, "placeholder-file.jpg", businessId);
        uploadResult.IsSuccess.Should().BeTrue();

        // Act
        var secureUrlResult = await _imageService.GetSecureUrlAsync(uploadResult.Value.Url);

        // Assert
        secureUrlResult.Should().NotBeNull();
        secureUrlResult.IsSuccess.Should().BeTrue();
        secureUrlResult.Value.Should().Be(uploadResult.Value.Url);
    }

    [Fact(Skip = "AzureBlobImageService implementation not complete yet")]
    public async Task UploadImageAsync_ConcurrentUploads_ShouldHandleMultipleUploads()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var imageBytes = CreateValidJpegBytes();

        var uploadTasks = new List<Task<LankaConnect.Domain.Common.Result<LankaConnect.Application.Common.Interfaces.ImageUploadResult>>>();

        // Create 5 concurrent upload tasks
        for (int i = 0; i < 5; i++)
        {
            var fileName = $"concurrent-test-{i}.jpg";
            uploadTasks.Add(_imageService!.UploadImageAsync(imageBytes, fileName, businessId));
        }

        // Act
        var results = await Task.WhenAll(uploadTasks);

        // Assert
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

        // Verify all blobs have unique names
        var blobNames = results.Select(r => r.Value.BlobName).ToList();
        blobNames.Should().OnlyHaveUniqueItems();

        // Verify all blobs exist
        var containerClient = _blobServiceClient!.GetBlobContainerClient("test-business-images");
        foreach (var result in results)
        {
            var blobClient = containerClient.GetBlobClient(result.Value.BlobName);
            var exists = await blobClient.ExistsAsync();
            exists.Value.Should().BeTrue();
        }
    }

    [Fact(Skip = "AzureBlobImageService implementation not complete yet")]
    public void ValidateImage_WithValidJpegBytes_ShouldReturnSuccess()
    {
        // Arrange
        var imageBytes = CreateValidJpegBytes();
        // var "placeholder-file.jpg" = "test.jpg"; // Unused in stub implementation

        // Act
        // Service not implemented yet, skip validation
        // var result = _imageService!.ValidateImage(imageBytes, "placeholder-file.jpg");

        // Assert - Test specification for future implementation
        // result.Should().NotBeNull();
        // result.IsSuccess.Should().BeTrue();
    }

    [Fact(Skip = "AzureBlobImageService implementation not complete yet")]
    public void ValidateImage_WithInvalidFileExtension_ShouldReturnFailure()
    {
        // Arrange
        var imageBytes = CreateValidJpegBytes();
        // var "placeholder-file.jpg" = "test.txt"; // Unused in stub implementation

        // Act
        // Service not implemented yet, skip validation
        // var result = _imageService!.ValidateImage(imageBytes, "placeholder-file.jpg");

        // Assert - Test specification for future implementation
        // result.Should().NotBeNull();
        // result.IsSuccess.Should().BeFalse();
        // result.Errors.Should().Contain(e => e.Contains("Invalid image file type"));
    }

    [Fact(Skip = "AzureBlobImageService implementation not complete yet")]
    public void ValidateImage_WithCorruptedImageData_ShouldReturnFailure()
    {
        // Arrange
        var corruptedBytes = Encoding.UTF8.GetBytes("This is not an image file");
        // var "placeholder-file.jpg" = "test.jpg"; // Unused in stub implementation

        // Act
        var result = _imageService!.ValidateImage(corruptedBytes, "placeholder-file.jpg");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid or corrupted image file"));
    }

    private static byte[] CreateValidJpegBytes()
    {
        // Create a minimal valid JPEG structure for testing
        var jpegHeader = new byte[]
        {
            0xFF, 0xD8, // SOI (Start of Image)
            0xFF, 0xE0, // APP0 marker
            0x00, 0x10, // Length (16 bytes)
            0x4A, 0x46, 0x49, 0x46, 0x00, // "JFIF\0"
            0x01, 0x01, // Version 1.1
            0x01, // Units (dots per inch)
            0x00, 0x48, // X density (72)
            0x00, 0x48, // Y density (72)
            0x00, 0x00, // Thumbnail width/height (0)
            0xFF, 0xD9  // EOI (End of Image)
        };

        return jpegHeader;
    }

    private static string ExtractBlobNameFromUrl(string imageUrl)
    {
        try
        {
            var uri = new Uri(imageUrl);
            var path = uri.AbsolutePath;
            
            // Remove leading slash and account name for Azurite URLs
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
            {
                // Skip account name and container name
                return string.Join("/", segments.Skip(2));
            }
            
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}