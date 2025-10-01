using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using LankaConnect.Application.Auth.Commands.RegisterUser;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Tests.Auth;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHashingService> _mockPasswordHashingService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<RegisterUserHandler>> _mockLogger;
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHashingService = new Mock<IPasswordHashingService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<RegisterUserHandler>>();
        
        _handler = new RegisterUserHandler(
            _mockUserRepository.Object,
            _mockPasswordHashingService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccessWithUserResponse()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe",
            UserRole.User);

        var email = Email.Create(request.Email).Value;
        
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((User?)null);
        
        _mockPasswordHashingService.Setup(p => p.ValidatePasswordStrength(request.Password))
                                  .Returns(Result.Success());
        
        _mockPasswordHashingService.Setup(p => p.HashPassword(request.Password))
                                  .Returns(Result<string>.Success("hashedpassword123"));
        
        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be(request.Email);
        result.Value.FullName.Should().Be("John Doe");
        result.Value.EmailVerificationRequired.Should().BeTrue();
        
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "invalid-email",
            "ValidPassword123!",
            "John",
            "Doe");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid email format");
        
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe");

        var email = Email.Create(request.Email).Value;
        var existingUser = User.Create(email, "Jane", "Smith").Value;
        
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("A user with this email already exists");
        
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithWeakPassword_ShouldReturnFailure()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "weak",
            "John",
            "Doe");

        var email = Email.Create(request.Email).Value;
        
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((User?)null);
        
        _mockPasswordHashingService.Setup(p => p.ValidatePasswordStrength(request.Password))
                                  .Returns(Result.Failure("Password is too weak"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Password is too weak");
        
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithPasswordHashingFailure_ShouldReturnFailure()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe");

        var email = Email.Create(request.Email).Value;
        
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((User?)null);
        
        _mockPasswordHashingService.Setup(p => p.ValidatePasswordStrength(request.Password))
                                  .Returns(Result.Success());
        
        _mockPasswordHashingService.Setup(p => p.HashPassword(request.Password))
                                  .Returns(Result<string>.Failure("Hashing failed"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Hashing failed");
        
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "ValidPassword123!", "John", "Doe")]
    [InlineData("test@example.com", "", "John", "Doe")]
    [InlineData("test@example.com", "ValidPassword123!", "", "Doe")]
    [InlineData("test@example.com", "ValidPassword123!", "John", "")]
    public async Task Handle_WithEmptyRequiredFields_ShouldReturnFailure(string email, string password, string firstName, string lastName)
    {
        // Arrange
        var request = new RegisterUserCommand(email, password, firstName, lastName);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithDatabaseError_ShouldReturnFailure()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe");

        var email = Email.Create(request.Email).Value;
        
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((User?)null);
        
        _mockPasswordHashingService.Setup(p => p.ValidatePasswordStrength(request.Password))
                                  .Returns(Result.Success());
        
        _mockPasswordHashingService.Setup(p => p.HashPassword(request.Password))
                                  .Returns(Result<string>.Success("hashedpassword123"));
        
        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("An error occurred during user registration");
    }

    [Theory]
    [InlineData(UserRole.User)]
    [InlineData(UserRole.BusinessOwner)]
    [InlineData(UserRole.Moderator)]
    [InlineData(UserRole.Admin)]
    public async Task Handle_WithDifferentRoles_ShouldCreateUserWithCorrectRole(UserRole role)
    {
        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe",
            role);

        var email = Email.Create(request.Email).Value;
        User? capturedUser = null;
        
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((User?)null);
        
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                          .Callback<User, CancellationToken>((user, _) => capturedUser = user);
        
        _mockPasswordHashingService.Setup(p => p.ValidatePasswordStrength(request.Password))
                                  .Returns(Result.Success());
        
        _mockPasswordHashingService.Setup(p => p.HashPassword(request.Password))
                                  .Returns(Result<string>.Success("hashedpassword123"));
        
        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedUser.Should().NotBeNull();
        capturedUser!.Role.Should().Be(role);
    }

    [Fact]
    public async Task Handle_ShouldSetEmailVerificationToken()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe");

        var email = Email.Create(request.Email).Value;
        User? capturedUser = null;
        
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((User?)null);
        
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                          .Callback<User, CancellationToken>((user, _) => capturedUser = user);
        
        _mockPasswordHashingService.Setup(p => p.ValidatePasswordStrength(request.Password))
                                  .Returns(Result.Success());
        
        _mockPasswordHashingService.Setup(p => p.HashPassword(request.Password))
                                  .Returns(Result<string>.Success("hashedpassword123"));
        
        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedUser.Should().NotBeNull();
        capturedUser!.EmailVerificationToken.Should().NotBeNullOrEmpty();
        capturedUser.EmailVerificationTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
        capturedUser.IsEmailVerified.Should().BeFalse();
    }
}