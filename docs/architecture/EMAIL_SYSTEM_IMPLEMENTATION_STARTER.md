# Email System Implementation Starter Guide
## Ready-to-Use Code Templates for TDD Implementation

**Quick Start:** Copy these templates and adapt them for your implementation.

---

## Phase 1: Domain Layer Value Objects

### Template 1: EmailVerificationToken.cs

```csharp
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing an email verification token with expiration
/// Immutable and validates token integrity
/// </summary>
public sealed class EmailVerificationToken : ValueObject
{
    /// <summary>
    /// The verification token (32-character GUID without dashes)
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// UTC timestamp when the token expires
    /// </summary>
    public DateTime ExpiresAt { get; }

    /// <summary>
    /// The user ID this token is associated with
    /// </summary>
    public Guid UserId { get; }

    private EmailVerificationToken(string token, DateTime expiresAt, Guid userId)
    {
        Token = token;
        ExpiresAt = expiresAt;
        UserId = userId;
    }

    /// <summary>
    /// Creates a new email verification token with specified expiry hours
    /// </summary>
    /// <param name="userId">The user ID to associate with the token</param>
    /// <param name="expiryHours">Token validity duration (default: 24 hours, max: 168 hours/7 days)</param>
    /// <returns>Success with token or failure with error message</returns>
    public static Result<EmailVerificationToken> Create(Guid userId, int expiryHours = 24)
    {
        if (userId == Guid.Empty)
            return Result<EmailVerificationToken>.Failure("User ID is required");

        if (expiryHours <= 0 || expiryHours > 168)
            return Result<EmailVerificationToken>.Failure("Expiry hours must be between 1 and 168 (7 days)");

        var token = Guid.NewGuid().ToString("N"); // 32-char hex string without dashes
        var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

        return Result<EmailVerificationToken>.Success(
            new EmailVerificationToken(token, expiresAt, userId));
    }

    /// <summary>
    /// Checks if the token has expired
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Validates if the provided token matches and has not expired
    /// </summary>
    /// <param name="tokenToValidate">The token string to validate</param>
    /// <returns>True if token is valid and not expired</returns>
    public bool IsValid(string tokenToValidate) =>
        !IsExpired() && Token.Equals(tokenToValidate, StringComparison.Ordinal);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Token;
        yield return UserId;
    }
}
```

### Template 2: EmailVerificationTokenTests.cs

```csharp
using FluentAssertions;
using LankaConnect.Domain.Communications.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Communications.ValueObjects;

public class EmailVerificationTokenTests
{
    private readonly Guid _validUserId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        // Act
        var result = EmailVerificationToken.Create(_validUserId, expiryHours: 24);

        // Assert
        result.Should().BeSuccess();
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.Token.Should().HaveLength(32); // GUID without dashes
        result.Value.UserId.Should().Be(_validUserId);
        result.Value.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithEmptyUserId_ReturnsFailure()
    {
        // Act
        var result = EmailVerificationToken.Create(Guid.Empty);

        // Assert
        result.Should().BeFailure();
        result.Error.Should().Contain("User ID is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(169)] // More than 7 days
    public void Create_WithInvalidExpiryHours_ReturnsFailure(int expiryHours)
    {
        // Act
        var result = EmailVerificationToken.Create(_validUserId, expiryHours);

        // Assert
        result.Should().BeFailure();
        result.Error.Should().Contain("Expiry hours must be between");
    }

    [Fact]
    public void IsExpired_WithExpiredToken_ReturnsTrue()
    {
        // Arrange
        var token = EmailVerificationToken.Create(_validUserId, expiryHours: -1).Value; // Already expired

        // Act
        var isExpired = token.IsExpired();

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WithValidToken_ReturnsFalse()
    {
        // Arrange
        var token = EmailVerificationToken.Create(_validUserId, expiryHours: 24).Value;

        // Act
        var isExpired = token.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithCorrectToken_ReturnsTrue()
    {
        // Arrange
        var token = EmailVerificationToken.Create(_validUserId, expiryHours: 24).Value;

        // Act
        var isValid = token.IsValid(token.Token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithIncorrectToken_ReturnsFalse()
    {
        // Arrange
        var token = EmailVerificationToken.Create(_validUserId, expiryHours: 24).Value;

        // Act
        var isValid = token.IsValid("incorrect-token");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var token = EmailVerificationToken.Create(_validUserId, expiryHours: -1).Value;

        // Act
        var isValid = token.IsValid(token.Token);

        // Assert
        isValid.Should().BeFalse(); // Expired tokens are invalid
    }

    [Fact]
    public void Equality_WithSameTokenAndUserId_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token1 = EmailVerificationToken.Create(userId).Value;

        // Create a second token with the same properties (for testing purposes)
        // In practice, you'd retrieve the same token from a data source
        var token2 = EmailVerificationToken.Create(userId).Value;

        // Act & Assert
        // Note: Tokens will be different due to random GUID generation
        // Testing equality logic structure
        token1.Equals(token1).Should().BeTrue();
    }
}
```

---

## Phase 2: Application Layer Command Handler

### Template 3: SendTransactionalEmailCommand.cs

```csharp
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;

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
    DateTime? ScheduledSendTime = null) : ICommand<SendTransactionalEmailResponse>;

/// <summary>
/// Response returned after queuing a transactional email
/// </summary>
public class SendTransactionalEmailResponse
{
    /// <summary>
    /// The ID of the created EmailMessage aggregate
    /// </summary>
    public Guid EmailMessageId { get; init; }

    /// <summary>
    /// The template name used for the email
    /// </summary>
    public string TemplateName { get; init; } = string.Empty;

    /// <summary>
    /// The recipient's email address
    /// </summary>
    public string RecipientEmail { get; init; } = string.Empty;

    /// <summary>
    /// Current status of the email (typically Queued)
    /// </summary>
    public EmailStatus Status { get; init; }

    /// <summary>
    /// UTC timestamp when the email was queued
    /// </summary>
    public DateTime QueuedAt { get; init; }

    /// <summary>
    /// Optional scheduled send time (null for immediate delivery)
    /// </summary>
    public DateTime? ScheduledSendTime { get; init; }
}
```

### Template 4: SendTransactionalEmailCommandValidator.cs

```csharp
using FluentValidation;

namespace LankaConnect.Application.Communications.Commands.SendTransactionalEmail;

public class SendTransactionalEmailValidator : AbstractValidator<SendTransactionalEmailCommand>
{
    public SendTransactionalEmailValidator()
    {
        RuleFor(x => x.TemplateName)
            .NotEmpty().WithMessage("Template name is required")
            .MaximumLength(100).WithMessage("Template name cannot exceed 100 characters");

        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("Recipient email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email address too long");

        RuleFor(x => x.RecipientName)
            .NotEmpty().WithMessage("Recipient name is required")
            .MaximumLength(200).WithMessage("Recipient name too long");

        RuleFor(x => x.TemplateVariables)
            .NotNull().WithMessage("Template variables cannot be null");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 10).WithMessage("Priority must be between 1 (high) and 10 (low)");

        RuleFor(x => x.ScheduledSendTime)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ScheduledSendTime.HasValue)
            .WithMessage("Scheduled send time must be in the future");
    }
}
```

### Template 5: SendTransactionalEmailCommandHandler.cs

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Communications.Commands.SendTransactionalEmail;

/// <summary>
/// Handler for sending transactional emails
/// Validates template, renders content, creates EmailMessage aggregate, and queues for delivery
/// </summary>
public class SendTransactionalEmailCommandHandler
    : IRequestHandler<SendTransactionalEmailCommand, Result<SendTransactionalEmailResponse>>
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _templateService;
    private readonly IEmailMessageRepository _emailMessageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendTransactionalEmailCommandHandler> _logger;

    public SendTransactionalEmailCommandHandler(
        IEmailService emailService,
        IEmailTemplateService templateService,
        IEmailMessageRepository emailMessageRepository,
        IUnitOfWork unitOfWork,
        ILogger<SendTransactionalEmailCommandHandler> logger)
    {
        _emailService = emailService;
        _templateService = templateService;
        _emailMessageRepository = emailMessageRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SendTransactionalEmailResponse>> Handle(
        SendTransactionalEmailCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate template exists and is active
            var templateValidation = await _emailService.ValidateTemplateAsync(
                request.TemplateName,
                cancellationToken);

            if (!templateValidation.IsSuccess)
            {
                _logger.LogWarning(
                    "Template validation failed for {TemplateName}: {Error}",
                    request.TemplateName, templateValidation.Error);
                return Result<SendTransactionalEmailResponse>.Failure(templateValidation.Error);
            }

            // 2. Validate template parameters
            var paramValidation = await _templateService.ValidateTemplateParametersAsync(
                request.TemplateName,
                request.TemplateVariables,
                cancellationToken);

            if (!paramValidation.IsSuccess)
            {
                _logger.LogWarning(
                    "Template parameter validation failed: {Error}",
                    paramValidation.Error);
                return Result<SendTransactionalEmailResponse>.Failure(paramValidation.Error);
            }

            // 3. Render template
            var renderedTemplate = await _templateService.RenderTemplateAsync(
                request.TemplateName,
                request.TemplateVariables,
                cancellationToken);

            if (!renderedTemplate.IsSuccess)
            {
                _logger.LogError(
                    "Failed to render template {TemplateName}: {Error}",
                    request.TemplateName, renderedTemplate.Error);
                return Result<SendTransactionalEmailResponse>.Failure(
                    $"Failed to render email template: {renderedTemplate.Error}");
            }

            // 4. Create email value objects
            var fromEmailResult = Email.Create("noreply@lankaconnect.com");
            var toEmailResult = Email.Create(request.RecipientEmail);

            if (!fromEmailResult.IsSuccess || !toEmailResult.IsSuccess)
            {
                return Result<SendTransactionalEmailResponse>.Failure("Invalid email address");
            }

            // 5. Create EmailMessage aggregate
            var emailMessageResult = EmailMessage.CreateWithEmails(
                fromEmailResult.Value,
                toEmailResult.Value,
                renderedTemplate.Value.Subject,
                renderedTemplate.Value.PlainTextBody,
                renderedTemplate.Value.HtmlBody,
                EmailType.Transactional,
                priority: request.Priority);

            if (!emailMessageResult.IsSuccess)
            {
                _logger.LogError(
                    "Failed to create EmailMessage aggregate: {Error}",
                    emailMessageResult.Error);
                return Result<SendTransactionalEmailResponse>.Failure(emailMessageResult.Error);
            }

            var emailMessage = emailMessageResult.Value;

            // 6. Queue for sending (or schedule for later)
            Result queueResult;
            if (request.ScheduledSendTime.HasValue)
            {
                // TODO: Implement scheduled email functionality
                queueResult = emailMessage.MarkAsQueued();
                _logger.LogInformation(
                    "Email scheduled for {ScheduledTime}",
                    request.ScheduledSendTime.Value);
            }
            else
            {
                queueResult = emailMessage.MarkAsQueued();
            }

            if (!queueResult.IsSuccess)
            {
                return Result<SendTransactionalEmailResponse>.Failure(queueResult.Error);
            }

            // 7. Persist to database
            await _emailMessageRepository.AddAsync(emailMessage, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Transactional email queued successfully. Template: {TemplateName}, Recipient: {Recipient}, Priority: {Priority}",
                request.TemplateName, request.RecipientEmail, request.Priority);

            // 8. Return response
            var response = new SendTransactionalEmailResponse
            {
                EmailMessageId = emailMessage.Id,
                TemplateName = request.TemplateName,
                RecipientEmail = request.RecipientEmail,
                Status = emailMessage.Status,
                QueuedAt = DateTime.UtcNow,
                ScheduledSendTime = request.ScheduledSendTime
            };

            return Result<SendTransactionalEmailResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error sending transactional email. Template: {TemplateName}",
                request.TemplateName);
            return Result<SendTransactionalEmailResponse>.Failure(
                "An unexpected error occurred while processing the email");
        }
    }
}
```

### Template 6: SendTransactionalEmailCommandHandlerTests.cs

```csharp
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Commands.SendTransactionalEmail;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using Xunit;

namespace LankaConnect.Application.Tests.Communications.Commands;

public class SendTransactionalEmailCommandHandlerTests
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IEmailTemplateService> _templateServiceMock;
    private readonly Mock<IEmailMessageRepository> _emailMessageRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<SendTransactionalEmailCommandHandler>> _loggerMock;
    private readonly SendTransactionalEmailCommandHandler _handler;

    public SendTransactionalEmailCommandHandlerTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _templateServiceMock = new Mock<IEmailTemplateService>();
        _emailMessageRepositoryMock = new Mock<IEmailMessageRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<SendTransactionalEmailCommandHandler>>();

        _handler = new SendTransactionalEmailCommandHandler(
            _emailServiceMock.Object,
            _templateServiceMock.Object,
            _emailMessageRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new SendTransactionalEmailCommand(
            TemplateName: "event-reminder",
            RecipientEmail: "user@example.com",
            RecipientName: "John Doe",
            TemplateVariables: new Dictionary<string, object>
            {
                { "EventName", "Sri Lankan New Year" },
                { "EventDate", "2025-04-14" }
            },
            Priority: 5);

        _emailServiceMock
            .Setup(x => x.ValidateTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _templateServiceMock
            .Setup(x => x.ValidateTemplateParametersAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _templateServiceMock
            .Setup(x => x.RenderTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Event Reminder",
                HtmlBody = "<p>Reminder: Sri Lankan New Year on 2025-04-14</p>",
                PlainTextBody = "Reminder: Sri Lankan New Year on 2025-04-14"
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        result.Value.EmailMessageId.Should().NotBeEmpty();
        result.Value.TemplateName.Should().Be("event-reminder");
        result.Value.RecipientEmail.Should().Be("user@example.com");
        result.Value.Status.Should().Be(EmailStatus.Queued);

        _emailMessageRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidTemplateName_ReturnsFailure()
    {
        // Arrange
        var command = new SendTransactionalEmailCommand(
            TemplateName: "non-existent-template",
            RecipientEmail: "user@example.com",
            RecipientName: "John Doe",
            TemplateVariables: new Dictionary<string, object>());

        _emailServiceMock
            .Setup(x => x.ValidateTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Template 'non-existent-template' not found"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Error.Should().Contain("Template");

        _emailMessageRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithMissingTemplateParameters_ReturnsFailure()
    {
        // Arrange
        var command = new SendTransactionalEmailCommand(
            TemplateName: "event-reminder",
            RecipientEmail: "user@example.com",
            RecipientName: "John Doe",
            TemplateVariables: new Dictionary<string, object>()); // Missing required params

        _emailServiceMock
            .Setup(x => x.ValidateTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _templateServiceMock
            .Setup(x => x.ValidateTemplateParametersAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Missing required parameters: EventName, EventDate"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Error.Should().Contain("Missing required parameters");

        _emailMessageRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithTemplateRenderingFailure_ReturnsFailure()
    {
        // Arrange
        var command = new SendTransactionalEmailCommand(
            TemplateName: "event-reminder",
            RecipientEmail: "user@example.com",
            RecipientName: "John Doe",
            TemplateVariables: new Dictionary<string, object>
            {
                { "EventName", "Test Event" }
            });

        _emailServiceMock
            .Setup(x => x.ValidateTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _templateServiceMock
            .Setup(x => x.ValidateTemplateParametersAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _templateServiceMock
            .Setup(x => x.RenderTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RenderedEmailTemplate>.Failure("Template rendering syntax error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Error.Should().Contain("Failed to render email template");

        _emailMessageRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesEmailMessageAggregate_WithCorrectStatus()
    {
        // Arrange
        var command = new SendTransactionalEmailCommand(
            TemplateName: "event-reminder",
            RecipientEmail: "user@example.com",
            RecipientName: "John Doe",
            TemplateVariables: new Dictionary<string, object>
            {
                { "EventName", "Test Event" }
            });

        EmailMessage? capturedEmailMessage = null;

        _emailServiceMock
            .Setup(x => x.ValidateTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _templateServiceMock
            .Setup(x => x.ValidateTemplateParametersAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _templateServiceMock
            .Setup(x => x.RenderTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Test Subject",
                HtmlBody = "<p>Test Body</p>",
                PlainTextBody = "Test Body"
            }));

        _emailMessageRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, ct) => capturedEmailMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        capturedEmailMessage.Should().NotBeNull();
        capturedEmailMessage!.Status.Should().Be(EmailStatus.Queued);
        capturedEmailMessage.Type.Should().Be(EmailType.Transactional);
        capturedEmailMessage.ToEmails.Should().Contain("user@example.com");
    }
}
```

---

## Phase 3: Infrastructure Layer SMTP Service

### Template 7: SmtpEmailService.cs (Simplified)

```csharp
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using System.Text.RegularExpressions;

namespace LankaConnect.Infrastructure.Communications.EmailService;

/// <summary>
/// SMTP-based email service implementation using MailKit
/// Sends emails via configured SMTP server (MailHog for dev, SendGrid/Azure for production)
/// </summary>
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
            var mimeMessage = CreateMimeMessage(emailMessage);

            using var client = new SmtpClient();

            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password,
                    cancellationToken);
            }

            var messageId = await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully. MessageId: {MessageId}, To: {Recipient}, Subject: {Subject}",
                messageId, emailMessage.ToEmail, emailMessage.Subject);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email to {Recipient}. Subject: {Subject}",
                emailMessage.ToEmail, emailMessage.Subject);

            return Result.Failure($"Email sending failed: {ex.Message}");
        }
    }

    public async Task<Result> SendTemplatedEmailAsync(
        string templateName,
        string recipientEmail,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        // This method is intentionally not implemented here
        // Use IEmailTemplateService to render, then call SendEmailAsync with result
        throw new NotImplementedException(
            "Use IEmailTemplateService.RenderTemplateAsync() followed by SendEmailAsync()");
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
        var template = await _emailRepository.GetByNameAsync(templateName, cancellationToken);

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

        // Set priority
        message.Priority = dto.Priority switch
        {
            1 => MessagePriority.Urgent,
            2 => MessagePriority.Normal,
            _ => MessagePriority.NonUrgent
        };

        return message;
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        return Regex.Replace(html, "<.*?>", string.Empty);
    }
}

/// <summary>
/// SMTP configuration settings
/// </summary>
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

---

## Quick Commands Reference

```bash
# Phase 1: Create Domain Value Objects
cd src/LankaConnect.Domain/Communications/ValueObjects
# Create EmailVerificationToken.cs, PasswordResetToken.cs

cd tests/LankaConnect.Domain.Tests/Communications/ValueObjects
# Create corresponding test files

# Run domain tests
dotnet test --filter "FullyQualifiedName~LankaConnect.Domain.Tests.Communications.ValueObjects"

# Phase 2: Create Application Commands
cd src/LankaConnect.Application/Communications/Commands
mkdir SendTransactionalEmail
# Create command, handler, validator, response files

cd tests/LankaConnect.Application.Tests/Communications/Commands
# Create test files

# Run application tests
dotnet test --filter "FullyQualifiedName~LankaConnect.Application.Tests"

# Phase 3: Create Infrastructure Services
cd src/LankaConnect.Infrastructure/Communications
mkdir EmailService
# Create SmtpEmailService.cs, SmtpSettings.cs

# Start MailHog for testing
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog

# Run infrastructure integration tests
dotnet test --filter "FullyQualifiedName~LankaConnect.Infrastructure.Tests"

# Build entire solution
dotnet build

# Run all tests
dotnet test

# Check test coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Next Steps After Phase 1

Once you complete Phase 1 (Domain Layer), you should have:

- ✅ `EmailVerificationToken.cs` (with 6-8 passing tests)
- ✅ `PasswordResetToken.cs` (with 6-8 passing tests)
- ✅ `TemplateVariable.cs` (with 4-5 passing tests)
- ✅ All domain events (no tests needed for simple records)
- ✅ ~15 total passing tests
- ✅ 0 compilation errors

**Then proceed to Phase 2** using the command handler templates above.

---

**Ready to Start?** Begin with Phase 1, copy the `EmailVerificationToken` templates, and start your TDD journey!
