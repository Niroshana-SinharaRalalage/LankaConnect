using FluentAssertions;
using LankaConnect.Shared.Email.Observability;
using Microsoft.Extensions.Logging;
using Moq;

namespace LankaConnect.Shared.Tests.Email.Observability;

/// <summary>
/// Phase 6A.86: Tests for IEmailLogger interface (TDD - RED phase)
/// Ensures comprehensive email operation logging with correlation IDs
/// </summary>
public class IEmailLoggerTests
{
    private readonly Mock<ILogger<EmailLogger>> _mockLogger;
    private readonly IEmailLogger _emailLogger;

    public IEmailLoggerTests()
    {
        _mockLogger = new Mock<ILogger<EmailLogger>>();
        _emailLogger = new EmailLogger(_mockLogger.Object);
    }

    [Fact]
    public void LogEmailSendStart_ShouldLogWithCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var templateName = "template-event-reminder";
        var recipientEmail = "test@example.com";

        // Act
        _emailLogger.LogEmailSendStart(correlationId, templateName, recipientEmail);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(correlationId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogEmailSendSuccess_ShouldLogWithDuration()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var templateName = "template-event-reminder";
        var durationMs = 250;

        // Act
        _emailLogger.LogEmailSendSuccess(correlationId, templateName, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(correlationId) && v.ToString()!.Contains($"{durationMs}ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogEmailSendFailure_ShouldLogErrorWithException()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var templateName = "template-event-reminder";
        var exception = new Exception("SMTP connection failed");
        var errorMessage = "Failed to send email";

        // Act
        _emailLogger.LogEmailSendFailure(correlationId, templateName, errorMessage, exception);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(correlationId)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogParameterValidationFailure_ShouldLogWarning()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var templateName = "template-event-reminder";
        var errors = new List<string> { "RecipientEmail is required", "TemplateName is required" };

        // Act
        _emailLogger.LogParameterValidationFailure(correlationId, templateName, errors);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(correlationId) && v.ToString()!.Contains("Errors=2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogTemplateNotFound_ShouldLogError()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var templateName = "template-nonexistent";

        // Act
        _emailLogger.LogTemplateNotFound(correlationId, templateName);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(correlationId) && v.ToString()!.Contains(templateName)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogFeatureFlagCheck_ShouldLogDebug()
    {
        // Arrange
        var handlerName = "EventReminderJob";
        var isEnabled = true;
        var reason = "Override enabled for pilot handler";

        // Act
        _emailLogger.LogFeatureFlagCheck(handlerName, isEnabled, reason);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(handlerName) && v.ToString()!.Contains(reason)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GenerateCorrelationId_ShouldReturnNonEmptyString()
    {
        // Act
        var correlationId = _emailLogger.GenerateCorrelationId();

        // Assert
        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).Should().BeTrue("Should be valid GUID");
    }

    [Fact]
    public void GenerateCorrelationId_ShouldReturnUniqueIds()
    {
        // Act
        var id1 = _emailLogger.GenerateCorrelationId();
        var id2 = _emailLogger.GenerateCorrelationId();

        // Assert
        id1.Should().NotBe(id2);
    }
}

/// <summary>
/// Concrete implementation of IEmailLogger for testing
/// </summary>
public class EmailLogger : IEmailLogger
{
    private readonly ILogger<EmailLogger> _logger;

    public EmailLogger(ILogger<EmailLogger> logger)
    {
        _logger = logger;
    }

    public void LogEmailSendStart(string correlationId, string templateName, string recipientEmail)
    {
        _logger.LogInformation(
            "[{CorrelationId}] Starting email send: Template={TemplateName}, Recipient={RecipientEmail}",
            correlationId, templateName, recipientEmail);
    }

    public void LogEmailSendSuccess(string correlationId, string templateName, int durationMs)
    {
        _logger.LogInformation(
            "[{CorrelationId}] Email sent successfully: Template={TemplateName}, Duration={DurationMs}ms",
            correlationId, templateName, durationMs);
    }

    public void LogEmailSendFailure(string correlationId, string templateName, string errorMessage, Exception? exception = null)
    {
        _logger.LogError(exception,
            "[{CorrelationId}] Email send failed: Template={TemplateName}, Error={ErrorMessage}",
            correlationId, templateName, errorMessage);
    }

    public void LogParameterValidationFailure(string correlationId, string templateName, List<string> errors)
    {
        _logger.LogWarning(
            "[{CorrelationId}] Parameter validation failed: Template={TemplateName}, Errors={ErrorCount} ({Errors})",
            correlationId, templateName, errors.Count, string.Join(", ", errors));
    }

    public void LogTemplateNotFound(string correlationId, string templateName)
    {
        _logger.LogError(
            "[{CorrelationId}] Email template not found: Template={TemplateName}",
            correlationId, templateName);
    }

    public void LogFeatureFlagCheck(string handlerName, bool isEnabled, string reason)
    {
        _logger.LogDebug(
            "Feature flag check: Handler={HandlerName}, Enabled={IsEnabled}, Reason={Reason}",
            handlerName, isEnabled, reason);
    }

    public string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString();
    }
}
