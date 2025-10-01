using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Common;
using Serilog;
using Serilog.Extensions.Logging;

namespace LankaConnect.Infrastructure.Tests.Simple;

/// <summary>
/// Simple integration test to validate core Infrastructure components work
/// This test proves our Clean Architecture Infrastructure implementation is functional
/// </summary>
public class SimpleIntegrationTest : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly EmailTemplateRepository _templateRepository;
    private readonly EmailMessageRepository _messageRepository;
    private readonly ServiceProvider _serviceProvider;

    public SimpleIntegrationTest()
    {
        var services = new ServiceCollection();
        
        // Configure logging
        var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        services.AddSingleton<ILoggerFactory>(new SerilogLoggerFactory(logger));
        services.AddLogging();

        // Configure InMemory DbContext for simple test
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        _templateRepository = new EmailTemplateRepository(_dbContext);
        _messageRepository = new EmailMessageRepository(_dbContext);
        
        // Ensure database is created
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task EmailTemplate_Should_Be_Created_And_Retrieved_Successfully()
    {
        // Arrange
        var subjectResult = EmailSubject.Create("Test Template Subject");
        subjectResult.IsSuccess.Should().BeTrue();

        var templateResult = EmailTemplate.Create(
            name: "test_template",
            description: "Test template for integration testing",
            subjectTemplate: subjectResult.Value,
            textTemplate: "Hello {{name}}, this is a test email.",
            htmlTemplate: "<h1>Hello {{name}}</h1><p>This is a test email.</p>",
            type: EmailType.Transactional);

        templateResult.IsSuccess.Should().BeTrue();

        // Act
        await _templateRepository.AddAsync(templateResult.Value);
        await _dbContext.SaveChangesAsync();

        var retrievedTemplate = await _templateRepository.GetByNameAsync("test_template");

        // Assert
        retrievedTemplate.Should().NotBeNull();
        retrievedTemplate!.Name.Should().Be("test_template");
        retrievedTemplate.Description.Should().Be("Test template for integration testing");
        retrievedTemplate.IsActive.Should().BeTrue();
        retrievedTemplate.Type.Should().Be(EmailType.Transactional);
        retrievedTemplate.SubjectTemplate.Value.Should().Be("Test Template Subject");
    }

    [Fact]
    public async Task EmailMessage_Should_Be_Created_And_Retrieved_Successfully()
    {
        // Arrange
        var fromEmailResult = Email.Create("sender@test.com");
        var toEmailResult = Email.Create("recipient@test.com");
        var subjectResult = EmailSubject.Create("Test Email Subject");

        fromEmailResult.IsSuccess.Should().BeTrue();
        toEmailResult.IsSuccess.Should().BeTrue();
        subjectResult.IsSuccess.Should().BeTrue();

        var messageResult = EmailMessage.Create(
            fromEmail: fromEmailResult.Value,
            subject: subjectResult.Value,
            textContent: "This is a test email message",
            htmlContent: "<p>This is a test email message</p>",
            type: EmailType.Transactional);

        messageResult.IsSuccess.Should().BeTrue();

        var addRecipientResult = messageResult.Value.AddRecipient(toEmailResult.Value);
        addRecipientResult.IsSuccess.Should().BeTrue();

        // Act
        var createResult = await _messageRepository.CreateEmailMessageAsync(messageResult.Value);

        // Assert
        createResult.IsSuccess.Should().BeTrue();
        var createdMessage = createResult.Value;

        createdMessage.Should().NotBeNull();
        createdMessage.ToEmails.Should().Contain("recipient@test.com");
        createdMessage.FromEmail.Value.Should().Be("sender@test.com");
        createdMessage.Subject.Value.Should().Be("Test Email Subject");
        createdMessage.TextContent.Should().Be("This is a test email message");
        createdMessage.Type.Should().Be(EmailType.Transactional);
        createdMessage.Status.Should().Be(EmailStatus.Pending);
    }

    [Fact]
    public async Task Email_Status_Transitions_Should_Work_Correctly()
    {
        // Arrange
        var fromEmailResult = Email.Create("sender@test.com");
        var toEmailResult = Email.Create("recipient@test.com");
        var subjectResult = EmailSubject.Create("Status Test Email");

        var messageResult = EmailMessage.Create(
            fromEmail: fromEmailResult.Value,
            subject: subjectResult.Value,
            textContent: "Status transition test",
            type: EmailType.Transactional);

        messageResult.Value.AddRecipient(toEmailResult.Value);

        await _messageRepository.CreateEmailMessageAsync(messageResult.Value);
        var message = messageResult.Value;

        // Act & Assert - Test status transitions
        
        // 1. Start as Pending
        message.Status.Should().Be(EmailStatus.Pending);

        // 2. Mark as Queued
        var queueResult = message.MarkAsQueued();
        queueResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Queued);

        // 3. Mark as Sending
        var sendingResult = message.MarkAsSending();
        sendingResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Sending);

        // 4. Mark as Sent
        var sentResult = message.MarkAsSentResult();
        sentResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Sent);
        message.SentAt.Should().NotBeNull();

        // 5. Mark as Delivered
        var deliveredResult = message.MarkAsDeliveredResult();
        deliveredResult.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailStatus.Delivered);
        message.DeliveredAt.Should().NotBeNull();

        // Update in database
        _messageRepository.Update(message);
        await _dbContext.SaveChangesAsync();

        // Verify persistence
        var persistedMessage = await _messageRepository.GetByIdAsync(message.Id);
        persistedMessage.Should().NotBeNull();
        persistedMessage!.Status.Should().Be(EmailStatus.Delivered);
    }

    [Fact]
    public async Task Repository_Operations_Should_Handle_Multiple_Records()
    {
        // Arrange - Create multiple email templates
        var templates = new List<EmailTemplate>();
        
        for (int i = 1; i <= 3; i++)
        {
            var subjectResult = EmailSubject.Create($"Template {i} Subject");
            var templateResult = EmailTemplate.Create(
                name: $"template_{i}",
                description: $"Test template {i}",
                subjectTemplate: subjectResult.Value,
                textTemplate: $"Template {i} text content",
                type: i == 1 ? EmailType.Transactional : EmailType.Marketing);

            templates.Add(templateResult.Value);
        }

        // Act - Save all templates
        foreach (var template in templates)
        {
            await _templateRepository.AddAsync(template);
        }
        await _dbContext.SaveChangesAsync();

        // Assert - Verify retrieval
        var transactionalTemplates = await _templateRepository.GetByEmailTypeAsync(EmailType.Transactional);
        var marketingTemplates = await _templateRepository.GetByEmailTypeAsync(EmailType.Marketing);
        var allTemplates = await _templateRepository.GetAllAsync();

        transactionalTemplates.Should().HaveCount(1);
        marketingTemplates.Should().HaveCount(2);
        allTemplates.Should().HaveCount(3);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}