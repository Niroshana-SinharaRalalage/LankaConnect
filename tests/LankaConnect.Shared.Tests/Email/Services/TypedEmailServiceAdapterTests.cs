using FluentAssertions;
using LankaConnect.Shared.Email.Configuration;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Observability;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LankaConnect.Shared.Tests.Email.Services;

/// <summary>
/// Phase 6A.87 Day 3: Tests for TypedEmailServiceAdapter (TDD - RED phase)
/// Tests the adapter that bridges typed parameters with existing email service
/// </summary>
public class TypedEmailServiceAdapterTests
{
    private readonly Mock<IEmailServiceBridge> _mockEmailService;
    private readonly Mock<IEmailLogger> _mockLogger;
    private readonly Mock<IEmailMetrics> _mockMetrics;
    private readonly EmailFeatureFlags _featureFlags;
    private readonly TypedEmailServiceAdapter _adapter;

    public TypedEmailServiceAdapterTests()
    {
        _mockEmailService = new Mock<IEmailServiceBridge>();
        _mockLogger = new Mock<IEmailLogger>();
        _mockMetrics = new Mock<IEmailMetrics>();
        _featureFlags = new EmailFeatureFlags();

        // Setup default mock behaviors
        _mockLogger.Setup(x => x.GenerateCorrelationId()).Returns(Guid.NewGuid().ToString());
        _mockEmailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(true);

        _adapter = new TypedEmailServiceAdapter(
            _mockEmailService.Object,
            _featureFlags,
            _mockLogger.Object,
            _mockMetrics.Object
        );
    }

    #region Feature Flag Tests

    [Fact]
    public async Task SendEmailAsync_WhenFeatureFlagDisabled_ShouldUseDictionaryApproach()
    {
        // Arrange
        _featureFlags.UseTypedParameters = false;
        var emailParams = CreateValidEventReminderParams();

        // Act
        await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        _mockEmailService.Verify(
            x => x.SendTemplatedEmailAsync(
                emailParams.TemplateName,
                emailParams.RecipientEmail,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockMetrics.Verify(
            x => x.RecordHandlerUsage("EventReminderJob", false),
            Times.Once,
            "Should record Dictionary usage when feature flag is disabled");
    }

    [Fact]
    public async Task SendEmailAsync_WhenFeatureFlagEnabled_ShouldUseTypedApproach()
    {
        // Arrange
        _featureFlags.UseTypedParameters = true;
        var emailParams = CreateValidEventReminderParams();

        // Act
        await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        _mockEmailService.Verify(
            x => x.SendTemplatedEmailAsync(
                emailParams.TemplateName,
                emailParams.RecipientEmail,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockMetrics.Verify(
            x => x.RecordHandlerUsage("EventReminderJob", true),
            Times.Once,
            "Should record typed usage when feature flag is enabled");
    }

    [Fact]
    public async Task SendEmailAsync_WhenHandlerOverrideEnabled_ShouldUseTypedApproach()
    {
        // Arrange
        _featureFlags.UseTypedParameters = false; // Global OFF
        _featureFlags.HandlerOverrides["EventReminderJob"] = true; // Override ON
        var emailParams = CreateValidEventReminderParams();

        // Act
        await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        _mockMetrics.Verify(
            x => x.RecordHandlerUsage("EventReminderJob", true),
            Times.Once,
            "Should use typed approach when handler override is enabled");
    }

    [Fact]
    public async Task SendEmailAsync_WhenHandlerOverrideDisabled_ShouldUseDictionaryApproach()
    {
        // Arrange
        _featureFlags.UseTypedParameters = true; // Global ON
        _featureFlags.HandlerOverrides["EventReminderJob"] = false; // Override OFF
        var emailParams = CreateValidEventReminderParams();

        // Act
        await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        _mockMetrics.Verify(
            x => x.RecordHandlerUsage("EventReminderJob", false),
            Times.Once,
            "Should use Dictionary approach when handler override is disabled");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task SendEmailAsync_WhenValidationEnabled_ShouldValidateParameters()
    {
        // Arrange
        _featureFlags.UseTypedParameters = true;
        _featureFlags.EnableValidation = true;
        var emailParams = new TestEmailParams
        {
            TemplateName = "", // Invalid - empty
            RecipientEmail = "test@example.com",
            RecipientName = "Test User"
        };

        // Act
        var result = await _adapter.SendEmailAsync(emailParams, "TestHandler");

        // Assert
        result.Success.Should().BeFalse("Validation should fail when TemplateName is empty");
        result.Errors.Should().Contain(e => e.Contains("TemplateName"));

        _mockLogger.Verify(
            x => x.LogParameterValidationFailure(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<List<string>>(errors => errors.Any(e => e.Contains("TemplateName")))),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WhenValidationDisabled_ShouldNotValidateParameters()
    {
        // Arrange
        _featureFlags.UseTypedParameters = true;
        _featureFlags.EnableValidation = false;
        var emailParams = new TestEmailParams
        {
            TemplateName = "", // Invalid but should be ignored
            RecipientEmail = "test@example.com",
            RecipientName = "Test User"
        };

        // Act
        var result = await _adapter.SendEmailAsync(emailParams, "TestHandler");

        // Assert - Should proceed despite invalid params (validation disabled)
        _mockEmailService.Verify(
            x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task SendEmailAsync_ShouldLogStart()
    {
        // Arrange
        var emailParams = CreateValidEventReminderParams();

        // Act
        await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        _mockLogger.Verify(
            x => x.LogEmailSendStart(
                It.IsAny<string>(),
                emailParams.TemplateName,
                emailParams.RecipientEmail),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WhenSuccess_ShouldLogSuccess()
    {
        // Arrange
        _mockEmailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(true);

        var emailParams = CreateValidEventReminderParams();

        // Act
        await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        _mockLogger.Verify(
            x => x.LogEmailSendSuccess(
                It.IsAny<string>(),
                emailParams.TemplateName,
                It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WhenFailure_ShouldLogFailure()
    {
        // Arrange
        _mockEmailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(false);

        var emailParams = CreateValidEventReminderParams();

        // Act
        await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        _mockLogger.Verify(
            x => x.LogEmailSendFailure(
                It.IsAny<string>(),
                emailParams.TemplateName,
                It.IsAny<string>(),
                It.IsAny<Exception>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldLogFeatureFlagCheck()
    {
        // Arrange
        _featureFlags.UseTypedParameters = true;
        var emailParams = CreateValidEventReminderParams();

        // Act
        await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        _mockLogger.Verify(
            x => x.LogFeatureFlagCheck(
                "EventReminderJob",
                true,
                It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region Metrics Tests

    [Fact]
    public async Task SendEmailAsync_WhenSuccess_ShouldRecordMetrics()
    {
        // Arrange
        _mockEmailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(true);

        var emailParams = CreateValidEventReminderParams();

        // Act
        await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        _mockMetrics.Verify(
            x => x.RecordEmailSent(
                emailParams.TemplateName,
                It.IsAny<int>(),
                true),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WhenFailure_ShouldRecordFailureMetrics()
    {
        // Arrange
        _mockEmailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(false);

        var emailParams = CreateValidEventReminderParams();

        // Act
        await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        _mockMetrics.Verify(
            x => x.RecordEmailSent(
                emailParams.TemplateName,
                It.IsAny<int>(),
                false),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WhenValidationFails_ShouldRecordValidationFailure()
    {
        // Arrange
        _featureFlags.UseTypedParameters = true;
        _featureFlags.EnableValidation = true;
        var emailParams = new TestEmailParams
        {
            TemplateName = "",
            RecipientEmail = "test@example.com",
            RecipientName = "Test User"
        };

        // Act
        await _adapter.SendEmailAsync(emailParams, "TestHandler");

        // Assert
        _mockMetrics.Verify(
            x => x.RecordParameterValidationFailure(It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task SendEmailAsync_WhenExceptionThrown_ShouldLogAndReturnFailure()
    {
        // Arrange
        var exception = new Exception("SMTP connection failed");
        _mockEmailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()
        )).ThrowsAsync(exception);

        var emailParams = CreateValidEventReminderParams();

        // Act
        var result = await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("SMTP connection failed"));

        _mockLogger.Verify(
            x => x.LogEmailSendFailure(
                It.IsAny<string>(),
                emailParams.TemplateName,
                It.IsAny<string>(),
                exception),
            Times.Once);
    }

    #endregion

    #region ToDictionary Tests

    [Fact]
    public async Task SendEmailAsync_ShouldConvertParamsToDictionary()
    {
        // Arrange
        var emailParams = CreateValidEventReminderParams();
        Dictionary<string, object>? capturedParams = null;

        _mockEmailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()
        )).Callback<string, string, Dictionary<string, object>, CancellationToken>((_, _, p, _) => capturedParams = p)
          .ReturnsAsync(true);

        // Act
        await _adapter.SendEmailAsync(emailParams, "EventReminderJob");

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams.Should().ContainKey("RecipientEmail");
        capturedParams.Should().ContainKey("RecipientName");
    }

    #endregion

    #region Helper Methods

    private TestEmailParams CreateValidEventReminderParams()
    {
        return new TestEmailParams
        {
            TemplateName = "template-event-reminder",
            RecipientEmail = "test@example.com",
            RecipientName = "Test User"
        };
    }

    #endregion
}

/// <summary>
/// Test implementation of IEmailParameters for testing
/// </summary>
public class TestEmailParams : IEmailParameters
{
    public string TemplateName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "RecipientEmail", RecipientEmail },
            { "RecipientName", RecipientName }
        };
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(TemplateName))
            errors.Add("TemplateName is required");

        if (string.IsNullOrWhiteSpace(RecipientEmail))
            errors.Add("RecipientEmail is required");

        if (string.IsNullOrWhiteSpace(RecipientName))
            errors.Add("RecipientName is required");

        return errors.Count == 0;
    }
}
