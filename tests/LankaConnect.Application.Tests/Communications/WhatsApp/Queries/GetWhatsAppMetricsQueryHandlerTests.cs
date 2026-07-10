using LankaConnect.Modules.Communications.Application.WhatsApp.Queries.GetWhatsAppMetrics;
using LankaConnect.Modules.Communications.Domain;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Communications.WhatsApp.Queries;

public class GetWhatsAppMetricsQueryHandlerTests
{
    private readonly Mock<IWhatsAppMessageRepository> _mockMessageRepo;
    private readonly Mock<IUserWhatsAppPreferencesRepository> _mockPreferencesRepo;
    private readonly Mock<ILogger<GetWhatsAppMetricsQueryHandler>> _mockLogger;
    private readonly GetWhatsAppMetricsQueryHandler _handler;

    public GetWhatsAppMetricsQueryHandlerTests()
    {
        _mockMessageRepo = new Mock<IWhatsAppMessageRepository>();
        _mockPreferencesRepo = new Mock<IUserWhatsAppPreferencesRepository>();
        _mockLogger = new Mock<ILogger<GetWhatsAppMetricsQueryHandler>>();

        _mockPreferencesRepo
            .Setup(x => x.GetUsersEnabledButUnverifiedCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _handler = new GetWhatsAppMetricsQueryHandler(
            _mockMessageRepo.Object,
            _mockPreferencesRepo.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidDateRange_ReturnsMetrics()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);
        var query = new GetWhatsAppMetricsQuery(from, to);

        var metrics = new WhatsAppMessageMetrics
        {
            TotalSent = 100,
            TotalDelivered = 95,
            TotalRead = 60,
            TotalFailed = 5,
            ByTemplate = new Dictionary<string, int>
            {
                { "event_reminder", 50 },
                { "registration_confirm", 50 }
            }
        };

        _mockMessageRepo
            .Setup(x => x.GetMetricsAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.TotalSent.Should().Be(100);
        dto.TotalDelivered.Should().Be(95);
        dto.TotalRead.Should().Be(60);
        dto.TotalFailed.Should().Be(5);
        dto.DeliveryRate.Should().Be(95.0);
        dto.ReadRate.Should().BeApproximately(63.16, 0.01);
        dto.ByTemplate.Should().HaveCount(2);
        dto.From.Should().Be(from);
        dto.To.Should().Be(to);
    }

    [Fact]
    public async Task Handle_InvalidDateRange_ReturnsFailure()
    {
        // Arrange — From is after To
        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetWhatsAppMetricsQuery(from, to);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("before");
        _mockMessageRepo.Verify(
            x => x.GetMetricsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SameDates_ReturnsFailure()
    {
        // Arrange — From == To
        var date = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetWhatsAppMetricsQuery(date, date);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("before");
    }

    [Fact]
    public async Task Handle_EmptyResults_ReturnsZeroMetrics()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);
        var query = new GetWhatsAppMetricsQuery(from, to);

        var metrics = new WhatsAppMessageMetrics
        {
            TotalSent = 0,
            TotalDelivered = 0,
            TotalRead = 0,
            TotalFailed = 0,
            ByTemplate = new Dictionary<string, int>()
        };

        _mockMessageRepo
            .Setup(x => x.GetMetricsAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSent.Should().Be(0);
        result.Value.DeliveryRate.Should().Be(0);
        result.Value.ReadRate.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Includes_UsersEnabledButUnverified_From_Preferences_Repository()
    {
        // Arrange
        var from = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc);
        var query = new GetWhatsAppMetricsQuery(from, to);

        _mockMessageRepo
            .Setup(x => x.GetMetricsAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WhatsAppMessageMetrics
            {
                TotalSent = 10,
                TotalDelivered = 10,
                TotalRead = 5,
                TotalFailed = 0,
                ByTemplate = new Dictionary<string, int>()
            });

        _mockPreferencesRepo
            .Setup(x => x.GetUsersEnabledButUnverifiedCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UsersEnabledButUnverified.Should().Be(7);
        _mockPreferencesRepo.Verify(
            x => x.GetUsersEnabledButUnverifiedCountAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);
        var query = new GetWhatsAppMetricsQuery(from, to);

        _mockMessageRepo
            .Setup(x => x.GetMetricsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");
    }
}
