using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using LankaConnect.Application.Auth.Commands.LoginUser;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Common;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Application.Tests.Auth;

public class LoginUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHashingService> _mockPasswordHashingService;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITokenConfiguration> _mockTokenConfiguration;
    private readonly Mock<ILogger<LoginUserHandler>> _mockLogger;
    private readonly LoginUserHandler _handler;
    private readonly User _testUser;

    public LoginUserHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHashingService = new Mock<IPasswordHashingService>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTokenConfiguration = new Mock<ITokenConfiguration>();
        _mockLogger = new Mock<ILogger<LoginUserHandler>>();

        _mockTokenConfiguration.Setup(c => c.AccessTokenExpirationMinutes).Returns(15);
        _mockTokenConfiguration.Setup(c => c.RefreshTokenExpirationDays).Returns(7);

        _handler = new LoginUserHandler(
            _mockUserRepository.Object,
            _mockPasswordHashingService.Object,
            _mockJwtTokenService.Object,
            _mockUnitOfWork.Object,
            _mockTokenConfiguration.Object,
            _mockLogger.Object);

        // Create test user
        var email = Email.Create("test@example.com").Value;
        _testUser = User.Create(email, "John", "Doe", UserRole.GeneralUser).Value;
        _testUser.SetPassword("hashedpassword123");
        // Make user email verified
        _testUser.GenerateEmailVerificationToken();
        _testUser.VerifyEmail(_testUser.EmailVerificationToken!);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnSuccessWithTokens()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "test@example.com", Password = "password123", RememberMe = false, IpAddress = "127.0.0.1" };
        var email = Email.Create(request.Email).Value;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(_testUser);

        _mockPasswordHashingService.Setup(p => p.VerifyPassword(request.Password, _testUser.PasswordHash!))
                                  .Returns(Result<bool>.Success(true));

        _mockJwtTokenService.Setup(j => j.GenerateAccessTokenAsync(_testUser))
                           .ReturnsAsync(Result<string>.Success("access-token"));

        _mockJwtTokenService.Setup(j => j.GenerateRefreshTokenAsync())
                           .ReturnsAsync(Result<string>.Success("refresh-token"));

        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(_testUser.Id);
        result.Value.Email.Should().Be(_testUser.Email.Value);
        result.Value.FullName.Should().Be(_testUser.FullName);
        result.Value.Role.Should().Be(_testUser.Role);
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.TokenExpiresAt.Should().BeAfter(DateTime.UtcNow);

        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "invalid-email", Password = "password123" };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email format");
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "nonexistent@example.com", Password = "password123" };
        var email = Email.Create(request.Email).Value;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task Handle_WithLockedAccount_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "test@example.com", Password = "password123" };
        var email = Email.Create(request.Email).Value;

        // Lock the user account
        for (int i = 0; i < 5; i++)
        {
            _testUser.RecordFailedLoginAttempt();
        }

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(_testUser);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("locked");
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "test@example.com", Password = "password123" };
        var email = Email.Create(request.Email).Value;

        _testUser.Deactivate();

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(_testUser);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is not active");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldReturnFailureAndRecordFailedAttempt()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "test@example.com", Password = "wrongpassword" };
        var email = Email.Create(request.Email).Value;
        var initialFailedAttempts = _testUser.FailedLoginAttempts;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(_testUser);

        _mockPasswordHashingService.Setup(p => p.VerifyPassword(request.Password, _testUser.PasswordHash!))
                                  .Returns(Result<bool>.Success(false));

        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password");
        _testUser.FailedLoginAttempts.Should().Be(initialFailedAttempts + 1);

        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnverifiedEmail_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "test@example.com", Password = "password123" };
        var email = Email.Create("unverified@example.com").Value;
        var unverifiedUser = User.Create(email, "Jane", "Doe").Value;
        unverifiedUser.SetPassword("hashedpassword123");
        // Don't verify email

        _mockUserRepository.Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(unverifiedUser);

        _mockPasswordHashingService.Setup(p => p.VerifyPassword(request.Password, unverifiedUser.PasswordHash!))
                                  .Returns(Result<bool>.Success(true));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email address must be verified before logging in");
    }

    [Fact]
    public async Task Handle_WithUserWithoutPasswordHash_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "test@example.com", Password = "password123" };
        var email = Email.Create(request.Email).Value;
        var userWithoutPassword = User.Create(email, "Jane", "Doe").Value;
        userWithoutPassword.GenerateEmailVerificationToken();
        userWithoutPassword.VerifyEmail(userWithoutPassword.EmailVerificationToken!);
        // Don't set password hash

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(userWithoutPassword);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task Handle_WithTokenGenerationFailure_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "test@example.com", Password = "password123" };
        var email = Email.Create(request.Email).Value;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(_testUser);

        _mockPasswordHashingService.Setup(p => p.VerifyPassword(request.Password, _testUser.PasswordHash!))
                                  .Returns(Result<bool>.Success(true));

        _mockJwtTokenService.Setup(j => j.GenerateAccessTokenAsync(_testUser))
                           .ReturnsAsync(Result<string>.Failure("Token generation failed"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Failed to generate access token");
    }

    [Fact]
    public async Task Handle_WithRefreshTokenGenerationFailure_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "test@example.com", Password = "password123" };
        var email = Email.Create(request.Email).Value;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(_testUser);

        _mockPasswordHashingService.Setup(p => p.VerifyPassword(request.Password, _testUser.PasswordHash!))
                                  .Returns(Result<bool>.Success(true));

        _mockJwtTokenService.Setup(j => j.GenerateAccessTokenAsync(_testUser))
                           .ReturnsAsync(Result<string>.Success("access-token"));

        _mockJwtTokenService.Setup(j => j.GenerateRefreshTokenAsync())
                           .ReturnsAsync(Result<string>.Failure("Refresh token generation failed"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Failed to generate refresh token");
    }

    [Fact]
    public async Task Handle_ShouldRecordSuccessfulLogin()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "test@example.com", Password = "password123" };
        var email = Email.Create(request.Email).Value;
        var lastLoginBefore = _testUser.LastLoginAt;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(_testUser);

        _mockPasswordHashingService.Setup(p => p.VerifyPassword(request.Password, _testUser.PasswordHash!))
                                  .Returns(Result<bool>.Success(true));

        _mockJwtTokenService.Setup(j => j.GenerateAccessTokenAsync(_testUser))
                           .ReturnsAsync(Result<string>.Success("access-token"));

        _mockJwtTokenService.Setup(j => j.GenerateRefreshTokenAsync())
                           .ReturnsAsync(Result<string>.Success("refresh-token"));

        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _testUser.LastLoginAt.Should().BeAfter(lastLoginBefore ?? DateTime.MinValue);
        _testUser.FailedLoginAttempts.Should().Be(0);
        _testUser.AccountLockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithDatabaseException_ShouldRethrowException()
    {
        // Arrange - Phase 6A.X: Handler now re-throws exceptions for proper error handling
        var request = new LoginUserCommand { Email = "test@example.com", Password = "password123" };
        var email = Email.Create(request.Email).Value;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new Exception("Database error"));

        // Act & Assert - Expect exception to be re-thrown (not caught)
        await Assert.ThrowsAsync<Exception>(async () =>
            await _handler.Handle(request, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldAddRefreshTokenToUser()
    {
        // Arrange
        var request = new LoginUserCommand { Email = "test@example.com", Password = "password123", RememberMe = false, IpAddress = "127.0.0.1" };
        var email = Email.Create(request.Email).Value;
        var initialRefreshTokenCount = _testUser.RefreshTokens.Count;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(_testUser);

        _mockPasswordHashingService.Setup(p => p.VerifyPassword(request.Password, _testUser.PasswordHash!))
                                  .Returns(Result<bool>.Success(true));

        _mockJwtTokenService.Setup(j => j.GenerateAccessTokenAsync(_testUser))
                           .ReturnsAsync(Result<string>.Success("access-token"));

        _mockJwtTokenService.Setup(j => j.GenerateRefreshTokenAsync())
                           .ReturnsAsync(Result<string>.Success("refresh-token"));

        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _testUser.RefreshTokens.Count.Should().Be(initialRefreshTokenCount + 1);
        _testUser.RefreshTokens.Should().Contain(rt => rt.Token == "refresh-token" && rt.CreatedByIp == "127.0.0.1");
    }
}