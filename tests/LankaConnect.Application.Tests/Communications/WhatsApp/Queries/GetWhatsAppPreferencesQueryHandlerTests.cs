using LankaConnect.Application.Communications.WhatsApp.Queries.GetWhatsAppPreferences;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Communications.WhatsApp.Queries;

public class GetWhatsAppPreferencesQueryHandlerTests
{
    private readonly Mock<IUserWhatsAppPreferencesRepository> _mockPreferencesRepo;
    private readonly Mock<ILogger<GetWhatsAppPreferencesQueryHandler>> _mockLogger;
    private readonly GetWhatsAppPreferencesQueryHandler _handler;

    public GetWhatsAppPreferencesQueryHandlerTests()
    {
        _mockPreferencesRepo = new Mock<IUserWhatsAppPreferencesRepository>();
        _mockLogger = new Mock<ILogger<GetWhatsAppPreferencesQueryHandler>>();

        _handler = new GetWhatsAppPreferencesQueryHandler(
            _mockPreferencesRepo.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_ReturnsDtoWithAllFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");
        preferences.UpdateNotificationPreferences(
            notifyRegistration: true,
            notifyReminder: false,
            notifyCancellation: true,
            notifyUpdate: false,
            notifySignup: true,
            notifyRefund: true,
            notifyNewsletter: false,
            notifyNewEvent: true,
            notifyPayment: false);
        preferences.UpdateCulturalPreferences("si", new TimeOnly(22, 0), new TimeOnly(7, 0), true);

        var query = new GetWhatsAppPreferencesQuery(userId);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.UserId.Should().Be(userId);
        dto.WhatsAppEnabled.Should().BeTrue();
        dto.WhatsAppPhoneNumber.Should().Be("+14155551234");
        dto.PhoneVerified.Should().BeFalse();
        dto.IsFullyVerified.Should().BeFalse();
        dto.IsLocked.Should().BeFalse();

        // Notification preferences
        dto.NotifyEventRegistration.Should().BeTrue();
        dto.NotifyEventReminder.Should().BeFalse();
        dto.NotifyEventCancellation.Should().BeTrue();
        dto.NotifyEventUpdate.Should().BeFalse();
        dto.NotifySignupCommitment.Should().BeTrue();
        dto.NotifyRefund.Should().BeTrue();
        dto.NotifyNewsletter.Should().BeFalse();
        dto.NotifyNewEvent.Should().BeTrue();
        dto.NotifyPayment.Should().BeFalse();

        // Cultural preferences
        dto.PreferredLanguage.Should().Be("si");
        dto.QuietHoursStart.Should().Be(new TimeOnly(22, 0));
        dto.QuietHoursEnd.Should().Be(new TimeOnly(7, 0));
        dto.RespectCulturalTiming.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistentUser_ReturnsNullSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetWhatsAppPreferencesQuery(userId);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWhatsAppPreferences?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_VerifiedUser_ReturnsDtoWithVerifiedFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = UserWhatsAppPreferences.Create(userId);
        preferences.EnableWhatsApp("+14155551234");
        preferences.GenerateVerificationCode();
        var code = preferences.VerificationCode!;
        preferences.VerifyPhone(code);

        var query = new GetWhatsAppPreferencesQuery(userId);

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        dto.PhoneVerified.Should().BeTrue();
        dto.IsFullyVerified.Should().BeTrue();
        dto.PhoneVerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var query = new GetWhatsAppPreferencesQuery(Guid.NewGuid());

        _mockPreferencesRepo
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");
    }
}
