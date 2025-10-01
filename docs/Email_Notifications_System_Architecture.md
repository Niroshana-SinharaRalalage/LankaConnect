# Email & Notifications System Architecture
## LankaConnect - Clean Architecture Design Document

### Executive Summary

This document outlines the comprehensive Email & Notifications System architecture for LankaConnect, designed to integrate seamlessly with the existing Clean Architecture .NET 8 application. The system follows Domain-Driven Design (DDD) principles, uses CQRS with MediatR, and maintains the existing Result pattern for error handling.

### System Overview

The Email & Notifications System is designed as a cross-cutting concern that integrates with the existing User domain model and authentication system. It provides:

- **Email Verification Flow**: Secure user email verification with tokens
- **Password Reset System**: Token-based password reset workflow  
- **Template-Based Emails**: Localized HTML/text templates with Razor engine
- **Domain Event Integration**: Automatic email triggers via domain events
- **Production-Ready**: Scalable from MailHog (dev) to SendGrid (prod)

## 1. Domain Layer Design

### 1.1 Core Email Interfaces

```csharp
// src/LankaConnect.Domain/Email/IEmailService.cs
public interface IEmailService
{
    Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task<Result> SendBulkAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);
    Task<Result> SendTemplatedEmailAsync(TemplatedEmailRequest request, CancellationToken cancellationToken = default);
}

// src/LankaConnect.Domain/Email/IEmailTemplateService.cs
public interface IEmailTemplateService
{
    Task<Result<string>> RenderTemplateAsync<TModel>(string templateName, TModel model, CultureInfo? culture = null, CancellationToken cancellationToken = default);
    Task<Result<EmailTemplate>> GetTemplateAsync(string templateName, EmailFormat format, CultureInfo? culture = null, CancellationToken cancellationToken = default);
}

// src/LankaConnect.Domain/Email/IEmailDeliveryService.cs
public interface IEmailDeliveryService
{
    Task<Result<EmailDeliveryResult>> DeliverAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task<Result<BulkEmailDeliveryResult>> DeliverBulkAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);
}
```

### 1.2 Value Objects

```csharp
// src/LankaConnect.Domain/Email/ValueObjects/EmailAddress.cs
public class EmailAddress : ValueObject
{
    public string Address { get; }
    public string? DisplayName { get; }

    private EmailAddress(string address, string? displayName = null)
    {
        Address = address;
        DisplayName = displayName;
    }

    public static Result<EmailAddress> Create(string address, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(address))
            return Result<EmailAddress>.Failure("Email address is required");

        if (!IsValidEmail(address))
            return Result<EmailAddress>.Failure("Invalid email address format");

        return Result<EmailAddress>.Success(new EmailAddress(address.ToLowerInvariant(), displayName?.Trim()));
    }

    public override string ToString() => DisplayName != null ? $"{DisplayName} <{Address}>" : Address;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Address;
        yield return DisplayName ?? string.Empty;
    }
}

// src/LankaConnect.Domain/Email/ValueObjects/EmailContent.cs
public class EmailContent : ValueObject
{
    public string Subject { get; }
    public string? HtmlBody { get; }
    public string? TextBody { get; }
    public EmailPriority Priority { get; }

    private EmailContent(string subject, string? htmlBody, string? textBody, EmailPriority priority)
    {
        Subject = subject;
        HtmlBody = htmlBody;
        TextBody = textBody;
        Priority = priority;
    }

    public static Result<EmailContent> Create(string subject, string? htmlBody = null, string? textBody = null, EmailPriority priority = EmailPriority.Normal)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return Result<EmailContent>.Failure("Email subject is required");

        if (string.IsNullOrWhiteSpace(htmlBody) && string.IsNullOrWhiteSpace(textBody))
            return Result<EmailContent>.Failure("Email must have either HTML or text content");

        return Result<EmailContent>.Success(new EmailContent(subject.Trim(), htmlBody?.Trim(), textBody?.Trim(), priority));
    }
}

// src/LankaConnect.Domain/Email/ValueObjects/EmailAttachment.cs
public class EmailAttachment : ValueObject
{
    public string FileName { get; }
    public string ContentType { get; }
    public byte[] Content { get; }
    public long Size => Content.Length;

    private EmailAttachment(string fileName, string contentType, byte[] content)
    {
        FileName = fileName;
        ContentType = contentType;
        Content = content;
    }

    public static Result<EmailAttachment> Create(string fileName, string contentType, byte[] content)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result<EmailAttachment>.Failure("File name is required");

        if (string.IsNullOrWhiteSpace(contentType))
            return Result<EmailAttachment>.Failure("Content type is required");

        if (content == null || content.Length == 0)
            return Result<EmailAttachment>.Failure("Content is required");

        const int maxSize = 10 * 1024 * 1024; // 10MB
        if (content.Length > maxSize)
            return Result<EmailAttachment>.Failure($"Attachment size cannot exceed {maxSize / (1024 * 1024)}MB");

        return Result<EmailAttachment>.Success(new EmailAttachment(fileName, contentType, content));
    }
}
```

### 1.3 Domain Entities

```csharp
// src/LankaConnect.Domain/Email/EmailMessage.cs
public class EmailMessage : BaseEntity
{
    public EmailAddress From { get; private set; }
    public EmailAddress To { get; private set; }
    public EmailContent Content { get; private set; }
    public EmailMessageStatus Status { get; private set; }
    public EmailMessageType Type { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? NextRetryAt { get; private set; }

    private readonly List<EmailAddress> _ccRecipients = new();
    private readonly List<EmailAddress> _bccRecipients = new();
    private readonly List<EmailAttachment> _attachments = new();

    public IReadOnlyList<EmailAddress> CcRecipients => _ccRecipients.AsReadOnly();
    public IReadOnlyList<EmailAddress> BccRecipients => _bccRecipients.AsReadOnly();
    public IReadOnlyList<EmailAttachment> Attachments => _attachments.AsReadOnly();

    private EmailMessage() 
    {
        From = null!;
        To = null!;
        Content = null!;
    }

    private EmailMessage(EmailAddress from, EmailAddress to, EmailContent content, EmailMessageType type, string? correlationId = null)
    {
        From = from;
        To = to;
        Content = content;
        Type = type;
        Status = EmailMessageStatus.Pending;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        RetryCount = 0;
    }

    public static Result<EmailMessage> Create(EmailAddress from, EmailAddress to, EmailContent content, EmailMessageType type, string? correlationId = null)
    {
        return Result<EmailMessage>.Success(new EmailMessage(from, to, content, type, correlationId));
    }

    public Result AddCcRecipient(EmailAddress recipient)
    {
        if (_ccRecipients.Any(cc => cc.Address == recipient.Address))
            return Result.Failure("Recipient already exists in CC list");

        _ccRecipients.Add(recipient);
        MarkAsUpdated();
        return Result.Success();
    }

    public Result MarkAsSent()
    {
        if (Status != EmailMessageStatus.Pending)
            return Result.Failure("Email can only be marked as sent from pending status");

        Status = EmailMessageStatus.Sent;
        SentAt = DateTime.UtcNow;
        MarkAsUpdated();

        RaiseDomainEvent(new EmailSentEvent(Id, To.Address, Content.Subject, Type, CorrelationId));
        return Result.Success();
    }

    public Result MarkAsDelivered()
    {
        if (Status != EmailMessageStatus.Sent)
            return Result.Failure("Email must be sent before marking as delivered");

        Status = EmailMessageStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        ErrorMessage = null;
        MarkAsUpdated();

        RaiseDomainEvent(new EmailDeliveredEvent(Id, To.Address, Content.Subject, Type));
        return Result.Success();
    }

    public Result MarkAsFailed(string errorMessage)
    {
        Status = EmailMessageStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        
        // Exponential backoff: 1min, 5min, 15min, 30min, 1hr
        var delayMinutes = RetryCount switch
        {
            1 => 1,
            2 => 5,
            3 => 15,
            4 => 30,
            _ => 60
        };
        
        NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
        MarkAsUpdated();

        RaiseDomainEvent(new EmailFailedEvent(Id, To.Address, Content.Subject, errorMessage, RetryCount));
        return Result.Success();
    }
}

// src/LankaConnect.Domain/Email/EmailTemplate.cs
public class EmailTemplate : BaseEntity
{
    public string Name { get; private set; }
    public string DisplayName { get; private set; }
    public string Description { get; private set; }
    public EmailTemplateCategory Category { get; private set; }
    public EmailFormat Format { get; private set; }
    public string Content { get; private set; }
    public string? Subject { get; private set; }
    public CultureInfo Culture { get; private set; }
    public bool IsActive { get; private set; }
    public string? PreviewData { get; private set; }

    private EmailTemplate() 
    {
        Name = null!;
        DisplayName = null!;
        Description = null!;
        Content = null!;
        Culture = null!;
    }

    private EmailTemplate(string name, string displayName, string description, 
        EmailTemplateCategory category, EmailFormat format, string content, 
        CultureInfo culture, string? subject = null)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
        Category = category;
        Format = format;
        Content = content;
        Subject = subject;
        Culture = culture;
        IsActive = true;
    }

    public static Result<EmailTemplate> Create(string name, string displayName, string description,
        EmailTemplateCategory category, EmailFormat format, string content, 
        CultureInfo culture, string? subject = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<EmailTemplate>.Failure("Template name is required");

        if (string.IsNullOrWhiteSpace(content))
            return Result<EmailTemplate>.Failure("Template content is required");

        return Result<EmailTemplate>.Success(new EmailTemplate(name, displayName, description, category, format, content, culture, subject));
    }
}
```

### 1.4 Domain Enums

```csharp
// src/LankaConnect.Domain/Email/Enums/EmailMessageStatus.cs
public enum EmailMessageStatus
{
    Pending = 0,
    Sent = 1,
    Delivered = 2,
    Failed = 3,
    Cancelled = 4
}

// src/LankaConnect.Domain/Email/Enums/EmailMessageType.cs
public enum EmailMessageType
{
    Welcome = 0,
    EmailVerification = 1,
    PasswordReset = 2,
    BusinessNotification = 3,
    EventNotification = 4,
    SystemNotification = 5,
    Marketing = 6
}

// src/LankaConnect.Domain/Email/Enums/EmailPriority.cs
public enum EmailPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

// src/LankaConnect.Domain/Email/Enums/EmailFormat.cs
public enum EmailFormat
{
    Text = 0,
    Html = 1
}

// src/LankaConnect.Domain/Email/Enums/EmailTemplateCategory.cs
public enum EmailTemplateCategory
{
    Authentication = 0,
    Notification = 1,
    Marketing = 2,
    System = 3,
    Business = 4,
    Event = 5
}
```

### 1.5 Domain Events

```csharp
// src/LankaConnect.Domain/Events/EmailSentEvent.cs
public record EmailSentEvent(
    Guid EmailId, 
    string RecipientEmail, 
    string Subject, 
    EmailMessageType Type,
    string? CorrelationId) : DomainEvent;

// src/LankaConnect.Domain/Events/EmailDeliveredEvent.cs
public record EmailDeliveredEvent(
    Guid EmailId, 
    string RecipientEmail, 
    string Subject, 
    EmailMessageType Type) : DomainEvent;

// src/LankaConnect.Domain/Events/EmailFailedEvent.cs
public record EmailFailedEvent(
    Guid EmailId, 
    string RecipientEmail, 
    string Subject, 
    string ErrorMessage, 
    int RetryCount) : DomainEvent;
```

## 2. Application Layer Design

### 2.1 Commands and Queries

```csharp
// src/LankaConnect.Application/Email/Commands/SendEmail/SendEmailCommand.cs
public record SendEmailCommand(
    string ToEmail,
    string? ToDisplayName,
    string Subject,
    string? HtmlBody,
    string? TextBody,
    EmailMessageType Type,
    EmailPriority Priority = EmailPriority.Normal,
    string? CorrelationId = null,
    List<string>? CcEmails = null,
    List<string>? BccEmails = null
) : IRequest<Result<Guid>>;

// src/LankaConnect.Application/Email/Commands/SendTemplatedEmail/SendTemplatedEmailCommand.cs
public record SendTemplatedEmailCommand(
    string ToEmail,
    string? ToDisplayName,
    string TemplateName,
    object TemplateData,
    EmailMessageType Type,
    string? CultureCode = null,
    EmailPriority Priority = EmailPriority.Normal,
    string? CorrelationId = null
) : IRequest<Result<Guid>>;

// src/LankaConnect.Application/Email/Commands/SendWelcomeEmail/SendWelcomeEmailCommand.cs
public record SendWelcomeEmailCommand(
    Guid UserId,
    string Email,
    string FullName,
    string? CultureCode = null
) : IRequest<Result>;

// src/LankaConnect.Application/Email/Commands/SendEmailVerification/SendEmailVerificationCommand.cs
public record SendEmailVerificationCommand(
    Guid UserId,
    string Email,
    string FullName,
    string VerificationToken,
    string? CultureCode = null
) : IRequest<Result>;

// src/LankaConnect.Application/Email/Commands/SendPasswordReset/SendPasswordResetCommand.cs
public record SendPasswordResetCommand(
    Guid UserId,
    string Email,
    string FullName,
    string ResetToken,
    string? CultureCode = null
) : IRequest<Result>;

// src/LankaConnect.Application/Email/Queries/GetEmailStatus/GetEmailStatusQuery.cs
public record GetEmailStatusQuery(Guid EmailId) : IRequest<Result<EmailStatusResponse>>;

// src/LankaConnect.Application/Email/Queries/GetEmailHistory/GetEmailHistoryQuery.cs
public record GetEmailHistoryQuery(
    string? RecipientEmail = null,
    EmailMessageType? Type = null,
    EmailMessageStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResponse<EmailHistoryResponse>>>;
```

### 2.2 Command Handlers

```csharp
// src/LankaConnect.Application/Email/Commands/SendEmail/SendEmailCommandHandler.cs
public class SendEmailCommandHandler : IRequestHandler<SendEmailCommand, Result<Guid>>
{
    private readonly IEmailService _emailService;
    private readonly IEmailRepository _emailRepository;
    private readonly ILogger<SendEmailCommandHandler> _logger;

    public SendEmailCommandHandler(
        IEmailService emailService,
        IEmailRepository emailRepository,
        ILogger<SendEmailCommandHandler> logger)
    {
        _emailService = emailService;
        _emailRepository = emailRepository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending email to {Email} with subject {Subject}", request.ToEmail, request.Subject);

        // Create email addresses
        var fromResult = EmailAddress.Create("noreply@lankaconnect.com", "LankaConnect");
        if (fromResult.IsFailure)
            return Result<Guid>.Failure(fromResult.Errors);

        var toResult = EmailAddress.Create(request.ToEmail, request.ToDisplayName);
        if (toResult.IsFailure)
            return Result<Guid>.Failure(toResult.Errors);

        // Create email content
        var contentResult = EmailContent.Create(request.Subject, request.HtmlBody, request.TextBody, request.Priority);
        if (contentResult.IsFailure)
            return Result<Guid>.Failure(contentResult.Errors);

        // Create email message
        var messageResult = EmailMessage.Create(fromResult.Value, toResult.Value, contentResult.Value, request.Type, request.CorrelationId);
        if (messageResult.IsFailure)
            return Result<Guid>.Failure(messageResult.Errors);

        var message = messageResult.Value;

        // Add CC recipients
        if (request.CcEmails != null)
        {
            foreach (var ccEmail in request.CcEmails)
            {
                var ccResult = EmailAddress.Create(ccEmail);
                if (ccResult.IsSuccess)
                {
                    message.AddCcRecipient(ccResult.Value);
                }
            }
        }

        // Save to database
        await _emailRepository.AddAsync(message, cancellationToken);

        // Send email
        var sendResult = await _emailService.SendAsync(message, cancellationToken);
        if (sendResult.IsFailure)
        {
            _logger.LogError("Failed to send email to {Email}: {Error}", request.ToEmail, sendResult.Error);
            return Result<Guid>.Failure(sendResult.Errors);
        }

        _logger.LogInformation("Email sent successfully to {Email} with ID {EmailId}", request.ToEmail, message.Id);
        return Result<Guid>.Success(message.Id);
    }
}

// src/LankaConnect.Application/Email/Commands/SendTemplatedEmail/SendTemplatedEmailCommandHandler.cs
public class SendTemplatedEmailCommandHandler : IRequestHandler<SendTemplatedEmailCommand, Result<Guid>>
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _templateService;
    private readonly IEmailRepository _emailRepository;
    private readonly ILogger<SendTemplatedEmailCommandHandler> _logger;

    public SendTemplatedEmailCommandHandler(
        IEmailService emailService,
        IEmailTemplateService templateService,
        IEmailRepository emailRepository,
        ILogger<SendTemplatedEmailCommandHandler> logger)
    {
        _emailService = emailService;
        _templateService = templateService;
        _emailRepository = emailRepository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SendTemplatedEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending templated email to {Email} using template {Template}", request.ToEmail, request.TemplateName);

        var culture = !string.IsNullOrWhiteSpace(request.CultureCode) 
            ? new CultureInfo(request.CultureCode) 
            : CultureInfo.CurrentCulture;

        // Render HTML template
        var htmlResult = await _templateService.RenderTemplateAsync(
            $"{request.TemplateName}.html", 
            request.TemplateData, 
            culture, 
            cancellationToken);

        // Render Text template (fallback)
        var textResult = await _templateService.RenderTemplateAsync(
            $"{request.TemplateName}.txt", 
            request.TemplateData, 
            culture, 
            cancellationToken);

        // Get subject from template or use default
        var subjectResult = await _templateService.RenderTemplateAsync(
            $"{request.TemplateName}.subject", 
            request.TemplateData, 
            culture, 
            cancellationToken);

        var subject = subjectResult.IsSuccess ? subjectResult.Value : "LankaConnect Notification";

        // Send the email
        var sendCommand = new SendEmailCommand(
            request.ToEmail,
            request.ToDisplayName,
            subject,
            htmlResult.IsSuccess ? htmlResult.Value : null,
            textResult.IsSuccess ? textResult.Value : null,
            request.Type,
            request.Priority,
            request.CorrelationId);

        // Delegate to SendEmailCommandHandler
        var handler = new SendEmailCommandHandler(_emailService, _emailRepository, _logger);
        return await handler.Handle(sendCommand, cancellationToken);
    }
}
```

### 2.3 Domain Event Handlers

```csharp
// src/LankaConnect.Application/Email/EventHandlers/UserCreatedEventHandler.cs
public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(IMediator mediator, ILogger<UserCreatedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending welcome email to new user {UserId} - {Email}", notification.UserId, notification.Email);

        var command = new SendWelcomeEmailCommand(
            notification.UserId,
            notification.Email,
            notification.FullName);

        await _mediator.Send(command, cancellationToken);
    }
}

// src/LankaConnect.Application/Email/EventHandlers/UserPasswordChangedEventHandler.cs
public class UserPasswordChangedEventHandler : INotificationHandler<UserPasswordChangedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserPasswordChangedEventHandler> _logger;

    public UserPasswordChangedEventHandler(IMediator mediator, ILogger<UserPasswordChangedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(UserPasswordChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending password change notification to user {Email}", notification.Email);

        var command = new SendTemplatedEmailCommand(
            notification.Email,
            null,
            "PasswordChanged",
            new { Email = notification.Email, ChangeDate = DateTime.UtcNow },
            EmailMessageType.SystemNotification);

        await _mediator.Send(command, cancellationToken);
    }
}
```

### 2.4 DTOs and Responses

```csharp
// src/LankaConnect.Application/Email/DTOs/EmailStatusResponse.cs
public record EmailStatusResponse(
    Guid Id,
    string ToEmail,
    string Subject,
    EmailMessageStatus Status,
    EmailMessageType Type,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? DeliveredAt,
    string? ErrorMessage,
    int RetryCount
);

// src/LankaConnect.Application/Email/DTOs/EmailHistoryResponse.cs
public record EmailHistoryResponse(
    Guid Id,
    string ToEmail,
    string Subject,
    EmailMessageStatus Status,
    EmailMessageType Type,
    DateTime CreatedAt,
    DateTime? SentAt,
    string? ErrorMessage
);

// src/LankaConnect.Application/Email/DTOs/TemplatedEmailRequest.cs
public record TemplatedEmailRequest(
    EmailAddress To,
    string TemplateName,
    object TemplateData,
    EmailMessageType Type,
    CultureInfo? Culture = null,
    EmailPriority Priority = EmailPriority.Normal,
    string? CorrelationId = null,
    List<EmailAddress>? CcRecipients = null
);
```

## 3. Infrastructure Implementation Plan

### 3.1 Email Service Implementation

```csharp
// src/LankaConnect.Infrastructure/Email/EmailService.cs
public class EmailService : IEmailService
{
    private readonly IEmailDeliveryService _deliveryService;
    private readonly IEmailRepository _repository;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailDeliveryService deliveryService,
        IEmailRepository repository,
        ILogger<EmailService> logger)
    {
        _deliveryService = deliveryService;
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Deliver the email
            var deliveryResult = await _deliveryService.DeliverAsync(message, cancellationToken);
            
            if (deliveryResult.IsSuccess)
            {
                // Mark as sent and update database
                var markSentResult = message.MarkAsSent();
                if (markSentResult.IsSuccess)
                {
                    _repository.Update(message);
                    _logger.LogInformation("Email {EmailId} sent successfully to {Email}", message.Id, message.To.Address);
                }
                return markSentResult;
            }
            else
            {
                // Mark as failed and update database
                var markFailedResult = message.MarkAsFailed(deliveryResult.Error);
                if (markFailedResult.IsSuccess)
                {
                    _repository.Update(message);
                }
                
                _logger.LogError("Failed to send email {EmailId} to {Email}: {Error}", 
                    message.Id, message.To.Address, deliveryResult.Error);
                
                return deliveryResult;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending email {EmailId}", message.Id);
            var failResult = message.MarkAsFailed(ex.Message);
            if (failResult.IsSuccess)
            {
                _repository.Update(message);
            }
            return Result.Failure("An unexpected error occurred while sending email");
        }
    }
}

// src/LankaConnect.Infrastructure/Email/SmtpEmailDeliveryService.cs
public class SmtpEmailDeliveryService : IEmailDeliveryService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailDeliveryService> _logger;

    public SmtpEmailDeliveryService(IOptions<EmailSettings> settings, ILogger<SmtpEmailDeliveryService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result<EmailDeliveryResult>> DeliverAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort);
            
            // Configure authentication if provided
            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
            }
            
            client.EnableSsl = _settings.EnableSsl;

            using var mailMessage = CreateMailMessage(message);
            
            await client.SendMailAsync(mailMessage, cancellationToken);
            
            _logger.LogInformation("Email delivered successfully via SMTP to {Email}", message.To.Address);
            
            return Result<EmailDeliveryResult>.Success(new EmailDeliveryResult
            {
                IsSuccess = true,
                DeliveredAt = DateTime.UtcNow,
                Provider = "SMTP"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver email via SMTP to {Email}", message.To.Address);
            return Result<EmailDeliveryResult>.Failure($"SMTP delivery failed: {ex.Message}");
        }
    }

    private MailMessage CreateMailMessage(EmailMessage message)
    {
        var mailMessage = new MailMessage();
        
        // Set sender
        mailMessage.From = new MailAddress(
            string.IsNullOrWhiteSpace(_settings.SenderEmail) ? message.From.Address : _settings.SenderEmail,
            string.IsNullOrWhiteSpace(_settings.SenderName) ? message.From.DisplayName : _settings.SenderName);
        
        // Set recipient
        mailMessage.To.Add(new MailAddress(message.To.Address, message.To.DisplayName));
        
        // Add CC recipients
        foreach (var cc in message.CcRecipients)
        {
            mailMessage.CC.Add(new MailAddress(cc.Address, cc.DisplayName));
        }
        
        // Add BCC recipients
        foreach (var bcc in message.BccRecipients)
        {
            mailMessage.Bcc.Add(new MailAddress(bcc.Address, bcc.DisplayName));
        }
        
        // Set content
        mailMessage.Subject = message.Content.Subject;
        mailMessage.Priority = message.Content.Priority switch
        {
            EmailPriority.Low => MailPriority.Low,
            EmailPriority.High => MailPriority.High,
            EmailPriority.Critical => MailPriority.High,
            _ => MailPriority.Normal
        };
        
        // Set body content
        if (!string.IsNullOrWhiteSpace(message.Content.HtmlBody))
        {
            mailMessage.Body = message.Content.HtmlBody;
            mailMessage.IsBodyHtml = true;
            
            // Add text alternative if available
            if (!string.IsNullOrWhiteSpace(message.Content.TextBody))
            {
                var textView = AlternateView.CreateAlternateViewFromString(message.Content.TextBody, null, "text/plain");
                mailMessage.AlternateViews.Add(textView);
            }
        }
        else if (!string.IsNullOrWhiteSpace(message.Content.TextBody))
        {
            mailMessage.Body = message.Content.TextBody;
            mailMessage.IsBodyHtml = false;
        }
        
        // Add attachments
        foreach (var attachment in message.Attachments)
        {
            var mailAttachment = new Attachment(new MemoryStream(attachment.Content), attachment.FileName, attachment.ContentType);
            mailMessage.Attachments.Add(mailAttachment);
        }
        
        return mailMessage;
    }
}
```

### 3.2 Template Service Implementation

```csharp
// src/LankaConnect.Infrastructure/Email/RazorEmailTemplateService.cs
public class RazorEmailTemplateService : IEmailTemplateService
{
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IRazorViewEngine _viewEngine;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RazorEmailTemplateService> _logger;

    public RazorEmailTemplateService(
        IEmailTemplateRepository templateRepository,
        IRazorViewEngine viewEngine,
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ILogger<RazorEmailTemplateService> logger)
    {
        _templateRepository = templateRepository;
        _viewEngine = viewEngine;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<string>> RenderTemplateAsync<TModel>(
        string templateName, 
        TModel model, 
        CultureInfo? culture = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            culture ??= CultureInfo.CurrentCulture;
            var cacheKey = $"email_template_{templateName}_{culture.Name}";

            // Try to get template from cache first
            if (!_cache.TryGetValue(cacheKey, out EmailTemplate? template))
            {
                // Get template from database
                var templateResult = await GetTemplateAsync(templateName, EmailFormat.Html, culture, cancellationToken);
                if (templateResult.IsFailure)
                    return Result<string>.Failure(templateResult.Errors);

                template = templateResult.Value;
                
                // Cache for 30 minutes
                _cache.Set(cacheKey, template, TimeSpan.FromMinutes(30));
            }

            // Set culture for rendering
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            
            try
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                // Render the template using Razor engine
                var renderedContent = await RenderRazorTemplate(template.Content, model);
                return Result<string>.Success(renderedContent);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render email template {TemplateName} for culture {Culture}", templateName, culture?.Name);
            return Result<string>.Failure($"Template rendering failed: {ex.Message}");
        }
    }

    public async Task<Result<EmailTemplate>> GetTemplateAsync(
        string templateName, 
        EmailFormat format, 
        CultureInfo? culture = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            culture ??= CultureInfo.CurrentCulture;
            
            // Try to find template with specific culture
            var template = await _templateRepository.FindFirstAsync(
                t => t.Name == templateName && 
                     t.Format == format && 
                     t.Culture.Name == culture.Name && 
                     t.IsActive,
                cancellationToken);

            // Fallback to default culture (en-US)
            if (template == null)
            {
                template = await _templateRepository.FindFirstAsync(
                    t => t.Name == templateName && 
                         t.Format == format && 
                         t.Culture.Name == "en-US" && 
                         t.IsActive,
                    cancellationToken);
            }

            if (template == null)
                return Result<EmailTemplate>.Failure($"Email template '{templateName}' not found for format '{format}' and culture '{culture.Name}'");

            return Result<EmailTemplate>.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve email template {TemplateName}", templateName);
            return Result<EmailTemplate>.Failure($"Failed to retrieve template: {ex.Message}");
        }
    }

    private async Task<string> RenderRazorTemplate<TModel>(string templateContent, TModel model)
    {
        // Implementation for Razor template rendering
        // This would use the Razor engine to compile and render the template
        // with the provided model data
        
        using var scope = _serviceProvider.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        using var output = new StringWriter();
        var viewContext = new ViewContext(actionContext, NullView.Instance, new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model }, new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()), output, new HtmlHelperOptions());

        // Create and execute view
        var view = new RazorStringView(templateContent);
        await view.RenderAsync(viewContext);

        return output.ToString();
    }
}
```

### 3.3 Repository Implementation

```csharp
// src/LankaConnect.Infrastructure/Data/Repositories/EmailRepository.cs
public class EmailRepository : Repository<EmailMessage>, IEmailRepository
{
    public EmailRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<EmailMessage>> GetPendingEmailsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<EmailMessage>()
            .Where(e => e.Status == EmailMessageStatus.Pending)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailMessage>> GetFailedEmailsForRetryAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<EmailMessage>()
            .Where(e => e.Status == EmailMessageStatus.Failed && 
                       e.NextRetryAt.HasValue && 
                       e.NextRetryAt.Value <= DateTime.UtcNow &&
                       e.RetryCount < 5)
            .OrderBy(e => e.NextRetryAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<EmailMessage> Items, int TotalCount)> GetEmailHistoryAsync(
        string? recipientEmail,
        EmailMessageType? type,
        EmailMessageStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<EmailMessage>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(recipientEmail))
            query = query.Where(e => e.To.Address.Contains(recipientEmail));

        if (type.HasValue)
            query = query.Where(e => e.Type == type.Value);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(e => e.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

// src/LankaConnect.Infrastructure/Data/Repositories/EmailTemplateRepository.cs
public class EmailTemplateRepository : Repository<EmailTemplate>, IEmailTemplateRepository
{
    public EmailTemplateRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<EmailTemplate>> GetByCategoyAsync(
        EmailTemplateCategory category, 
        CultureInfo? culture = null, 
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<EmailTemplate>()
            .Where(t => t.Category == category && t.IsActive);

        if (culture != null)
            query = query.Where(t => t.Culture.Name == culture.Name);

        return await query
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailTemplate?> GetByNameAndCultureAsync(
        string name, 
        CultureInfo culture, 
        EmailFormat format,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<EmailTemplate>()
            .FirstOrDefaultAsync(t => 
                t.Name == name && 
                t.Culture.Name == culture.Name && 
                t.Format == format && 
                t.IsActive, 
                cancellationToken);
    }
}
```

## 4. API Controller Design

```csharp
// src/LankaConnect.API/Controllers/EmailController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IMediator mediator, ILogger<EmailController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Send a custom email
    /// </summary>
    /// <param name="request">Email details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Email ID if successful</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Guid>>> SendEmail(
        [FromBody] SendEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SendEmailCommand(
            request.ToEmail,
            request.ToDisplayName,
            request.Subject,
            request.HtmlBody,
            request.TextBody,
            request.Type,
            request.Priority,
            request.CorrelationId,
            request.CcEmails,
            request.BccEmails);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(ApiResponse<Guid>.Success(result.Value, "Email sent successfully"));
        }

        return BadRequest(ApiResponse.Failure(result.Errors));
    }

    /// <summary>
    /// Send a templated email
    /// </summary>
    /// <param name="request">Templated email details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Email ID if successful</returns>
    [HttpPost("send-template")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Guid>>> SendTemplatedEmail(
        [FromBody] SendTemplatedEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SendTemplatedEmailCommand(
            request.ToEmail,
            request.ToDisplayName,
            request.TemplateName,
            request.TemplateData,
            request.Type,
            request.CultureCode,
            request.Priority,
            request.CorrelationId);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(ApiResponse<Guid>.Success(result.Value, "Templated email sent successfully"));
        }

        return BadRequest(ApiResponse.Failure(result.Errors));
    }

    /// <summary>
    /// Resend email verification
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> ResendEmailVerification(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var userEmail = User.GetEmail();
        var userName = User.GetFullName();

        if (userId == null || string.IsNullOrWhiteSpace(userEmail))
        {
            return BadRequest(ApiResponse.Failure("User information not found"));
        }

        // Generate new verification token (this would typically be done in a separate service)
        var verificationToken = Guid.NewGuid().ToString("N");

        var command = new SendEmailVerificationCommand(
            userId.Value,
            userEmail,
            userName ?? "User",
            verificationToken);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(ApiResponse.Success("Verification email sent successfully"));
        }

        return BadRequest(ApiResponse.Failure(result.Errors));
    }

    /// <summary>
    /// Get email status
    /// </summary>
    /// <param name="emailId">Email ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Email status details</returns>
    [HttpGet("{emailId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<EmailStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EmailStatusResponse>>> GetEmailStatus(
        Guid emailId,
        CancellationToken cancellationToken)
    {
        var query = new GetEmailStatusQuery(emailId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(ApiResponse<EmailStatusResponse>.Success(result.Value));
        }

        return NotFound(ApiResponse.Failure(result.Errors));
    }

    /// <summary>
    /// Get email history
    /// </summary>
    /// <param name="request">Filter parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated email history</returns>
    [HttpGet("history")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<EmailHistoryResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<EmailHistoryResponse>>>> GetEmailHistory(
        [FromQuery] GetEmailHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetEmailHistoryQuery(
            request.RecipientEmail,
            request.Type,
            request.Status,
            request.FromDate,
            request.ToDate,
            request.Page,
            request.PageSize);

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(ApiResponse<PagedResponse<EmailHistoryResponse>>.Success(result.Value));
        }

        return BadRequest(ApiResponse.Failure(result.Errors));
    }
}
```

## 5. Configuration Strategy

### 5.1 Email Settings Configuration

```json
// Enhanced appsettings.json
{
  "EmailSettings": {
    "SmtpServer": "localhost",
    "SmtpPort": 1025,
    "SenderEmail": "noreply@lankaconnect.com",
    "SenderName": "LankaConnect",
    "Username": "",
    "Password": "",
    "EnableSsl": false,
    "MaxRetryAttempts": 5,
    "RetryDelayMinutes": [1, 5, 15, 30, 60],
    "DefaultCulture": "en-US",
    "SupportedCultures": ["en-US", "si-LK"],
    "Templates": {
      "BasePath": "wwwroot/templates/email",
      "CacheDurationMinutes": 30
    }
  },
  "SendGridSettings": {
    "ApiKey": "",
    "SenderEmail": "noreply@lankaconnect.com",
    "SenderName": "LankaConnect",
    "TemplateIds": {
      "Welcome": "d-12345",
      "EmailVerification": "d-12346",
      "PasswordReset": "d-12347"
    }
  }
}
```

### 5.2 Production Configuration

```json
// appsettings.Production.json
{
  "EmailSettings": {
    "Provider": "SendGrid",
    "SmtpServer": "",
    "SmtpPort": 587,
    "EnableSsl": true,
    "MaxRetryAttempts": 3,
    "RetryDelayMinutes": [2, 10, 30]
  },
  "SendGridSettings": {
    "ApiKey": "{{SENDGRID_API_KEY}}",
    "SenderEmail": "noreply@lankaconnect.com",
    "SenderName": "LankaConnect"
  }
}
```

## 6. Testing Strategy

### 6.1 Unit Tests Structure

```csharp
// tests/LankaConnect.Domain.Tests/Email/EmailMessageTests.cs
public class EmailMessageTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldReturnSuccess()
    {
        // Arrange
        var fromResult = EmailAddress.Create("noreply@lankaconnect.com", "LankaConnect");
        var toResult = EmailAddress.Create("user@example.com", "Test User");
        var contentResult = EmailContent.Create("Test Subject", "<h1>Test</h1>", "Test");

        // Act
        var result = EmailMessage.Create(
            fromResult.Value,
            toResult.Value,
            contentResult.Value,
            EmailMessageType.Welcome);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(EmailMessageStatus.Pending);
        result.Value.Type.Should().Be(EmailMessageType.Welcome);
    }

    [Fact]
    public void MarkAsSent_WhenPending_ShouldUpdateStatusAndRaiseDomainEvent()
    {
        // Arrange
        var message = CreateValidEmailMessage();

        // Act
        var result = message.MarkAsSent();

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(EmailMessageStatus.Sent);
        message.SentAt.Should().NotBeNull();
        message.DomainEvents.Should().ContainSingle(e => e is EmailSentEvent);
    }
}

// tests/LankaConnect.Application.Tests/Email/Commands/SendEmailCommandHandlerTests.cs
public class SendEmailCommandHandlerTests
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IEmailRepository> _emailRepositoryMock;
    private readonly Mock<ILogger<SendEmailCommandHandler>> _loggerMock;
    private readonly SendEmailCommandHandler _handler;

    public SendEmailCommandHandlerTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _emailRepositoryMock = new Mock<IEmailRepository>();
        _loggerMock = new Mock<ILogger<SendEmailCommandHandler>>();
        _handler = new SendEmailCommandHandler(_emailServiceMock.Object, _emailRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSendEmailAndReturnId()
    {
        // Arrange
        var command = new SendEmailCommand(
            "test@example.com",
            "Test User",
            "Test Subject",
            "<h1>Test</h1>",
            "Test",
            EmailMessageType.Welcome);

        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _emailRepositoryMock.Verify(x => x.AddAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### 6.2 Integration Tests

```csharp
// tests/LankaConnect.IntegrationTests/Email/EmailIntegrationTests.cs
public class EmailIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task SendEmail_WithValidData_ShouldStoreInDatabaseAndSendEmail()
    {
        // Arrange
        var request = new SendEmailRequest
        {
            ToEmail = "test@example.com",
            ToDisplayName = "Test User",
            Subject = "Integration Test Email",
            HtmlBody = "<h1>This is a test</h1>",
            TextBody = "This is a test",
            Type = EmailMessageType.Welcome
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/email/send", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<Guid>>(content, JsonOptions);
        apiResponse!.IsSuccess.Should().BeTrue();
        apiResponse.Data.Should().NotBeEmpty();

        // Verify email was stored in database
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailMessage = await dbContext.Set<EmailMessage>()
            .FirstOrDefaultAsync(e => e.Id == apiResponse.Data);
        
        emailMessage.Should().NotBeNull();
        emailMessage!.To.Address.Should().Be(request.ToEmail);
        emailMessage.Content.Subject.Should().Be(request.Subject);
    }
}
```

## 7. Implementation Phases

### Phase 1: Core Foundation (Week 1)
- Domain layer implementation (value objects, entities, enums)
- Domain events for email operations
- Basic interfaces and repository contracts
- Unit tests for domain logic

### Phase 2: Application Layer (Week 2)
- CQRS commands and queries implementation
- Command/query handlers
- Domain event handlers for automatic emails
- Application layer unit tests

### Phase 3: Infrastructure & Templates (Week 3)
- SMTP email delivery service
- Razor template service implementation
- Repository implementations
- Email templates creation (HTML/text)
- Database migrations

### Phase 4: API & Integration (Week 4)
- API controller implementation
- Request/response DTOs
- API validation and error handling
- Integration tests
- MailHog testing verification

### Phase 5: Production Readiness (Week 5)
- SendGrid integration for production
- Email delivery monitoring
- Performance optimization
- Comprehensive testing
- Documentation and deployment guides

## 8. Key Design Decisions

### 8.1 Architecture Decision Records (ADRs)

**ADR-001: Use Domain Events for Email Triggers**
- **Decision**: Use existing domain events (UserCreatedEvent, UserPasswordChangedEvent) to automatically trigger email sending
- **Rationale**: Loose coupling, follows existing patterns, enables audit trail
- **Consequences**: Clean separation of concerns, but requires careful event ordering

**ADR-002: Separate Email Message Storage and Delivery**
- **Decision**: Store email messages in database before attempting delivery
- **Rationale**: Reliability, retry capability, audit trail, monitoring
- **Consequences**: Additional database storage, but improved reliability

**ADR-003: Template-Based Email System with Razor Engine**
- **Decision**: Use Razor templates for email content rendering
- **Rationale**: Type-safe, familiar to .NET developers, supports localization
- **Consequences**: Requires template compilation, but provides flexibility

**ADR-004: SMTP for Development, SendGrid for Production**
- **Decision**: Use MailHog/SMTP locally, SendGrid in production
- **Rationale**: Cost-effective development, reliable production delivery
- **Consequences**: Different configurations per environment

## 9. Monitoring and Observability

### 9.1 Metrics to Track
- Email delivery success/failure rates
- Template rendering performance
- SMTP connection reliability
- Email queue depth and processing time
- Retry attempt patterns

### 9.2 Logging Strategy
- Structured logging with correlation IDs
- Email delivery status changes
- Template rendering failures
- SMTP connectivity issues
- Performance metrics

## 10. Security Considerations

### 10.1 Data Protection
- Email content encryption at rest
- Secure token generation for verification/reset
- PII handling compliance
- Rate limiting on email endpoints

### 10.2 Authentication & Authorization
- JWT token validation for API endpoints
- Role-based access for email history
- User can only resend their own verification emails
- Admin-only access to system email logs

This comprehensive architecture provides a production-ready, maintainable, and scalable email & notifications system that integrates seamlessly with your existing LankaConnect Clean Architecture application.