using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Tests.TestHelpers;
using UserEmail = LankaConnect.Domain.Users.ValueObjects.Email;

namespace LankaConnect.Domain.Tests.Communications.Entities;

public class EmailMessageComprehensiveTests
{
    private readonly UserEmail _validFromEmail;
    private readonly UserEmail _validToEmail;
    private readonly EmailSubject _validSubject;

    public EmailMessageComprehensiveTests()
    {
        _validFromEmail = TestDataFactory.ValidEmail("sender@lankaconnect.com");
        _validToEmail = TestDataFactory.ValidEmail("recipient@example.com");
        _validSubject = EmailSubject.Create("Test Email Subject").Value;
    }

    #region Creation Tests

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var textContent = "Test email content";
        var htmlContent = "<p>Test email content</p>";
        var type = EmailType.Transactional;
        var maxRetries = 5;

        // Act
        var result = EmailMessage.Create(_validFromEmail, _validSubject, textContent, htmlContent, type, maxRetries);

        // Assert
        Assert.True(result.IsSuccess);
        var email = result.Value;
        Assert.Equal(_validFromEmail, email.FromEmail);
        Assert.Equal(_validSubject, email.Subject);
        Assert.Equal(textContent, email.TextContent);
        Assert.Equal(htmlContent, email.HtmlContent);
        Assert.Equal(type, email.Type);
        Assert.Equal(EmailStatus.Pending, email.Status);
        Assert.Equal(0, email.RetryCount);
        Assert.Equal(maxRetries, email.MaxRetries);
        Assert.NotEqual(Guid.Empty, email.Id);
        Assert.True((DateTime.UtcNow - email.CreatedAt).TotalSeconds < 1);
    }

    [Fact]
    public void Create_WithDefaultParameters_ShouldUseDefaultValues()
    {
        // Arrange
        var textContent = "Test email content";

        // Act
        var result = EmailMessage.Create(_validFromEmail, _validSubject, textContent);

        // Assert
        Assert.True(result.IsSuccess);
        var email = result.Value;
        Assert.Null(email.HtmlContent);
        Assert.Equal(EmailType.Transactional, email.Type);
        Assert.Equal(3, email.MaxRetries);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceTextContent_ShouldReturnFailure(string textContent)
    {
        // Act
        var result = EmailMessage.Create(_validFromEmail, _validSubject, textContent);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Email text content is required", result.Errors);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void Create_WithInvalidMaxRetries_ShouldReturnFailure(int maxRetries)
    {
        // Act
        var result = EmailMessage.Create(_validFromEmail, _validSubject, "Test content", null, EmailType.Transactional, maxRetries);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Max retries must be between 0 and 10", result.Errors);
    }

    #endregion

    #region Recipient Management Tests

    [Fact]
    public void AddRecipient_WithValidEmail_ShouldAddSuccessfully()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        var recipient = UserEmail.Create("recipient@example.com").Value;

        // Act
        var result = email.AddRecipient(recipient);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(recipient.Value, email.ToEmails);
    }

    [Fact]
    public void AddRecipient_WithDuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        var recipient = UserEmail.Create("recipient@example.com").Value;
        email.AddRecipient(recipient);

        // Act
        var result = email.AddRecipient(recipient);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Email already added as recipient", result.Errors);
    }

    [Fact]
    public void AddCcRecipient_WithValidEmail_ShouldAddSuccessfully()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        var ccRecipient = TestDataFactory.ValidEmail("cc@example.com");

        // Act
        var result = email.AddCcRecipient(ccRecipient);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(ccRecipient.Value, email.CcEmails);
    }

    [Fact]
    public void AddCcRecipient_WithDuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        var ccRecipient = TestDataFactory.ValidEmail("cc@example.com");
        email.AddCcRecipient(ccRecipient);

        // Act
        var result = email.AddCcRecipient(ccRecipient);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Email already added as CC recipient", result.Errors);
    }

    [Fact]
    public void AddBccRecipient_WithValidEmail_ShouldAddSuccessfully()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        var bccRecipient = TestDataFactory.ValidEmail("bcc@example.com");

        // Act
        var result = email.AddBccRecipient(bccRecipient);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(bccRecipient.Value, email.BccEmails);
    }

    [Fact]
    public void AddBccRecipient_WithDuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        var bccRecipient = TestDataFactory.ValidEmail("bcc@example.com");
        email.AddBccRecipient(bccRecipient);

        // Act
        var result = email.AddBccRecipient(bccRecipient);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Email already added as BCC recipient", result.Errors);
    }

    #endregion

    #region State Machine Tests

    [Fact]
    public void MarkAsQueued_WhenPending_ShouldSucceed()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        var originalUpdatedAt = email.UpdatedAt;

        // Act
        var result = email.MarkAsQueued();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(EmailStatus.Queued, email.Status);
        // UpdatedAt should be set after marking as queued
        Assert.NotNull(email.UpdatedAt);
        // If there was no previous UpdatedAt, just verify it's recent
        if (originalUpdatedAt.HasValue)
        {
            Assert.True(email.UpdatedAt > originalUpdatedAt);
        }
        else
        {
            Assert.True((DateTime.UtcNow - email.UpdatedAt.Value).TotalSeconds < 1);
        }
    }

    [Fact]
    public void MarkAsQueued_WhenNotPending_ShouldReturnFailure()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        email.MarkAsQueued();
        email.MarkAsSending();

        // Act
        var result = email.MarkAsQueued();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Cannot mark email as queued when status is Sending", result.Errors);
    }

    [Fact]
    public void MarkAsSending_WhenQueued_ShouldSucceed()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        email.MarkAsQueued();

        // Act
        var result = email.MarkAsSending();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(EmailStatus.Sending, email.Status);
    }

    [Fact]
    public void MarkAsSending_WhenFailed_ShouldSucceed()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        email.MarkAsQueued();
        email.MarkAsSending();
        email.MarkAsFailed("Test error", DateTime.UtcNow);

        // Act
        var result = email.MarkAsSending();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(EmailStatus.Sending, email.Status);
    }

    [Fact]
    public void MarkAsSending_WhenInvalidStatus_ShouldReturnFailure()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        
        // Act
        var result = email.MarkAsSending();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Cannot mark email as sending when status is Pending", result.Errors);
    }

    [Fact]
    public void MarkAsSent_WhenSending_ShouldSucceed()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        email.MarkAsQueued();
        email.MarkAsSending();

        // Act
        var result = email.MarkAsSentResult();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(EmailStatus.Sent, email.Status);
        Assert.NotNull(email.SentAt);
        Assert.True((DateTime.UtcNow - email.SentAt.Value).TotalSeconds < 1);
    }

    [Fact]
    public void MarkAsSent_WhenNotSending_ShouldReturnFailure()
    {
        // Arrange
        var email = CreateValidEmailMessage();

        // Act
        var result = email.MarkAsSentResult();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Cannot mark email as sent when status is Pending", result.Errors);
    }

    [Fact]
    public void MarkAsDelivered_WhenSent_ShouldSucceed()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        email.MarkAsQueued();
        email.MarkAsSending();
        email.MarkAsSent();

        // Act
        email.MarkAsDelivered();

        // Assert
        // Status should be updated
        Assert.Equal(EmailStatus.Delivered, email.Status);
        Assert.NotNull(email.DeliveredAt);
        Assert.True((DateTime.UtcNow - email.DeliveredAt.Value).TotalSeconds < 1);
    }

    [Fact]
    public void MarkAsDelivered_WhenNotSent_ShouldReturnFailure()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        email.MarkAsQueued();

        // Act & Assert
        // This should not change the status from Queued
        email.MarkAsDelivered();
        
        // Verify status remains Queued
        Assert.Equal(EmailStatus.Queued, email.Status);
    }

    [Fact]
    public void MarkAsFailed_WithValidErrorMessage_ShouldSucceed()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        var errorMessage = "SMTP server unreachable";

        // Act
        var result = email.MarkAsFailed(errorMessage, DateTime.UtcNow);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(EmailStatus.Failed, email.Status);
        Assert.Equal(errorMessage, email.ErrorMessage);
        Assert.Equal(1, email.RetryCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkAsFailed_WithInvalidErrorMessage_ShouldReturnFailure(string errorMessage)
    {
        // Arrange
        var email = CreateValidEmailMessage();

        // Act
        var result = email.MarkAsFailed(errorMessage, DateTime.UtcNow);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Error message is required when marking email as failed", result.Errors);
    }

    [Fact]
    public void MarkAsFailed_CalledMultipleTimes_ShouldIncrementRetryCount()
    {
        // Arrange
        var email = CreateValidEmailMessage();

        // Act
        email.MarkAsFailed("Error 1", DateTime.UtcNow);
        email.MarkAsFailed("Error 2", DateTime.UtcNow);
        email.MarkAsFailed("Error 3", DateTime.UtcNow);

        // Assert
        Assert.Equal(3, email.RetryCount);
        Assert.Equal("Error 3", email.ErrorMessage);
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public void CanRetry_WhenFailedWithinRetryLimit_ShouldReturnTrue()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        email.MarkAsFailed("Test error", DateTime.UtcNow);

        // Act
        var canRetry = email.CanRetry();

        // Assert
        Assert.True(canRetry);
    }

    [Fact]
    public void CanRetry_WhenExceedsMaxRetries_ShouldReturnFalse()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        
        // Exceed max retries
        for (int i = 0; i < 4; i++)
        {
            email.MarkAsFailed($"Error {i + 1}", DateTime.UtcNow);
        }

        // Act
        var canRetry = email.CanRetry();

        // Assert
        Assert.False(canRetry);
    }

    [Fact]
    public void CanRetry_WhenNotFailed_ShouldReturnFalse()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        email.MarkAsQueued();
        email.MarkAsSending();
        email.MarkAsSent();

        // Act
        var canRetry = email.CanRetry();

        // Assert
        Assert.False(canRetry);
    }

    [Fact]
    public void Retry_WhenCanRetry_ShouldSucceed()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        email.MarkAsFailed("Test error", DateTime.UtcNow);

        // Act
        var result = email.Retry();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(EmailStatus.Queued, email.Status);
        Assert.Null(email.ErrorMessage);
    }

    [Fact]
    public void Retry_WhenCannotRetry_ShouldReturnFailure()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        
        // Act
        var result = email.Retry();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Email cannot be retried", result.Errors);
    }

    [Fact]
    public void Retry_WhenMaxRetriesExceeded_ShouldReturnFailure()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        
        // Exceed max retries
        for (int i = 0; i < 4; i++)
        {
            email.MarkAsFailed($"Error {i + 1}", DateTime.UtcNow);
        }

        // Act
        var result = email.Retry();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Email cannot be retried", result.Errors);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void EmailMessage_FullLifecycle_ShouldTransitionCorrectly()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        var recipient = TestDataFactory.ValidEmail("user@example.com");

        // Act & Assert - Full lifecycle
        Assert.Equal(EmailStatus.Pending, email.Status);

        email.AddRecipient(recipient);
        Assert.Contains(recipient.Value, email.ToEmails);

        var queueResult = email.MarkAsQueued();
        Assert.True(queueResult.IsSuccess);
        Assert.Equal(EmailStatus.Queued, email.Status);

        var sendingResult = email.MarkAsSending();
        Assert.True(sendingResult.IsSuccess);
        Assert.Equal(EmailStatus.Sending, email.Status);

        var sentResult = email.MarkAsSentResult();
        Assert.True(sentResult.IsSuccess);
        Assert.Equal(EmailStatus.Sent, email.Status);
        Assert.NotNull(email.SentAt);

        email.MarkAsDelivered();
        // Status should be updated
        Assert.Equal(EmailStatus.Delivered, email.Status);
        Assert.NotNull(email.DeliveredAt);
    }

    [Fact]
    public void EmailMessage_FailureAndRetryLifecycle_ShouldTransitionCorrectly()
    {
        // Arrange
        var email = CreateValidEmailMessage();

        // Act & Assert - Failure and retry cycle
        email.MarkAsQueued();
        email.MarkAsSending();
        
        var failResult = email.MarkAsFailed("Network timeout", DateTime.UtcNow);
        Assert.True(failResult.IsSuccess);
        Assert.Equal(EmailStatus.Failed, email.Status);
        Assert.Equal("Network timeout", email.ErrorMessage);
        Assert.Equal(1, email.RetryCount);

        Assert.True(email.CanRetry());
        
        var retryResult = email.Retry();
        Assert.True(retryResult.IsSuccess);
        Assert.Equal(EmailStatus.Queued, email.Status);
        Assert.Null(email.ErrorMessage);

        // Second attempt
        email.MarkAsSending();
        email.MarkAsSent();
        email.MarkAsDelivered();
        
        Assert.Equal(EmailStatus.Delivered, email.Status);
        Assert.Equal(1, email.RetryCount); // Retain retry count for audit
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithZeroMaxRetries_ShouldCreateEmailThatCannotRetry()
    {
        // Arrange & Act
        var result = EmailMessage.Create(_validFromEmail, _validSubject, "Test", null, EmailType.Transactional, 0);

        // Assert
        Assert.True(result.IsSuccess);
        var email = result.Value;
        Assert.Equal(0, email.MaxRetries);

        // Even when failed, should not be able to retry
        email.MarkAsFailed("Error");
        Assert.False(email.CanRetry());
    }

    [Fact]
    public void EmailMessage_WithMultipleRecipients_ShouldManageAllTypes()
    {
        // Arrange
        var email = CreateValidEmailMessage();
        var toEmail = TestDataFactory.ValidEmail("to@example.com");
        var ccEmail = TestDataFactory.ValidEmail("cc@example.com");
        var bccEmail = TestDataFactory.ValidEmail("bcc@example.com");

        // Act
        email.AddRecipient(toEmail);
        email.AddCcRecipient(ccEmail);
        email.AddBccRecipient(bccEmail);

        // Assert
        Assert.Single(email.ToEmails);
        Assert.Single(email.CcEmails);
        Assert.Single(email.BccEmails);
        Assert.Contains(toEmail.Value, email.ToEmails);
        Assert.Contains(ccEmail.Value, email.CcEmails);
        Assert.Contains(bccEmail.Value, email.BccEmails);
    }

    #endregion

    #region Helper Methods

    private EmailMessage CreateValidEmailMessage()
    {
        return EmailMessage.Create(
            _validFromEmail,
            _validSubject,
            "Test email content",
            "<p>Test email content</p>",
            EmailType.Transactional,
            3
        ).Value;
    }

    #endregion
}