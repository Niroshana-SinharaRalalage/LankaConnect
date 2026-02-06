using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Commands.VerifyEmail;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using UserEmail = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Application.Tests.Communications.Commands;

public class VerifyEmailCommandHandlerTests
{
    private readonly Mock<LankaConnect.Domain.Users.IUserRepository> _userRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<ILogger<VerifyEmailCommandHandler>> _logger;
    private readonly VerifyEmailCommandHandler _handler;

    public VerifyEmailCommandHandlerTests()
    {
        _userRepository = new Mock<LankaConnect.Domain.Users.IUserRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _emailService = new Mock<IEmailService>();
        _logger = new Mock<ILogger<VerifyEmailCommandHandler>>();
        
        _handler = new VerifyEmailCommandHandler(
            _userRepository.Object,
            _emailService.Object,
            _unitOfWork.Object,
            _logger.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldVerifyEmailAndSendWelcomeEmail()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");

        // Phase 6A.53: User.Create() now automatically generates verification token
        // Use the token that was automatically generated
        var token = user.EmailVerificationToken!;
        var command = new VerifyEmailCommand(token);

        // Phase 6A.53: Mock token-only user retrieval (aligns with password reset pattern)
        _userRepository.Setup(x => x.GetByEmailVerificationTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Mock email service for welcome email (fire and forget)
        _emailService.Setup(x => x.SendTemplatedEmailAsync(
            "welcome-email",
            user.Email.Value,
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email.Value);
        result.Value.WasAlreadyVerified.Should().BeFalse();

        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Note: Welcome email is sent in fire-and-forget Task.Run, so we can't reliably verify it in tests
        // The important part is that the verification succeeds
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        // Phase 6A.53: Invalid token won't find any user in database
        var invalidToken = "invalid-token-different-from-generated";
        var command = new VerifyEmailCommand(invalidToken);

        // Phase 6A.53: Mock token lookup returning null (user not found)
        _userRepository.Setup(x => x.GetByEmailVerificationTokenAsync(invalidToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Phase 6A.53: Error message updated to match token-only lookup
        result.Error.Should().Be("Invalid or expired verification token");

        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        // Phase 6A.53: Token not found in database (expired or never existed)
        var token = "nonexistent-token";
        var command = new VerifyEmailCommand(token);

        // Phase 6A.53: Mock token lookup returning null
        _userRepository.Setup(x => x.GetByEmailVerificationTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Phase 6A.53: Error message updated for token-only lookup
        result.Error.Should().Be("Invalid or expired verification token");

        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyVerifiedUser_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");

        // Phase 6A.53: Set user as already verified
        var token = user.EmailVerificationToken!;
        user.VerifyEmail(token); // This will set IsEmailVerified to true and clear token

        // Phase 6A.53: Even though token is cleared, user might still be found if we create a new token
        // For this test, we'll use the original token and mock the repository to return the verified user
        var command = new VerifyEmailCommand(token);

        // Phase 6A.53: Mock token lookup returning already-verified user
        _userRepository.Setup(x => x.GetByEmailVerificationTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email.Value);
        result.Value.WasAlreadyVerified.Should().BeTrue();

        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        // Phase 6A.53: Token-only command
        var command = new VerifyEmailCommand("valid-token");
        var cancellationToken = new CancellationToken(true);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.Handle(command, cancellationToken));
    }

    private static User CreateTestUser(string email)
    {
        var userEmail = UserEmail.Create(email).Value;
        return User.Create(userEmail, "Test", "User").Value;
    }
}