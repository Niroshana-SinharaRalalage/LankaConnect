using LankaConnect.Application.Communications.WhatsApp.Commands.EnableWhatsApp;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Options;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LankaConnect.Application.Tests.Communications.WhatsApp.Commands;

public class EnableWhatsAppCommandHandlerTests
{
    private readonly Mock<IUserWhatsAppPreferencesRepository> _mockPreferencesRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<EnableWhatsAppCommandHandler>> _mockLogger;
    private readonly WhatsAppOptions _options;
    private readonly EnableWhatsAppCommandHandler _handler;

    public EnableWhatsAppCommandHandlerTests()
    {
        _mockPreferencesRepo = new Mock<IUserWhatsAppPreferencesRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<EnableWhatsAppCommandHandler>>();
        _options = new WhatsAppOptions { Enabled = true };

        _handler = new EnableWhatsAppCommandHandler(
            _mockPreferencesRepo.Object,
            _mockUnitOfWork.Object,
            Options.Create(_options),
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_NewUser_CreatesPreferencesAndEnables()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var phone = "+14155551234";
        var command = new EnableWhatsAppCommand(userId, phone);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWhatsAppPreferences?)null);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.PhoneNumber.Should().Be(phone);
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.PhoneVerified.Should().BeFalse();

        _mockPreferencesRepo.Verify(
            x => x.AddAsync(It.Is<UserWhatsAppPreferences>(p => p.UserId == userId), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingUser_UpdatesPhoneNumber()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingPrefs = UserWhatsAppPreferences.Create(userId);
        existingPrefs.EnableWhatsApp("+10000000000"); // old phone

        var newPhone = "+14155559999";
        var command = new EnableWhatsAppCommand(userId, newPhone);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPrefs);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PhoneNumber.Should().Be(newPhone);
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.PhoneVerified.Should().BeFalse();

        // Should NOT create a new entity — only update existing
        _mockPreferencesRepo.Verify(
            x => x.AddAsync(It.IsAny<UserWhatsAppPreferences>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-phone")]
    [InlineData("14155551234")] // missing + prefix
    [InlineData("+0123456789")] // starts with +0
    public async Task Handle_InvalidPhone_ReturnsFailure(string invalidPhone)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new EnableWhatsAppCommand(userId, invalidPhone);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWhatsAppPreferences?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhatsAppDisabledByFlag_ReturnsFailure()
    {
        // Arrange
        var disabledOptions = new WhatsAppOptions { Enabled = false };
        var handler = new EnableWhatsAppCommandHandler(
            _mockPreferencesRepo.Object,
            _mockUnitOfWork.Object,
            Options.Create(disabledOptions),
            _mockLogger.Object);

        var command = new EnableWhatsAppCommand(Guid.NewGuid(), "+14155551234");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("disabled");
        _mockPreferencesRepo.Verify(
            x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var command = new EnableWhatsAppCommand(Guid.NewGuid(), "+14155551234");

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");
    }
}
