using LankaConnect.Modules.Communications.Application.WhatsApp.Commands.SendTestWhatsApp;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Application.Common.Options;
using LankaConnect.BuildingBlocks.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LankaConnect.Application.Tests.Communications.WhatsApp.Commands;

public class SendTestWhatsAppCommandHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<ILogger<SendTestWhatsAppCommandHandler>> _mockLogger;
    private readonly WhatsAppOptions _options;
    private readonly SendTestWhatsAppCommandHandler _handler;

    public SendTestWhatsAppCommandHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockLogger = new Mock<ILogger<SendTestWhatsAppCommandHandler>>();
        _options = new WhatsAppOptions { Enabled = true };

        _handler = new SendTestWhatsAppCommandHandler(
            _mockWhatsAppService.Object,
            Options.Create(_options),
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_SendsSuccessfully()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var parameters = new Dictionary<string, string> { { "name", "Test User" } };
        var command = new SendTestWhatsAppCommand(adminId, "+14155551234", "event_reminder", parameters);

        var sendResult = WhatsAppSendResult.Sent(Guid.NewGuid(), "acs-msg-123");

        _mockWhatsAppService
            .Setup(x => x.SendTemplateMessageToPhoneAsync(
                "+14155551234", "event_reminder", parameters, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WhatsAppSendResult>.Success(sendResult));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.MessageId.Should().Be("acs-msg-123");
        result.Value.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhatsAppDisabled_ReturnsFailure()
    {
        // Arrange
        var disabledOptions = new WhatsAppOptions { Enabled = false };
        var handler = new SendTestWhatsAppCommandHandler(
            _mockWhatsAppService.Object,
            Options.Create(disabledOptions),
            _mockLogger.Object);

        var command = new SendTestWhatsAppCommand(
            Guid.NewGuid(), "+14155551234", "template", new Dictionary<string, string>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("disabled");
    }

    [Theory]
    [InlineData("", "event_reminder")]
    [InlineData(" ", "event_reminder")]
    public async Task Handle_EmptyPhoneNumber_ReturnsFailure(string phone, string template)
    {
        // Arrange
        var command = new SendTestWhatsAppCommand(
            Guid.NewGuid(), phone, template, new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Phone number");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_EmptyTemplateName_ReturnsFailure(string template)
    {
        // Arrange
        var command = new SendTestWhatsAppCommand(
            Guid.NewGuid(), "+14155551234", template, new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Template name");
    }

    [Fact]
    public async Task Handle_ServiceSendFails_ReturnsSuccessWithError()
    {
        // Arrange
        var command = new SendTestWhatsAppCommand(
            Guid.NewGuid(), "+14155551234", "event_reminder", new Dictionary<string, string>());

        _mockWhatsAppService
            .Setup(x => x.SendTemplateMessageToPhoneAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WhatsAppSendResult>.Failure("ACS send failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — handler wraps failure in a Success(response) with Success=false
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorMessage.Should().Be("ACS send failed");
    }

    [Fact]
    public async Task Handle_MessageWasSkipped_ReturnsSuccessWithSkipReason()
    {
        // Arrange
        var command = new SendTestWhatsAppCommand(
            Guid.NewGuid(), "+14155551234", "event_reminder", new Dictionary<string, string>());

        var skippedResult = WhatsAppSendResult.Skipped("User opted out");

        _mockWhatsAppService
            .Setup(x => x.SendTemplateMessageToPhoneAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WhatsAppSendResult>.Success(skippedResult));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorMessage.Should().Be("User opted out");
    }

    [Fact]
    public async Task Handle_ServiceThrows_PropagatesException()
    {
        // Arrange
        var command = new SendTestWhatsAppCommand(
            Guid.NewGuid(), "+14155551234", "event_reminder", new Dictionary<string, string>());

        _mockWhatsAppService
            .Setup(x => x.SendTemplateMessageToPhoneAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Network error");
    }
}
