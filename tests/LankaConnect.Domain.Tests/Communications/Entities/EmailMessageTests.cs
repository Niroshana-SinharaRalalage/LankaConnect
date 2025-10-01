using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Tests.TestHelpers;
using UserEmail = LankaConnect.Domain.Users.ValueObjects.Email;
using FluentAssertions;
using Xunit;

namespace LankaConnect.Domain.Tests.Communications.Entities;

public class EmailMessageTests
{
    private readonly UserEmail _validFromEmail = UserEmail.Create("sender@lankaconnect.com").Value;
    private readonly UserEmail _validToEmail = UserEmail.Create("test@example.com").Value;
    private readonly EmailSubject _validSubject = EmailSubject.Create("Test Subject").Value;

    [Fact]
    public void Create_WithValidData_ShouldCreateEmailMessage()
    {
        // Arrange
        var textContent = "Test email body";
        var type = EmailType.Transactional;
        var maxRetries = 3;

        // Act
        var result = EmailMessage.Create(_validFromEmail, _validSubject, textContent, null, type, maxRetries);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var email = result.Value;
        email.FromEmail.Should().Be(_validFromEmail);
        email.Subject.Should().Be(_validSubject);
        email.TextContent.Should().Be(textContent);
        email.HtmlContent.Should().BeNull();
        email.Type.Should().Be(type);
        email.Status.Should().Be(EmailStatus.Pending);
        email.RetryCount.Should().Be(0);
        email.MaxRetries.Should().Be(maxRetries);
        email.Id.Should().NotBeEmpty();
        email.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithHtmlBody_ShouldSetHtmlBody()
    {
        // Arrange
        var htmlBody = "<h1>Test HTML Body</h1>";

        // Act
        var email = new EmailMessage(_validToEmail, "Subject", "Body", htmlBody: htmlBody);

        // Assert
        email.HtmlBody.Should().Be(htmlBody);
    }

    [Fact]
    public void Create_WithTemplateData_ShouldSetTemplateProperties()
    {
        // Arrange
        var templateName = "welcome-email";
        var templateData = new Dictionary<string, object>
        {
            ["userName"] = "John Doe",
            ["confirmationUrl"] = "https://example.com/confirm"
        };

        // Act
        var email = new EmailMessage(_validToEmail, "Subject", "Body", 
            templateName: templateName, templateData: templateData);

        // Assert
        email.TemplateName.Should().Be(templateName);
        email.TemplateData.Should().BeEquivalentTo(templateData);
    }

    [Fact]
    public void Create_WithCustomPriority_ShouldSetPriority()
    {
        // Arrange
        var highPriority = 1;

        // Act
        var email = new EmailMessage(_validToEmail, "Urgent", "Urgent message", priority: highPriority);

        // Assert
        email.Priority.Should().Be(highPriority);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_WithNullOrEmptySubject_ShouldThrowArgumentNullException(string subject)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new EmailMessage(_validToEmail, subject, "Body"));
        
        exception.ParamName.Should().Be("subject");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_WithNullOrEmptyBody_ShouldThrowArgumentNullException(string body)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new EmailMessage(_validToEmail, "Subject", body));
        
        exception.ParamName.Should().Be("body");
    }

    [Fact]
    public void MarkAsSent_ShouldUpdateStatusAndSentTime()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        var messageId = "msg-123";

        // Act
        email.MarkAsSent(messageId);

        // Assert
        email.DeliveryStatus.Should().Be(EmailDeliveryStatus.Sent);
        email.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        email.MessageId.Should().Be(messageId);
        email.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsDelivered_ShouldUpdateStatusAndDeliveredTime()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        email.MarkAsSent();

        // Act
        email.MarkAsDelivered();

        // Assert
        email.DeliveryStatus.Should().Be(EmailDeliveryStatus.Delivered);
        email.DeliveredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        email.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatusAndErrorInfo()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        var errorMessage = "SMTP server unavailable";
        var nextRetryAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        email.MarkAsFailed(errorMessage, nextRetryAt);

        // Assert
        email.DeliveryStatus.Should().Be(EmailDeliveryStatus.Failed);
        email.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        email.ErrorMessage.Should().Be(errorMessage);
        email.AttemptCount.Should().Be(1);
        email.NextRetryAt.Should().Be(nextRetryAt);
        email.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsFailed_CalledMultipleTimes_ShouldIncrementAttemptCount()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");

        // Act
        email.MarkAsFailed("Error 1");
        email.MarkAsFailed("Error 2");
        email.MarkAsFailed("Error 3");

        // Assert
        email.AttemptCount.Should().Be(3);
        email.ErrorMessage.Should().Be("Error 3"); // Should have the latest error
    }

    [Fact]
    public void MarkAsOpened_FirstTime_ShouldSetOpenedFlag()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");

        // Act
        email.MarkAsOpened();

        // Assert
        email.IsOpened.Should().BeTrue();
        email.OpenedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        email.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsOpened_CalledMultipleTimes_ShouldNotUpdateOpenedTime()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        email.MarkAsOpened();
        var firstOpenedAt = email.OpenedAt;

        // Act
        Thread.Sleep(100); // Small delay
        email.MarkAsOpened();

        // Assert
        email.OpenedAt.Should().Be(firstOpenedAt);
    }

    [Fact]
    public void MarkAsClicked_FirstTime_ShouldSetClickedFlag()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");

        // Act
        email.MarkAsClicked();

        // Assert
        email.IsClicked.Should().BeTrue();
        email.ClickedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        email.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsClicked_CalledMultipleTimes_ShouldNotUpdateClickedTime()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        email.MarkAsClicked();
        var firstClickedAt = email.ClickedAt;

        // Act
        Thread.Sleep(100); // Small delay
        email.MarkAsClicked();

        // Assert
        email.ClickedAt.Should().Be(firstClickedAt);
    }

    [Fact]
    public void RetryEmail_WithFailedStatus_ShouldResetTopending()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        var nextRetryAt = DateTime.UtcNow.AddMinutes(-1); // In the past, ready for retry
        email.MarkAsFailed("Error", nextRetryAt);

        // Act
        email.RetryEmail();

        // Assert
        email.DeliveryStatus.Should().Be(EmailDeliveryStatus.Pending);
        email.NextRetryAt.Should().BeNull();
        email.ErrorMessage.Should().BeNull();
        email.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RetryEmail_WithPendingStatus_ShouldNotChange()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        var originalUpdatedAt = email.UpdatedAt;

        // Act
        Thread.Sleep(100);
        email.RetryEmail();

        // Assert
        email.DeliveryStatus.Should().Be(EmailDeliveryStatus.Pending);
        email.UpdatedAt.Should().Be(originalUpdatedAt); // Should not be updated
    }

    [Fact]
    public void RetryEmail_WithFutureRetryTime_ShouldNotRetry()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        var futureRetryAt = DateTime.UtcNow.AddMinutes(5);
        email.MarkAsFailed("Error", futureRetryAt);

        // Act
        email.RetryEmail();

        // Assert
        email.DeliveryStatus.Should().Be(EmailDeliveryStatus.Failed);
        email.NextRetryAt.Should().Be(futureRetryAt);
    }

    [Theory]
    [InlineData(0, 3, true)]  // No attempts, can retry
    [InlineData(2, 3, true)]  // Under max attempts, can retry
    [InlineData(3, 3, false)] // At max attempts, cannot retry
    [InlineData(5, 3, false)] // Over max attempts, cannot retry
    public void CanRetry_WithDifferentAttemptCounts_ShouldReturnExpectedResult(
        int attemptCount, int maxRetries, bool expectedCanRetry)
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        var nextRetryAt = DateTime.UtcNow.AddMinutes(-1); // In the past, ready for retry
        
        // Simulate attempt count by calling MarkAsFailed
        for (int i = 0; i < attemptCount; i++)
        {
            email.MarkAsFailed("Error", nextRetryAt);
        }

        // Act
        var canRetry = email.CanRetry(maxRetries);

        // Assert
        canRetry.Should().Be(expectedCanRetry);
    }

    [Fact]
    public void CanRetry_WithSentStatus_ShouldReturnFalse()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        email.MarkAsSent();

        // Act
        var canRetry = email.CanRetry();

        // Assert
        canRetry.Should().BeFalse();
    }

    [Fact]
    public void CanRetry_WithNullNextRetryTime_ShouldReturnFalse()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        email.MarkAsFailed("Error"); // No next retry time

        // Act
        var canRetry = email.CanRetry();

        // Assert
        canRetry.Should().BeFalse();
    }

    [Fact]
    public void CanRetry_WithFutureNextRetryTime_ShouldReturnFalse()
    {
        // Arrange
        var email = new EmailMessage(_validToEmail, "Subject", "Body");
        var futureRetryAt = DateTime.UtcNow.AddMinutes(5);
        email.MarkAsFailed("Error", futureRetryAt);

        // Act
        var canRetry = email.CanRetry();

        // Assert
        canRetry.Should().BeFalse();
    }

    [Fact]
    public void EmailMessage_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var email = new EmailMessage(_validToEmail, "Subject", "Body");

        // Assert
        email.Should().BeAssignableTo<LankaConnect.Domain.Common.BaseEntity>();
        email.Id.Should().NotBeEmpty();
        email.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}