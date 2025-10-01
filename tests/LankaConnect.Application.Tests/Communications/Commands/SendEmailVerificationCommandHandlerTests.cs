using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using UserEmail = LankaConnect.Domain.Users.ValueObjects.Email;
using DomainEmailMessage = LankaConnect.Domain.Communications.Entities.EmailMessage;

namespace LankaConnect.Application.Tests.Communications.Commands;

public class SendEmailVerificationCommandHandlerTests
{
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ITokenService> _tokenService;
    private readonly Mock<ILogger<SendEmailVerificationCommandHandler>> _logger;
    private readonly SendEmailVerificationCommandHandler _handler;

    public SendEmailVerificationCommandHandlerTests()
    {
        _emailService = new Mock<IEmailService>();
        _userRepository = new Mock<IUserRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _tokenService = new Mock<ITokenService>();
        _logger = new Mock<ILogger<SendEmailVerificationCommandHandler>>();
        
        _handler = new SendEmailVerificationCommandHandler(
            _emailService.Object,
            _userRepository.Object,
            _unitOfWork.Object,
            _tokenService.Object,
            _logger.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidEmail_ShouldSendVerificationEmail()
    {
        // Arrange
        var email = "test@example.com";
        var command = new SendEmailVerificationCommand(email);
        var user = CreateTestUser(email);
        var verificationToken = "secure-token-123";

        _userRepository.Setup(x => x.GetByEmailAsync(It.IsAny<UserEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        
        _tokenService.Setup(x => x.GenerateEmailVerificationTokenAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationToken);
        
        _emailService.Setup(x => x.SendTemplatedEmailAsync(
            "email-verification",
            email,
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            "email-verification",
            email,
            It.Is<Dictionary<string, object>>(data => 
                data.ContainsKey("verificationToken") && 
                data["verificationToken"].ToString() == verificationToken),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var command = new SendEmailVerificationCommand("nonexistent@example.com");
        
        _userRepository.Setup(x => x.GetByEmailAsync(It.IsAny<UserEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("User not found");
        
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new SendEmailVerificationCommand("invalid-email");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Invalid email format");
        
        _userRepository.Verify(x => x.GetByEmailAsync(
            It.IsAny<UserEmail>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static User CreateTestUser(string email)
    {
        var userEmail = UserEmail.Create(email).Value;
        return User.Create(userEmail, "Test", "User").Value;
    }
}

// Placeholder classes - these should be implemented in the actual application
public record SendEmailVerificationCommand(string Email) : IRequest<Result>;

public class SendEmailVerificationCommandHandler : IRequestHandler<SendEmailVerificationCommand, Result>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ILogger<SendEmailVerificationCommandHandler> _logger;

    public SendEmailVerificationCommandHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        ILogger<SendEmailVerificationCommandHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result> Handle(SendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate email format
            var emailResult = UserEmail.Create(request.Email);
            if (emailResult.IsFailure)
                return Result.Failure("Invalid email format");

            // Find user
            var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
            if (user == null)
                return Result.Failure("User not found");

            // Generate verification token
            var token = await _tokenService.GenerateEmailVerificationTokenAsync(user.Id, cancellationToken);

            // Prepare template data
            var templateData = new Dictionary<string, object>
            {
                ["userName"] = $"{user.FirstName} {user.LastName}",
                ["email"] = request.Email,
                ["verificationToken"] = token,
                ["verificationUrl"] = $"https://lankaconnect.com/verify?token={token}"
            };

            // Send verification email
            var result = await _emailService.SendTemplatedEmailAsync(
                "email-verification",
                request.Email,
                templateData,
                cancellationToken);

            return result.IsSuccess ? Result.Success() : Result.Failure("Failed to send verification email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending verification email for {Email}", request.Email);
            return Result.Failure("An unexpected error occurred");
        }
    }
}

public interface ITokenService
{
    Task<string> GenerateEmailVerificationTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default);
}