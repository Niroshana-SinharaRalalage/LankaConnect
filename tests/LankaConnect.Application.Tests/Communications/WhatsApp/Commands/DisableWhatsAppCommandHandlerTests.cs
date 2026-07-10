using LankaConnect.Modules.Communications.Application.WhatsApp.Commands.DisableWhatsApp;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Communications.WhatsApp.Commands;

public class DisableWhatsAppCommandHandlerTests
{
    private readonly Mock<IUserWhatsAppPreferencesRepository> _mockPreferencesRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<DisableWhatsAppCommandHandler>> _mockLogger;
    private readonly DisableWhatsAppCommandHandler _handler;

    public DisableWhatsAppCommandHandlerTests()
    {
        _mockPreferencesRepo = new Mock<IUserWhatsAppPreferencesRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<DisableWhatsAppCommandHandler>>();

        _handler = new DisableWhatsAppCommandHandler(
            _mockPreferencesRepo.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_DisablesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");

        var command = new DisableWhatsAppCommand(userId);

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
        preferences.WhatsAppEnabled.Should().BeFalse();
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentPreferences_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DisableWhatsAppCommand(userId);

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
    public async Task Handle_AlreadyDisabled_StillSucceeds()
    {
        // Arrange — preferences exist but WhatsApp is already disabled
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        // Create() leaves WhatsAppEnabled = false

        var command = new DisableWhatsAppCommand(userId);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — DisableWhatsApp() always returns success
        result.IsSuccess.Should().BeTrue();
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var command = new DisableWhatsAppCommand(Guid.NewGuid());

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");
    }
}
