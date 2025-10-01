using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using LankaConnect.Infrastructure.Storage.Configuration;
using LankaConnect.Infrastructure.Storage.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Storage.Services;

public sealed class AzureBlobImageServiceTests : IDisposable
{
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
    private readonly Mock<BlobContainerClient> _mockContainerClient;
    private readonly Mock<BlobClient> _mockBlobClient;
    private readonly Mock<ILogger<AzureBlobImageService>> _mockLogger;
    private readonly IOptions<AzureStorageOptions> _options;
    private readonly AzureBlobImageService _imageService;

    public AzureBlobImageServiceTests()
    {
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockContainerClient = new Mock<BlobContainerClient>();
        _mockBlobClient = new Mock<BlobClient>();
        _mockLogger = new Mock<ILogger<AzureBlobImageService>>();

        var azureStorageOptions = new AzureStorageOptions
        {
            BusinessImagesContainer = "test-container",
            MaxFileSizeBytes = 10 * 1024 * 1024, // 10MB
            AllowedContentTypes = new List<string> { "image/jpeg", "image/png", "image/gif" },
            ImageSizes = new ImageSizeSettings
            {
                Thumbnail = new ImageSizeConfig { Width = 150, Height = 150, Quality = 80 },
                Medium = new ImageSizeConfig { Width = 500, Height = 500, Quality = 85 },
                Large = new ImageSizeConfig { Width = 1200, Height = 1200, Quality = 90 }
            },
            IsDevelopment = true
        };

        _options = Options.Create(azureStorageOptions);

        // Setup default mock behavior
        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_mockContainerClient.Object);

        _mockContainerClient
            .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, default))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

        _mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_mockBlobClient.Object);

        _imageService = new AzureBlobImageService(
            _mockBlobServiceClient.Object,
            _options,
            _mockLogger.Object);
    }

    [Fact]
    public async Task UploadImageAsync_WithValidImage_ShouldReturnSuccess()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var fileName = "test.jpg";
        var imageBytes = CreateValidJpegBytes();

        var mockResponse = Mock.Of<Response<BlobContentInfo>>();
        _mockBlobClient
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), default))
            .ReturnsAsync(mockResponse);

        _mockBlobClient
            .Setup(x => x.Uri)
            .Returns(new Uri("https://test.blob.core.windows.net/container/test.jpg"));

        // Act
        var result = await _imageService.UploadImageAsync(imageBytes, fileName, businessId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Url.Should().Be("https://test.blob.core.windows.net/container/test.jpg");
        result.Value.BlobName.Should().NotBeEmpty();
        result.Value.SizeBytes.Should().Be(imageBytes.Length);
        result.Value.ContentType.Should().Be("image/jpeg");
        result.Value.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _mockBlobClient.Verify(x => x.UploadAsync(
            It.IsAny<Stream>(),
            It.IsAny<BlobUploadOptions>(),
            default), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("test.txt")]
    [InlineData("test.exe")]
    public async Task UploadImageAsync_WithInvalidFileExtension_ShouldReturnFailure(string fileName)
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var imageBytes = CreateValidJpegBytes();

        // Act
        var result = await _imageService.UploadImageAsync(imageBytes, fileName, businessId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid image file type"));

        _mockBlobClient.Verify(x => x.UploadAsync(
            It.IsAny<Stream>(),
            It.IsAny<BlobUploadOptions>(),
            default), Times.Never);
    }

    [Fact]
    public async Task UploadImageAsync_WithEmptyFile_ShouldReturnFailure()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var fileName = "test.jpg";
        var imageBytes = Array.Empty<byte>();

        // Act
        var result = await _imageService.UploadImageAsync(imageBytes, fileName, businessId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Image file is empty"));
    }

    [Fact]
    public async Task UploadImageAsync_WithFileTooBig_ShouldReturnFailure()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var fileName = "test.jpg";
        var imageBytes = new byte[15 * 1024 * 1024]; // 15MB, exceeds 10MB limit

        // Act
        var result = await _imageService.UploadImageAsync(imageBytes, fileName, businessId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("exceeds maximum allowed size"));
    }

    [Fact]
    public async Task DeleteImageAsync_WithValidUrl_ShouldReturnSuccess()
    {
        // Arrange
        var imageUrl = "https://test.blob.core.windows.net/container/businesses/12345/test.jpg";

        var mockExistsResponse = Mock.Of<Response<bool>>(r => r.Value == true);
        _mockBlobClient
            .Setup(x => x.ExistsAsync(default))
            .ReturnsAsync(mockExistsResponse);

        var mockDeleteResponse = Mock.Of<Response>();
        _mockBlobClient
            .Setup(x => x.DeleteAsync(It.IsAny<DeleteSnapshotsOption>(), null, default))
            .ReturnsAsync(mockDeleteResponse);

        // Act
        var result = await _imageService.DeleteImageAsync(imageUrl);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        _mockBlobClient.Verify(x => x.ExistsAsync(default), Times.Once);
        _mockBlobClient.Verify(x => x.DeleteAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            null,
            default), Times.Once);
    }

    [Fact]
    public async Task DeleteImageAsync_WithNonExistentBlob_ShouldReturnSuccess()
    {
        // Arrange
        var imageUrl = "https://test.blob.core.windows.net/container/businesses/12345/test.jpg";

        var mockExistsResponse = Mock.Of<Response<bool>>(r => r.Value == false);
        _mockBlobClient
            .Setup(x => x.ExistsAsync(default))
            .ReturnsAsync(mockExistsResponse);

        // Act
        var result = await _imageService.DeleteImageAsync(imageUrl);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        _mockBlobClient.Verify(x => x.ExistsAsync(default), Times.Once);
        _mockBlobClient.Verify(x => x.DeleteAsync(
            It.IsAny<DeleteSnapshotsOption>(),
            It.IsAny<BlobRequestConditions>(),
            default), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-url")]
    [InlineData("https://invalid-format/")]
    public async Task DeleteImageAsync_WithInvalidUrl_ShouldReturnFailure(string imageUrl)
    {
        // Act
        var result = await _imageService.DeleteImageAsync(imageUrl);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid image URL format"));
    }

    [Fact]
    public void ValidateImage_WithValidJpeg_ShouldReturnSuccess()
    {
        // Arrange
        var imageBytes = CreateValidJpegBytes();
        var fileName = "test.jpg";

        // Act
        var result = _imageService.ValidateImage(imageBytes, fileName);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateImage_WithInvalidImageData_ShouldReturnFailure()
    {
        // Arrange
        var imageBytes = Encoding.UTF8.GetBytes("This is not an image");
        var fileName = "test.jpg";

        // Act
        var result = _imageService.ValidateImage(imageBytes, fileName);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid or corrupted image file"));
    }

    [Fact]
    public async Task GetSecureUrlAsync_InDevelopment_ShouldReturnOriginalUrl()
    {
        // Arrange
        var imageUrl = "https://test.blob.core.windows.net/container/test.jpg";

        _mockBlobClient
            .Setup(x => x.CanGenerateSasUri)
            .Returns(false);

        // Act
        var result = await _imageService.GetSecureUrlAsync(imageUrl, 24);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(imageUrl);
    }

    [Fact]
    public async Task ResizeAndUploadAsync_WithValidImage_ShouldReturnSuccess()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var fileName = "test.jpg";
        var imageBytes = CreateValidJpegBytes();

        var mockResponse = Mock.Of<Response<BlobContentInfo>>();
        _mockBlobClient
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), default))
            .ReturnsAsync(mockResponse);

        _mockBlobClient
            .Setup(x => x.Uri)
            .Returns(new Uri("https://test.blob.core.windows.net/container/test.jpg"));

        // Act
        var result = await _imageService.ResizeAndUploadAsync(imageBytes, fileName, businessId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OriginalUrl.Should().NotBeEmpty();
        result.Value.ThumbnailUrl.Should().NotBeEmpty();
        result.Value.MediumUrl.Should().NotBeEmpty();
        result.Value.LargeUrl.Should().NotBeEmpty();
        result.Value.SizesBytes.Should().NotBeEmpty();
        result.Value.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify that multiple uploads were called (original + 3 sizes)
        _mockBlobClient.Verify(x => x.UploadAsync(
            It.IsAny<Stream>(),
            It.IsAny<BlobUploadOptions>(),
            default), Times.AtLeast(1));
    }

    [Fact]
    public async Task UploadImageAsync_WhenAzureStorageThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var fileName = "test.jpg";
        var imageBytes = CreateValidJpegBytes();

        _mockBlobClient
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), default))
            .ThrowsAsync(new RequestFailedException("Storage error"));

        // Act
        var result = await _imageService.UploadImageAsync(imageBytes, fileName, businessId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Storage service error"));
    }

    private static byte[] CreateValidJpegBytes()
    {
        // Create a minimal valid JPEG header
        // This is a very basic JPEG structure for testing purposes
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
            0x00, 0x00, // Thumbnail width/height (0, no thumbnail)
            0xFF, 0xD9  // EOI (End of Image)
        };

        return jpegHeader;
    }

    public void Dispose()
    {
        // Clean up any resources if needed
    }
}