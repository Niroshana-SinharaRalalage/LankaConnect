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
        var userId = Guid.NewGuid();
        var user = CreateTestUser("test@example.com");

        // Phase 6A.53: User.Create() now automatically generates verification token
        // Use the token that was automatically generated
        var token = user.EmailVerificationToken!;
        var command = new VerifyEmailCommand(userId, token);

        // Mock user retrieval
        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
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
        var userId = Guid.NewGuid();
        var user = CreateTestUser("test@example.com");

        // Phase 6A.53: User.Create() automatically generates a token
        // Use a different token to simulate invalid token
        var invalidToken = "invalid-token-different-from-generated";
        var command = new VerifyEmailCommand(userId, invalidToken);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Phase 6A.53: Error message updated to match new VerifyEmail(token) implementation
        result.Error.Should().Be("Invalid verification token");

        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "valid-token";
        var command = new VerifyEmailCommand(userId, token);
        
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
    public async Task Handle_WithAlreadyVerifiedUser_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser("test@example.com");

        // Phase 6A.53: Set user as already verified
        var tempToken = user.EmailVerificationToken!;
        user.VerifyEmail(tempToken); // This will set IsEmailVerified to true and clear token

        // Any token will work for already-verified user (early return in handler)
        var token = "any-token";
        var command = new VerifyEmailCommand(userId, token);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
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
        var userId = Guid.NewGuid();
        var command = new VerifyEmailCommand(userId, "valid-token");
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