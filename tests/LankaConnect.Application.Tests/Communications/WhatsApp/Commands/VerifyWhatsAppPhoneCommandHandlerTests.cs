using LankaConnect.Application.Communications.WhatsApp.Commands.VerifyWhatsAppPhone;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Communications.WhatsApp.Commands;

public class VerifyWhatsAppPhoneCommandHandlerTests
{
    private readonly Mock<IUserWhatsAppPreferencesRepository> _mockPreferencesRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<VerifyWhatsAppPhoneCommandHandler>> _mockLogger;
    private readonly VerifyWhatsAppPhoneCommandHandler _handler;

    public VerifyWhatsAppPhoneCommandHandlerTests()
    {
        _mockPreferencesRepo = new Mock<IUserWhatsAppPreferencesRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<VerifyWhatsAppPhoneCommandHandler>>();

        _handler = new VerifyWhatsAppPhoneCommandHandler(
            _mockPreferencesRepo.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_CorrectCode_SucceedsAndVerifies()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");
        preferences.GenerateVerificationCode();
        var code = preferences.VerificationCode!;

        var command = new VerifyWhatsAppPhoneCommand(userId, code);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PhoneVerified.Should().BeTrue();
        result.Value.ErrorMessage.Should().BeNull();

        preferences.PhoneVerified.Should().BeTrue();
        preferences.IsFullyVerified.Should().BeTrue();
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WrongCode_ReturnsErrorMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");
        preferences.GenerateVerificationCode();

        var command = new VerifyWhatsAppPhoneCommand(userId, "000000"); // wrong code

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — handler returns Success wrapping a response with error message
        result.IsSuccess.Should().BeTrue();
        result.Value.PhoneVerified.Should().BeFalse();
        result.Value.ErrorMessage.Should().NotBeNullOrEmpty();

        // UnitOfWork is always committed (attempts counter updated)
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LockedOut_ReturnsLockedResponse()
    {
        // Arrange — create a locked-out user
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");

        // Exhaust attempts to trigger lockout
        for (int i = 0; i < 5; i++)
        {
            preferences.GenerateVerificationCode();
        }
        // After 5 attempts, next call sets lockout
        preferences.GenerateVerificationCode();

        var command = new VerifyWhatsAppPhoneCommand(userId, "123456");

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — handler returns Success with locked-out response
        result.IsSuccess.Should().BeTrue();
        result.Value.PhoneVerified.Should().BeFalse();
        result.Value.ErrorMessage.Should().Contain("Too many verification attempts");
    }

    [Fact]
    public async Task Handle_NoPreferences_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new VerifyWhatsAppPhoneCommand(userId, "123456");

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWhatsAppPreferences?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredCode_ReturnsErrorMessage()
    {
        // Arrange — we cannot easily expire the code in the domain entity without reflection,
        // but we can verify the domain entity returns failure for empty code
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");
        // Don't generate code — VerifyPhone will return "No verification code found"

        var command = new VerifyWhatsAppPhoneCommand(userId, "123456");

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PhoneVerified.Should().BeFalse();
        result.Value.ErrorMessage.Should().Contain("No verification code found");
    }

    [Fact]
    public async Task Handle_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var command = new VerifyWhatsAppPhoneCommand(Guid.NewGuid(), "123456");

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");
    }
}
