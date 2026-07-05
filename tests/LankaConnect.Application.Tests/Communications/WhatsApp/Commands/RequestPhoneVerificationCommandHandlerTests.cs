using LankaConnect.Modules.Communications.Application.WhatsApp.Commands.RequestPhoneVerification;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Communications.WhatsApp.Commands;

public class RequestPhoneVerificationCommandHandlerTests
{
    private readonly Mock<IUserWhatsAppPreferencesRepository> _mockPreferencesRepo;
    private readonly Mock<IPhoneVerificationService> _mockPhoneVerificationService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<RequestPhoneVerificationCommandHandler>> _mockLogger;
    private readonly RequestPhoneVerificationCommandHandler _handler;

    public RequestPhoneVerificationCommandHandlerTests()
    {
        _mockPreferencesRepo = new Mock<IUserWhatsAppPreferencesRepository>();
        _mockPhoneVerificationService = new Mock<IPhoneVerificationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<RequestPhoneVerificationCommandHandler>>();

        _handler = new RequestPhoneVerificationCommandHandler(
            _mockPreferencesRepo.Object,
            _mockPhoneVerificationService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidUser_GeneratesCodeAndSends()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");

        var command = new RequestPhoneVerificationCommand(userId);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockPhoneVerificationService
            .Setup(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verification code should have been generated on the domain entity
        preferences.VerificationCode.Should().NotBeNullOrEmpty();
        preferences.VerificationAttempts.Should().Be(1);

        // Code was persisted before sending
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verification service was called with the phone and code
        _mockPhoneVerificationService.Verify(
            x => x.SendVerificationCodeAsync(
                "+14155551234",
                It.Is<string>(code => code.Length == 6),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LockedOut_ReturnsFailure()
    {
        // Arrange — exhaust verification attempts to trigger lockout
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");

        // Exhaust 5 attempts to trigger lockout
        for (int i = 0; i < 5; i++)
        {
            preferences.GenerateVerificationCode();
        }
        // The 5th attempt should have locked the account
        // Generate one more to confirm lockout is set
        var lockResult = preferences.GenerateVerificationCode();

        // preferences.IsLocked should now be true
        var command = new RequestPhoneVerificationCommand(userId);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Too many verification attempts");
        _mockPhoneVerificationService.Verify(
            x => x.SendVerificationCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NoPreferences_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RequestPhoneVerificationCommand(userId);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWhatsAppPreferences?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
        _mockPhoneVerificationService.Verify(
            x => x.SendVerificationCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SmsSendFails_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");

        var command = new RequestPhoneVerificationCommand(userId);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockPhoneVerificationService
            .Setup(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("SMS delivery failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("failed to send");

        // Code should still have been persisted before the send attempt
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SmsSendThrowsException_ReturnsFailureInsteadOfThrowing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");

        var command = new RequestPhoneVerificationCommand(userId);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockPhoneVerificationService
            .Setup(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — handler catches SMS exceptions and returns a failure result
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("failed to send");
    }

    [Fact]
    public async Task Handle_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var command = new RequestPhoneVerificationCommand(Guid.NewGuid());

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");
    }
}
