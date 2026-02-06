using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using Microsoft.Extensions.Logging;
using EmailValueObject = LankaConnect.Domain.Shared.ValueObjects.Email;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Email service implementation using SMTP for sending emails
/// Integrates with domain entities and follows Clean Architecture principles
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailMessageRepository _emailMessageRepository;
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly IEmailTemplateService _templateService;
    private readonly SmtpSettings _smtpSettings;

    public EmailService(
        ILogger<EmailService> logger,
        IEmailMessageRepository emailMessageRepository,
        IEmailTemplateRepository emailTemplateRepository,
        IEmailTemplateService templateService,
        IOptions<SmtpSettings> smtpSettings)
    {
        _logger = logger;
        _emailMessageRepository = emailMessageRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _templateService = templateService;
        _smtpSettings = smtpSettings.Value;
    }

    public async Task<Result> SendEmailAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to send email to {ToEmail} with subject '{Subject}'", 
                emailMessage.ToEmail, emailMessage.Subject);

            // Validate input
            var validationResult = ValidateEmailMessage(emailMessage);
            if (validationResult.IsFailure)
            {
                _logger.LogWarning("Email validation failed: {Error}", validationResult.Error);
                return validationResult;
            }

            // Create domain entity
            var domainEmailResult = await CreateDomainEmailMessage(emailMessage, cancellationToken);
            if (domainEmailResult.IsFailure)
            {
                return Result.Failure(domainEmailResult.Error);
            }

            // Send via SMTP
            var sendResult = await SendViaSmtpAsync(emailMessage, cancellationToken);
            if (sendResult.IsFailure)
            {
                // Mark domain entity as failed
                domainEmailResult.Value.MarkAsFailed(sendResult.Error);
                _emailMessageRepository.Update(domainEmailResult.Value);
                return sendResult;
            }

            // Mark domain entity as sent
            domainEmailResult.Value.MarkAsSent();
            _emailMessageRepository.Update(domainEmailResult.Value);

            _logger.LogInformation("Email sent successfully to {ToEmail}", emailMessage.ToEmail);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", emailMessage.ToEmail);
            return Result.Failure($"Failed to send email: {ex.Message}");
        }
    }

    public async Task<Result> SendTemplatedEmailAsync(string templateName, string recipientEmail,
        Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[EMAIL-SEND] ▶️ START SendTemplatedEmailAsync - Template: '{TemplateName}', Recipient: {RecipientEmail}",
            templateName, recipientEmail);

        try
        {
            // Validate template exists
            _logger.LogInformation("[EMAIL-SEND] Step 1: Loading template '{TemplateName}' from repository", templateName);
            var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);

            if (template == null)
            {
                _logger.LogError("[EMAIL-SEND] ❌ FAILED: Email template '{TemplateName}' not found in database", templateName);
                return Result.Failure($"Email template '{templateName}' not found");
            }

            _logger.LogInformation("[EMAIL-SEND] ✅ Template loaded: Id={TemplateId}, IsActive={IsActive}, Category={Category}",
                template.Id, template.IsActive, template.Category.Value);

            if (!template.IsActive)
            {
                _logger.LogError("[EMAIL-SEND] ❌ FAILED: Email template '{TemplateName}' is not active (IsActive=false)", templateName);
                return Result.Failure($"Email template '{templateName}' is not active");
            }

            // Render template
            _logger.LogInformation("[EMAIL-SEND] Step 2: Rendering template with {ParameterCount} parameters", parameters.Count);
            var renderResult = await _templateService.RenderTemplateAsync(templateName, parameters, cancellationToken);

            if (renderResult.IsFailure)
            {
                _logger.LogError("[EMAIL-SEND] ❌ FAILED: Template rendering failed: {Error}", renderResult.Error);
                return Result.Failure(renderResult.Error);
            }

            _logger.LogInformation("[EMAIL-SEND] ✅ Template rendered successfully");

            // Create email message DTO
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                Subject = renderResult.Value.Subject,
                HtmlBody = renderResult.Value.HtmlBody,
                PlainTextBody = renderResult.Value.PlainTextBody,
                FromEmail = _smtpSettings.FromEmail,
                FromName = _smtpSettings.FromName,
                Priority = 2 // Normal priority for templated emails
            };

            _logger.LogInformation("[EMAIL-SEND] Step 3: Sending email - Subject: '{Subject}', From: {FromEmail}",
                emailMessage.Subject, emailMessage.FromEmail);

            // Send the email
            var sendResult = await SendEmailAsync(emailMessage, cancellationToken);

            if (sendResult.IsSuccess)
            {
                _logger.LogInformation("[EMAIL-SEND] ✅ SUCCESS: Email sent to {RecipientEmail}", recipientEmail);
            }
            else
            {
                _logger.LogError("[EMAIL-SEND] ❌ FAILED: Email send failed: {Errors}", string.Join(", ", sendResult.Errors));
            }

            return sendResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL-SEND] ❌ EXCEPTION: Failed to send templated email '{TemplateName}' to {RecipientEmail}: {Message}, StackTrace: {StackTrace}",
                templateName, recipientEmail, ex.Message, ex.StackTrace);
            return Result.Failure($"Failed to send templated email: {ex.Message}");
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

        _logger.LogInformation("Starting bulk email send for {Count} emails", messages.Count);

        foreach (var message in messages)
        {
            try
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
            catch (Exception ex)
            {
                result.FailedSends++;
                result.Errors.Add($"Failed to send to {message.ToEmail}: {ex.Message}");
            }

            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
        }

        _logger.LogInformation("Bulk email send completed: {Successful}/{Total} successful", 
            result.SuccessfulSends, result.TotalEmails);

        return Result<BulkEmailResult>.Success(result);
    }

    public async Task<Result> ValidateTemplateAsync(string templateName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                return Result.Failure("Template name is required");
            }

            var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
            if (template == null)
            {
                return Result.Failure($"Template '{templateName}' not found");
            }

            if (!template.IsActive)
            {
                return Result.Failure($"Template '{templateName}' is not active");
            }

            // Additional validation could include template syntax validation
            _logger.LogInformation("Template '{TemplateName}' validation passed", templateName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating template '{TemplateName}'", templateName);
            return Result.Failure($"Error validating template: {ex.Message}");
        }
    }

    /// <summary>
    /// Phase 6A.37: Sends an email using a template with parameters and inline image attachments.
    /// This SMTP version delegates to the base method since SMTP doesn't require special handling.
    /// </summary>
    public Task<Result> SendTemplatedEmailAsync(string templateName, string recipientEmail,
        Dictionary<string, object> parameters, List<EmailAttachment>? attachments,
        CancellationToken cancellationToken = default)
    {
        // For SMTP, we use the same approach as the base method since attachments are handled in SendViaSmtpAsync
        // The attachments will be added to the EmailMessageDto and sent with the email
        _logger.LogInformation("Sending templated email '{TemplateName}' to {RecipientEmail} with {AttachmentCount} attachments",
            templateName, recipientEmail, attachments?.Count ?? 0);

        // Create a wrapper that includes attachments in the email message
        return SendTemplatedEmailWithAttachmentsAsync(templateName, recipientEmail, parameters, attachments, cancellationToken);
    }

    private async Task<Result> SendTemplatedEmailWithAttachmentsAsync(string templateName, string recipientEmail,
        Dictionary<string, object> parameters, List<EmailAttachment>? attachments,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate template exists
            var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
            if (template == null)
            {
                _logger.LogWarning("Email template '{TemplateName}' not found", templateName);
                return Result.Failure($"Email template '{templateName}' not found");
            }

            if (!template.IsActive)
            {
                _logger.LogWarning("Email template '{TemplateName}' is not active", templateName);
                return Result.Failure($"Email template '{templateName}' is not active");
            }

            // Render template
            var renderResult = await _templateService.RenderTemplateAsync(templateName, parameters, cancellationToken);
            if (renderResult.IsFailure)
            {
                return Result.Failure(renderResult.Error);
            }

            // Create email message DTO with attachments
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                Subject = renderResult.Value.Subject,
                HtmlBody = renderResult.Value.HtmlBody,
                PlainTextBody = renderResult.Value.PlainTextBody,
                FromEmail = _smtpSettings.FromEmail,
                FromName = _smtpSettings.FromName,
                Priority = 2,
                Attachments = attachments
            };

            // Send the email
            return await SendEmailAsync(emailMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send templated email '{TemplateName}' to {RecipientEmail}",
                templateName, recipientEmail);
            return Result.Failure($"Failed to send templated email: {ex.Message}");
        }
    }

    private static Result ValidateEmailMessage(EmailMessageDto emailMessage)
    {
        if (string.IsNullOrWhiteSpace(emailMessage.ToEmail))
            return Result.Failure("Recipient email is required");

        if (string.IsNullOrWhiteSpace(emailMessage.Subject))
            return Result.Failure("Email subject is required");

        if (string.IsNullOrWhiteSpace(emailMessage.HtmlBody) && string.IsNullOrWhiteSpace(emailMessage.PlainTextBody))
            return Result.Failure("Email content is required (HTML or plain text)");

        // Basic email format validation
        try
        {
            var addr = new MailAddress(emailMessage.ToEmail);
            if (addr.Address != emailMessage.ToEmail)
                return Result.Failure("Invalid recipient email format");
        }
        catch
        {
            return Result.Failure("Invalid recipient email format");
        }

        return Result.Success();
    }

    private async Task<Result<EmailMessage>> CreateDomainEmailMessage(EmailMessageDto dto, CancellationToken cancellationToken)
    {
        try
        {
            // Create domain value objects
            var fromEmailResult = EmailValueObject.Create(_smtpSettings.FromEmail);
            if (fromEmailResult.IsFailure)
                return Result<EmailMessage>.Failure(fromEmailResult.Error);

            var toEmailResult = EmailValueObject.Create(dto.ToEmail);
            if (toEmailResult.IsFailure)
                return Result<EmailMessage>.Failure(toEmailResult.Error);

            var subjectResult = EmailSubject.Create(dto.Subject);
            if (subjectResult.IsFailure)
                return Result<EmailMessage>.Failure(subjectResult.Error);

            // Create domain entity
            var emailMessageResult = EmailMessage.Create(
                fromEmailResult.Value,
                subjectResult.Value,
                dto.PlainTextBody ?? dto.HtmlBody,
                dto.HtmlBody,
                EmailType.Transactional);

            if (emailMessageResult.IsFailure)
                return emailMessageResult;

            var domainEmail = emailMessageResult.Value;

            // Add recipient
            var addRecipientResult = domainEmail.AddRecipient(toEmailResult.Value);
            if (addRecipientResult.IsFailure)
                return Result<EmailMessage>.Failure(addRecipientResult.Error);

            // Mark as queued
            var queueResult = domainEmail.MarkAsQueued();
            if (queueResult.IsFailure)
                return Result<EmailMessage>.Failure(queueResult.Error);

            // Save to database
            await _emailMessageRepository.AddAsync(domainEmail, cancellationToken);

            return Result<EmailMessage>.Success(domainEmail);
        }
        catch (Exception ex)
        {
            return Result<EmailMessage>.Failure($"Failed to create domain email message: {ex.Message}");
        }
    }

    private async Task<Result> SendViaSmtpAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken)
    {
        try
        {
            using var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                EnableSsl = _smtpSettings.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
                Subject = emailMessage.Subject,
                Body = emailMessage.HtmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(emailMessage.ToEmail, emailMessage.ToName));

            // Add plain text alternative if provided
            if (!string.IsNullOrWhiteSpace(emailMessage.PlainTextBody))
            {
                var plainTextView = AlternateView.CreateAlternateViewFromString(
                    emailMessage.PlainTextBody, Encoding.UTF8, "text/plain");
                mailMessage.AlternateViews.Add(plainTextView);
            }

            // Add attachments if any
            if (emailMessage.Attachments?.Any() == true)
            {
                foreach (var attachment in emailMessage.Attachments)
                {
                    var mailAttachment = new Attachment(
                        new MemoryStream(attachment.Content), 
                        attachment.FileName, 
                        attachment.ContentType);
                    mailMessage.Attachments.Add(mailAttachment);
                }
            }

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed for email to {ToEmail}", emailMessage.ToEmail);
            return Result.Failure($"SMTP send failed: {ex.Message}");
        }
    }
}

/// <summary>
/// SMTP configuration settings
/// </summary>
public class SmtpSettings
{
    public const string SectionName = "SmtpSettings";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}