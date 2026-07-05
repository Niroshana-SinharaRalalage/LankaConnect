using LankaConnect.Modules.Communications.Application.WhatsApp.Queries.GetWhatsAppMessageHistory;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Communications.WhatsApp.Queries;

public class GetWhatsAppMessageHistoryQueryHandlerTests
{
    private readonly Mock<IWhatsAppMessageRepository> _mockMessageRepo;
    private readonly Mock<ILogger<GetWhatsAppMessageHistoryQueryHandler>> _mockLogger;
    private readonly GetWhatsAppMessageHistoryQueryHandler _handler;

    public GetWhatsAppMessageHistoryQueryHandlerTests()
    {
        _mockMessageRepo = new Mock<IWhatsAppMessageRepository>();
        _mockLogger = new Mock<ILogger<GetWhatsAppMessageHistoryQueryHandler>>();

        _handler = new GetWhatsAppMessageHistoryQueryHandler(
            _mockMessageRepo.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ByUserId_ReturnsMessagesWithMaskedPhone()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var message = WhatsAppMessageRecord.Create(
            fromPhoneNumber: "+14155550000",
            toPhoneNumber: "+14155551234",
            messageType: WhatsAppMessageType.Template,
            templateName: "event_reminder",
            parameters: new Dictionary<string, string> { { "name", "Test" } },
            language: "en",
            userId: userId);
        message.MarkAsSent("acs-123");

        var messages = new List<WhatsAppMessageRecord> { message };

        var query = new GetWhatsAppMessageHistoryQuery(UserId: userId, EventId: null, Page: 1, PageSize: 20);

        _mockMessageRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dtos = result.Value;
        dtos.Should().HaveCount(1);

        var dto = dtos[0];
        dto.TemplateName.Should().Be("event_reminder");
        dto.Status.Should().Be(WhatsAppMessageStatus.Sent);
        dto.MessageType.Should().Be(WhatsAppMessageType.Template);
        dto.Language.Should().Be("en");
        dto.UserId.Should().Be(userId);

        // Phone number should be masked
        dto.ToPhoneNumber.Should().NotBe("+14155551234");
        dto.ToPhoneNumber.Should().Contain("***");
    }

    [Fact]
    public async Task Handle_ByEventId_ReturnsMessages()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var message = WhatsAppMessageRecord.Create(
            fromPhoneNumber: "+14155550000",
            toPhoneNumber: "+14155551234",
            messageType: WhatsAppMessageType.Template,
            templateName: "event_update",
            parameters: null,
            language: "en",
            eventId: eventId);

        var messages = new List<WhatsAppMessageRecord> { message };

        var query = new GetWhatsAppMessageHistoryQuery(UserId: null, EventId: eventId, Page: 1, PageSize: 20);

        _mockMessageRepo
            .Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task Handle_NoFilter_ReturnsFailure()
    {
        // Arrange — both UserId and EventId are null
        var query = new GetWhatsAppMessageHistoryQuery(UserId: null, EventId: null, Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one filter");
    }

    [Fact]
    public async Task Handle_Pagination_RespectsPageAndPageSize()
    {
        // Arrange — create 5 messages
        var userId = Guid.NewGuid();
        var messages = Enumerable.Range(0, 5).Select(i =>
        {
            var msg = WhatsAppMessageRecord.Create(
                fromPhoneNumber: "+14155550000",
                toPhoneNumber: "+14155551234",
                messageType: WhatsAppMessageType.Template,
                templateName: $"template_{i}",
                parameters: null,
                language: "en",
                userId: userId);
            msg.MarkAsSent($"acs-{i}");
            return msg;
        }).ToList();

        var query = new GetWhatsAppMessageHistoryQuery(UserId: userId, EventId: null, Page: 2, PageSize: 2);

        _mockMessageRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2); // Page 2, PageSize 2 of 5 items
    }

    [Fact]
    public async Task Handle_EmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetWhatsAppMessageHistoryQuery(UserId: userId, EventId: null, Page: 1, PageSize: 20);

        _mockMessageRepo
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WhatsAppMessageRecord>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var query = new GetWhatsAppMessageHistoryQuery(UserId: Guid.NewGuid(), EventId: null, Page: 1, PageSize: 20);

        _mockMessageRepo
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");
    }
}
