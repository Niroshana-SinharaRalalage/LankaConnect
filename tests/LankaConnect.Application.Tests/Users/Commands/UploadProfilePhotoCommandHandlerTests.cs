using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Application.Users.Commands.UploadProfilePhoto;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Commands;

/// <summary>
/// TDD RED Phase - Tests for UploadProfilePhotoCommandHandler
/// These tests will FAIL until we implement the command and handler
/// </summary>
public class UploadProfilePhotoCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IImageService> _imageService;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly UploadProfilePhotoCommandHandler _handler;

    public UploadProfilePhotoCommandHandlerTests()
    {
        _userRepository = TestHelpers.MockRepository.CreateUserRepository();
        _imageService = new Mock<IImageService>();
        _unitOfWork = TestHelpers.MockRepository.CreateUnitOfWork();
        _handler = new UploadProfilePhotoCommandHandler(
            _userRepository.Object,
            _imageService.Object,
            _unitOfWork.Object,
            NullLogger<UploadProfilePhotoCommandHandler>.Instance);
    }

    private IFormFile CreateMockImageFile(string fileName = "profile.jpg", long size = 1024000)
    {
        var fileMock = new Mock<IFormFile>();
        var content = new byte[size];
        var ms = new MemoryStream(content);

        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(size);
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        return fileMock.Object;
    }

    [Fact]
    public async Task Handle_WithValidImageAndExistingUser_ShouldUploadAndUpdateUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var imageFile = CreateMockImageFile();
        var command = new UploadProfilePhotoCommand
        {
            UserId = userId,
            ImageFile = imageFile
        };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageService.Setup(x => x.UploadImageAsync(
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            userId,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageUploadResult>.Success(new ImageUploadResult
            {
                Url = "https://storage.blob.core.windows.net/users/photo.jpg",
                BlobName = "users/photo.jpg",
                SizeBytes = 1024000,
                ContentType = "image/jpeg",
                UploadedAt = DateTime.UtcNow
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.PhotoUrl.Should().Contain("users/photo.jpg");

        _userRepository.Verify(x => x.Update(user), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UploadProfilePhotoCommand
        {
            UserId = userId,
            ImageFile = CreateMockImageFile()
        };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User not found");

        _imageService.Verify(x => x.UploadImageAsync(
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenImageUploadFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var command = new UploadProfilePhotoCommand
        {
            UserId = userId,
            ImageFile = CreateMockImageFile()
        };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageService.Setup(x => x.UploadImageAsync(
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageUploadResult>.Failure("Image upload failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Image upload failed");

        _userRepository.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserHasExistingPhoto_ShouldDeleteOldPhotoFirst()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        // Set existing photo
        user.UpdateProfilePhoto(
            "https://storage.blob.core.windows.net/users/old-photo.jpg",
            "users/old-photo.jpg");

        var command = new UploadProfilePhotoCommand
        {
            UserId = userId,
            ImageFile = CreateMockImageFile()
        };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageService.Setup(x => x.DeleteImageAsync(
            "https://storage.blob.core.windows.net/users/old-photo.jpg",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _imageService.Setup(x => x.UploadImageAsync(
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            userId,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageUploadResult>.Success(new ImageUploadResult
            {
                Url = "https://storage.blob.core.windows.net/users/new-photo.jpg",
                BlobName = "users/new-photo.jpg",
                SizeBytes = 1024000,
                ContentType = "image/jpeg",
                UploadedAt = DateTime.UtcNow
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _imageService.Verify(x => x.DeleteImageAsync(
            "https://storage.blob.core.windows.net/users/old-photo.jpg",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyFile_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyFile = CreateMockImageFile(size: 0);
        var command = new UploadProfilePhotoCommand
        {
            UserId = userId,
            ImageFile = emptyFile
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Image file is required");
    }

    [Fact]
    public async Task Handle_WithNullFile_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UploadProfilePhotoCommand
        {
            UserId = userId,
            ImageFile = null!
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Image file is required");
    }
}
