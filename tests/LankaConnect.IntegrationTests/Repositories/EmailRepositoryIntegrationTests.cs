using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.IntegrationTests.Common;
using LankaConnect.TestUtilities.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for Email-related repositories with proper EF Core DI setup
/// </summary>
public class EmailRepositoryIntegrationTests : DatabaseIntegrationTestBase
{
    [Fact]
    public async Task EmailTemplateRepository_CreateAndRetrieve_ShouldWork()
    {
        // Arrange
        var templateName = $"test-template-{Guid.NewGuid()}";
        var category = EmailTemplateCategory.FromValue("marketing").Value;
        var subjectResult = EmailSubject.Create("Test Subject");
        var template = EmailTemplate.Create(templateName, "Test Description", subjectResult.Value, "Test Body").Value;

        try
        {
            // Act - Create
            await EmailTemplateRepository.AddAsync(template);
            await UnitOfWork.CommitAsync();

            // Act - Retrieve in separate scope
            var retrievedTemplate = await ExecuteInSeparateScope(async serviceProvider =>
            {
                var templateRepo = serviceProvider.GetRequiredService<IEmailTemplateRepository>();
                return await templateRepo.GetByNameAsync(templateName);
            });

            // Assert
            Assert.NotNull(retrievedTemplate);
            Assert.Equal(templateName, retrievedTemplate.Name);
            Assert.Equal("Test Subject", retrievedTemplate.SubjectTemplate.Value);
            Assert.Equal("Test Body", retrievedTemplate.TextTemplate);
            Assert.Equal(category.Value, retrievedTemplate.Category.Value);
        }
        finally
        {
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task EmailMessageRepository_CreateAndRetrieve_ShouldWork()
    {
        // Arrange - Create user first
        var userEmail = EmailTestDataBuilder.CreateValidEmail($"message-test-{Guid.NewGuid()}@example.com");
        var user = User.Create(userEmail, "Email", "Test").Value;
        await UserRepository.AddAsync(user);
        await UnitOfWork.CommitAsync();

        // Arrange - Create email message
        var recipientEmail = EmailTestDataBuilder.CreateValidEmail("recipient@example.com");
        var subjectValue = EmailSubject.Create("Test Subject").Value;
        var message = EmailMessage.Create(
            userEmail, // fromEmail
            subjectValue, // subject as EmailSubject
            "Test Body" // textContent
        ).Value;
        message.AddRecipient(recipientEmail);

        try
        {
            // Act - Create message
            await EmailMessageRepository.AddAsync(message);
            await UnitOfWork.CommitAsync();

            // Act - Retrieve in separate scope
            var retrievedMessage = await ExecuteInSeparateScope(async serviceProvider =>
            {
                var messageRepo = serviceProvider.GetRequiredService<IEmailMessageRepository>();
                return await messageRepo.GetByIdAsync(message.Id);
            });

            // Assert
            Assert.NotNull(retrievedMessage);
            Assert.Equal("recipient@example.com", retrievedMessage.ToEmails.First());
            Assert.Equal("Test Subject", retrievedMessage.Subject.Value);
            Assert.Equal("Test Body", retrievedMessage.TextContent);
        }
        finally
        {
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task UserEmailPreferencesRepository_CreateAndRetrieve_ShouldWork()
    {
        // Arrange - Create user first
        var userEmail = EmailTestDataBuilder.CreateValidEmail($"preferences-test-{Guid.NewGuid()}@example.com");
        var user = User.Create(userEmail, "Preferences", "Test").Value;
        await UserRepository.AddAsync(user);
        await UnitOfWork.CommitAsync();

        // Arrange - Create email preferences
        var preferences = UserEmailPreferences.Create(user.Id).Value;
        preferences.UpdateMarketingPreference(false);

        try
        {
            // Act - Create preferences
            await UserEmailPreferencesRepository.AddAsync(preferences);
            await UnitOfWork.CommitAsync();

            // Act - Retrieve in separate scope
            var retrievedPreferences = await ExecuteInSeparateScope(async serviceProvider =>
            {
                var prefRepo = serviceProvider.GetRequiredService<IUserEmailPreferencesRepository>();
                return await prefRepo.GetByUserIdAsync(user.Id);
            });

            // Assert
            Assert.NotNull(retrievedPreferences);
            Assert.Equal(user.Id, retrievedPreferences.UserId);
            Assert.False(retrievedPreferences.AllowMarketing);
        }
        finally
        {
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task EmailStatusRepository_CreateAndRetrieve_ShouldWork()
    {
        // Arrange - Create user and message first
        var userEmail = EmailTestDataBuilder.CreateValidEmail($"status-test-{Guid.NewGuid()}@example.com");
        var user = User.Create(userEmail, "Status", "Test").Value;
        await UserRepository.AddAsync(user);

        var recipientEmail = EmailTestDataBuilder.CreateValidEmail("status-recipient@example.com");
        var subject = EmailSubject.Create("Status Test Subject").Value;
        var message = EmailMessage.Create(
            userEmail,
            subject,
            "Status Test Body",
            null,
            EmailType.Transactional
        ).Value;
        await EmailMessageRepository.AddAsync(message);
        await UnitOfWork.CommitAsync();

        // Arrange - Update message status (EmailStatus is an enum, not an entity)
        message.MarkAsSent(); // Update the EmailMessage status directly

        try
        {
            // Act - Update message status
            EmailMessageRepository.Update(message);
            await UnitOfWork.CommitAsync();

            // Act - Retrieve updated message in separate scope
            var retrievedMessage = await ExecuteInSeparateScope(async serviceProvider =>
            {
                var messageRepo = serviceProvider.GetRequiredService<IEmailMessageRepository>();
                return await messageRepo.GetByIdAsync(message.Id);
            });

            // Assert
            Assert.NotNull(retrievedMessage);
            Assert.Equal(message.Id, retrievedMessage.Id);
            Assert.Equal(EmailStatus.Sent, retrievedMessage.Status);
        }
        finally
        {
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task AllEmailRepositories_DependencyInjection_ShouldResolveWithoutErrors()
    {
        // This test specifically validates that all email repositories resolve
        // their dependencies correctly, including ILoggerFactory and EF Core diagnostics
        
        // Assert - All repositories should be properly resolved
        Assert.NotNull(EmailTemplateRepository);
        Assert.NotNull(EmailMessageRepository);
        Assert.NotNull(UserEmailPreferencesRepository);
        Assert.NotNull(EmailStatusRepository);

        // Act & Assert - Basic operations should not throw DI exceptions
        var templateCount = await EmailTemplateRepository.CountAsync();
        var messageCount = await EmailMessageRepository.CountAsync();
        var preferencesCount = await UserEmailPreferencesRepository.CountAsync();
        var queueStats = await EmailStatusRepository.GetQueueStatsAsync();

        Assert.True(templateCount >= 0);
        Assert.True(messageCount >= 0);
        Assert.True(preferencesCount >= 0);
        Assert.NotNull(queueStats);
    }
}