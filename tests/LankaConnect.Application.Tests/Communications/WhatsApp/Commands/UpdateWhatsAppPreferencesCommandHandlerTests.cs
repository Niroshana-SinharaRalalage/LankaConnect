using LankaConnect.Application.Communications.WhatsApp.Commands.UpdateWhatsAppPreferences;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Communications.WhatsApp.Commands;

public class UpdateWhatsAppPreferencesCommandHandlerTests
{
    private readonly Mock<IUserWhatsAppPreferencesRepository> _mockPreferencesRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<UpdateWhatsAppPreferencesCommandHandler>> _mockLogger;
    private readonly UpdateWhatsAppPreferencesCommandHandler _handler;

    public UpdateWhatsAppPreferencesCommandHandlerTests()
    {
        _mockPreferencesRepo = new Mock<IUserWhatsAppPreferencesRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<UpdateWhatsAppPreferencesCommandHandler>>();

        _handler = new UpdateWhatsAppPreferencesCommandHandler(
            _mockPreferencesRepo.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidPreferences_UpdatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");

        var command = new UpdateWhatsAppPreferencesCommand(
            UserId: userId,
            EventRegistration: true,
            EventReminder: false,
            EventCancellation: true,
            EventUpdate: false,
            SignupCommitment: true,
            Refund: true,
            Newsletter: false,
            NewEvent: true,
            Payment: false,
            PreferredLanguage: "si",
            QuietHoursStart: new TimeOnly(22, 0),
            QuietHoursEnd: new TimeOnly(8, 0),
            RespectCulturalTiming: true);

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

        preferences.NotifyEventRegistration.Should().BeTrue();
        preferences.NotifyEventReminder.Should().BeFalse();
        preferences.NotifyEventCancellation.Should().BeTrue();
        preferences.NotifyEventUpdate.Should().BeFalse();
        preferences.NotifySignupCommitment.Should().BeTrue();
        preferences.NotifyRefund.Should().BeTrue();
        preferences.NotifyNewsletter.Should().BeFalse();
        preferences.NotifyNewEvent.Should().BeTrue();
        preferences.NotifyPayment.Should().BeFalse();

        preferences.PreferredLanguage.Should().Be("si");
        preferences.QuietHoursStart.Should().Be(new TimeOnly(22, 0));
        preferences.QuietHoursEnd.Should().Be(new TimeOnly(8, 0));
        preferences.RespectCulturalTiming.Should().BeTrue();

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoPreferences_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateWhatsAppPreferencesCommand(
            UserId: userId,
            EventRegistration: true,
            EventReminder: true,
            EventCancellation: true,
            EventUpdate: true,
            SignupCommitment: true,
            Refund: true,
            Newsletter: true,
            NewEvent: true,
            Payment: true,
            PreferredLanguage: "en",
            QuietHoursStart: null,
            QuietHoursEnd: null,
            RespectCulturalTiming: false);

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
    public async Task Handle_NullPreferredLanguage_DefaultsToEnglish()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");

        var command = new UpdateWhatsAppPreferencesCommand(
            UserId: userId,
            EventRegistration: true,
            EventReminder: true,
            EventCancellation: true,
            EventUpdate: true,
            SignupCommitment: true,
            Refund: true,
            Newsletter: true,
            NewEvent: true,
            Payment: true,
            PreferredLanguage: null, // null should default to "en"
            QuietHoursStart: null,
            QuietHoursEnd: null,
            RespectCulturalTiming: false);

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
        preferences.PreferredLanguage.Should().Be("en");
    }

    [Fact]
    public async Task Handle_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var command = new UpdateWhatsAppPreferencesCommand(
            UserId: Guid.NewGuid(),
            EventRegistration: true,
            EventReminder: true,
            EventCancellation: true,
            EventUpdate: true,
            SignupCommitment: true,
            Refund: true,
            Newsletter: true,
            NewEvent: true,
            Payment: true,
            PreferredLanguage: "en",
            QuietHoursStart: null,
            QuietHoursEnd: null,
            RespectCulturalTiming: false);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");
    }
}
