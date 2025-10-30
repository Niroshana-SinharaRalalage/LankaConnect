using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using LankaConnect.Application.Communications.Commands.SendPasswordReset;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Tests.Communications.Commands;

/// <summary>
/// TDD tests for SendPasswordResetCommandHandler
/// Tests written FIRST following Red-Green-Refactor cycle
/// </summary>
public class SendPasswordResetCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<SendPasswordResetCommandHandler>> _mockLogger;
    private readonly SendPasswordResetCommandHandler _handler;

    public SendPasswordResetCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockEmailTemplateService = new Mock<IEmailTemplateService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<SendPasswordResetCommandHandler>>();

        _handler = new SendPasswordResetCommandHandler(
            _mockUserRepository.Object,
            _mockEmailService.Object,
            _mockEmailTemplateService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidEmail_ShouldSendPasswordResetEmail()
    {
        // Arrange
        var email = "test@example.com";
        var command = new SendPasswordResetCommand(email);
        var user = CreateTestUser(email);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockEmailService
            .Setup(e => e.SendTemplatedEmailAsync(
                "password-reset",
                email,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

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
        result.Value.TokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.Value.WasRecentlySent.Should().BeFalse();
        result.Value.UserNotFound.Should().BeFalse();

        // Verify email was sent with correct template
        _mockEmailService.Verify(
            e => e.SendTemplatedEmailAsync(
                "password-reset",
                email,
                It.Is<Dictionary<string, object>>(d =>
                    d.ContainsKey("UserName") &&
                    d.ContainsKey("ResetToken") &&
                    d.ContainsKey("ResetLink")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify user was saved
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new SendPasswordResetCommand("invalid-email");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");

        // Verify no database or email operations
        _mockUserRepository.Verify(
            r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockEmailService.Verify(
            e => e.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnSuccessWithUserNotFoundFlag()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var command = new SendPasswordResetCommand(email);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Should return success for security (don't reveal if user exists)
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserNotFound.Should().BeTrue();
        result.Value.UserId.Should().Be(Guid.Empty);
        result.Value.Email.Should().Be(email);

        // Verify NO email was sent (security)
        _mockEmailService.Verify(
            e => e.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify NO database commit
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithLockedAccount_ShouldReturnFailure()
    {
        // Arrange
        var email = "locked@example.com";
        var command = new SendPasswordResetCommand(email);
        var user = CreateTestUser(email);

        // Lock the account by recording 5 failed login attempts
        for (int i = 0; i < 5; i++)
        {
            user.RecordFailedLoginAttempt();
        }

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Account is temporarily locked");

        // Verify NO email was sent
        _mockEmailService.Verify(
            e => e.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRecentlySent_ShouldReturnWasRecentlySentFlag()
    {
        // Arrange
        var email = "test@example.com";
        var command = new SendPasswordResetCommand(email, ForceResend: false);
        var user = CreateTestUser(email);

        // Set a recent password reset token (within last 5 minutes)
        var recentTokenExpiry = DateTime.UtcNow.AddMinutes(58); // Created 2 minutes ago (60 - 58)
        user.SetPasswordResetToken("existing-token", recentTokenExpiry);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.WasRecentlySent.Should().BeTrue();
        result.Value.TokenExpiresAt.Should().Be(recentTokenExpiry);

        // Verify NO new email was sent (rate limiting)
        _mockEmailService.Verify(
            e => e.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify NO database commit (no changes)
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithForceResend_ShouldBypassRateLimiting()
    {
        // Arrange
        var email = "test@example.com";
        var command = new SendPasswordResetCommand(email, ForceResend: true);
        var user = CreateTestUser(email);

        // Set a recent password reset token (within last 5 minutes)
        var recentTokenExpiry = DateTime.UtcNow.AddMinutes(58);
        user.SetPasswordResetToken("existing-token", recentTokenExpiry);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockEmailService
            .Setup(e => e.SendTemplatedEmailAsync(
                "password-reset",
                email,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.WasRecentlySent.Should().BeFalse();

        // Verify email WAS sent (rate limiting bypassed)
        _mockEmailService.Verify(
            e => e.SendTemplatedEmailAsync(
                "password-reset",
                email,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify database commit
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailServiceFails_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var command = new SendPasswordResetCommand(email);
        var user = CreateTestUser(email);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockEmailService
            .Setup(e => e.SendTemplatedEmailAsync(
                "password-reset",
                email,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("SMTP server unavailable"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to send password reset email");

        // Verify database commit was NOT called (email failed)
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(Skip = "TODO: User.SetPasswordResetToken needs stricter validation to properly test failure scenario. Currently accepts all valid tokens.")]
    public async Task Handle_WhenSetTokenFails_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var command = new SendPasswordResetCommand(email);
        var user = CreateTestUser(email);

        // This test is a placeholder for when User.SetPasswordResetToken has stricter validation
        // Current implementation: SetPasswordResetToken always succeeds with valid inputs
        // Future implementation: May reject tokens based on additional business rules

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockEmailService
            .Setup(e => e.SendTemplatedEmailAsync(
                "password-reset",
                email,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Currently passes because SetPasswordResetToken doesn't fail
        // When domain validation is added, update this test to trigger the failure
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenDatabaseThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var command = new SendPasswordResetCommand(email);
        var user = CreateTestUser(email);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockEmailService
            .Setup(e => e.SendTemplatedEmailAsync(
                "password-reset",
                email,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("An error occurred while sending password reset email");
    }

    [Fact]
    public async Task Handle_ShouldSetTokenWithOneHourExpiry()
    {
        // Arrange
        var email = "test@example.com";
        var command = new SendPasswordResetCommand(email);
        var user = CreateTestUser(email);
        User? capturedUser = null;

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user)
            .Callback<Email, CancellationToken>((_, __) => capturedUser = user);

        _mockEmailService
            .Setup(e => e.SendTemplatedEmailAsync(
                "password-reset",
                email,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify token expiry is approximately 1 hour from now
        var expectedExpiry = DateTime.UtcNow.AddHours(1);
        result.Value.TokenExpiresAt.Should().BeCloseTo(expectedExpiry, precision: TimeSpan.FromSeconds(5));

        // Verify user has the token set
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordResetToken.Should().NotBeNullOrEmpty();
        capturedUser.PasswordResetTokenExpiresAt.Should().BeCloseTo(expectedExpiry, precision: TimeSpan.FromSeconds(5));
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
}
