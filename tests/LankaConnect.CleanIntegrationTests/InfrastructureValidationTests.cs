using Xunit;
using FluentAssertions;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.CleanIntegrationTests.Common;

namespace LankaConnect.CleanIntegrationTests;

/// <summary>
/// Integration tests that validate our Infrastructure implementation works correctly
/// Tests the complete email workflow: EmailTemplate → EmailMessage → Repository → Database
/// Uses architect's solution with proper EF Core DI setup and TestContainers
/// </summary>
public class InfrastructureValidationTests : DatabaseIntegrationTestBase
{

    [Fact]
    public async Task Infrastructure_EmailTemplate_CRUD_Operations_Work()
    {
        // Arrange
        var subjectResult = EmailSubject.Create("Welcome to LankaConnect!");
        subjectResult.IsSuccess.Should().BeTrue();

        var templateResult = EmailTemplate.Create(
            name: "welcome_template",
            description: "Welcome email template for new users",
            subjectTemplate: subjectResult.Value,
            textTemplate: "Hello {{name}}, welcome to LankaConnect!",
            htmlTemplate: "<h1>Hello {{name}}</h1><p>Welcome to LankaConnect!</p>",
            type: EmailType.Transactional);

        templateResult.IsSuccess.Should().BeTrue();
        var template = templateResult.Value;

        // Act - Create
        await EmailTemplateRepository.AddAsync(template);
        await DbContext.SaveChangesAsync();

        // Act - Read
        var retrievedTemplate = await EmailTemplateRepository.GetByNameAsync("welcome_template");
        var byIdTemplate = await EmailTemplateRepository.GetByIdAsync(template.Id);
        var transactionalTemplates = await EmailTemplateRepository.GetByEmailTypeAsync(EmailType.Transactional);

        // Assert
        retrievedTemplate.Should().NotBeNull();
        retrievedTemplate!.Name.Should().Be("welcome_template");
        retrievedTemplate.Description.Should().Be("Welcome email template for new users");
        retrievedTemplate.IsActive.Should().BeTrue();
        retrievedTemplate.Type.Should().Be(EmailType.Transactional);
        
        byIdTemplate.Should().NotBeNull();
        byIdTemplate!.Id.Should().Be(template.Id);
        
        transactionalTemplates.Should().HaveCount(1);
        transactionalTemplates.First().Name.Should().Be("welcome_template");

        // Act - Update
        var updateResult = retrievedTemplate.UpdateTemplate(
            subjectResult.Value, 
            "Updated text content", 
            "<p>Updated HTML content</p>");
        updateResult.IsSuccess.Should().BeTrue();
        
        await EmailTemplateRepository.UpdateAsync(retrievedTemplate);
        
        // Verify Update
        var updatedTemplate = await EmailTemplateRepository.GetByNameAsync("welcome_template");
        updatedTemplate.Should().NotBeNull();
        updatedTemplate!.TextTemplate.Should().Be("Updated text content");
        updatedTemplate.HtmlTemplate.Should().Be("<p>Updated HTML content</p>");
    }

    [Fact]
    public async Task Infrastructure_EmailMessage_CRUD_Operations_Work()
    {
        // Arrange
        var fromEmailResult = Email.Create("noreply@lankaconnect.com");
        var toEmailResult = Email.Create("user@example.com");
        var subjectResult = EmailSubject.Create("Test Email Message");

        fromEmailResult.IsSuccess.Should().BeTrue();
        toEmailResult.IsSuccess.Should().BeTrue();
        subjectResult.IsSuccess.Should().BeTrue();

        var messageResult = EmailMessage.Create(
            fromEmail: fromEmailResult.Value,
            subject: subjectResult.Value,
            textContent: "This is a test email message content",
            htmlContent: "<p>This is a test email message content</p>",
            type: EmailType.Transactional);

        messageResult.IsSuccess.Should().BeTrue();
        var message = messageResult.Value;

        var addRecipientResult = message.AddRecipient(toEmailResult.Value);
        addRecipientResult.IsSuccess.Should().BeTrue();

        // Act - Create
        var createResult = await EmailMessageRepository.CreateEmailMessageAsync(message);

        // Assert - Create
        createResult.IsSuccess.Should().BeTrue();
        var createdMessage = createResult.Value;

        createdMessage.Id.Should().Be(message.Id);
        createdMessage.ToEmails.Should().Contain("user@example.com");
        createdMessage.FromEmail.Value.Should().Be("noreply@lankaconnect.com");
        createdMessage.Subject.Value.Should().Be("Test Email Message");
        createdMessage.Status.Should().Be(EmailStatus.Pending);

        // Act - Read Operations
        var retrievedMessage = await EmailMessageRepository.GetByIdAsync(message.Id);
        var transactionalMessages = await EmailMessageRepository.GetEmailsByTypeAsync(EmailType.Transactional);
        var pendingMessages = await EmailMessageRepository.GetEmailsByStatusAsync(EmailStatus.Pending);

        // Assert - Read Operations
        retrievedMessage.Should().NotBeNull();
        retrievedMessage!.Id.Should().Be(message.Id);

        transactionalMessages.Should().HaveCount(1);
        transactionalMessages.First().Id.Should().Be(message.Id);

        pendingMessages.Should().HaveCount(1);
        pendingMessages.First().Id.Should().Be(message.Id);

        // Act - Status Updates
        var queueResult = message.MarkAsQueued();
        queueResult.IsSuccess.Should().BeTrue();
        
        EmailMessageRepository.Update(message);
        await DbContext.SaveChangesAsync();

        var queuedMessages = await EmailMessageRepository.GetEmailsByStatusAsync(EmailStatus.Queued);
        queuedMessages.Should().HaveCount(1);
        queuedMessages.First().Status.Should().Be(EmailStatus.Queued);
    }

    [Fact]
    public async Task Infrastructure_Email_Status_Workflow_Works_End_To_End()
    {
        // Arrange
        var fromEmailResult = Email.Create("system@lankaconnect.com");
        var toEmailResult = Email.Create("test@example.com");
        var subjectResult = EmailSubject.Create("Status Workflow Test");

        var messageResult = EmailMessage.Create(
            fromEmail: fromEmailResult.Value,
            subject: subjectResult.Value,
            textContent: "Testing the complete email status workflow",
            type: EmailType.Marketing);

        messageResult.Value.AddRecipient(toEmailResult.Value);
        await EmailMessageRepository.CreateEmailMessageAsync(messageResult.Value);
        var message = messageResult.Value;

        // Act & Assert - Complete Email Status Workflow
        
        // 1. Initial state: Pending
        message.Status.Should().Be(EmailStatus.Pending);
        var pendingCount = await EmailMessageRepository.CountAsync(m => m.Status == EmailStatus.Pending);
        pendingCount.Should().Be(1);

        // 2. Queue the email
        var queueResult = message.MarkAsQueued();
        queueResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Queued);
        
        EmailMessageRepository.Update(message);
        await DbContext.SaveChangesAsync();

        var queuedEmails = await EmailMessageRepository.GetQueuedEmailsAsync(batchSize: 10);
        queuedEmails.Should().HaveCount(1);
        queuedEmails.First().Id.Should().Be(message.Id);

        // 3. Start sending
        var sendingResult = message.MarkAsSending();
        sendingResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Sending);
        
        EmailMessageRepository.Update(message);
        await DbContext.SaveChangesAsync();

        // 4. Mark as sent
        var sentResult = message.MarkAsSentResult();
        sentResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Sent);
        message.SentAt.Should().NotBeNull();
        message.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        EmailMessageRepository.Update(message);
        await DbContext.SaveChangesAsync();

        var sentMessages = await EmailMessageRepository.GetEmailsByStatusAsync(EmailStatus.Sent);
        sentMessages.Should().HaveCount(1);

        // 5. Mark as delivered
        var deliveredResult = message.MarkAsDeliveredResult();
        deliveredResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Delivered);
        message.DeliveredAt.Should().NotBeNull();
        
        EmailMessageRepository.Update(message);
        await DbContext.SaveChangesAsync();

        // 6. Verify final state
        var finalMessage = await EmailMessageRepository.GetByIdAsync(message.Id);
        finalMessage.Should().NotBeNull();
        finalMessage!.Status.Should().Be(EmailStatus.Delivered);
        finalMessage.DeliveredAt.Should().NotBeNull();
        finalMessage.SentAt.Should().NotBeNull();
        finalMessage.IsOpened.Should().BeFalse(); // Not opened yet
        finalMessage.IsClicked.Should().BeFalse(); // Not clicked yet
    }

    [Fact]
    public async Task Infrastructure_Email_Queue_Statistics_Are_Calculated_Correctly()
    {
        // Arrange - Create emails in different states
        var fromEmail = Email.Create("system@lankaconnect.com").Value;
        var subject = EmailSubject.Create("Queue Stats Test").Value;
        var toEmails = new[] { "user1@test.com", "user2@test.com", "user3@test.com", "user4@test.com", "user5@test.com" };

        var messages = new List<EmailMessage>();

        // Create 5 messages in different states
        for (int i = 0; i < 5; i++)
        {
            var recipient = Email.Create(toEmails[i]).Value;
            var messageResult = EmailMessage.Create(
                fromEmail: fromEmail,
                subject: subject,
                textContent: $"Test message {i + 1}",
                type: EmailType.Transactional);

            var message = messageResult.Value;
            message.AddRecipient(recipient);
            messages.Add(message);
        }

        // Save all messages (all start as Pending)
        foreach (var message in messages)
        {
            await EmailMessageRepository.CreateEmailMessageAsync(message);
        }

        // Set different statuses
        messages[0].MarkAsQueued(); // 1 Queued
        messages[1].MarkAsQueued();
        messages[1].MarkAsSending(); // 1 Sending
        messages[2].MarkAsQueued();
        messages[2].MarkAsSending();
        messages[2].MarkAsSentResult(); // 1 Sent
        messages[3].MarkAsQueued();
        messages[3].MarkAsSending();
        messages[3].MarkAsSentResult();
        messages[3].MarkAsDeliveredResult(); // 1 Delivered
        // messages[4] remains Pending // 1 Pending

        // Update all in database
        foreach (var message in messages)
        {
            EmailMessageRepository.Update(message);
        }
        await DbContext.SaveChangesAsync();

        // Act
        var stats = await EmailMessageRepository.GetEmailQueueStatsAsync();
        var statusCounts = await EmailMessageRepository.GetStatusCountsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.PendingCount.Should().Be(1);
        stats.QueuedCount.Should().Be(1);
        stats.SendingCount.Should().Be(1);
        stats.SentCount.Should().Be(1);
        stats.DeliveredCount.Should().Be(1);
        stats.FailedCount.Should().Be(0);
        stats.TotalCount.Should().Be(5);

        statusCounts.Should().HaveCount(5); // 5 different statuses
        statusCounts[EmailStatus.Pending].Should().Be(1);
        statusCounts[EmailStatus.Queued].Should().Be(1);
        statusCounts[EmailStatus.Sending].Should().Be(1);
        statusCounts[EmailStatus.Sent].Should().Be(1);
        statusCounts[EmailStatus.Delivered].Should().Be(1);
    }

    [Fact]
    public async Task Infrastructure_Multiple_Templates_Different_Types_Work()
    {
        // Arrange - Create multiple templates of different types
        var templates = new[]
        {
            ("welcome", "Welcome Email", EmailType.Transactional, "Welcome {{name}}!"),
            ("newsletter", "Monthly Newsletter", EmailType.Marketing, "Newsletter for {{month}}"),
            ("password_reset", "Reset Password", EmailType.Transactional, "Reset your password {{name}}")
        };

        var createdTemplates = new List<EmailTemplate>();

        foreach (var (name, description, type, content) in templates)
        {
            var subject = EmailSubject.Create($"{description} Subject").Value;
            var templateResult = EmailTemplate.Create(
                name: name,
                description: description,
                subjectTemplate: subject,
                textTemplate: content,
                type: type);

            createdTemplates.Add(templateResult.Value);
            await EmailTemplateRepository.AddAsync(templateResult.Value);
        }

        await DbContext.SaveChangesAsync();

        // Act - Query by different criteria
        var allTemplates = await EmailTemplateRepository.GetTemplatesAsync(isActive: true);
        var transactionalTemplates = await EmailTemplateRepository.GetByEmailTypeAsync(EmailType.Transactional);
        var marketingTemplates = await EmailTemplateRepository.GetByEmailTypeAsync(EmailType.Marketing);
        var welcomeTemplate = await EmailTemplateRepository.GetByNameAsync("welcome");

        // Assert
        allTemplates.Should().HaveCount(3);
        transactionalTemplates.Should().HaveCount(2); // welcome + password_reset
        marketingTemplates.Should().HaveCount(1); // newsletter
        
        welcomeTemplate.Should().NotBeNull();
        welcomeTemplate!.Name.Should().Be("welcome");
        welcomeTemplate.Type.Should().Be(EmailType.Transactional);
        
        // Verify all templates are active by default
        allTemplates.All(t => t.IsActive).Should().BeTrue();
        
        // Verify template configurations are valid
        foreach (var template in allTemplates)
        {
            var configResult = template.ValidateConfiguration();
            configResult.IsSuccess.Should().BeTrue();
        }
    }

}