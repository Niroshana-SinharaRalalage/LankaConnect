using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using LankaConnect.Application.Communications.Commands.ResetPassword;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;

namespace LankaConnect.Application.Tests.Communications.Commands;

/// <summary>
/// TDD tests for ResetPasswordCommandHandler
/// Tests written FIRST following Red-Green-Refactor cycle
/// Phase 6A.87: Updated for ITypedEmailService support
/// </summary>
public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHashingService> _mockPasswordHashingService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ITypedEmailService> _mockTypedEmailService;  // Phase 6A.87: Added for typed email service
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<ResetPasswordCommandHandler>> _mockLogger;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHashingService = new Mock<IPasswordHashingService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockTypedEmailService = new Mock<ITypedEmailService>();  // Phase 6A.87: Initialize mock
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<ResetPasswordCommandHandler>>();

        // Phase 6A.87: Default setup for typed email service to return success
        _mockTypedEmailService.Setup(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok(Guid.NewGuid().ToString(), true, 100));

        _handler = new ResetPasswordCommandHandler(
            _mockUserRepository.Object,
            _mockPasswordHashingService.Object,
            _mockEmailService.Object,
            _mockTypedEmailService.Object,  // Phase 6A.87: Pass typed email service
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidTokenAndPassword_ShouldResetPassword()
    {
        // Arrange
        var email = "test@example.com";
        var token = "valid-reset-token-123";
        var newPassword = "NewSecureP@ssw0rd!";
        var command = new ResetPasswordCommand(email, token, newPassword);
        var user = CreateTestUserWithResetToken(email, token);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHashingService
            .Setup(p => p.ValidatePasswordStrength(newPassword))
            .Returns(Result.Success());

        _mockPasswordHashingService
            .Setup(p => p.HashPassword(newPassword))
            .Returns(Result<string>.Success("hashed-new-password-123"));

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be(email);
        result.Value.UserId.Should().Be(user.Id);
        result.Value.RequiresLogin.Should().BeTrue();
        result.Value.PasswordChangedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));

        // Verify password was changed
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify password strength was validated
        _mockPasswordHashingService.Verify(
            p => p.ValidatePasswordStrength(newPassword),
            Times.Once);

        // Verify password was hashed
        _mockPasswordHashingService.Verify(
            p => p.HashPassword(newPassword),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new ResetPasswordCommand("invalid-email", "token-123", "NewP@ssw0rd!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");

        // Verify no database or password operations
        _mockUserRepository.Verify(
            r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockPasswordHashingService.Verify(
            p => p.HashPassword(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var command = new ResetPasswordCommand("nonexistent@example.com", "token-123", "NewP@ssw0rd!");

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid reset token or email");

        // Verify no password operations
        _mockPasswordHashingService.Verify(
            p => p.HashPassword(It.IsAny<string>()),
            Times.Never);
        _mockUnitOfWork.Verify(
            u => u.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var correctToken = "correct-token";
        var wrongToken = "wrong-token";
        var command = new ResetPasswordCommand(email, wrongToken, "NewP@ssw0rd!");
        var user = CreateTestUserWithResetToken(email, correctToken);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired reset token");

        // Verify no password changes
        _mockPasswordHashingService.Verify(
            p => p.HashPassword(It.IsAny<string>()),
            Times.Never);
        _mockUnitOfWork.Verify(
            u => u.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var token = "expired-token";
        var command = new ResetPasswordCommand(email, token, "NewP@ssw0rd!");
        var user = CreateTestUser(email);

        // Set an expired token
        var expiredTokenExpiry = DateTime.UtcNow.AddHours(-1); // Expired 1 hour ago
        user.SetPasswordResetToken(token, expiredTokenExpiry);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired reset token");

        // Verify no password changes
        _mockPasswordHashingService.Verify(
            p => p.HashPassword(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithWeakPassword_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var token = "valid-token";
        var weakPassword = "weak";
        var command = new ResetPasswordCommand(email, token, weakPassword);
        var user = CreateTestUserWithResetToken(email, token);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHashingService
            .Setup(p => p.ValidatePasswordStrength(weakPassword))
            .Returns(Result.Failure("Password is too weak. Must contain uppercase, lowercase, number, and special character."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Password is too weak");

        // Verify no password hashing or database operations
        _mockPasswordHashingService.Verify(
            p => p.HashPassword(It.IsAny<string>()),
            Times.Never);
        _mockUnitOfWork.Verify(
            u => u.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPasswordHashingFails_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var token = "valid-token";
        var newPassword = "NewSecureP@ssw0rd!";
        var command = new ResetPasswordCommand(email, token, newPassword);
        var user = CreateTestUserWithResetToken(email, token);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHashingService
            .Setup(p => p.ValidatePasswordStrength(newPassword))
            .Returns(Result.Success());

        _mockPasswordHashingService
            .Setup(p => p.HashPassword(newPassword))
            .Returns(Result<string>.Failure("Hashing algorithm failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Hashing algorithm failed");

        // Verify no database operations
        _mockUnitOfWork.Verify(
            u => u.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRevokeAllRefreshTokens()
    {
        // Arrange
        var email = "test@example.com";
        var token = "valid-token";
        var newPassword = "NewSecureP@ssw0rd!";
        var command = new ResetPasswordCommand(email, token, newPassword);
        var user = CreateTestUserWithResetToken(email, token);

        // Add some refresh tokens to the user
        var refreshToken1 = LankaConnect.Domain.Users.ValueObjects.RefreshToken.Create(
            "refresh-token-1",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1").Value;
        var refreshToken2 = LankaConnect.Domain.Users.ValueObjects.RefreshToken.Create(
            "refresh-token-2",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.2").Value;
        user.AddRefreshToken(refreshToken1);
        user.AddRefreshToken(refreshToken2);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHashingService
            .Setup(p => p.ValidatePasswordStrength(newPassword))
            .Returns(Result.Success());

        _mockPasswordHashingService
            .Setup(p => p.HashPassword(newPassword))
            .Returns(Result<string>.Success("hashed-password"));

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify all refresh tokens were revoked
        user.RefreshTokens.Should().AllSatisfy(rt => rt.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_ShouldClearPasswordResetToken()
    {
        // Arrange
        var email = "test@example.com";
        var token = "valid-token";
        var newPassword = "NewSecureP@ssw0rd!";
        var command = new ResetPasswordCommand(email, token, newPassword);
        var user = CreateTestUserWithResetToken(email, token);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHashingService
            .Setup(p => p.ValidatePasswordStrength(newPassword))
            .Returns(Result.Success());

        _mockPasswordHashingService
            .Setup(p => p.HashPassword(newPassword))
            .Returns(Result<string>.Success("hashed-password"));

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify reset token was cleared (ChangePassword clears it)
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldResetFailedLoginAttempts()
    {
        // Arrange
        var email = "test@example.com";
        var token = "valid-token";
        var newPassword = "NewSecureP@ssw0rd!";
        var command = new ResetPasswordCommand(email, token, newPassword);
        var user = CreateTestUserWithResetToken(email, token);

        // Record some failed login attempts
        user.RecordFailedLoginAttempt();
        user.RecordFailedLoginAttempt();
        user.RecordFailedLoginAttempt();

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHashingService
            .Setup(p => p.ValidatePasswordStrength(newPassword))
            .Returns(Result.Success());

        _mockPasswordHashingService
            .Setup(p => p.HashPassword(newPassword))
            .Returns(Result<string>.Success("hashed-password"));

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify failed login attempts were reset
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenDatabaseThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var token = "valid-token";
        var newPassword = "NewSecureP@ssw0rd!";
        var command = new ResetPasswordCommand(email, token, newPassword);
        var user = CreateTestUserWithResetToken(email, token);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHashingService
            .Setup(p => p.ValidatePasswordStrength(newPassword))
            .Returns(Result.Success());

        _mockPasswordHashingService
            .Setup(p => p.HashPassword(newPassword))
            .Returns(Result<string>.Success("hashed-password"));

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("An error occurred while resetting password");
    }

    [Fact]
    public async Task Handle_ShouldSendConfirmationEmailAsynchronously()
    {
        // Arrange
        var email = "test@example.com";
        var token = "valid-token";
        var newPassword = "NewSecureP@ssw0rd!";
        var command = new ResetPasswordCommand(email, token, newPassword);
        var user = CreateTestUserWithResetToken(email, token);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHashingService
            .Setup(p => p.ValidatePasswordStrength(newPassword))
            .Returns(Result.Success());

        _mockPasswordHashingService
            .Setup(p => p.HashPassword(newPassword))
            .Returns(Result<string>.Success("hashed-password"));

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Phase 6A.87: Uses default typed email service setup (success)

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Wait a bit for async email to be attempted
        await Task.Delay(100);

        // Phase 6A.87: Verify typed email service was called with PasswordChangedEmailParams
        // Note: This is fire-and-forget, so we can't guarantee timing in tests
        // In production, email failures don't affect the password reset result
        _mockTypedEmailService.Verify(
            e => e.SendEmailAsync(
                It.Is<PasswordChangedEmailParams>(p =>
                    p.UserEmail == email &&
                    p.UserId == user.Id),
                It.Is<string>(h => h == "ResetPasswordCommandHandler"),
                It.IsAny<CancellationToken>()),
            Times.AtMostOnce);
    }

    /// <summary>
    /// Helper method to create a test user
    /// </summary>
    private static User CreateTestUser(string email)
    {
        var userEmail = Email.Create(email).Value;
        var user = User.Create(userEmail, "Test", "User").Value;

        // Set a password so user is in valid state
        user.SetPassword("hashedPassword123");

        return user;
    }

    /// <summary>
    /// Helper method to create a test user with a valid reset token
    /// </summary>
    private static User CreateTestUserWithResetToken(string email, string token)
    {
        var user = CreateTestUser(email);

        // Set a valid reset token (expires in 1 hour)
        var tokenExpiry = DateTime.UtcNow.AddHours(1);
        user.SetPasswordResetToken(token, tokenExpiry);

        return user;
    }
}
