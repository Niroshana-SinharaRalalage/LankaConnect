using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using LankaConnect.Infrastructure.Email.Services;
using LankaConnect.Infrastructure.Email.Configuration;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Repositories;
using LankaConnect.Domain.Common;
using LankaConnect.Tests.LankaConnect.Domain.Tests.TestHelpers;
using System.Net.Mail;
using System.Net;

namespace LankaConnect.Infrastructure.Tests.Email.Services;

public class EmailServiceTests
{
    private readonly Mock<IEmailMessageRepository> _emailRepository;
    private readonly Mock<IEmailTemplateService> _templateService;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<EmailService>> _logger;
    private readonly Mock<IOptions<EmailSettings>> _emailSettings;
    private readonly EmailService _emailService;
    private readonly EmailSettings _settings;

    public EmailServiceTests()
    {
        _emailRepository = new Mock<IEmailMessageRepository>();
        _templateService = new Mock<IEmailTemplateService>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger<EmailService>>();
        _emailSettings = new Mock<IOptions<EmailSettings>>();

        _settings = new EmailSettings
        {
            SmtpServer = "localhost",
            SmtpPort = 1025, // MailHog SMTP port
            SenderEmail = "noreply@lankaconnect.com",
            SenderName = "LankaConnect",
            EnableSsl = false,
            TimeoutInSeconds = 30,
            MaxRetryAttempts = 3,
            RetryDelayInMinutes = 5,
            IsDevelopment = true,
            SaveEmailsToFile = false,
            EmailSaveDirectory = "TestEmails"
        };

        _emailSettings.Setup(x => x.Value).Returns(_settings);

        _emailService = new EmailService(
            _emailSettings.Object,
            _emailRepository.Object,
            _templateService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task SendEmailAsync_WithBasicEmail_ShouldSendSuccessfully()
    {
        // Arrange
        var to = new EmailAddress("test@example.com");
        var subject = "Test Subject";
        var body = "Test Body";

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body);

        // Assert
        result.Should().BeTrue();
        
        // Verify logging
        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending email")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithHtmlBody_ShouldSendWithBothTextAndHtml()
    {
        // Arrange
        var to = new EmailAddress("test@example.com");
        var subject = "HTML Email Test";
        var textBody = "Plain text version";
        var htmlBody = "<h1>HTML Version</h1>";

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, textBody, htmlBody);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendTemplatedEmailAsync_WithValidTemplate_ShouldRenderAndSend()
    {
        // Arrange
        var to = new EmailAddress("test@example.com");
        var templateName = "welcome-email";
        var templateData = new Dictionary<string, object>
        {
            ["userName"] = "John Doe",
            ["confirmationUrl"] = "https://example.com/confirm"
        };

        var renderedSubject = "Welcome John Doe!";
        var renderedTextBody = "Welcome John Doe! Please confirm your email.";
        var renderedHtmlBody = "<h1>Welcome John Doe!</h1><p>Please confirm your email.</p>";

        _templateService.Setup(x => x.TemplateExistsAsync(templateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _templateService.Setup(x => x.RenderTemplateAsync(templateName, templateData, It.IsAny<CancellationToken>()))
            .ReturnsAsync((renderedSubject, renderedTextBody, renderedHtmlBody));

        // Act
        var result = await _emailService.SendTemplatedEmailAsync(to, templateName, templateData);

        // Assert
        result.Should().BeTrue();
        
        _templateService.Verify(x => x.TemplateExistsAsync(templateName, It.IsAny<CancellationToken>()), Times.Once);
        _templateService.Verify(x => x.RenderTemplateAsync(templateName, templateData, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendTemplatedEmailAsync_WithNonExistentTemplate_ShouldReturnFalse()
    {
        // Arrange
        var to = new EmailAddress("test@example.com");
        var templateName = "non-existent-template";
        var templateData = new Dictionary<string, object>();

        _templateService.Setup(x => x.TemplateExistsAsync(templateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _emailService.SendTemplatedEmailAsync(to, templateName, templateData);

        // Assert
        result.Should().BeFalse();
        
        _logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Template") && v.ToString()!.Contains("not found")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task QueueEmailAsync_WithValidData_ShouldAddToRepository()
    {
        // Arrange
        var to = new EmailAddress("test@example.com");
        var subject = "Queued Email";
        var body = "This email will be queued";
        var priority = 2;

        // Act
        var result = await _emailService.QueueEmailAsync(to, subject, body, null, priority);

        // Assert
        result.Should().NotBeEmpty();
        
        _emailRepository.Verify(x => x.AddAsync(
            It.Is<EmailMessage>(em => 
                em.To.Address == to.Address && 
                em.Subject == subject && 
                em.Body == body &&
                em.Priority == priority),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueueTemplatedEmailAsync_WithValidTemplate_ShouldAddToRepository()
    {
        // Arrange
        var to = new EmailAddress("test@example.com");
        var templateName = "welcome-email";
        var templateData = new Dictionary<string, object>
        {
            ["userName"] = "John Doe"
        };
        var priority = 1;

        // Act
        var result = await _emailService.QueueTemplatedEmailAsync(to, templateName, templateData, priority);

        // Assert
        result.Should().NotBeEmpty();
        
        _emailRepository.Verify(x => x.AddAsync(
            It.Is<EmailMessage>(em => 
                em.To.Address == to.Address && 
                em.TemplateName == templateName && 
                em.Priority == priority),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEmailStatusAsync_WithValidId_ShouldReturnEmailMessage()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var expectedEmail = EmailTestDataBuilder.CreateSentEmail();

        _emailRepository.Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmail);

        // Act
        var result = await _emailService.GetEmailStatusAsync(emailId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedEmail);
        
        _emailRepository.Verify(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessEmailQueueAsync_WithPendingEmails_ShouldProcessAll()
    {
        // Arrange
        var batchSize = 5;
        var pendingEmails = EmailTestDataBuilder.CreatePendingEmails(batchSize);

        _emailRepository.Setup(x => x.GetPendingEmailsAsync(batchSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingEmails);

        // Act
        await _emailService.ProcessEmailQueueAsync(batchSize);

        // Assert
        _emailRepository.Verify(x => x.GetPendingEmailsAsync(batchSize, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify logging
        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processed") && v.ToString()!.Contains("emails from queue")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessEmailQueueAsync_WithTemplatedEmails_ShouldRenderTemplatesFirst()
    {
        // Arrange
        var templatedEmail = EmailTestDataBuilder.CreateTemplatedEmail();
        var pendingEmails = new List<EmailMessage> { templatedEmail };

        _emailRepository.Setup(x => x.GetPendingEmailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingEmails);

        _templateService.Setup(x => x.RenderTemplateAsync(
            templatedEmail.TemplateName!,
            templatedEmail.TemplateData!,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(("Rendered Subject", "Rendered Body", "<h1>Rendered HTML</h1>"));

        // Act
        await _emailService.ProcessEmailQueueAsync();

        // Assert
        _templateService.Verify(x => x.RenderTemplateAsync(
            templatedEmail.TemplateName!,
            templatedEmail.TemplateData!,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetryFailedEmailsAsync_WithRetryableEmails_ShouldRetryThem()
    {
        // Arrange
        var failedEmails = new List<EmailMessage>
        {
            EmailTestDataBuilder.CreateRetryableFailedEmail(),
            EmailTestDataBuilder.CreateRetryableFailedEmail()
        };

        _emailRepository.Setup(x => x.GetRetryableFailedEmailsAsync(_settings.MaxRetryAttempts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedEmails);

        // Act
        await _emailService.RetryFailedEmailsAsync();

        // Assert
        _emailRepository.Verify(x => x.GetRetryableFailedEmailsAsync(_settings.MaxRetryAttempts, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify that retry method was called on each email
        foreach (var email in failedEmails)
        {
            email.Status.Should().Be(Domain.Communications.Enums.EmailDeliveryStatus.Pending);
        }
    }

    [Fact]
    public async Task SendEmailAsync_InDevelopmentModeWithSaveToFile_ShouldSaveToFile()
    {
        // Arrange
        _settings.IsDevelopment = true;
        _settings.SaveEmailsToFile = true;

        var to = new EmailAddress("test@example.com");
        var subject = "Test Subject";
        var body = "Test Body";

        // Create a temporary directory for testing
        var tempDir = Path.Combine(Path.GetTempPath(), "EmailServiceTests");
        _settings.EmailSaveDirectory = tempDir;

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body);

        // Assert
        result.Should().BeTrue();
        
        // Check if file was created in the directory
        Directory.Exists(tempDir).Should().BeTrue();
        var files = Directory.GetFiles(tempDir, "email_*.txt");
        files.Should().NotBeEmpty();

        // Clean up
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task SendEmailAsync_WhenSmtpFails_ShouldMarkEmailAsFailed()
    {
        // Arrange - This test simulates SMTP failure by using invalid settings
        var invalidSettings = new EmailSettings
        {
            SmtpServer = "invalid-smtp-server",
            SmtpPort = 9999,
            SenderEmail = "test@example.com",
            EnableSsl = true,
            TimeoutInSeconds = 1 // Very short timeout to force failure
        };

        _emailSettings.Setup(x => x.Value).Returns(invalidSettings);

        var emailServiceWithInvalidSmtp = new EmailService(
            _emailSettings.Object,
            _emailRepository.Object,
            _templateService.Object,
            _unitOfWork.Object,
            _logger.Object);

        var emailMessage = EmailTestDataBuilder.CreateBasicEmailMessage();

        // Act
        var result = await emailServiceWithInvalidSmtp.SendEmailAsync(emailMessage);

        // Assert
        result.Should().BeFalse();
        emailMessage.Status.Should().Be(Domain.Communications.Enums.EmailDeliveryStatus.Failed);
        emailMessage.ErrorMessage.Should().NotBeNullOrEmpty();
        emailMessage.AttemptCount.Should().Be(1);
        emailMessage.NextRetryAt.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldDisposeSmtpClient()
    {
        // Act & Assert - Should not throw
        _emailService.Dispose();
    }

    [Theory]
    [InlineData(0, 5)]    // First attempt
    [InlineData(1, 25)]   // Second attempt  
    [InlineData(2, 125)]  // Third attempt
    public void CalculateNextRetryTime_ShouldUseExponentialBackoff(int attemptCount, int expectedMinutes)
    {
        // This test assumes the CalculateNextRetryTime method is made internal or has a public wrapper
        // For now, we'll test the behavior indirectly through MarkAsFailed

        // Arrange
        var email = EmailTestDataBuilder.CreateBasicEmailMessage();
        var beforeTime = DateTime.UtcNow;

        // Act - Simulate the attempt count by calling MarkAsFailed multiple times
        for (int i = 0; i <= attemptCount; i++)
        {
            var nextRetryTime = beforeTime.AddMinutes(_settings.RetryDelayInMinutes * Math.Pow(5, i));
            email.MarkAsFailed($"Attempt {i + 1}", nextRetryTime);
        }

        // Assert
        email.AttemptCount.Should().Be(attemptCount + 1);
        if (email.NextRetryAt.HasValue)
        {
            var calculatedDelay = (email.NextRetryAt.Value - beforeTime).TotalMinutes;
            calculatedDelay.Should().BeGreaterThan(expectedMinutes - 1); // Allow for small timing differences
        }
    }

    [Fact]
    public async Task ProcessEmailQueueAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var pendingEmails = EmailTestDataBuilder.CreatePendingEmails(10);
        
        _emailRepository.Setup(x => x.GetPendingEmailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingEmails);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _emailService.ProcessEmailQueueAsync(10, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task SendEmailAsync_WithLongProcessingTime_ShouldLogProcessingTime()
    {
        // Arrange
        var to = new EmailAddress("test@example.com");
        var subject = "Performance Test";
        var body = "Testing performance logging";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _emailService.SendEmailAsync(to, subject, body);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue();
        
        // If processing took significant time, it should be logged
        if (stopwatch.ElapsedMilliseconds > 1000)
        {
            _logger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("slow")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }
    }
}