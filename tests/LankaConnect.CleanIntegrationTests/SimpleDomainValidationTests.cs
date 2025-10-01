using Xunit;
using FluentAssertions;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Common;

namespace LankaConnect.CleanIntegrationTests;

/// <summary>
/// Simple domain validation tests that prove our domain logic works correctly
/// These tests validate the core business logic without database dependencies
/// </summary>
public class SimpleDomainValidationTests
{
    [Fact]
    public void EmailTemplate_Can_Be_Created_With_Valid_Data()
    {
        // Arrange
        var subjectResult = EmailSubject.Create("Welcome to LankaConnect!");
        var name = "welcome_template";
        var description = "Welcome email template";
        var textTemplate = "Hello {{name}}, welcome to LankaConnect!";
        var htmlTemplate = "<h1>Hello {{name}}</h1><p>Welcome to LankaConnect!</p>";

        // Act
        subjectResult.IsSuccess.Should().BeTrue();
        var templateResult = EmailTemplate.Create(
            name: name,
            description: description,
            subjectTemplate: subjectResult.Value,
            textTemplate: textTemplate,
            htmlTemplate: htmlTemplate,
            type: EmailType.Transactional);

        // Assert
        templateResult.IsSuccess.Should().BeTrue();
        var template = templateResult.Value;
        
        template.Name.Should().Be(name);
        template.Description.Should().Be(description);
        template.TextTemplate.Should().Be(textTemplate);
        template.HtmlTemplate.Should().Be(htmlTemplate);
        template.Type.Should().Be(EmailType.Transactional);
        template.IsActive.Should().BeTrue();
        template.SubjectTemplate.Value.Should().Be("Welcome to LankaConnect!");
        template.Id.Should().NotBe(Guid.Empty);
        template.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void EmailTemplate_Can_Be_Updated_Successfully()
    {
        // Arrange
        var subjectResult = EmailSubject.Create("Original Subject");
        var templateResult = EmailTemplate.Create(
            "test_template",
            "Test template",
            subjectResult.Value,
            "Original text",
            type: EmailType.Marketing);

        templateResult.IsSuccess.Should().BeTrue();
        var template = templateResult.Value;
        var originalUpdatedAt = template.UpdatedAt;

        // Act
        var newSubject = EmailSubject.Create("Updated Subject");
        newSubject.IsSuccess.Should().BeTrue();
        
        var updateResult = template.UpdateTemplate(
            newSubject.Value,
            "Updated text content",
            "<p>Updated HTML content</p>");

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        template.SubjectTemplate.Value.Should().Be("Updated Subject");
        template.TextTemplate.Should().Be("Updated text content");
        template.HtmlTemplate.Should().Be("<p>Updated HTML content</p>");
        template.UpdatedAt!.Should().BeAfter(originalUpdatedAt ?? DateTime.MinValue);
    }

    [Fact]
    public void EmailMessage_Can_Be_Created_With_Valid_Data()
    {
        // Arrange
        var fromEmailResult = Email.Create("sender@lankaconnect.com");
        var toEmailResult = Email.Create("recipient@example.com");
        var subjectResult = EmailSubject.Create("Test Email Subject");

        // Act
        fromEmailResult.IsSuccess.Should().BeTrue();
        toEmailResult.IsSuccess.Should().BeTrue();
        subjectResult.IsSuccess.Should().BeTrue();

        var messageResult = EmailMessage.Create(
            fromEmail: fromEmailResult.Value,
            subject: subjectResult.Value,
            textContent: "Test email content",
            htmlContent: "<p>Test email content</p>",
            type: EmailType.Transactional);

        // Assert
        messageResult.IsSuccess.Should().BeTrue();
        var message = messageResult.Value;

        message.FromEmail.Value.Should().Be("sender@lankaconnect.com");
        message.Subject.Value.Should().Be("Test Email Subject");
        message.TextContent.Should().Be("Test email content");
        message.HtmlContent.Should().Be("<p>Test email content</p>");
        message.Type.Should().Be(EmailType.Transactional);
        message.Status.Should().Be(EmailStatus.Pending);
        message.RetryCount.Should().Be(0);
        message.MaxRetries.Should().Be(3);
        message.ToEmails.Should().BeEmpty(); // No recipients added yet
        message.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void EmailMessage_Can_Add_Recipients_Successfully()
    {
        // Arrange
        var fromEmailResult = Email.Create("sender@lankaconnect.com");
        var messageResult = EmailMessage.Create(
            fromEmailResult.Value,
            EmailSubject.Create("Test Subject").Value,
            "Test content",
            type: EmailType.Transactional);

        var message = messageResult.Value;

        // Act
        var recipient1 = Email.Create("user1@example.com").Value;
        var recipient2 = Email.Create("user2@example.com").Value;
        var ccRecipient = Email.Create("cc@example.com").Value;
        var bccRecipient = Email.Create("bcc@example.com").Value;

        var addResult1 = message.AddRecipient(recipient1);
        var addResult2 = message.AddRecipient(recipient2);
        var addCcResult = message.AddCcRecipient(ccRecipient);
        var addBccResult = message.AddBccRecipient(bccRecipient);

        // Assert
        addResult1.IsSuccess.Should().BeTrue();
        addResult2.IsSuccess.Should().BeTrue();
        addCcResult.IsSuccess.Should().BeTrue();
        addBccResult.IsSuccess.Should().BeTrue();

        message.ToEmails.Should().HaveCount(2);
        message.ToEmails.Should().Contain("user1@example.com");
        message.ToEmails.Should().Contain("user2@example.com");
        message.CcEmails.Should().HaveCount(1);
        message.CcEmails.Should().Contain("cc@example.com");
        message.BccEmails.Should().HaveCount(1);
        message.BccEmails.Should().Contain("bcc@example.com");

        // Test duplicate prevention
        var duplicateResult = message.AddRecipient(recipient1);
        duplicateResult.IsSuccess.Should().BeFalse();
        duplicateResult.Error.Should().Contain("already added");
    }

    [Fact]
    public void EmailMessage_Status_Transitions_Work_Correctly()
    {
        // Arrange
        var fromEmailResult = Email.Create("sender@lankaconnect.com");
        var messageResult = EmailMessage.Create(
            fromEmailResult.Value,
            EmailSubject.Create("Status Test").Value,
            "Status transition test content",
            type: EmailType.Transactional);

        var message = messageResult.Value;

        // Act & Assert - Test valid status transitions
        
        // 1. Initial state: Pending
        message.Status.Should().Be(EmailStatus.Pending);

        // 2. Pending -> Queued
        var queueResult = message.MarkAsQueued();
        queueResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Queued);

        // 3. Queued -> Sending
        var sendingResult = message.MarkAsSending();
        sendingResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Sending);

        // 4. Sending -> Sent
        var sentResult = message.MarkAsSentResult();
        sentResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Sent);
        message.SentAt.Should().NotBeNull();
        message.SentAt!.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // 5. Sent -> Delivered
        var deliveredResult = message.MarkAsDeliveredResult();
        deliveredResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Delivered);
        message.DeliveredAt.Should().NotBeNull();
        message.DeliveredAt!.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void EmailMessage_Failure_And_Retry_Logic_Works()
    {
        // Arrange
        var fromEmailResult = Email.Create("sender@lankaconnect.com");
        var messageResult = EmailMessage.Create(
            fromEmailResult.Value,
            EmailSubject.Create("Retry Test").Value,
            "Retry test content",
            type: EmailType.Transactional,
            maxRetries: 2);

        var message = messageResult.Value;
        message.MarkAsQueued();
        message.MarkAsSending();

        // Act & Assert - Test failure and retry logic

        // 1. Mark as failed with retry time
        var nextRetryTime = DateTime.UtcNow.AddMinutes(5);
        var failResult = message.MarkAsFailed("SMTP connection failed", nextRetryTime);
        
        failResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Failed);
        message.ErrorMessage.Should().Be("SMTP connection failed");
        message.RetryCount.Should().Be(1);
        message.FailedAt.Should().NotBeNull();
        message.NextRetryAt.Should().Be(nextRetryTime);

        // 2. Test retry eligibility (should not be ready yet)
        message.CanRetry().Should().BeFalse(); // NextRetryAt is in the future

        // 3. Test retry when time is right
        var failResultPast = message.MarkAsFailed("Second failure", DateTime.UtcNow.AddMinutes(-1));
        failResultPast.IsSuccess.Should().BeTrue();
        message.CanRetry().Should().BeTrue(); // NextRetryAt is in the past

        // 4. Perform retry
        var retryResult = message.Retry();
        retryResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Queued);
        message.ErrorMessage.Should().BeNull();

        // 5. Test max retries exceeded
        message.MarkAsSending();
        var finalFailResult = message.MarkAsFailed("Final failure", DateTime.UtcNow.AddMinutes(-1));
        finalFailResult.IsSuccess.Should().BeTrue();
        message.RetryCount.Should().Be(3); // Exceeded maxRetries of 2
        message.CanRetry().Should().BeFalse();
    }

    [Fact]
    public void EmailMessage_Tracking_Features_Work()
    {
        // Arrange
        var fromEmailResult = Email.Create("sender@lankaconnect.com");
        var messageResult = EmailMessage.Create(
            fromEmailResult.Value,
            EmailSubject.Create("Tracking Test").Value,
            "Tracking test content",
            type: EmailType.Marketing);

        var message = messageResult.Value;
        message.MarkAsQueued();
        message.MarkAsSending();
        message.MarkAsSent("msg-12345");

        // Act & Assert - Test tracking features

        // 1. Initial state - not opened or clicked
        message.IsOpened.Should().BeFalse();
        message.IsClicked.Should().BeFalse();
        message.OpenedAt.Should().BeNull();
        message.ClickedAt.Should().BeNull();

        // 2. Mark as opened
        message.MarkAsOpened();
        message.IsOpened.Should().BeTrue();
        message.OpenedAt.Should().NotBeNull();
        message.OpenedAt!.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // 3. Mark as clicked
        message.MarkAsClicked();
        message.IsClicked.Should().BeTrue();
        message.ClickedAt.Should().NotBeNull();
        message.ClickedAt!.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // 4. Test message ID
        message.MessageId.Should().Be("msg-12345");

        // 5. Multiple opens/clicks don't change timestamps
        var originalOpenedAt = message.OpenedAt;
        var originalClickedAt = message.ClickedAt;
        
        message.MarkAsOpened();
        message.MarkAsClicked();
        
        message.OpenedAt.Should().Be(originalOpenedAt);
        message.ClickedAt.Should().Be(originalClickedAt);
    }

    [Fact]
    public void Value_Objects_Have_Proper_Validation()
    {
        // Test EmailSubject validation
        var emptySubjectResult = EmailSubject.Create("");
        emptySubjectResult.IsSuccess.Should().BeFalse();
        emptySubjectResult.Error.Should().Contain("required");

        var validSubjectResult = EmailSubject.Create("Valid Subject");
        validSubjectResult.IsSuccess.Should().BeTrue();
        validSubjectResult.Value.Value.Should().Be("Valid Subject");

        // Test Email validation
        var invalidEmailResult = Email.Create("invalid-email");
        invalidEmailResult.IsSuccess.Should().BeFalse();
        invalidEmailResult.Error.Should().Contain("format");

        var validEmailResult = Email.Create("user@example.com");
        validEmailResult.IsSuccess.Should().BeTrue();
        validEmailResult.Value.Value.Should().Be("user@example.com");

        // Test EmailTemplateCategory
        var transactionalCategory = EmailTemplateCategory.ForEmailType(EmailType.Transactional);
        transactionalCategory.Value.Should().Be("System"); // Transactional emails map to System category

        var marketingCategory = EmailTemplateCategory.ForEmailType(EmailType.Marketing);
        marketingCategory.Value.Should().Be("Marketing");
    }

    [Fact]
    public void Domain_Entities_Have_Proper_Base_Entity_Behavior()
    {
        // Arrange
        var fromEmailResult = Email.Create("sender@lankaconnect.com");
        var messageResult = EmailMessage.Create(
            fromEmailResult.Value,
            EmailSubject.Create("Base Entity Test").Value,
            "Base entity test content",
            type: EmailType.Transactional);

        var message = messageResult.Value;
        var originalCreatedAt = message.CreatedAt;
        var originalUpdatedAt = message.UpdatedAt;

        // Act - Trigger an update
        message.MarkAsQueued();

        // Assert - Base entity behavior
        message.Id.Should().NotBe(Guid.Empty);
        message.CreatedAt.Should().Be(originalCreatedAt); // Should not change
        message.UpdatedAt!.Should().BeAfter(originalUpdatedAt ?? DateTime.MinValue); // Should be updated
        message.UpdatedAt!.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }
}