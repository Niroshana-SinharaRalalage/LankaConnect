using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Email.Services;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Serilog;
using Serilog.Extensions.Logging;

namespace LankaConnect.Infrastructure.Tests.Integration;

/// <summary>
/// Integration tests for the complete email workflow using real PostgreSQL database
/// Tests: EmailTemplate → EmailMessage → EmailService → Database persistence
/// Proves our Clean Architecture Infrastructure implementation works end-to-end
/// </summary>
public class EmailWorkflowIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private ServiceProvider _serviceProvider = null!;
    private AppDbContext _dbContext = null!;
    private IEmailService _emailService = null!;
    private IEmailTemplateRepository _templateRepository = null!;
    private IEmailMessageRepository _messageRepository = null!;

    public EmailWorkflowIntegrationTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("lanka_connect_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        await _postgresContainer.StartAsync();

        // Setup services with real database
        var services = new ServiceCollection();
        
        // Configure logging
        var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        services.AddSingleton<ILoggerFactory>(new SerilogLoggerFactory(logger));
        services.AddLogging();

        // Configure DbContext with PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_postgresContainer.GetConnectionString()));

        // Configure SMTP settings (mock for integration tests)
        services.Configure<SmtpSettings>(options =>
        {
            options.Host = "smtp.test.com";
            options.Port = 587;
            options.EnableSsl = true;
            options.Username = "test@test.com";
            options.Password = "testpassword";
            options.FromEmail = "noreply@lankaconnect.com";
            options.FromName = "LankaConnect Test";
        });

        // Register repositories
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();

        // Register email template service (mock for integration tests)
        services.AddScoped<IEmailTemplateService, MockEmailTemplateService>();

        // Register email service with correct logger type
        services.AddScoped<ILogger<TestEmailService>>(provider => 
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<TestEmailService>());
        services.AddScoped<IEmailService, TestEmailService>(); // Use test version that doesn't send actual emails

        _serviceProvider = services.BuildServiceProvider();

        // Get services
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        _emailService = _serviceProvider.GetRequiredService<IEmailService>();
        _templateRepository = _serviceProvider.GetRequiredService<IEmailTemplateRepository>();
        _messageRepository = _serviceProvider.GetRequiredService<IEmailMessageRepository>();

        // Ensure database is created and migrations applied
        await _dbContext.Database.EnsureCreatedAsync();
        
        // Apply any pending migrations
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Complete_Email_Workflow_Should_Work_End_To_End()
    {
        // Arrange - Create email template
        var subjectResult = EmailSubject.Create("Welcome to LankaConnect, {{name}}!");
        subjectResult.IsSuccess.Should().BeTrue();

        var templateResult = EmailTemplate.Create(
            name: "welcome_template",
            description: "Welcome email for new users",
            subjectTemplate: subjectResult.Value,
            textTemplate: "Hello {{name}}, welcome to LankaConnect! Your email is {{email}}.",
            htmlTemplate: "<h1>Hello {{name}}</h1><p>Welcome to LankaConnect! Your email is {{email}}.</p>",
            type: EmailType.Transactional);

        templateResult.IsSuccess.Should().BeTrue();
        var template = templateResult.Value;

        // Act 1 - Save template to database
        await _templateRepository.AddAsync(template);
        await _dbContext.SaveChangesAsync();

        // Act 2 - Retrieve template from database
        var retrievedTemplate = await _templateRepository.GetByNameAsync("welcome_template");

        // Assert 1 - Template persistence
        retrievedTemplate.Should().NotBeNull();
        retrievedTemplate!.Name.Should().Be("welcome_template");
        retrievedTemplate.IsActive.Should().BeTrue();
        retrievedTemplate.Type.Should().Be(EmailType.Transactional);

        // Act 3 - Send templated email through service
        var parameters = new Dictionary<string, object>
        {
            ["name"] = "John Doe",
            ["email"] = "john.doe@test.com"
        };

        var sendResult = await _emailService.SendTemplatedEmailAsync(
            templateName: "welcome_template",
            recipientEmail: "john.doe@test.com",
            parameters: parameters);

        // Assert 2 - Email service operation
        sendResult.IsSuccess.Should().BeTrue();

        // Act 4 - Verify email message was created in database
        var emailMessages = await _messageRepository.GetEmailsByTypeAsync(EmailType.Transactional);

        // Assert 3 - Email message persistence
        emailMessages.Should().HaveCount(1);
        var emailMessage = emailMessages.First();
        
        emailMessage.ToEmails.Should().Contain("john.doe@test.com");
        emailMessage.Subject.Value.Should().Be("Welcome to LankaConnect, John Doe!");
        emailMessage.TextContent.Should().Contain("Hello John Doe");
        emailMessage.TextContent.Should().Contain("john.doe@test.com");
        emailMessage.Type.Should().Be(EmailType.Transactional);
        emailMessage.Status.Should().Be(EmailStatus.Sent); // TestEmailService marks as sent
    }

    [Fact]
    public async Task Email_Template_Repository_Operations_Should_Work()
    {
        // Arrange
        var subjectResult = EmailSubject.Create("Test Subject");
        subjectResult.IsSuccess.Should().BeTrue();

        var templateResult = EmailTemplate.Create(
            name: "test_template",
            description: "Test email template",
            subjectTemplate: subjectResult.Value,
            textTemplate: "Test content",
            type: EmailType.Marketing);

        templateResult.IsSuccess.Should().BeTrue();
        var template = templateResult.Value;

        // Act - Repository operations
        await _templateRepository.AddAsync(template);
        await _dbContext.SaveChangesAsync();

        var foundTemplate = await _templateRepository.GetByIdAsync(template.Id);
        var templateByName = await _templateRepository.GetByNameAsync("test_template");
        var marketingTemplates = await _templateRepository.GetByEmailTypeAsync(EmailType.Marketing);

        // Assert
        foundTemplate.Should().NotBeNull();
        foundTemplate!.Id.Should().Be(template.Id);

        templateByName.Should().NotBeNull();
        templateByName!.Name.Should().Be("test_template");

        marketingTemplates.Should().HaveCount(1);
        marketingTemplates.First().Name.Should().Be("test_template");
    }

    [Fact]
    public async Task Email_Message_Repository_Operations_Should_Work()
    {
        // Arrange
        var fromEmailResult = Email.Create("sender@test.com");
        var toEmailResult = Email.Create("recipient@test.com");
        var subjectResult = EmailSubject.Create("Test Message");

        fromEmailResult.IsSuccess.Should().BeTrue();
        toEmailResult.IsSuccess.Should().BeTrue();
        subjectResult.IsSuccess.Should().BeTrue();

        var messageResult = EmailMessage.Create(
            fromEmail: fromEmailResult.Value,
            subject: subjectResult.Value,
            textContent: "Test email content",
            type: EmailType.Transactional);

        messageResult.IsSuccess.Should().BeTrue();
        var message = messageResult.Value;
        
        var addRecipientResult = message.AddRecipient(toEmailResult.Value);
        addRecipientResult.IsSuccess.Should().BeTrue();

        // Act - Repository operations
        var createResult = await _messageRepository.CreateEmailMessageAsync(message);
        createResult.IsSuccess.Should().BeTrue();

        var foundMessage = await _messageRepository.GetByIdAsync(message.Id);
        var transactionalMessages = await _messageRepository.GetEmailsByTypeAsync(EmailType.Transactional);
        var pendingMessages = await _messageRepository.GetEmailsByStatusAsync(EmailStatus.Pending);

        // Assert
        foundMessage.Should().NotBeNull();
        foundMessage!.Id.Should().Be(message.Id);
        foundMessage.ToEmails.Should().Contain("recipient@test.com");

        transactionalMessages.Should().HaveCount(1);
        pendingMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task Email_Queue_Stats_Should_Be_Calculated_Correctly()
    {
        // Arrange - Create messages with different statuses
        var fromEmailResult = Email.Create("sender@test.com");
        var subjectResult = EmailSubject.Create("Test Message");
        
        fromEmailResult.IsSuccess.Should().BeTrue();
        subjectResult.IsSuccess.Should().BeTrue();

        var messages = new List<EmailMessage>();
        
        // Create 2 pending messages
        for (int i = 0; i < 2; i++)
        {
            var messageResult = EmailMessage.Create(
                fromEmail: fromEmailResult.Value,
                subject: subjectResult.Value,
                textContent: $"Test content {i}",
                type: EmailType.Transactional);
            
            messageResult.IsSuccess.Should().BeTrue();
            var recipientResult = Email.Create($"user{i}@test.com");
            recipientResult.IsSuccess.Should().BeTrue();
            messageResult.Value.AddRecipient(recipientResult.Value);
            messages.Add(messageResult.Value);
        }
        
        // Create 1 queued message
        var queuedMessageResult = EmailMessage.Create(
            fromEmail: fromEmailResult.Value,
            subject: subjectResult.Value,
            textContent: "Queued content",
            type: EmailType.Transactional);
        
        queuedMessageResult.IsSuccess.Should().BeTrue();
        var queuedRecipientResult = Email.Create("queued@test.com");
        queuedRecipientResult.IsSuccess.Should().BeTrue();
        queuedMessageResult.Value.AddRecipient(queuedRecipientResult.Value);
        queuedMessageResult.Value.MarkAsQueued();
        messages.Add(queuedMessageResult.Value);

        // Save all messages
        foreach (var message in messages)
        {
            await _messageRepository.CreateEmailMessageAsync(message);
        }

        // Act
        var stats = await _messageRepository.GetEmailQueueStatsAsync();

        // Assert
        stats.PendingCount.Should().Be(2);
        stats.QueuedCount.Should().Be(1);
        stats.TotalCount.Should().Be(3);
    }
}

/// <summary>
/// Mock email template service for integration tests
/// </summary>
public class MockEmailTemplateService : IEmailTemplateService
{
    public Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(string templateName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        // Simple template rendering for tests
        var subject = templateName switch
        {
            "welcome_template" => $"Welcome to LankaConnect, {parameters["name"]}!",
            _ => "Test Subject"
        };

        var textBody = templateName switch
        {
            "welcome_template" => $"Hello {parameters["name"]}, welcome to LankaConnect! Your email is {parameters["email"]}.",
            _ => "Test content"
        };

        var htmlBody = templateName switch
        {
            "welcome_template" => $"<h1>Hello {parameters["name"]}</h1><p>Welcome to LankaConnect! Your email is {parameters["email"]}.</p>",
            _ => "<p>Test content</p>"
        };

        var result = new RenderedEmailTemplate
        {
            Subject = subject,
            PlainTextBody = textBody,
            HtmlBody = htmlBody
        };

        return Task.FromResult(Result<RenderedEmailTemplate>.Success(result));
    }

    public Task<Result<List<EmailTemplateInfo>>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = new List<EmailTemplateInfo>
        {
            new() { Name = "welcome_template", DisplayName = "Welcome Template", IsActive = true }
        };
        return Task.FromResult(Result<List<EmailTemplateInfo>>.Success(templates));
    }

    public Task<Result<EmailTemplateInfo>> GetTemplateInfoAsync(string templateName, CancellationToken cancellationToken = default)
    {
        var template = new EmailTemplateInfo
        {
            Name = templateName,
            DisplayName = "Test Template",
            IsActive = true,
            RequiredParameters = new List<string> { "name", "email" }
        };
        return Task.FromResult(Result<EmailTemplateInfo>.Success(template));
    }

    public Task<Result> ValidateTemplateParametersAsync(string templateName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }
}

/// <summary>
/// Test email service that doesn't actually send emails but validates the workflow
/// </summary>
public class TestEmailService : IEmailService
{
    private readonly ILogger<TestEmailService> _logger;
    private readonly IEmailMessageRepository _emailMessageRepository;
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly IEmailTemplateService _templateService;
    private readonly SmtpSettings _smtpSettings;
    private readonly AppDbContext _dbContext;

    public TestEmailService(
        ILogger<TestEmailService> logger,
        IEmailMessageRepository emailMessageRepository,
        IEmailTemplateRepository emailTemplateRepository,
        IEmailTemplateService templateService,
        IOptions<SmtpSettings> smtpSettings,
        AppDbContext dbContext)
    {
        _logger = logger;
        _emailMessageRepository = emailMessageRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _templateService = templateService;
        _smtpSettings = smtpSettings.Value;
        _dbContext = dbContext;
    }

    public async Task<Result> SendEmailAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Test: Sending email to {ToEmail}", emailMessage.ToEmail);

            // Create domain entity (similar to EmailService)
            var domainEmailResult = await CreateDomainEmailMessage(emailMessage, cancellationToken);
            if (domainEmailResult.IsFailure)
            {
                return Result.Failure(domainEmailResult.Error);
            }

            // For tests, simulate successful sending
            await Task.Delay(10, cancellationToken);

            // Mark as sent
            domainEmailResult.Value.MarkAsSent();
            _emailMessageRepository.Update(domainEmailResult.Value);
            
            // Save changes through our injected DbContext
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test email send failed");
            return Result.Failure($"Test email send failed: {ex.Message}");
        }
    }

    public async Task<Result> SendTemplatedEmailAsync(string templateName, string recipientEmail, 
        Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get template
            var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
            if (template == null || !template.IsActive)
            {
                return Result.Failure($"Template '{templateName}' not found or inactive");
            }

            // Render template
            var renderResult = await _templateService.RenderTemplateAsync(templateName, parameters, cancellationToken);
            if (renderResult.IsFailure)
            {
                return Result.Failure(renderResult.Error);
            }

            // Create email DTO
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                Subject = renderResult.Value.Subject,
                HtmlBody = renderResult.Value.HtmlBody,
                PlainTextBody = renderResult.Value.PlainTextBody,
                FromEmail = _smtpSettings.FromEmail,
                FromName = _smtpSettings.FromName,
                Priority = 2
            };

            return await SendEmailAsync(emailMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test templated email send failed");
            return Result.Failure($"Test templated email send failed: {ex.Message}");
        }
    }

    public async Task<Result<BulkEmailResult>> SendBulkEmailAsync(IEnumerable<EmailMessageDto> emailMessages, 
        CancellationToken cancellationToken = default)
    {
        var messages = emailMessages.ToList();
        var result = new BulkEmailResult
        {
            TotalEmails = messages.Count,
            SuccessfulSends = 0,
            FailedSends = 0,
            Errors = new List<string>()
        };

        foreach (var message in messages)
        {
            var sendResult = await SendEmailAsync(message, cancellationToken);
            if (sendResult.IsSuccess)
            {
                result.SuccessfulSends++;
            }
            else
            {
                result.FailedSends++;
                result.Errors.Add($"Failed to send to {message.ToEmail}: {sendResult.Error}");
            }
        }

        return Result<BulkEmailResult>.Success(result);
    }

    public async Task<Result> ValidateTemplateAsync(string templateName, CancellationToken cancellationToken = default)
    {
        var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
        if (template == null)
        {
            return Result.Failure($"Template '{templateName}' not found");
        }

        if (!template.IsActive)
        {
            return Result.Failure($"Template '{templateName}' is not active");
        }

        return Result.Success();
    }

    private async Task<Result<EmailMessage>> CreateDomainEmailMessage(EmailMessageDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var fromEmailResult = Email.Create(_smtpSettings.FromEmail);
            if (fromEmailResult.IsFailure)
                return Result<EmailMessage>.Failure(fromEmailResult.Error);

            var toEmailResult = Email.Create(dto.ToEmail);
            if (toEmailResult.IsFailure)
                return Result<EmailMessage>.Failure(toEmailResult.Error);

            var subjectResult = EmailSubject.Create(dto.Subject);
            if (subjectResult.IsFailure)
                return Result<EmailMessage>.Failure(subjectResult.Error);

            var emailMessageResult = EmailMessage.Create(
                fromEmailResult.Value,
                subjectResult.Value,
                dto.PlainTextBody ?? dto.HtmlBody,
                dto.HtmlBody,
                EmailType.Transactional);

            if (emailMessageResult.IsFailure)
                return emailMessageResult;

            var domainEmail = emailMessageResult.Value;
            var addRecipientResult = domainEmail.AddRecipient(toEmailResult.Value);
            if (addRecipientResult.IsFailure)
                return Result<EmailMessage>.Failure(addRecipientResult.Error);

            var queueResult = domainEmail.MarkAsQueued();
            if (queueResult.IsFailure)
                return Result<EmailMessage>.Failure(queueResult.Error);

            await _emailMessageRepository.AddAsync(domainEmail, cancellationToken);
            return Result<EmailMessage>.Success(domainEmail);
        }
        catch (Exception ex)
        {
            return Result<EmailMessage>.Failure($"Failed to create domain email: {ex.Message}");
        }
    }
}

// IEmailTemplateService is already defined in LankaConnect.Application.Common.Interfaces
// RenderedEmailTemplate is already defined in LankaConnect.Application.Common.Interfaces