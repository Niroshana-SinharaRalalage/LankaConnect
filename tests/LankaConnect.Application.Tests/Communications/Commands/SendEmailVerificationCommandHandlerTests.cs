using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Commands.SendEmailVerification;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using UserEmail = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Application.Tests.Communications.Commands;

/// <summary>
/// Phase 6A.100: Tests for SendEmailVerificationCommandHandler
/// Email is sent via MemberVerificationRequestedEvent domain event handler
/// </summary>
public class SendEmailVerificationCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<SendEmailVerificationCommandHandler>> _logger;
    private readonly SendEmailVerificationCommandHandler _handler;

    public SendEmailVerificationCommandHandlerTests()
    {
        _userRepository = new Mock<IUserRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger<SendEmailVerificationCommandHandler>>();

        _handler = new SendEmailVerificationCommandHandler(
            _userRepository.Object,
            _unitOfWork.Object,
            _logger.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidUserId_ShouldGenerateTokenAndCommit()
    {
        // Arrange
        var user = CreateTestUserWithExpiredToken("test@example.com");
        var command = new SendEmailVerificationCommand(user.Id);

        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email.Value);

        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new SendEmailVerificationCommand(userId);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User not found");

        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyVerifiedUser_ShouldReturnSuccessWithoutResending()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        // Verify the email first
        var token = user.EmailVerificationToken!;
        user.VerifyEmail(token);

        var command = new SendEmailVerificationCommand(user.Id);

        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.WasRecentlySent.Should().BeFalse();

        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithForceResend_ShouldSendEvenIfRecentlySent()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        // Token was generated recently (within 5 minutes)
        // Note: User.Create() generates a token automatically with 24-hour expiry

        var command = new SendEmailVerificationCommand(user.Id, ForceResend: true);

        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithRecentlyGeneratedToken_ShouldReturnSuccessWithRecentlySentTrue()
    {
        // Arrange
        // User.Create() generates a token that's considered "recent" (within 5 minutes)
        var user = CreateTestUser("test@example.com");
        var command = new SendEmailVerificationCommand(user.Id);

        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Since token was just generated, it's considered "recently sent"
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.WasRecentlySent.Should().BeTrue();

        // Should NOT commit because token was recently generated
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static User CreateTestUser(string email)
    {
        var userEmail = UserEmail.Create(email).Value;
        return User.Create(userEmail, "Test", "User").Value;
    }

    private static User CreateTestUserWithExpiredToken(string email)
    {
        var userEmail = UserEmail.Create(email).Value;
        var user = User.Create(userEmail, "Test", "User").Value;

        // Clear the token expiration to simulate an expired/old token
        // Use reflection to set EmailVerificationTokenExpiresAt to a past date
        var expiresAtField = typeof(User).GetProperty("EmailVerificationTokenExpiresAt",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Set to a time more than 5 minutes ago (token was created 6 minutes ago)
        var oldExpiry = DateTime.UtcNow.AddHours(24).AddMinutes(-6);
        expiresAtField?.SetValue(user, oldExpiry);

        return user;
    }
}
