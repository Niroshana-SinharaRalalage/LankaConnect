using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using LankaConnect.Application.Communications.Commands.SendPasswordReset;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;

namespace LankaConnect.Application.Tests.Communications.Commands;

/// <summary>
/// TDD tests for SendPasswordResetCommandHandler
/// Tests written FIRST following Red-Green-Refactor cycle
/// Phase 6A.87: Updated for ITypedEmailService support
/// Phase 6A.100: Removed IEmailService - now uses only ITypedEmailService
/// </summary>
public class SendPasswordResetCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITypedEmailService> _mockTypedEmailService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;
    private readonly Mock<IEmailUrlHelper> _mockEmailUrlHelper;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<SendPasswordResetCommandHandler>> _mockLogger;
    private readonly SendPasswordResetCommandHandler _handler;

    public SendPasswordResetCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTypedEmailService = new Mock<ITypedEmailService>();
        _mockEmailTemplateService = new Mock<IEmailTemplateService>();
        _mockEmailUrlHelper = new Mock<IEmailUrlHelper>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<SendPasswordResetCommandHandler>>();

        // Default setup for typed email service to return success
        _mockTypedEmailService.Setup(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok(Guid.NewGuid().ToString(), 100));

        // Default setup for email URL helper
        _mockEmailUrlHelper.Setup(x => x.BuildPasswordResetUrl(It.IsAny<string>()))
            .Returns<string>(token => $"https://staging.lankaconnect.com/reset-password?token={token}");

        _handler = new SendPasswordResetCommandHandler(
            _mockUserRepository.Object,
            _mockTypedEmailService.Object,
            _mockEmailTemplateService.Object,
            _mockEmailUrlHelper.Object,
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

        // Phase 6A.87: Setup typed email service (default success setup in constructor)
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

        // Phase 6A.87: Verify typed email service was called with correct parameters
        _mockTypedEmailService.Verify(
            e => e.SendEmailAsync(
                It.Is<PasswordResetEmailParams>(p =>
                    p.UserEmail == email &&
                    !string.IsNullOrEmpty(p.ResetToken) &&
                    !string.IsNullOrEmpty(p.ResetLink)),
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
        // Phase 6A.87: Verify typed email service was NOT called
        _mockTypedEmailService.Verify(
            e => e.SendEmailAsync(
                It.IsAny<IEmailParameters>(),
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

        // Phase 6A.87: Verify NO email was sent via typed service (security)
        _mockTypedEmailService.Verify(
            e => e.SendEmailAsync(
                It.IsAny<IEmailParameters>(),
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

        // Phase 6A.87: Verify NO email was sent via typed service
        _mockTypedEmailService.Verify(
            e => e.SendEmailAsync(
                It.IsAny<IEmailParameters>(),
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

        // Phase 6A.87: Verify NO new email was sent via typed service (rate limiting)
        _mockTypedEmailService.Verify(
            e => e.SendEmailAsync(
                It.IsAny<IEmailParameters>(),
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

        // Phase 6A.87: Uses default typed email service setup (success)
        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.WasRecentlySent.Should().BeFalse();

        // Phase 6A.87: Verify email WAS sent via typed service (rate limiting bypassed)
        _mockTypedEmailService.Verify(
            e => e.SendEmailAsync(
                It.IsAny<PasswordResetEmailParams>(),
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

        // Phase 6A.101: Commit happens BEFORE email send to prevent concurrency issues
        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Phase 6A.87: Setup typed email service to return failure
        _mockTypedEmailService.Setup(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Fail(Guid.NewGuid().ToString(), new List<string> { "SMTP server unavailable" }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to send password reset email");

        // Phase 6A.101: Database commit IS called (before email send) - token is saved
        // This allows user to retry if email fails
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
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

        // Phase 6A.87: Uses default typed email service setup (success)
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

        // Phase 6A.101: Commit happens BEFORE email send - if it fails, no email is sent
        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("An error occurred while sending password reset email");

        // Phase 6A.101: Verify email was NOT sent (database failed first)
        _mockTypedEmailService.Verify(
            e => e.SendEmailAsync(
                It.IsAny<IEmailParameters>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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

        // Phase 6A.87: Uses default typed email service setup (success)
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
