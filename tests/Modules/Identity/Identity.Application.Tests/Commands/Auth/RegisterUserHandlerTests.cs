using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MediatR;
using LankaConnect.Modules.Identity.Application.Commands.Auth.RegisterUser;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Application.Commands.SendEmailVerification;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Domain.ValueObjects;
using LankaConnect.Modules.Identity.Domain.Enums;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Domain.Entities;
using Email = LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Modules.Identity.Application.Tests.Commands.Auth;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHashingService> _mockPasswordHashingService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<RegisterUserHandler>> _mockLogger;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IUserWhatsAppPreferencesRepository> _mockWhatsAppPreferencesRepository;
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHashingService = new Mock<IPasswordHashingService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<RegisterUserHandler>>();
        _mockMediator = new Mock<IMediator>();
        _mockWhatsAppPreferencesRepository = new Mock<IUserWhatsAppPreferencesRepository>();

        // Setup default success response for email verification
        _mockMediator
            .Setup(m => m.Send(It.IsAny<SendEmailVerificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SendEmailVerificationResponse>.Success(
                new SendEmailVerificationResponse(
                    Guid.NewGuid(),
                    "test@example.com",
                    DateTime.UtcNow.AddHours(24),
                    false)));

        _handler = new RegisterUserHandler(
            _mockUserRepository.Object,
            _mockPasswordHashingService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockMediator.Object,
            _mockWhatsAppPreferencesRepository.Object);
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
            UserRole.GeneralUser);

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
    public async Task Handle_WithDatabaseError_ShouldRethrowException()
    {
        // Arrange - Phase 6A.X: Handler now re-throws exceptions for proper error handling
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

        // Act & Assert - Expect exception to be re-thrown (not caught)
        await Assert.ThrowsAsync<Exception>(async () =>
            await _handler.Handle(request, CancellationToken.None));
    }

    [Theory]
    [InlineData(UserRole.GeneralUser)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.AdminManager)]
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
    public async Task Handle_WithEventOrganizerRole_ShouldCreateUserAsGeneralUserWithPendingUpgrade()
    {
        // Arrange: Phase 6A.0 - EventOrganizer selection converts to GeneralUser with pending upgrade role
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe",
            UserRole.EventOrganizer);

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
        // Phase 6A.0: EventOrganizer should be converted to GeneralUser with pending upgrade role for admin approval
        capturedUser!.Role.Should().Be(UserRole.GeneralUser);
        capturedUser!.PendingUpgradeRole.Should().Be(UserRole.EventOrganizer);
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

    [Fact]
    public async Task Handle_WithValidRequest_ShouldSendVerificationEmail()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe",
            UserRole.GeneralUser);

        var email = Email.Create(request.Email).Value;
        Guid capturedUserId = Guid.Empty;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((User?)null);

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                          .Callback<User, CancellationToken>((user, _) => capturedUserId = user.Id);

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
        result.Value.EmailVerificationRequired.Should().BeTrue();

        // Phase 6A.53: Email is now sent automatically via domain event (MemberVerificationRequestedEvent)
        // when CommitAsync() is called, NOT via explicit MediatR SendEmailVerificationCommand.
        // The domain event handler (MemberVerificationRequestedEventHandler) sends the email.
        // We verify that CommitAsync was called, which triggers domain event dispatch.
        _mockUnitOfWork.Verify(
            u => u.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once,
            "CommitAsync should be called to save user and dispatch domain events");
    }

    [Fact]
    public async Task Handle_WhenEmailFails_ShouldStillSucceedRegistration()
    {
        // Phase 6A.53: This test is NO LONGER RELEVANT after removing explicit SendEmailVerificationCommand.
        // Email sending now happens asynchronously via domain event (MemberVerificationRequestedEvent).
        // The domain event handler runs AFTER CommitAsync() completes, so email failures don't affect
        // registration success. Registration completes successfully regardless of email delivery status.

        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe",
            UserRole.GeneralUser);

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
        result.IsSuccess.Should().BeTrue(); // Registration always succeeds now
        result.Value.Should().NotBeNull();
        result.Value.EmailVerificationRequired.Should().BeTrue();

        // Verify user was saved (which triggers email via domain event handler)
        _mockUnitOfWork.Verify(
            u => u.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once,
            "CommitAsync should be called to save user and dispatch verification email via domain events");
    }

    // === Phase 7A.6A: WhatsApp Opt-In During Registration ===

    [Fact]
    public async Task Handle_WithWhatsAppPhone_ShouldCreateWhatsAppPreferences()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe",
            UserRole.GeneralUser,
            null,
            WhatsAppPhoneNumber: "+14155551234");

        var email = Email.Create(request.Email).Value;
        UserWhatsAppPreferences? capturedPrefs = null;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((User?)null);

        _mockPasswordHashingService.Setup(p => p.ValidatePasswordStrength(request.Password))
                                  .Returns(Result.Success());

        _mockPasswordHashingService.Setup(p => p.HashPassword(request.Password))
                                  .Returns(Result<string>.Success("hashedpassword123"));

        _mockWhatsAppPreferencesRepository
            .Setup(r => r.AddAsync(It.IsAny<UserWhatsAppPreferences>(), It.IsAny<CancellationToken>()))
            .Callback<UserWhatsAppPreferences, CancellationToken>((prefs, _) => capturedPrefs = prefs);

        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedPrefs.Should().NotBeNull();
        capturedPrefs!.WhatsAppEnabled.Should().BeTrue();
        capturedPrefs.WhatsAppPhoneNumber.Should().Be("+14155551234");
        capturedPrefs.PhoneVerified.Should().BeFalse();

        _mockWhatsAppPreferencesRepository.Verify(
            r => r.AddAsync(It.IsAny<UserWhatsAppPreferences>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutWhatsAppPhone_ShouldNotCreateWhatsAppPreferences()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe",
            UserRole.GeneralUser);

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
        _mockWhatsAppPreferencesRepository.Verify(
            r => r.AddAsync(It.IsAny<UserWhatsAppPreferences>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyWhatsAppPhone_ShouldNotCreateWhatsAppPreferences()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe",
            UserRole.GeneralUser,
            null,
            WhatsAppPhoneNumber: "");

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
        _mockWhatsAppPreferencesRepository.Verify(
            r => r.AddAsync(It.IsAny<UserWhatsAppPreferences>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidWhatsAppPhone_ShouldSkipPreferencesButSucceed()
    {
        // Arrange: Invalid E.164 should be caught by domain method, but registration still succeeds
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe",
            UserRole.GeneralUser,
            null,
            WhatsAppPhoneNumber: "not-a-phone");

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

        // Assert: Registration succeeds, but WhatsApp preferences not created
        result.IsSuccess.Should().BeTrue();
        _mockWhatsAppPreferencesRepository.Verify(
            r => r.AddAsync(It.IsAny<UserWhatsAppPreferences>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithWhatsAppPhone_ShouldCommitAtomically()
    {
        // Arrange: Verify that user + WhatsApp preferences are committed in single transaction
        var request = new RegisterUserCommand(
            "test@example.com",
            "ValidPassword123!",
            "John",
            "Doe",
            UserRole.GeneralUser,
            null,
            WhatsAppPhoneNumber: "+14155551234");

        var email = Email.Create(request.Email).Value;
        var callOrder = new List<string>();

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((User?)null);

        _mockPasswordHashingService.Setup(p => p.ValidatePasswordStrength(request.Password))
                                  .Returns(Result.Success());

        _mockPasswordHashingService.Setup(p => p.HashPassword(request.Password))
                                  .Returns(Result<string>.Success("hashedpassword123"));

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                          .Callback<User, CancellationToken>((_, _) => callOrder.Add("UserAdd"));

        _mockWhatsAppPreferencesRepository
            .Setup(r => r.AddAsync(It.IsAny<UserWhatsAppPreferences>(), It.IsAny<CancellationToken>()))
            .Callback<UserWhatsAppPreferences, CancellationToken>((_, _) => callOrder.Add("WhatsAppAdd"));

        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                      .Callback<CancellationToken>(_ => callOrder.Add("Commit"))
                      .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert: User added first, then WhatsApp prefs, then single Commit
        result.IsSuccess.Should().BeTrue();
        callOrder.Should().ContainInOrder("UserAdd", "WhatsAppAdd", "Commit");
        callOrder.Count(c => c == "Commit").Should().Be(1, "should be exactly one CommitAsync call");
    }
}
