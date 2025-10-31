using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Application.Users.Commands.DeleteProfilePhoto;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Commands;

/// <summary>
/// TDD RED Phase - Tests for DeleteProfilePhotoCommandHandler
/// </summary>
public class DeleteProfilePhotoCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IImageService> _imageService;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly DeleteProfilePhotoCommandHandler _handler;

    public DeleteProfilePhotoCommandHandlerTests()
    {
        _userRepository = TestHelpers.MockRepository.CreateUserRepository();
        _imageService = new Mock<IImageService>();
        _unitOfWork = TestHelpers.MockRepository.CreateUnitOfWork();
        _handler = new DeleteProfilePhotoCommandHandler(
            _userRepository.Object,
            _imageService.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithExistingPhoto_ShouldDeletePhotoAndUpdateUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var photoUrl = "https://storage.blob.core.windows.net/users/photo.jpg";
        var blobName = "users/photo.jpg";
        user.UpdateProfilePhoto(photoUrl, blobName);

        var command = new DeleteProfilePhotoCommand { UserId = userId };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageService.Setup(x => x.DeleteImageAsync(photoUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ProfilePhotoUrl.Should().BeNull();
        user.ProfilePhotoBlobName.Should().BeNull();

        _imageService.Verify(x => x.DeleteImageAsync(photoUrl, It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(x => x.Update(user), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteProfilePhotoCommand { UserId = userId };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User not found");

        _imageService.Verify(x => x.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoPhoto_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var command = new DeleteProfilePhotoCommand { UserId = userId };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("No profile photo to remove");

        _imageService.Verify(x => x.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenImageDeletionFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var photoUrl = "https://storage.blob.core.windows.net/users/photo.jpg";
        user.UpdateProfilePhoto(photoUrl, "users/photo.jpg");

        var command = new DeleteProfilePhotoCommand { UserId = userId };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageService.Setup(x => x.DeleteImageAsync(photoUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Failed to delete image from storage"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to delete image from storage");

        _userRepository.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
