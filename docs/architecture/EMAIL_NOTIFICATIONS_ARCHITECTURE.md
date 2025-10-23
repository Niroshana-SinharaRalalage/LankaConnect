# Email & Notifications System Architecture
## Clean Architecture + DDD + CQRS + TDD Design Document

**Project:** LankaConnect MVP
**Version:** 1.0
**Last Updated:** 2025-10-22
**Status:** Architecture Design - Ready for Implementation

---

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Clean Architecture Layer Breakdown](#clean-architecture-layer-breakdown)
3. [Domain Model Design](#domain-model-design)
4. [CQRS Commands & Queries](#cqrs-commands--queries)
5. [Infrastructure Services Design](#infrastructure-services-design)
6. [Integration Flow Diagrams](#integration-flow-diagrams)
7. [TDD Implementation Roadmap](#tdd-implementation-roadmap)
8. [Deployment Architecture](#deployment-architecture)

---

## Executive Summary

### System Purpose
Implement a robust, scalable email verification, password reset, and transactional email system following Clean Architecture, DDD, and CQRS patterns with TDD Zero Tolerance approach.

### Key Design Principles
- **Clean Architecture**: Strict dependency inversion (Domain → Application → Infrastructure → API)
- **Domain-Driven Design**: Rich domain models, value objects, domain events
- **CQRS**: Command Query Responsibility Segregation with MediatR
- **Result Pattern**: All operations return `Result<T>` or `Result` for explicit error handling
- **TDD Zero Tolerance**: No compilation errors at any stage - all tests pass always

### Existing Foundation
- ✅ EmailMessage aggregate (38 passing tests) with state machine
- ✅ User aggregate with email verification/password reset token support
- ✅ Repository interfaces (IEmailMessageRepository, IEmailTemplateRepository)
- ✅ MailHog SMTP configured (localhost:1025, UI: :8025)
- ✅ IEmailService and IEmailTemplateService interfaces defined

---

## Clean Architecture Layer Breakdown

### 1. Domain Layer (`LankaConnect.Domain`)

**Location:** `src/LankaConnect.Domain/Communications/`

#### New Value Objects Needed

```
src/LankaConnect.Domain/Communications/ValueObjects/
├── EmailVerificationToken.cs         (NEW)
├── PasswordResetToken.cs              (NEW)
├── TokenExpiry.cs                     (NEW)
├── TemplateVariable.cs                (NEW)
└── EmailPriority.cs                   (NEW)
```

**EmailVerificationToken** - Value Object
```csharp
public sealed class EmailVerificationToken : ValueObject
{
    public string Token { get; }
    public DateTime ExpiresAt { get; }
    public Guid UserId { get; }

    private EmailVerificationToken(string token, DateTime expiresAt, Guid userId)
    {
        Token = token;
        ExpiresAt = expiresAt;
        UserId = userId;
    }

    public static Result<EmailVerificationToken> Create(Guid userId, int expiryHours = 24)
    {
        if (userId == Guid.Empty)
            return Result<EmailVerificationToken>.Failure("User ID is required");

        if (expiryHours <= 0 || expiryHours > 168) // Max 7 days
            return Result<EmailVerificationToken>.Failure("Expiry hours must be between 1 and 168");

        var token = Guid.NewGuid().ToString("N"); // 32-char hex string (no dashes)
        var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

        return Result<EmailVerificationToken>.Success(
            new EmailVerificationToken(token, expiresAt, userId));
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    public bool IsValid(string tokenToValidate) =>
        !IsExpired() && Token.Equals(tokenToValidate, StringComparison.Ordinal);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Token;
        yield return UserId;
    }
}
```

**PasswordResetToken** - Value Object
```csharp
public sealed class PasswordResetToken : ValueObject
{
    public string Token { get; }
    public DateTime ExpiresAt { get; }
    public Guid UserId { get; }
    public bool IsUsed { get; private set; }

    private PasswordResetToken(string token, DateTime expiresAt, Guid userId)
    {
        Token = token;
        ExpiresAt = expiresAt;
        UserId = userId;
        IsUsed = false;
    }

    public static Result<PasswordResetToken> Create(Guid userId, int expiryHours = 1)
    {
        if (userId == Guid.Empty)
            return Result<PasswordResetToken>.Failure("User ID is required");

        if (expiryHours <= 0 || expiryHours > 24) // Max 24 hours
            return Result<PasswordResetToken>.Failure("Expiry hours must be between 1 and 24");

        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

        return Result<PasswordResetToken>.Success(
            new PasswordResetToken(token, expiresAt, userId));
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    public bool IsValid(string tokenToValidate) =>
        !IsExpired() && !IsUsed && Token.Equals(tokenToValidate, StringComparison.Ordinal);

    public Result MarkAsUsed()
    {
        if (IsUsed)
            return Result.Failure("Token has already been used");
        if (IsExpired())
            return Result.Failure("Token has expired");

        IsUsed = true;
        return Result.Success();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Token;
        yield return UserId;
    }
}
```

**TemplateVariable** - Value Object
```csharp
public sealed class TemplateVariable : ValueObject
{
    public string Name { get; }
    public object Value { get; }

    private TemplateVariable(string name, object value)
    {
        Name = name;
        Value = value;
    }

    public static Result<TemplateVariable> Create(string name, object value)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<TemplateVariable>.Failure("Variable name is required");

        if (!IsValidVariableName(name))
            return Result<TemplateVariable>.Failure("Variable name must be alphanumeric with underscores");

        if (value == null)
            return Result<TemplateVariable>.Failure("Variable value cannot be null");

        return Result<TemplateVariable>.Success(new TemplateVariable(name, value));
    }

    private static bool IsValidVariableName(string name) =>
        Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Value;
    }
}
```

#### New Domain Events Needed

```
src/LankaConnect.Domain/Events/
├── UserRegisteredEvent.cs                      (NEW)
├── EmailVerificationSentEvent.cs               (NEW)
├── EmailVerificationFailedEvent.cs             (NEW)
├── PasswordResetRequestedEvent.cs              (NEW)
├── PasswordResetCompletedEvent.cs              (NEW)
├── TransactionalEmailQueuedEvent.cs            (NEW)
└── EmailTemplateRenderedEvent.cs               (NEW)
```

**UserRegisteredEvent** - Domain Event
```csharp
namespace LankaConnect.Domain.Events;

/// <summary>
/// Raised when a new user completes registration
/// Triggers email verification flow
/// </summary>
public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string FullName,
    DateTime RegisteredAt) : DomainEvent;
```

**EmailVerificationSentEvent** - Domain Event
```csharp
namespace LankaConnect.Domain.Events;

/// <summary>
/// Raised when email verification is successfully sent
/// </summary>
public record EmailVerificationSentEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime ExpiresAt,
    Guid EmailMessageId) : DomainEvent;
```

**PasswordResetRequestedEvent** - Domain Event
```csharp
namespace LankaConnect.Domain.Events;

/// <summary>
/// Raised when user requests password reset
/// Triggers password reset email flow
/// </summary>
public record PasswordResetRequestedEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime ExpiresAt,
    string RequestIpAddress) : DomainEvent;
```

**PasswordResetCompletedEvent** - Domain Event
```csharp
namespace LankaConnect.Domain.Events;

/// <summary>
/// Raised when user successfully resets password
/// </summary>
public record PasswordResetCompletedEvent(
    Guid UserId,
    string Email,
    DateTime ResetAt) : DomainEvent;
```

**TransactionalEmailQueuedEvent** - Domain Event
```csharp
namespace LankaConnect.Domain.Events;

/// <summary>
/// Raised when a transactional email is queued for sending
/// </summary>
public record TransactionalEmailQueuedEvent(
    Guid EmailMessageId,
    string TemplateName,
    string RecipientEmail,
    int Priority,
    DateTime QueuedAt) : DomainEvent;
```

#### Existing Aggregates (No New Aggregates Needed)

**EmailMessage Aggregate** ✅ (Already exists with 38 tests)
- Handles state machine: Pending → Queued → Sending → Sent/Failed
- Contains retry logic, priority, template support

**User Aggregate** ✅ (Already exists)
- Contains: `EmailVerificationToken`, `EmailVerificationTokenExpiresAt`
- Contains: `PasswordResetToken`, `PasswordResetTokenExpiresAt`
- Methods: `SetEmailVerificationToken()`, `SetPasswordResetToken()`, `VerifyEmail()`, `ChangePassword()`

**EmailTemplate Entity** ✅ (Already exists)
- Not an aggregate root, but an entity with validation logic

---

### 2. Application Layer (`LankaConnect.Application`)

**Location:** `src/LankaConnect.Application/Communications/`

#### Interface Placement Decision

**❓ Where should IEmailService live?**
**✅ ANSWER: Application Layer** (`LankaConnect.Application.Common.Interfaces`)

**Rationale:**
- `IEmailService` is an **infrastructure concern** abstracted for application use
- Application layer defines contracts for infrastructure services
- Domain layer should NOT depend on infrastructure abstractions
- Follows Clean Architecture dependency rule: Domain ← Application ← Infrastructure

**❓ Where should IEmailTemplateService live?**
**✅ ANSWER: Application Layer** (`LankaConnect.Application.Common.Interfaces`)

**Rationale:**
- Template rendering is an infrastructure concern (Razor engine, file I/O)
- Application command handlers consume this service
- Domain models (EmailTemplate entity) are data structures, not rendering logic

#### CQRS Commands Structure

```
src/LankaConnect.Application/Communications/Commands/
├── SendEmailVerification/
│   ├── SendEmailVerificationCommand.cs         ✅ (EXISTS)
│   ├── SendEmailVerificationCommandHandler.cs  ✅ (EXISTS)
│   ├── SendEmailVerificationValidator.cs       ✅ (EXISTS)
│   └── SendEmailVerificationResponse.cs        ✅ (EXISTS)
├── VerifyEmail/
│   ├── VerifyEmailCommand.cs                   ✅ (EXISTS)
│   ├── VerifyEmailCommandHandler.cs            ✅ (EXISTS)
│   ├── VerifyEmailValidator.cs                 ✅ (EXISTS)
│   └── VerifyEmailResponse.cs                  ✅ (EXISTS)
├── SendPasswordReset/
│   ├── SendPasswordResetCommand.cs             ✅ (EXISTS)
│   ├── SendPasswordResetCommandHandler.cs      ✅ (EXISTS)
│   ├── SendPasswordResetValidator.cs           ✅ (EXISTS)
│   └── SendPasswordResetResponse.cs            (ENHANCE)
├── ResetPassword/
│   ├── ResetPasswordCommand.cs                 ✅ (EXISTS)
│   ├── ResetPasswordCommandHandler.cs          ✅ (EXISTS)
│   ├── ResetPasswordValidator.cs               ✅ (EXISTS)
│   └── ResetPasswordResponse.cs                (ADD)
├── SendTransactionalEmail/                     (NEW)
│   ├── SendTransactionalEmailCommand.cs        (NEW)
│   ├── SendTransactionalEmailCommandHandler.cs (NEW)
│   ├── SendTransactionalEmailValidator.cs      (NEW)
│   └── SendTransactionalEmailResponse.cs       (NEW)
└── ProcessEmailQueue/                          (NEW)
    ├── ProcessEmailQueueCommand.cs             (NEW)
    ├── ProcessEmailQueueCommandHandler.cs      (NEW)
    └── ProcessEmailQueueResponse.cs            (NEW)
```

#### Command DTOs Design

**SendTransactionalEmailCommand** (NEW)
```csharp
namespace LankaConnect.Application.Communications.Commands.SendTransactionalEmail;

/// <summary>
/// Command to send a transactional email using a template
/// Examples: event reminders, booking confirmations, system notifications
/// </summary>
public record SendTransactionalEmailCommand(
    string TemplateName,
    string RecipientEmail,
    string RecipientName,
    Dictionary<string, object> TemplateVariables,
    int Priority = 5,
    string? ScheduledSendTime = null) : ICommand<SendTransactionalEmailResponse>;

public class SendTransactionalEmailResponse
{
    public Guid EmailMessageId { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string RecipientEmail { get; init; } = string.Empty;
    public EmailStatus Status { get; init; }
    public DateTime QueuedAt { get; init; }
    public DateTime? ScheduledSendTime { get; init; }
}

public class SendTransactionalEmailValidator : AbstractValidator<SendTransactionalEmailCommand>
{
    public SendTransactionalEmailValidator()
    {
        RuleFor(x => x.TemplateName)
            .NotEmpty().WithMessage("Template name is required")
            .MaximumLength(100).WithMessage("Template name too long");

        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("Recipient email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.RecipientName)
            .NotEmpty().WithMessage("Recipient name is required")
            .MaximumLength(200);

        RuleFor(x => x.TemplateVariables)
            .NotNull().WithMessage("Template variables cannot be null");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 10).WithMessage("Priority must be between 1-10");
    }
}
```

**ProcessEmailQueueCommand** (NEW)
```csharp
namespace LankaConnect.Application.Communications.Commands.ProcessEmailQueue;

/// <summary>
/// Background job command to process queued emails
/// Executed by IHostedService or Hangfire/Quartz
/// </summary>
public record ProcessEmailQueueCommand(
    int BatchSize = 50,
    bool IncludeRetries = true) : ICommand<ProcessEmailQueueResponse>;

public class ProcessEmailQueueResponse
{
    public int ProcessedCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public int RetryCount { get; init; }
    public List<string> Errors { get; init; } = new();
    public TimeSpan ProcessingDuration { get; init; }
}
```

#### CQRS Queries Structure

```
src/LankaConnect.Application/Communications/Queries/
├── GetEmailHistory/                            (NEW)
│   ├── GetEmailHistoryQuery.cs                 (NEW)
│   ├── GetEmailHistoryQueryHandler.cs          (NEW)
│   ├── GetEmailHistoryValidator.cs             (NEW)
│   └── GetEmailHistoryResponse.cs              (NEW)
├── GetEmailStatus/
│   ├── GetEmailStatusQuery.cs                  ✅ (EXISTS)
│   ├── GetEmailStatusQueryHandler.cs           ✅ (EXISTS)
│   └── GetEmailStatusResponse.cs               (ENHANCE)
├── SearchEmails/                               (NEW)
│   ├── SearchEmailsQuery.cs                    (NEW)
│   ├── SearchEmailsQueryHandler.cs             (NEW)
│   └── SearchEmailsResponse.cs                 (NEW)
└── GetEmailTemplates/
    ├── GetEmailTemplatesQuery.cs               ✅ (EXISTS)
    ├── GetEmailTemplatesQueryHandler.cs        ✅ (EXISTS)
    └── GetEmailTemplatesResponse.cs            (ENHANCE)
```

#### Query DTOs Design

**GetEmailHistoryQuery** (NEW)
```csharp
namespace LankaConnect.Application.Communications.Queries.GetEmailHistory;

/// <summary>
/// Query to get email history for a specific user
/// </summary>
public record GetEmailHistoryQuery(
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 20,
    EmailType? FilterByType = null,
    EmailStatus? FilterByStatus = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IQuery<GetEmailHistoryResponse>;

public class GetEmailHistoryResponse
{
    public Guid UserId { get; init; }
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public List<EmailHistoryItem> Emails { get; init; } = new();
}

public class EmailHistoryItem
{
    public Guid Id { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string? TemplateName { get; init; }
    public EmailType Type { get; init; }
    public EmailStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public string? ErrorMessage { get; init; }
}

public class GetEmailHistoryValidator : AbstractValidator<GetEmailHistoryQuery>
{
    public GetEmailHistoryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.FromDate)
            .LessThan(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("FromDate must be before ToDate");
    }
}
```

**SearchEmailsQuery** (NEW)
```csharp
namespace LankaConnect.Application.Communications.Queries.SearchEmails;

/// <summary>
/// Query to search emails by various criteria (admin functionality)
/// </summary>
public record SearchEmailsQuery(
    string? EmailAddress = null,
    string? Subject = null,
    EmailType? Type = null,
    EmailStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 50) : IQuery<SearchEmailsResponse>;

public class SearchEmailsResponse
{
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public List<EmailSearchResult> Results { get; init; } = new();
}

public class EmailSearchResult
{
    public Guid Id { get; init; }
    public string FromEmail { get; init; } = string.Empty;
    public string ToEmail { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public EmailType Type { get; init; }
    public EmailStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SentAt { get; init; }
    public int RetryCount { get; init; }
}
```

---

### 3. Infrastructure Layer (`LankaConnect.Infrastructure`)

**Location:** `src/LankaConnect.Infrastructure/Communications/`

#### Services to Implement

```
src/LankaConnect.Infrastructure/Communications/
├── EmailService/
│   ├── SmtpEmailService.cs                     (NEW - implements IEmailService)
│   ├── SmtpSettings.cs                         (NEW - configuration)
│   └── EmailServiceExtensions.cs               (NEW - DI registration)
├── TemplateEngine/
│   ├── RazorTemplateEngine.cs                  (NEW - implements IEmailTemplateService)
│   ├── TemplateLoader.cs                       (NEW - loads templates from disk/db)
│   ├── TemplateCache.cs                        (NEW - caches compiled templates)
│   └── TemplateEngineExtensions.cs             (NEW - DI registration)
├── BackgroundJobs/
│   ├── EmailQueueProcessor.cs                  (NEW - IHostedService)
│   └── EmailQueueProcessorSettings.cs          (NEW - configuration)
└── Templates/                                  (NEW - template files)
    ├── EmailVerification.cshtml                (NEW)
    ├── PasswordReset.cshtml                    (NEW)
    ├── WelcomeEmail.cshtml                     ✅ (EXISTS)
    └── TransactionalBase.cshtml                (NEW - layout)
```

#### IEmailService Implementation

**SmtpEmailService.cs** (NEW)
```csharp
namespace LankaConnect.Infrastructure.Communications.EmailService;

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly IEmailMessageRepository _emailRepository;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<SmtpSettings> settings,
        IEmailMessageRepository emailRepository,
        ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _emailRepository = emailRepository;
        _logger = logger;
    }

    public async Task<Result> SendEmailAsync(
        EmailMessageDto emailMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create MimeMessage
            var mimeMessage = CreateMimeMessage(emailMessage);

            // Send via SMTP
            using var client = new SmtpClient();
            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                SecureSocketOptions.StartTlsWhenAvailable,
                cancellationToken);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password,
                    cancellationToken);
            }

            // Send email
            var messageId = await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully. MessageId: {MessageId}, To: {Recipient}",
                messageId, emailMessage.ToEmail);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email to {Recipient}",
                emailMessage.ToEmail);
            return Result.Failure($"Email sending failed: {ex.Message}");
        }
    }

    public async Task<Result> SendTemplatedEmailAsync(
        string templateName,
        string recipientEmail,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        // This method delegates to IEmailTemplateService for rendering
        // then calls SendEmailAsync with rendered content
        throw new NotImplementedException("Use IEmailTemplateService + SendEmailAsync");
    }

    public async Task<Result<BulkEmailResult>> SendBulkEmailAsync(
        IEnumerable<EmailMessageDto> emailMessages,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkEmailResult { TotalEmails = emailMessages.Count() };

        foreach (var email in emailMessages)
        {
            var sendResult = await SendEmailAsync(email, cancellationToken);
            if (sendResult.IsSuccess)
                result.SuccessfulSends++;
            else
            {
                result.FailedSends++;
                result.Errors.Add($"{email.ToEmail}: {sendResult.Error}");
            }
        }

        return Result<BulkEmailResult>.Success(result);
    }

    public async Task<Result> ValidateTemplateAsync(
        string templateName,
        CancellationToken cancellationToken = default)
    {
        // Validate template exists in database
        var template = await _emailRepository
            .GetByNameAsync(templateName, cancellationToken);

        if (template == null)
            return Result.Failure($"Template '{templateName}' not found");

        if (!template.IsActive)
            return Result.Failure($"Template '{templateName}' is inactive");

        return Result.Success();
    }

    private MimeMessage CreateMimeMessage(EmailMessageDto dto)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            dto.FromName ?? _settings.DefaultFromName,
            dto.FromEmail ?? _settings.DefaultFromEmail));
        message.To.Add(new MailboxAddress(dto.ToName, dto.ToEmail));
        message.Subject = dto.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = dto.HtmlBody,
            TextBody = dto.PlainTextBody ?? StripHtml(dto.HtmlBody)
        };

        // Add attachments if any
        if (dto.Attachments != null)
        {
            foreach (var attachment in dto.Attachments)
            {
                bodyBuilder.Attachments.Add(
                    attachment.FileName,
                    attachment.Content,
                    ContentType.Parse(attachment.ContentType));
            }
        }

        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }

    private string StripHtml(string html)
    {
        // Simple HTML stripping for plain text fallback
        return Regex.Replace(html, "<.*?>", string.Empty);
    }
}

public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string DefaultFromEmail { get; set; } = "noreply@lankaconnect.com";
    public string DefaultFromName { get; set; } = "LankaConnect";
    public bool EnableSsl { get; set; } = false;
}
```

#### IEmailTemplateService Implementation

**RazorTemplateEngine.cs** (NEW)
```csharp
namespace LankaConnect.Infrastructure.Communications.TemplateEngine;

using RazorEngineCore;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

public class RazorTemplateEngine : IEmailTemplateService
{
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly ILogger<RazorTemplateEngine> _logger;
    private readonly TemplateCache _cache;

    public RazorTemplateEngine(
        IEmailTemplateRepository templateRepository,
        TemplateCache cache,
        ILogger<RazorTemplateEngine> logger)
    {
        _templateRepository = templateRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(
        string templateName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get template from database
            var template = await _templateRepository
                .GetByNameAsync(templateName, cancellationToken);

            if (template == null)
                return Result<RenderedEmailTemplate>.Failure(
                    $"Template '{templateName}' not found");

            if (!template.IsActive)
                return Result<RenderedEmailTemplate>.Failure(
                    $"Template '{templateName}' is inactive");

            // Compile and cache template
            var compiledTemplate = await _cache
                .GetOrCompileAsync(template.HtmlTemplate ?? string.Empty);

            // Render HTML body
            var htmlBody = await compiledTemplate
                .RunAsync(parameters);

            // Render subject (simple string replacement for now)
            var subject = RenderSimpleTemplate(
                template.SubjectTemplate.Value,
                parameters);

            // Render plain text (strip HTML or use text template)
            var plainText = !string.IsNullOrEmpty(template.TextTemplate)
                ? RenderSimpleTemplate(template.TextTemplate, parameters)
                : StripHtml(htmlBody);

            return Result<RenderedEmailTemplate>.Success(
                new RenderedEmailTemplate
                {
                    Subject = subject,
                    HtmlBody = htmlBody,
                    PlainTextBody = plainText
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to render template {TemplateName}",
                templateName);
            return Result<RenderedEmailTemplate>.Failure(
                $"Template rendering failed: {ex.Message}");
        }
    }

    public async Task<Result<List<EmailTemplateInfo>>> GetAvailableTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _templateRepository
                .GetTemplatesAsync(
                    isActive: true,
                    cancellationToken: cancellationToken);

            var templateInfos = templates.Select(t => new EmailTemplateInfo
            {
                Name = t.Name,
                DisplayName = t.Name,
                Description = t.Description,
                Category = t.Category.ToString(),
                IsActive = t.IsActive,
                LastModified = t.UpdatedAt
            }).ToList();

            return Result<List<EmailTemplateInfo>>.Success(templateInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available templates");
            return Result<List<EmailTemplateInfo>>.Failure(
                "Failed to retrieve templates");
        }
    }

    public async Task<Result<EmailTemplateInfo>> GetTemplateInfoAsync(
        string templateName,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository
            .GetByNameAsync(templateName, cancellationToken);

        if (template == null)
            return Result<EmailTemplateInfo>.Failure(
                $"Template '{templateName}' not found");

        var info = new EmailTemplateInfo
        {
            Name = template.Name,
            DisplayName = template.Name,
            Description = template.Description,
            Category = template.Category.ToString(),
            IsActive = template.IsActive,
            LastModified = template.UpdatedAt,
            RequiredParameters = ExtractVariables(template.HtmlTemplate ?? string.Empty)
        };

        return Result<EmailTemplateInfo>.Success(info);
    }

    public async Task<Result> ValidateTemplateParametersAsync(
        string templateName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var templateInfo = await GetTemplateInfoAsync(
            templateName,
            cancellationToken);

        if (!templateInfo.IsSuccess)
            return Result.Failure(templateInfo.Error);

        var missingParams = templateInfo.Value.RequiredParameters
            .Where(p => !parameters.ContainsKey(p))
            .ToList();

        if (missingParams.Any())
            return Result.Failure(
                $"Missing required parameters: {string.Join(", ", missingParams)}");

        return Result.Success();
    }

    private string RenderSimpleTemplate(
        string template,
        Dictionary<string, object> parameters)
    {
        var result = template;
        foreach (var param in parameters)
        {
            result = result.Replace(
                $"{{{{{param.Key}}}}}",
                param.Value?.ToString() ?? string.Empty);
        }
        return result;
    }

    private List<string> ExtractVariables(string template)
    {
        var matches = Regex.Matches(template, @"@Model\.(\w+)");
        return matches
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
    }

    private string StripHtml(string html) =>
        Regex.Replace(html, "<.*?>", string.Empty);
}

public class TemplateCache
{
    private readonly ConcurrentDictionary<string, IRazorEngineCompiledTemplate> _cache = new();
    private readonly RazorEngine _engine = new();

    public async Task<IRazorEngineCompiledTemplate> GetOrCompileAsync(string template)
    {
        var hash = ComputeHash(template);

        if (_cache.TryGetValue(hash, out var compiled))
            return compiled;

        compiled = await _engine.CompileAsync(template);
        _cache.TryAdd(hash, compiled);

        return compiled;
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

#### Background Job for Email Queue Processing

**EmailQueueProcessor.cs** (NEW)
```csharp
namespace LankaConnect.Infrastructure.Communications.BackgroundJobs;

public class EmailQueueProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailQueueProcessor> _logger;
    private readonly EmailQueueProcessorSettings _settings;

    public EmailQueueProcessor(
        IServiceProvider serviceProvider,
        IOptions<EmailQueueProcessorSettings> settings,
        ILogger<EmailQueueProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Queue Processor starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueBatch(stoppingToken);
                await Task.Delay(
                    TimeSpan.FromSeconds(_settings.PollingIntervalSeconds),
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email queue");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Email Queue Processor stopped");
    }

    private async Task ProcessQueueBatch(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new ProcessEmailQueueCommand(
            BatchSize: _settings.BatchSize,
            IncludeRetries: true);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess && result.Value.ProcessedCount > 0)
        {
            _logger.LogInformation(
                "Processed {Count} emails: {Success} succeeded, {Failed} failed",
                result.Value.ProcessedCount,
                result.Value.SuccessCount,
                result.Value.FailedCount);
        }
    }
}

public class EmailQueueProcessorSettings
{
    public int BatchSize { get; set; } = 50;
    public int PollingIntervalSeconds { get; set; } = 30;
    public bool Enabled { get; set; } = true;
}
```

---

### 4. API Layer (`LankaConnect.API`)

**Location:** `src/LankaConnect.API/Controllers/`

#### Controllers Needed

```
src/LankaConnect.API/Controllers/
├── EmailController.cs                          (NEW or ENHANCE)
└── AuthController.cs                           ✅ (EXISTS - add email verification endpoints)
```

**EmailController.cs** (NEW)
```csharp
namespace LankaConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get email history for current user
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    [ProducesResponseType(typeof(GetEmailHistoryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmailHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId(); // Extension method to extract user ID from claims

        var query = new GetEmailHistoryQuery(
            UserId: userId,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Get specific email status
    /// </summary>
    [HttpGet("{emailId}/status")]
    [Authorize]
    [ProducesResponseType(typeof(GetEmailStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmailStatus(
        Guid emailId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmailStatusQuery(EmailId: emailId);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    /// <summary>
    /// Resend verification email
    /// </summary>
    [HttpPost("verification/resend")]
    [Authorize]
    [ProducesResponseType(typeof(SendEmailVerificationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendVerificationEmail(
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();

        var command = new SendEmailVerificationCommand(
            UserId: userId,
            ForceResend: true);

        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }
}
```

---

## Integration Flow Diagrams

### Flow 1: User Registration → Email Verification

```
┌─────────────────────────────────────────────────────────────────────┐
│ USER REGISTRATION → EMAIL VERIFICATION FLOW                         │
└─────────────────────────────────────────────────────────────────────┘

1. User submits registration form
   ↓
2. API: RegisterUserCommand → RegisterUserHandler
   ↓
3. DOMAIN: User.Create() → RaiseDomainEvent(UserCreatedEvent)
   ↓
4. APPLICATION: UserCreatedEventHandler listens for UserCreatedEvent
   ↓
5. APPLICATION: Sends SendEmailVerificationCommand
   ↓
6. APPLICATION: SendEmailVerificationCommandHandler
   ├─→ Generate token: EmailVerificationToken.Create(userId, 24 hours)
   ├─→ User.SetEmailVerificationToken(token, expiresAt)
   ├─→ IEmailTemplateService.RenderTemplateAsync("email-verification", params)
   ├─→ Create EmailMessage aggregate (Pending status)
   ├─→ EmailMessage.MarkAsQueued()
   ├─→ IEmailMessageRepository.AddAsync(emailMessage)
   ├─→ IUnitOfWork.CommitAsync()
   └─→ RaiseDomainEvent(EmailVerificationSentEvent)
   ↓
7. INFRASTRUCTURE: EmailQueueProcessor (background job)
   ├─→ Polls: IEmailMessageRepository.GetQueuedEmailsAsync(50)
   ├─→ For each email: EmailMessage.MarkAsSending()
   ├─→ IEmailService.SendEmailAsync(EmailMessageDto)
   ├─→ SmtpEmailService → MailKit → MailHog (localhost:1025)
   ├─→ EmailMessage.MarkAsSent() OR MarkAsFailed(errorMsg, retryAt)
   └─→ IUnitOfWork.CommitAsync()
   ↓
8. User receives email with verification link
   ↓
9. User clicks link: GET /api/auth/verify-email?token={token}&userId={userId}
   ↓
10. API: VerifyEmailCommand → VerifyEmailCommandHandler
   ↓
11. DOMAIN: User.IsEmailVerificationTokenValid(token) → true
   ↓
12. DOMAIN: User.VerifyEmail() → RaiseDomainEvent(UserEmailVerifiedEvent)
   ↓
13. APPLICATION: Commit changes, return success
   ↓
14. User account activated ✓
```

### Flow 2: Password Reset Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│ PASSWORD RESET FLOW                                                 │
└─────────────────────────────────────────────────────────────────────┘

1. User clicks "Forgot Password" → enters email
   ↓
2. API: SendPasswordResetCommand → SendPasswordResetCommandHandler
   ↓
3. APPLICATION:
   ├─→ Find user by email
   ├─→ Generate token: PasswordResetToken.Create(userId, 1 hour)
   ├─→ User.SetPasswordResetToken(token, expiresAt)
   ├─→ IEmailTemplateService.RenderTemplateAsync("password-reset", params)
   ├─→ Create EmailMessage aggregate (Pending → Queued)
   ├─→ IEmailMessageRepository.AddAsync(emailMessage)
   ├─→ IUnitOfWork.CommitAsync()
   └─→ RaiseDomainEvent(PasswordResetRequestedEvent)
   ↓
4. INFRASTRUCTURE: EmailQueueProcessor sends email via SMTP
   ↓
5. User receives email with reset link (expires in 1 hour)
   ↓
6. User clicks link: GET /reset-password?token={token}&userId={userId}
   ↓
7. Frontend displays "Set New Password" form
   ↓
8. User submits new password
   ↓
9. API: ResetPasswordCommand → ResetPasswordCommandHandler
   ↓
10. DOMAIN:
    ├─→ User.IsPasswordResetTokenValid(token) → true
    ├─→ Hash new password
    ├─→ User.ChangePassword(newPasswordHash)
    │   ├─→ Clears PasswordResetToken
    │   ├─→ Resets FailedLoginAttempts
    │   └─→ RaiseDomainEvent(PasswordResetCompletedEvent)
    └─→ IUnitOfWork.CommitAsync()
    ↓
11. Password reset complete ✓
```

### Flow 3: Transactional Email Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│ TRANSACTIONAL EMAIL FLOW (e.g., Event Reminder)                    │
└─────────────────────────────────────────────────────────────────────┘

1. Business logic triggers email (e.g., event created, booking confirmed)
   ↓
2. APPLICATION: Send SendTransactionalEmailCommand
   ↓
3. APPLICATION: SendTransactionalEmailCommandHandler
   ├─→ Validate template exists: IEmailService.ValidateTemplateAsync()
   ├─→ Validate parameters: IEmailTemplateService.ValidateTemplateParametersAsync()
   ├─→ Render template: IEmailTemplateService.RenderTemplateAsync(templateName, vars)
   ├─→ Create EmailMessage aggregate
   │   ├─→ EmailMessage.Create(from, subject, textBody, htmlBody)
   │   ├─→ EmailMessage.AddRecipient(toEmail)
   │   ├─→ EmailMessage.SetPriority(priority)
   │   └─→ EmailMessage.MarkAsQueued()
   ├─→ IEmailMessageRepository.AddAsync(emailMessage)
   ├─→ IUnitOfWork.CommitAsync()
   └─→ RaiseDomainEvent(TransactionalEmailQueuedEvent)
   ↓
4. INFRASTRUCTURE: EmailQueueProcessor (background job)
   ├─→ Polls queue every 30 seconds
   ├─→ Gets batch: IEmailMessageRepository.GetQueuedEmailsAsync(50)
   ├─→ Process each email:
   │   ├─→ EmailMessage.MarkAsSending()
   │   ├─→ IEmailService.SendEmailAsync(dto)
   │   ├─→ SmtpEmailService → MailKit → MailHog/Production SMTP
   │   ├─→ SUCCESS: EmailMessage.MarkAsSent(messageId)
   │   └─→ FAILURE: EmailMessage.MarkAsFailed(error, nextRetryAt)
   └─→ IUnitOfWork.CommitAsync()
   ↓
5. Retry logic (if failed):
   ├─→ EmailQueueProcessor calls GetFailedEmailsForRetryAsync()
   ├─→ For each eligible email:
   │   ├─→ EmailMessage.CanRetry() → true (if retries < MaxRetries)
   │   ├─→ EmailMessage.Retry() → status = Queued
   │   └─→ Re-added to queue
   └─→ Exponential backoff: NextRetryAt = Now + (2^RetryCount * BaseDelay)
   ↓
6. Email delivered ✓
```

---

## TDD Implementation Roadmap

### Zero Tolerance Strategy

**Golden Rule:** Never commit code that doesn't compile. All tests must pass at every stage.

### Phase 1: Domain Layer (Value Objects + Domain Events)

**Duration:** 2-3 hours
**Test Count:** ~15 tests
**Compilation Errors Expected:** 0

#### Steps (RED → GREEN → REFACTOR)

**1.1 Create EmailVerificationToken Value Object**
```bash
# RED: Write test first
tests/LankaConnect.Domain.Tests/Communications/ValueObjects/EmailVerificationTokenTests.cs

# Test cases:
✓ Create_WithValidData_ReturnsSuccess
✓ Create_WithEmptyUserId_ReturnsFailure
✓ Create_WithInvalidExpiryHours_ReturnsFailure
✓ IsExpired_WhenExpired_ReturnsTrue
✓ IsValid_WithCorrectToken_ReturnsTrue
✓ IsValid_WithExpiredToken_ReturnsFalse
```

```bash
# GREEN: Implement EmailVerificationToken
src/LankaConnect.Domain/Communications/ValueObjects/EmailVerificationToken.cs

# Run tests: dotnet test --filter "FullyQualifiedName~EmailVerificationTokenTests"
# All tests pass ✓
```

**1.2 Create PasswordResetToken Value Object**
```bash
# RED: Write test first
tests/LankaConnect.Domain.Tests/Communications/ValueObjects/PasswordResetTokenTests.cs

# Test cases:
✓ Create_WithValidData_ReturnsSuccess
✓ MarkAsUsed_WhenNotUsed_ReturnsSuccess
✓ MarkAsUsed_WhenAlreadyUsed_ReturnsFailure
✓ IsValid_WhenUsed_ReturnsFalse
```

```bash
# GREEN: Implement PasswordResetToken
src/LankaConnect.Domain/Communications/ValueObjects/PasswordResetToken.cs

# Run tests: dotnet test
# All tests pass ✓
```

**1.3 Create Domain Events**
```bash
# GREEN: Implement domain events (no tests needed for simple records)
src/LankaConnect.Domain/Events/UserRegisteredEvent.cs
src/LankaConnect.Domain/Events/EmailVerificationSentEvent.cs
src/LankaConnect.Domain/Events/PasswordResetRequestedEvent.cs
src/LankaConnect.Domain/Events/PasswordResetCompletedEvent.cs
src/LankaConnect.Domain/Events/TransactionalEmailQueuedEvent.cs

# Compile: dotnet build
# No errors ✓
```

**Phase 1 Checkpoint:**
- ✓ All domain value objects created with 100% test coverage
- ✓ All domain events created
- ✓ 15 tests passing
- ✓ 0 compilation errors

---

### Phase 2: Application Layer (Command Handlers) - Part 1

**Duration:** 4-5 hours
**Test Count:** ~25 tests
**Compilation Errors Expected:** 0

#### Steps (RED → GREEN → REFACTOR)

**2.1 Implement SendTransactionalEmailCommand**
```bash
# RED: Write test first
tests/LankaConnect.Application.Tests/Communications/Commands/SendTransactionalEmailCommandTests.cs

# Test cases:
✓ Handle_WithValidCommand_ReturnsSuccess
✓ Handle_WithInvalidTemplateName_ReturnsFailure
✓ Handle_WithMissingTemplateParameters_ReturnsFailure
✓ Handle_CreatesEmailMessageAggregate_WithCorrectStatus
✓ Handle_RaisesTransactionalEmailQueuedEvent
```

```bash
# GREEN: Implement command + handler
src/LankaConnect.Application/Communications/Commands/SendTransactionalEmail/
├── SendTransactionalEmailCommand.cs
├── SendTransactionalEmailCommandHandler.cs (mock IEmailService, IEmailTemplateService)
├── SendTransactionalEmailValidator.cs
└── SendTransactionalEmailResponse.cs

# Run tests with mocks: dotnet test
# All tests pass ✓
```

**2.2 Implement ProcessEmailQueueCommand**
```bash
# RED: Write test first
tests/LankaConnect.Application.Tests/Communications/Commands/ProcessEmailQueueCommandTests.cs

# Test cases:
✓ Handle_ProcessesBatchOfQueuedEmails
✓ Handle_MarksEmailsAsSending_BeforeSending
✓ Handle_MarksEmailsAsSent_OnSuccess
✓ Handle_MarksEmailsAsFailed_OnError_WithRetryTime
✓ Handle_RespectsMaxRetries
```

```bash
# GREEN: Implement command + handler
src/LankaConnect.Application/Communications/Commands/ProcessEmailQueue/
├── ProcessEmailQueueCommand.cs
├── ProcessEmailQueueCommandHandler.cs
└── ProcessEmailQueueResponse.cs

# Run tests: dotnet test
# All tests pass ✓
```

**2.3 Implement GetEmailHistoryQuery**
```bash
# RED: Write test first
tests/LankaConnect.Application.Tests/Communications/Queries/GetEmailHistoryQueryTests.cs

# GREEN: Implement query + handler
src/LankaConnect.Application/Communications/Queries/GetEmailHistory/
├── GetEmailHistoryQuery.cs
├── GetEmailHistoryQueryHandler.cs
├── GetEmailHistoryValidator.cs
└── GetEmailHistoryResponse.cs

# Run tests: dotnet test
# All tests pass ✓
```

**Phase 2 Checkpoint:**
- ✓ All command handlers implemented with mocked dependencies
- ✓ 25 tests passing (15 from Phase 1 + 25 new = 40 total)
- ✓ 0 compilation errors

---

### Phase 3: Infrastructure Layer (Real Implementations)

**Duration:** 6-8 hours
**Test Count:** ~30 integration tests
**Compilation Errors Expected:** 0

#### Steps (RED → GREEN → REFACTOR)

**3.1 Implement SmtpEmailService (with MailHog integration tests)**
```bash
# RED: Write integration test first
tests/LankaConnect.Infrastructure.Tests/Communications/SmtpEmailServiceTests.cs

# Test cases (require MailHog running):
✓ SendEmailAsync_WithValidEmail_SendsSuccessfully
✓ SendEmailAsync_WithInvalidRecipient_ReturnsFailure
✓ SendEmailAsync_WithAttachments_SendsCorrectly
✓ SendBulkEmailAsync_SendsMultipleEmails

# Setup: docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog
```

```bash
# GREEN: Implement SmtpEmailService
src/LankaConnect.Infrastructure/Communications/EmailService/SmtpEmailService.cs

# Install NuGet: MailKit, MimeKit
# Configure appsettings.Development.json:
{
  "SmtpSettings": {
    "Host": "localhost",
    "Port": 1025,
    "DefaultFromEmail": "noreply@lankaconnect.com"
  }
}

# Run integration tests: dotnet test --filter "FullyQualifiedName~SmtpEmailServiceTests"
# All tests pass ✓
```

**3.2 Implement RazorTemplateEngine**
```bash
# RED: Write test first
tests/LankaConnect.Infrastructure.Tests/Communications/RazorTemplateEngineTests.cs

# Test cases:
✓ RenderTemplateAsync_WithValidTemplate_RendersCorrectly
✓ RenderTemplateAsync_WithMissingParameters_ReturnsFailure
✓ RenderTemplateAsync_UsesCache_OnSecondRender
```

```bash
# GREEN: Implement RazorTemplateEngine
src/LankaConnect.Infrastructure/Communications/TemplateEngine/RazorTemplateEngine.cs

# Install NuGet: RazorEngineCore
# Run tests: dotnet test
# All tests pass ✓
```

**3.3 Implement EmailQueueProcessor (Background Service)**
```bash
# RED: Write test first
tests/LankaConnect.Infrastructure.Tests/Communications/EmailQueueProcessorTests.cs

# Test cases:
✓ ExecuteAsync_ProcessesQueuePeriodically
✓ ExecuteAsync_HandlesExceptionsGracefully
```

```bash
# GREEN: Implement EmailQueueProcessor
src/LankaConnect.Infrastructure/Communications/BackgroundJobs/EmailQueueProcessor.cs

# Register in Program.cs:
builder.Services.AddHostedService<EmailQueueProcessor>();

# Run tests: dotnet test
# All tests pass ✓
```

**Phase 3 Checkpoint:**
- ✓ All infrastructure services implemented with real dependencies
- ✓ 30 integration tests passing (70 total)
- ✓ 0 compilation errors
- ✓ MailHog integration verified

---

### Phase 4: API Layer (Controllers + End-to-End Tests)

**Duration:** 3-4 hours
**Test Count:** ~20 E2E tests
**Compilation Errors Expected:** 0

#### Steps (RED → GREEN → REFACTOR)

**4.1 Implement EmailController**
```bash
# RED: Write E2E test first
tests/LankaConnect.API.Tests/Controllers/EmailControllerTests.cs

# Test cases (use WebApplicationFactory):
✓ GetEmailHistory_ReturnsUserEmails
✓ GetEmailStatus_ReturnsCorrectStatus
✓ ResendVerificationEmail_SendsEmail
```

```bash
# GREEN: Implement EmailController
src/LankaConnect.API/Controllers/EmailController.cs

# Run E2E tests: dotnet test
# All tests pass ✓
```

**4.2 Enhance AuthController (Email Verification Endpoints)**
```bash
# RED: Write E2E test first
tests/LankaConnect.API.Tests/Controllers/AuthControllerTests.cs

# Test cases:
✓ Register_SendsVerificationEmail
✓ VerifyEmail_WithValidToken_ActivatesAccount
✓ VerifyEmail_WithExpiredToken_ReturnsError
```

```bash
# GREEN: Enhance AuthController
src/LankaConnect.API/Controllers/AuthController.cs

# Add endpoints:
POST /api/auth/verify-email
POST /api/auth/resend-verification

# Run E2E tests: dotnet test
# All tests pass ✓
```

**Phase 4 Checkpoint:**
- ✓ All API endpoints implemented
- ✓ 20 E2E tests passing (90 total)
- ✓ 0 compilation errors
- ✓ Full integration verified

---

### Final Phase: Template Files + Documentation

**Duration:** 2-3 hours
**Test Count:** N/A
**Compilation Errors Expected:** 0

#### Steps

**5.1 Create Email Templates**
```bash
# Create Razor templates
src/LankaConnect.Infrastructure/Communications/Templates/
├── EmailVerification.cshtml
├── PasswordReset.cshtml
├── TransactionalBase.cshtml (layout)
└── WelcomeEmail.cshtml (already exists)

# Seed templates to database via migration
migrations/SeedEmailTemplates.cs
```

**5.2 Update Configuration**
```bash
# Update appsettings.json
{
  "SmtpSettings": {
    "Host": "localhost",
    "Port": 1025
  },
  "EmailQueueProcessorSettings": {
    "BatchSize": 50,
    "PollingIntervalSeconds": 30,
    "Enabled": true
  }
}
```

**5.3 Final Integration Test**
```bash
# Run full test suite
dotnet test

# Expected results:
✓ Domain tests: 15 passing
✓ Application tests: 25 passing
✓ Infrastructure tests: 30 passing
✓ API tests: 20 passing
✓ Total: 90 tests passing
✓ 0 compilation errors
```

---

## Deployment Architecture

### Development Environment
```
┌─────────────────────────────────────────────────────────────┐
│ Local Development                                           │
├─────────────────────────────────────────────────────────────┤
│ ASP.NET Core API (localhost:5000)                           │
│   ↓ sends to                                                │
│ MailHog SMTP (localhost:1025)                               │
│   ↓ view at                                                 │
│ MailHog UI (localhost:8025)                                 │
└─────────────────────────────────────────────────────────────┘
```

### Production Environment
```
┌─────────────────────────────────────────────────────────────┐
│ Production                                                  │
├─────────────────────────────────────────────────────────────┤
│ ASP.NET Core API (Azure App Service)                        │
│   ↓ sends to                                                │
│ Azure Communication Services / SendGrid SMTP                │
│   ↓ delivers to                                             │
│ User's Email Inbox                                          │
└─────────────────────────────────────────────────────────────┘
```

---

## Summary

### What We Built

✅ **Domain Layer:**
- 3 new value objects (EmailVerificationToken, PasswordResetToken, TemplateVariable)
- 5 new domain events (UserRegistered, EmailVerificationSent, PasswordResetRequested, etc.)
- Reused existing aggregates (EmailMessage, User, EmailTemplate)

✅ **Application Layer:**
- 2 new commands (SendTransactionalEmail, ProcessEmailQueue)
- 2 new queries (GetEmailHistory, SearchEmails)
- Enhanced existing commands (SendEmailVerification, ResetPassword)
- All interfaces in Application.Common.Interfaces

✅ **Infrastructure Layer:**
- SmtpEmailService (MailKit integration)
- RazorTemplateEngine (RazorEngineCore)
- EmailQueueProcessor (IHostedService background job)
- Template cache for performance

✅ **API Layer:**
- New EmailController (email history, status, resend)
- Enhanced AuthController (verification endpoints)

### Key Architectural Decisions

1. **No New Aggregates:** Reuse EmailMessage and User aggregates
2. **Interface Placement:** IEmailService and IEmailTemplateService in Application layer
3. **Template Storage:** Hybrid approach - files in Infrastructure, metadata in database
4. **Queue Processing:** Background IHostedService polling every 30 seconds
5. **Retry Strategy:** Exponential backoff with max 3 retries
6. **Token Security:** GUID-based tokens, 24h expiry for email verification, 1h for password reset

### Test Coverage

- **Domain:** 15 tests (value objects, token validation)
- **Application:** 25 tests (command handlers with mocks)
- **Infrastructure:** 30 tests (integration tests with MailHog)
- **API:** 20 tests (E2E tests with WebApplicationFactory)
- **Total:** 90 tests with 100% compilation success

### Next Steps

1. Run Phase 1 implementation (Domain layer value objects)
2. Verify all tests pass before moving to Phase 2
3. Implement phases sequentially with Zero Tolerance
4. Deploy to staging with MailHog for testing
5. Switch to production SMTP (SendGrid/Azure Communication Services) for production

---

**Architecture Approved By:** System Architect
**Ready for Implementation:** YES ✓
**Estimated Implementation Time:** 15-20 hours (with TDD)
**Risk Level:** LOW (leveraging existing foundation)
