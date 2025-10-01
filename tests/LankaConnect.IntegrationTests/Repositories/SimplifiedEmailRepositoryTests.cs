using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.IntegrationTests.Common;
using LankaConnect.TestUtilities.Builders;
using Email = LankaConnect.Domain.Users.ValueObjects.Email;

namespace LankaConnect.IntegrationTests.Repositories;

/// <summary>
/// Simplified integration tests following architectural decision for email-based approach
/// Tests focus on repository contracts without complex FK relationships
/// </summary>
public class SimplifiedEmailRepositoryTests : DatabaseIntegrationTestBase
{
    [Fact]
    public async Task EmailMessage_CreateWithEmailAddresses_ShouldPersistSuccessfully()
    {
        // Arrange - Use email addresses directly (no FK dependencies)
        var fromEmail = EmailTestDataBuilder.CreateValidEmail("sender@lankaconnect.com");
        var toEmail = EmailTestDataBuilder.CreateValidEmail("recipient@example.com");
        var subject = EmailSubject.Create("Integration Test Subject").Value;
        
        var message = EmailMessage.Create(fromEmail, subject, "Test email body", null, EmailType.Transactional).Value;
        message.AddRecipient(toEmail);

        try
        {
            // Act - Persist to database
            await EmailMessageRepository.AddAsync(message);
            await UnitOfWork.CommitAsync();

            // Assert - Retrieve and validate
            var retrieved = await EmailMessageRepository.GetByIdAsync(message.Id);
            
            Assert.NotNull(retrieved);
            Assert.Equal("sender@lankaconnect.com", retrieved.FromEmail.Value);
            Assert.Equal("Integration Test Subject", retrieved.Subject.Value);
            Assert.Equal("Test email body", retrieved.TextContent);
            Assert.Equal(EmailStatus.Pending, retrieved.Status);
            Assert.Contains("recipient@example.com", retrieved.ToEmails);
        }
        finally
        {
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task EmailTemplate_CreateWithCategory_ShouldPersistSuccessfully()
    {
        // Arrange - Create template with proper value objects
        var templateName = $"test-template-{Guid.NewGuid()}";
        var subjectTemplate = EmailSubject.Create("Welcome {{UserName}}!").Value;
        
        var template = EmailTemplate.Create(
            templateName,
            "Welcome email template for new users",
            subjectTemplate,
            "Welcome {{UserName}}, thank you for joining!",
            "<h1>Welcome {{UserName}}!</h1><p>Thank you for joining!</p>",
            EmailType.Welcome
        ).Value;

        try
        {
            // Act - Persist to database
            await EmailTemplateRepository.AddAsync(template);
            await UnitOfWork.CommitAsync();

            // Assert - Retrieve and validate
            var retrieved = await EmailTemplateRepository.GetByNameAsync(templateName);
            
            Assert.NotNull(retrieved);
            Assert.Equal(templateName, retrieved.Name);
            Assert.Equal("Welcome email template for new users", retrieved.Description);
            Assert.Equal("Welcome {{UserName}}!", retrieved.SubjectTemplate.Value);
            Assert.Equal(EmailType.Welcome, retrieved.Type);
            Assert.True(retrieved.IsActive);
        }
        finally
        {
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task EmailMessage_CompleteWorkflow_ShouldUpdateStatusCorrectly()
    {
        // Arrange - Create email message
        var fromEmail = EmailTestDataBuilder.CreateValidEmail("system@lankaconnect.com");
        var toEmail = EmailTestDataBuilder.CreateValidEmail("user@example.com");
        
        var message = new EmailMessage(toEmail, "Workflow Test", "Testing complete workflow");
        
        try
        {
            // Act - Save initial message
            await EmailMessageRepository.AddAsync(message);
            await UnitOfWork.CommitAsync();
            
            // Act - Update status through workflow
            message.MarkAsQueued();
            message.MarkAsSending();
            message.MarkAsSent();
            
            EmailMessageRepository.Update(message);
            await UnitOfWork.CommitAsync();

            // Assert - Validate status updates
            var retrieved = await EmailMessageRepository.GetByIdAsync(message.Id);
            
            Assert.NotNull(retrieved);
            Assert.Equal(EmailStatus.Sent, retrieved.Status);
            Assert.NotNull(retrieved.SentAt);
            Assert.True(retrieved.SentAt <= DateTime.UtcNow);
        }
        finally
        {
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task EmailMessage_WithMultipleRecipients_ShouldPersistCorrectly()
    {
        // Arrange - Create message with multiple recipients
        var fromEmail = EmailTestDataBuilder.CreateValidEmail("newsletter@lankaconnect.com");
        var subject = EmailSubject.Create("Newsletter Update").Value;
        
        var message = EmailMessage.Create(fromEmail, subject, "This week's newsletter", null, EmailType.Newsletter).Value;
        
        // Add multiple recipients
        message.AddRecipient(EmailTestDataBuilder.CreateValidEmail("user1@example.com"));
        message.AddRecipient(EmailTestDataBuilder.CreateValidEmail("user2@example.com"));
        message.AddCcRecipient(EmailTestDataBuilder.CreateValidEmail("cc@example.com"));
        message.AddBccRecipient(EmailTestDataBuilder.CreateValidEmail("bcc@example.com"));

        try
        {
            // Act - Persist message
            await EmailMessageRepository.AddAsync(message);
            await UnitOfWork.CommitAsync();

            // Assert - Validate multiple recipients
            var retrieved = await EmailMessageRepository.GetByIdAsync(message.Id);
            
            Assert.NotNull(retrieved);
            Assert.Equal(2, retrieved.ToEmails.Count);
            Assert.Single(retrieved.CcEmails);
            Assert.Single(retrieved.BccEmails);
            Assert.Contains("user1@example.com", retrieved.ToEmails);
            Assert.Contains("user2@example.com", retrieved.ToEmails);
            Assert.Contains("cc@example.com", retrieved.CcEmails);
            Assert.Contains("bcc@example.com", retrieved.BccEmails);
        }
        finally
        {
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task EmailTemplate_CategoryMapping_ShouldWorkCorrectly()
    {
        // Arrange - Test all email types and their categories
        var testCases = new[]
        {
            (EmailType.Welcome, "Notification"),
            (EmailType.EmailVerification, "Authentication"),
            (EmailType.PasswordReset, "Authentication"),
            (EmailType.BusinessNotification, "Business"),
            (EmailType.Marketing, "Marketing"),
            (EmailType.Transactional, "System")
        };

        var templates = new List<EmailTemplate>();
        
        try
        {
            foreach (var (emailType, expectedCategory) in testCases)
            {
                // Arrange - Create template for each type
                var templateName = $"test-{emailType.ToString().ToLower()}-{Guid.NewGuid()}";
                var subject = EmailSubject.Create($"Test {emailType} Email").Value;
                
                var template = EmailTemplate.Create(
                    templateName,
                    $"Test template for {emailType}",
                    subject,
                    "Test content",
                    null,
                    emailType
                ).Value;

                templates.Add(template);

                // Act - Persist template
                await EmailTemplateRepository.AddAsync(template);
            }
            
            await UnitOfWork.CommitAsync();

            // Assert - Validate category mappings
            foreach (var template in templates)
            {
                var retrieved = await EmailTemplateRepository.GetByNameAsync(template.Name);
                Assert.NotNull(retrieved);
                
                var expectedCategory = testCases.First(tc => tc.Item1 == template.Type).Item2;
                Assert.Equal(expectedCategory, retrieved.Category.Value);
            }
        }
        finally
        {
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task EmailRepositories_DependencyInjection_ShouldResolveCorrectly()
    {
        // This test validates that all email repositories are properly configured
        // with EF Core and can perform basic operations without FK violations
        
        // Assert - All repositories should be available
        Assert.NotNull(EmailMessageRepository);
        Assert.NotNull(EmailTemplateRepository);
        Assert.NotNull(UnitOfWork);

        // Act & Assert - Basic operations should work
        var messageCount = await EmailMessageRepository.CountAsync();
        var templateCount = await EmailTemplateRepository.CountAsync();

        Assert.True(messageCount >= 0);
        Assert.True(templateCount >= 0);
        
        // Validate DbContext is working
        Assert.NotNull(DbContext);
        Assert.True(DbContext.Database.CanConnect());
    }
}