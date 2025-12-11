using FluentAssertions;
using LankaConnect.Application.Businesses.Commands.UploadBusinessImage;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Businesses.Commands;

public sealed class UploadBusinessImageCommandTests
{
    private readonly Mock<IBusinessRepository> _mockBusinessRepository;
    private readonly Mock<IImageService> _mockImageService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<UploadBusinessImageCommandHandler>> _mockLogger;
    private readonly UploadBusinessImageCommandHandler _handler;

    public UploadBusinessImageCommandTests()
    {
        _mockBusinessRepository = new Mock<IBusinessRepository>();
        _mockImageService = new Mock<IImageService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<UploadBusinessImageCommandHandler>>();

        _handler = new UploadBusinessImageCommandHandler(
            _mockBusinessRepository.Object,
            _mockImageService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccessResult()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = CreateTestBusiness(businessId);
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg");
        var command = new UploadBusinessImageCommand
        {
            BusinessId = businessId,
            Image = mockFile.Object,
            AltText = "Test alt",
            Caption = "Test caption",
            IsPrimary = true,
            DisplayOrder = 1
        };

        var resizeResult = new ImageResizeResult
        {
            OriginalUrl = "https://test.com/original.jpg",
            ThumbnailUrl = "https://test.com/thumbnail.jpg",
            MediumUrl = "https://test.com/medium.jpg",
            LargeUrl = "https://test.com/large.jpg",
            SizesBytes = new Dictionary<string, long> { { "original", 1024 } },
            ProcessedAt = DateTime.UtcNow
        };

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        _mockImageService
            .Setup(x => x.ResizeAndUploadAsync(
                It.IsAny<byte[]>(),
                "test.jpg",
                businessId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageResizeResult>.Success(resizeResult));

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OriginalUrl.Should().Be(resizeResult.OriginalUrl);
        result.Value.ThumbnailUrl.Should().Be(resizeResult.ThumbnailUrl);
        result.Value.MediumUrl.Should().Be(resizeResult.MediumUrl);
        result.Value.LargeUrl.Should().Be(resizeResult.LargeUrl);

        _mockBusinessRepository.Verify(x => x.Update(business), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentBusiness_ShouldReturnFailure()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg");
        var command = new UploadBusinessImageCommand
        {
            BusinessId = businessId,
            Image = mockFile.Object,
            AltText = "Test alt",
            Caption = "Test caption",
            IsPrimary = false,
            DisplayOrder = 1
        };

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LankaConnect.Domain.Business.Business?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Business not found");

        _mockImageService.Verify(x => x.ResizeAndUploadAsync(
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenImageServiceFails_ShouldReturnFailure()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = CreateTestBusiness(businessId);
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg");
        var command = new UploadBusinessImageCommand
        {
            BusinessId = businessId,
            Image = mockFile.Object,
            AltText = "Test alt",
            Caption = "Test caption",
            IsPrimary = false,
            DisplayOrder = 1
        };

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        _mockImageService
            .Setup(x => x.ResizeAndUploadAsync(
                It.IsAny<byte[]>(),
                "test.jpg",
                businessId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageResizeResult>.Failure("Image upload failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Image upload failed");

        _mockBusinessRepository.Verify(x => x.Update(It.IsAny<LankaConnect.Domain.Business.Business>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBusinessImageCreationFails_ShouldCleanupUploadedImages()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = CreateTestBusiness(businessId);
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg");
        var command = new UploadBusinessImageCommand
        {
            BusinessId = businessId,
            Image = mockFile.Object,
            AltText = "Test alt",
            Caption = "Test caption",
            IsPrimary = false,
            DisplayOrder = -1 // Invalid display order to force BusinessImage creation failure
        };

        var resizeResult = new ImageResizeResult
        {
            OriginalUrl = "https://test.com/original.jpg",
            ThumbnailUrl = "https://test.com/thumbnail.jpg",
            MediumUrl = "https://test.com/medium.jpg",
            LargeUrl = "https://test.com/large.jpg",
            SizesBytes = new Dictionary<string, long> { { "original", 1024 } },
            ProcessedAt = DateTime.UtcNow
        };

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        _mockImageService
            .Setup(x => x.ResizeAndUploadAsync(
                It.IsAny<byte[]>(),
                "test.jpg",
                businessId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageResizeResult>.Success(resizeResult));

        _mockImageService
            .Setup(x => x.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();

        // Verify cleanup was attempted for all uploaded images
        _mockImageService.Verify(x => x.DeleteImageAsync(resizeResult.OriginalUrl, It.IsAny<CancellationToken>()), Times.Once);
        _mockImageService.Verify(x => x.DeleteImageAsync(resizeResult.ThumbnailUrl, It.IsAny<CancellationToken>()), Times.Once);
        _mockImageService.Verify(x => x.DeleteImageAsync(resizeResult.MediumUrl, It.IsAny<CancellationToken>()), Times.Once);
        _mockImageService.Verify(x => x.DeleteImageAsync(resizeResult.LargeUrl, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAddingImageToBusinessFails_ShouldCleanupUploadedImages()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = CreateTestBusiness(businessId);

        // Add an image with the same URL to force duplicate failure
        var existingImage = BusinessImage.Create(
            "https://test.com/original.jpg",
            "https://test.com/thumb.jpg",
            "https://test.com/med.jpg",
            "https://test.com/large.jpg",
            "existing",
            "existing",
            0,
            false,
            1024L,
            "image/jpeg").Value;
        
        business.AddImage(existingImage);

        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg");
        var command = new UploadBusinessImageCommand
        {
            BusinessId = businessId,
            Image = mockFile.Object,
            AltText = "Test alt",
            Caption = "Test caption",
            IsPrimary = false,
            DisplayOrder = 1
        };

        var resizeResult = new ImageResizeResult
        {
            OriginalUrl = "https://test.com/original.jpg", // Same URL as existing image
            ThumbnailUrl = "https://test.com/thumbnail.jpg",
            MediumUrl = "https://test.com/medium.jpg",
            LargeUrl = "https://test.com/large.jpg",
            SizesBytes = new Dictionary<string, long> { { "original", 1024 } },
            ProcessedAt = DateTime.UtcNow
        };

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        _mockImageService
            .Setup(x => x.ResizeAndUploadAsync(
                It.IsAny<byte[]>(),
                "test.jpg",
                businessId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageResizeResult>.Success(resizeResult));

        _mockImageService
            .Setup(x => x.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("An image with this URL already exists");

        // Verify cleanup was attempted
        _mockImageService.Verify(x => x.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeast(1));
    }

    private static LankaConnect.Domain.Business.Business CreateTestBusiness(Guid businessId)
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

        return LankaConnect.Domain.Business.Business.Create(
            businessProfileResult.Value,
            businessLocationResult.Value,
            contactInfoResult.Value,
            businessHoursResult.Value,
            BusinessCategory.Restaurant,
            businessId).Value;
    }

    private static Mock<IFormFile> CreateMockFormFile(string fileName, string contentType)
    {
        var fileMock = new Mock<IFormFile>();
        var content = "fake image content";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.ContentType).Returns(contentType);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream stream, CancellationToken token) => ms.CopyToAsync(stream, token));

        return fileMock;
    }
}