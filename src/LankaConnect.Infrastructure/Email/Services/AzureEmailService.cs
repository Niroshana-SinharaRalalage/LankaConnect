using Azure;
using Azure.Communication.Email;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Infrastructure.Email.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EmailValueObject = LankaConnect.Domain.Shared.ValueObjects.Email;
using DomainEmailMessage = LankaConnect.Domain.Communications.Entities.EmailMessage;
using AzureEmailMessage = Azure.Communication.Email.EmailMessage;
using AzureEmailAttachment = Azure.Communication.Email.EmailAttachment;

namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Phase 6A.43 Fix: Email service implementation using Azure Communication Services SDK.
/// Now implements both IEmailService and IEmailTemplateService to provide unified email functionality.
/// This ensures all email templates (free and paid events) use database-stored templates consistently.
/// </summary>
public class AzureEmailService : IEmailService, IEmailTemplateService
{
    private readonly ILogger<AzureEmailService> _logger;
    private readonly IEmailMessageRepository _emailMessageRepository;
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly EmailSettings _emailSettings;
    private readonly EmailClient? _azureEmailClient;

    public AzureEmailService(
        ILogger<AzureEmailService> logger,
        IEmailMessageRepository emailMessageRepository,
        IEmailTemplateRepository emailTemplateRepository,
        IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _emailMessageRepository = emailMessageRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _emailSettings = emailSettings.Value;

        // Initialize Azure Email Client if Azure provider is configured
        if (_emailSettings.Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(_emailSettings.AzureConnectionString))
        {
            _azureEmailClient = new EmailClient(_emailSettings.AzureConnectionString);
            _logger.LogInformation("Azure Email Service initialized with sender: {Sender}",
                _emailSettings.AzureSenderAddress);
        }
        else
        {
            _logger.LogWarning("Azure Email Service not configured. Provider: {Provider}",
                _emailSettings.Provider);
        }
    }

    public async Task<Result> SendEmailAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO-REMOVE: Diagnostic logging
            _logger.LogError("[DIAG-EMAIL] SendEmailAsync START - ToEmail: {ToEmail}, Subject: {Subject}",
                emailMessage.ToEmail, emailMessage.Subject);

            _logger.LogInformation("Attempting to send email to {ToEmail} with subject '{Subject}'",
                emailMessage.ToEmail, emailMessage.Subject);

            // Validate input
            var validationResult = ValidateEmailMessage(emailMessage);
            if (validationResult.IsFailure)
            {
                // TODO-REMOVE: Diagnostic logging
                _logger.LogError("[DIAG-EMAIL] Validation FAILED - Error: {Error}", validationResult.Error);
                _logger.LogWarning("Email validation failed: {Error}", validationResult.Error);
                return validationResult;
            }

            // TODO-REMOVE: Diagnostic logging
            _logger.LogError("[DIAG-EMAIL] Validation PASSED - Creating domain entity");

            // Create domain entity for tracking
            var domainEmailResult = await CreateDomainEmailMessage(emailMessage, cancellationToken);
            if (domainEmailResult.IsFailure)
            {
                // TODO-REMOVE: Diagnostic logging
                _logger.LogError("[DIAG-EMAIL] Domain entity creation FAILED - Error: {Error}", domainEmailResult.Error);
                return Result.Failure(domainEmailResult.Error);
            }

            // TODO-REMOVE: Diagnostic logging
            _logger.LogError("[DIAG-EMAIL] Domain entity CREATED - Provider: {Provider}", _emailSettings.Provider);

            // Send based on provider
            Result sendResult;
            if (_emailSettings.Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                // TODO-REMOVE: Diagnostic logging
                _logger.LogError("[DIAG-EMAIL] Calling SendViaAzureAsync");
                sendResult = await SendViaAzureAsync(emailMessage, cancellationToken);
            }
            else
            {
                // TODO-REMOVE: Diagnostic logging
                _logger.LogError("[DIAG-EMAIL] Calling SendViaSmtpAsync");
                sendResult = await SendViaSmtpAsync(emailMessage, cancellationToken);
            }

            // TODO-REMOVE: Diagnostic logging
            _logger.LogError("[DIAG-EMAIL] Provider send COMPLETED - Success: {Success}, Error: {Error}",
                sendResult.IsSuccess, sendResult.Error ?? "None");

            if (sendResult.IsFailure)
            {
                // Mark domain entity as failed
                domainEmailResult.Value.MarkAsFailed(sendResult.Error ?? "Unknown error");
                _emailMessageRepository.Update(domainEmailResult.Value);
                // TODO-REMOVE: Diagnostic logging
                _logger.LogError("[DIAG-EMAIL] Domain entity marked as FAILED");
                return sendResult;
            }

            // Mark domain entity as sent
            domainEmailResult.Value.MarkAsSent();
            _emailMessageRepository.Update(domainEmailResult.Value);

            // TODO-REMOVE: Diagnostic logging
            _logger.LogError("[DIAG-EMAIL] Domain entity marked as SENT - SUCCESS");

            _logger.LogInformation("Email sent successfully to {ToEmail}", emailMessage.ToEmail);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // TODO-REMOVE: Diagnostic logging
            _logger.LogError(ex, "[DIAG-EMAIL] EXCEPTION in SendEmailAsync - ExceptionType: {ExceptionType}, Message: {Message}",
                ex.GetType().Name, ex.Message);

            _logger.LogError(ex, "Failed to send email to {ToEmail}", emailMessage.ToEmail);
            return Result.Failure($"Failed to send email: {ex.Message}");
        }
    }

    public async Task<Result> SendTemplatedEmailAsync(string templateName, string recipientEmail,
        Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO-REMOVE: Diagnostic logging - using LogError to bypass log filtering in staging
            _logger.LogError("[DIAG-EMAIL] SendTemplatedEmailAsync START - Template: {TemplateName}, Recipient: {RecipientEmail}, Provider: {Provider}, HasAzureClient: {HasClient}",
                templateName, recipientEmail, _emailSettings.Provider, _azureEmailClient != null);

            _logger.LogInformation("Sending templated email '{TemplateName}' to {RecipientEmail}",
                templateName, recipientEmail);

            // Get template from database (Phase 6A.34 Fix: Use database template directly for rendering)
            var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
            if (template == null)
            {
                // TODO-REMOVE: Diagnostic logging
                _logger.LogError("[DIAG-EMAIL] Template NOT FOUND - TemplateName: {TemplateName}", templateName);
                _logger.LogWarning("Email template '{TemplateName}' not found in database", templateName);
                return Result.Failure($"Email template '{templateName}' not found");
            }

            // TODO-REMOVE: Diagnostic logging
            _logger.LogError("[DIAG-EMAIL] Template FOUND - IsActive: {IsActive}, HasHtml: {HasHtml}, SubjectLength: {SubjectLen}",
                template.IsActive, !string.IsNullOrEmpty(template.HtmlTemplate), template.SubjectTemplate.Value?.Length ?? 0);

            if (!template.IsActive)
            {
                // TODO-REMOVE: Diagnostic logging
                _logger.LogError("[DIAG-EMAIL] Template INACTIVE - TemplateName: {TemplateName}", templateName);
                _logger.LogWarning("Email template '{TemplateName}' is not active", templateName);
                return Result.Failure($"Email template '{templateName}' is not active");
            }

            // Phase 6A.34 Fix: Render template directly from database content
            // Previously used RazorEmailTemplateService which reads from filesystem files
            // This caused a mismatch: database template was validated but filesystem template was rendered
            var subject = RenderTemplateContent(template.SubjectTemplate.Value ?? string.Empty, parameters);
            var htmlBody = RenderTemplateContent(template.HtmlTemplate ?? string.Empty, parameters);
            var textBody = RenderTemplateContent(template.TextTemplate ?? string.Empty, parameters);

            // TODO-REMOVE: Diagnostic logging
            _logger.LogError("[DIAG-EMAIL] Template RENDERED - SubjectLen: {SubjectLen}, HtmlLen: {HtmlLen}, TextLen: {TextLen}",
                subject?.Length ?? 0, htmlBody?.Length ?? 0, textBody?.Length ?? 0);

            _logger.LogInformation("Template '{TemplateName}' rendered from database successfully", templateName);

            // Create email message DTO
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                Subject = subject ?? string.Empty,
                HtmlBody = htmlBody ?? string.Empty,
                PlainTextBody = textBody ?? string.Empty,
                FromEmail = _emailSettings.FromEmail,
                FromName = _emailSettings.FromName,
                Priority = 2 // Normal priority for templated emails
            };

            // TODO-REMOVE: Diagnostic logging
            _logger.LogError("[DIAG-EMAIL] Calling SendEmailAsync - FromEmail: {FromEmail}, FromName: {FromName}, ToEmail: {ToEmail}",
                _emailSettings.FromEmail, _emailSettings.FromName, recipientEmail);

            // Send the email
            var result = await SendEmailAsync(emailMessage, cancellationToken);

            // TODO-REMOVE: Diagnostic logging
            _logger.LogError("[DIAG-EMAIL] SendEmailAsync COMPLETED - Success: {Success}, Error: {Error}",
                result.IsSuccess, result.Error ?? "None");

            return result;
        }
        catch (Exception ex)
        {
            // TODO-REMOVE: Diagnostic logging
            _logger.LogError(ex, "[DIAG-EMAIL] EXCEPTION in SendTemplatedEmailAsync - Template: {TemplateName}, Recipient: {RecipientEmail}, ExceptionType: {ExceptionType}, Message: {Message}",
                templateName, recipientEmail, ex.GetType().Name, ex.Message);

            _logger.LogError(ex, "Failed to send templated email '{TemplateName}' to {RecipientEmail}",
                templateName, recipientEmail);
            return Result.Failure($"Failed to send templated email: {ex.Message}");
        }
    }

    /// <summary>
    /// Phase 6A.37: Sends an email using a template with parameters and inline image attachments.
    /// Attachments with ContentId are embedded using CID for immediate display in email clients.
    /// </summary>
    public async Task<Result> SendTemplatedEmailAsync(string templateName, string recipientEmail,
        Dictionary<string, object> parameters, List<Application.Common.Interfaces.EmailAttachment>? attachments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending templated email '{TemplateName}' to {RecipientEmail} with {AttachmentCount} attachments",
                templateName, recipientEmail, attachments?.Count ?? 0);

            // Get template from database
            var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
            if (template == null)
            {
                _logger.LogWarning("Email template '{TemplateName}' not found in database", templateName);
                return Result.Failure($"Email template '{templateName}' not found");
            }

            if (!template.IsActive)
            {
                _logger.LogWarning("Email template '{TemplateName}' is not active", templateName);
                return Result.Failure($"Email template '{templateName}' is not active");
            }

            // Render template directly from database content
            var subject = RenderTemplateContent(template.SubjectTemplate.Value, parameters);
            var htmlBody = RenderTemplateContent(template.HtmlTemplate ?? string.Empty, parameters);
            var textBody = RenderTemplateContent(template.TextTemplate, parameters);

            _logger.LogInformation("Template '{TemplateName}' rendered from database successfully", templateName);

            // Create email message DTO with attachments
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                Subject = subject,
                HtmlBody = htmlBody,
                PlainTextBody = textBody,
                FromEmail = _emailSettings.FromEmail,
                FromName = _emailSettings.FromName,
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

    /// <summary>
    /// Phase 6A.34: Render template content by replacing {{variable}} placeholders
    /// Phase 6A.74 Part 14 Fix: Now supports Handlebars-style syntax:
    /// - {{#if variable}}...{{/if}} conditionals (with proper NESTED support)
    /// - {{#variable}}...{{/variable}} conditionals (legacy)
    /// - {{{variable}}} triple-brace unescaped placeholders
    /// - {{variable}} double-brace placeholders
    /// Phase 6A.75 Fix: Implemented bracket-matching algorithm for nested {{#if}} conditionals
    /// </summary>
    private static string RenderTemplateContent(string template, Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        var result = template;

        // Phase 6A.54: Validate parameters before processing to prevent Result<T> objects
        foreach (var param in parameters)
        {
            if (param.Value != null && param.Value.GetType().FullName?.Contains("Result") == true)
            {
                throw new InvalidOperationException(
                    $"Template parameter '{param.Key}' contains a Result<T> object (Type: {param.Value.GetType().FullName}). " +
                    "All parameters must be unwrapped to primitive values before template rendering. " +
                    "Use .Value property to extract the value from Result<T> objects.");
            }
        }

        // Phase 6A.75 Fix: Process ALL Handlebars-style conditionals with proper nesting support
        // We need to process from innermost to outermost, so we repeatedly process until no more changes
        result = ProcessNestedConditionals(result, parameters);

        // Also support legacy {{#variable}}...{{/variable}} syntax
        foreach (var param in parameters)
        {
            var isTruthy = IsTruthyValue(param.Value);

            var legacyOpenTag = $"{{{{#{param.Key}}}}}";
            var legacyCloseTag = $"{{{{/{param.Key}}}}}";

            int startIndex = 0;
            while ((startIndex = result.IndexOf(legacyOpenTag, startIndex, StringComparison.Ordinal)) != -1)
            {
                var endIndex = result.IndexOf(legacyCloseTag, startIndex, StringComparison.Ordinal);
                if (endIndex == -1) break;

                if (isTruthy)
                {
                    result = result.Remove(endIndex, legacyCloseTag.Length);
                    result = result.Remove(startIndex, legacyOpenTag.Length);
                }
                else
                {
                    result = result.Remove(startIndex, endIndex - startIndex + legacyCloseTag.Length);
                }
            }
        }

        // Phase 6A.74 Part 14 Fix: Replace triple-brace placeholders first {{{variable}}} (unescaped HTML)
        foreach (var param in parameters)
        {
            var triplePlaceholder = $"{{{{{{{param.Key}}}}}}}";
            result = result.Replace(triplePlaceholder, param.Value?.ToString() ?? string.Empty);
        }

        // Then replace double-brace placeholders: {{variable}}
        foreach (var param in parameters)
        {
            var placeholder = $"{{{{{param.Key}}}}}";
            result = result.Replace(placeholder, param.Value?.ToString() ?? string.Empty);
        }

        return result;
    }

    /// <summary>
    /// Phase 6A.75: Helper method to determine if a value is truthy for conditional processing
    /// </summary>
    private static bool IsTruthyValue(object? value)
    {
        return value switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            null => false,
            _ => true
        };
    }

    /// <summary>
    /// Phase 6A.75: Process nested {{#if variable}}...{{/if}} conditionals using bracket-matching
    /// This algorithm correctly handles nested conditionals by finding the MATCHING {{/if}} tag
    /// using a depth counter to track open/close pairs.
    /// </summary>
    private static string ProcessNestedConditionals(string template, Dictionary<string, object> parameters)
    {
        var result = template;
        const string ifOpenPattern = "{{#if ";
        const string ifCloseTag = "{{/if}}";

        // Keep processing until no more {{#if}} tags remain
        bool madeChanges;
        int maxIterations = 100; // Safety limit to prevent infinite loops
        int iteration = 0;

        do
        {
            madeChanges = false;
            iteration++;

            // Find the innermost {{#if}} block (one that has no nested {{#if}} inside it)
            // This ensures we process from inside-out
            int searchStart = 0;
            int innermostStart = -1;
            string? innermostVariable = null;

            while (true)
            {
                var ifStart = result.IndexOf(ifOpenPattern, searchStart, StringComparison.Ordinal);
                if (ifStart == -1) break;

                // Extract the variable name
                var varStart = ifStart + ifOpenPattern.Length;
                var varEnd = result.IndexOf("}}", varStart, StringComparison.Ordinal);
                if (varEnd == -1) break;

                var variableName = result.Substring(varStart, varEnd - varStart).Trim();
                var fullOpenTag = $"{{{{#if {variableName}}}}}";
                var openTagEnd = ifStart + fullOpenTag.Length;

                // Find the matching {{/if}} using bracket counting
                int matchingCloseIndex = FindMatchingCloseTag(result, openTagEnd);

                if (matchingCloseIndex == -1)
                {
                    // No matching close tag, skip this one
                    searchStart = openTagEnd;
                    continue;
                }

                // Check if there are any nested {{#if}} between this open and its matching close
                var contentBetween = result.Substring(openTagEnd, matchingCloseIndex - openTagEnd);
                if (!contentBetween.Contains(ifOpenPattern))
                {
                    // This is an innermost block (no nested {{#if}} inside)
                    innermostStart = ifStart;
                    innermostVariable = variableName;
                    break;
                }

                // There are nested blocks, continue searching for a deeper one
                searchStart = openTagEnd;
            }

            // Process the innermost block we found
            if (innermostStart != -1 && innermostVariable != null)
            {
                var fullOpenTag = $"{{{{#if {innermostVariable}}}}}";
                var openTagEnd = innermostStart + fullOpenTag.Length;
                var matchingCloseIndex = FindMatchingCloseTag(result, openTagEnd);

                if (matchingCloseIndex != -1)
                {
                    // Check if this variable has a truthy value in parameters
                    var isTruthy = false;
                    if (parameters.TryGetValue(innermostVariable, out var paramValue))
                    {
                        isTruthy = IsTruthyValue(paramValue);
                    }

                    if (isTruthy)
                    {
                        // Keep the content, remove only the tags
                        var contentStart = openTagEnd;
                        var content = result.Substring(contentStart, matchingCloseIndex - contentStart);

                        // Replace the entire block (open tag + content + close tag) with just the content
                        result = result.Substring(0, innermostStart) +
                                 content +
                                 result.Substring(matchingCloseIndex + ifCloseTag.Length);
                    }
                    else
                    {
                        // Remove the entire section including tags
                        result = result.Substring(0, innermostStart) +
                                 result.Substring(matchingCloseIndex + ifCloseTag.Length);
                    }

                    madeChanges = true;
                }
            }

        } while (madeChanges && iteration < maxIterations);

        return result;
    }

    /// <summary>
    /// Phase 6A.75: Find the matching {{/if}} tag for an opening {{#if}} tag
    /// Uses bracket-counting to handle nested conditionals correctly.
    /// </summary>
    /// <param name="template">The template string</param>
    /// <param name="startAfterOpenTag">Position right after the opening {{#if variable}} tag</param>
    /// <returns>Index of the matching {{/if}} tag, or -1 if not found</returns>
    private static int FindMatchingCloseTag(string template, int startAfterOpenTag)
    {
        const string ifOpenPattern = "{{#if ";
        const string ifCloseTag = "{{/if}}";

        int depth = 1; // We start at depth 1 because we've already passed one opening tag
        int currentPos = startAfterOpenTag;

        while (depth > 0 && currentPos < template.Length)
        {
            // Find the next occurrence of either {{#if or {{/if}}
            var nextOpen = template.IndexOf(ifOpenPattern, currentPos, StringComparison.Ordinal);
            var nextClose = template.IndexOf(ifCloseTag, currentPos, StringComparison.Ordinal);

            if (nextClose == -1)
            {
                // No more closing tags found
                return -1;
            }

            if (nextOpen == -1 || nextClose < nextOpen)
            {
                // The next tag is a closing tag
                depth--;
                if (depth == 0)
                {
                    // This is our matching close tag
                    return nextClose;
                }
                currentPos = nextClose + ifCloseTag.Length;
            }
            else
            {
                // The next tag is an opening tag
                depth++;
                // Skip past this opening tag - find its closing }}
                var closeBrace = template.IndexOf("}}", nextOpen + ifOpenPattern.Length, StringComparison.Ordinal);
                if (closeBrace == -1) return -1;
                currentPos = closeBrace + 2;
            }
        }

        return -1;
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

            _logger.LogInformation("Template '{TemplateName}' validation passed", templateName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating template '{TemplateName}'", templateName);
            return Result.Failure($"Error validating template: {ex.Message}");
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
            var addr = new System.Net.Mail.MailAddress(emailMessage.ToEmail);
            if (addr.Address != emailMessage.ToEmail)
                return Result.Failure("Invalid recipient email format");
        }
        catch
        {
            return Result.Failure("Invalid recipient email format");
        }

        return Result.Success();
    }

    private async Task<Result<DomainEmailMessage>> CreateDomainEmailMessage(EmailMessageDto dto, CancellationToken cancellationToken)
    {
        try
        {
            // Create domain value objects
            _logger.LogInformation("[DIAG-EMAIL-1] Creating domain email - From: {From}, To: {To}, Subject length: {SubjectLen}",
                _emailSettings.FromEmail, dto.ToEmail, dto.Subject?.Length ?? 0);

            var fromEmailResult = EmailValueObject.Create(_emailSettings.FromEmail);
            if (fromEmailResult.IsFailure)
            {
                _logger.LogError("[DIAG-EMAIL-2] FromEmail validation failed: {Error}", fromEmailResult.Error);
                return Result<DomainEmailMessage>.Failure(fromEmailResult.Error);
            }

            var toEmailResult = EmailValueObject.Create(dto.ToEmail);
            if (toEmailResult.IsFailure)
            {
                _logger.LogError("[DIAG-EMAIL-3] ToEmail validation failed for {Email}: {Error}", dto.ToEmail, toEmailResult.Error);
                return Result<DomainEmailMessage>.Failure(toEmailResult.Error);
            }

            _logger.LogInformation("[DIAG-EMAIL-4] About to create EmailSubject from: '{Subject}'", dto.Subject);
            var subjectResult = EmailSubject.Create(dto.Subject ?? string.Empty);
            if (subjectResult.IsFailure)
            {
                _logger.LogError("[DIAG-EMAIL-5] EmailSubject validation failed for '{Subject}': {Error}", dto.Subject, subjectResult.Error);
                return Result<DomainEmailMessage>.Failure(subjectResult.Error);
            }

            // Create domain entity
            var emailMessageResult = DomainEmailMessage.Create(
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
                return Result<DomainEmailMessage>.Failure(addRecipientResult.Error);

            // Mark as queued
            var queueResult = domainEmail.MarkAsQueued();
            if (queueResult.IsFailure)
                return Result<DomainEmailMessage>.Failure(queueResult.Error);

            // Save to database
            await _emailMessageRepository.AddAsync(domainEmail, cancellationToken);

            return Result<DomainEmailMessage>.Success(domainEmail);
        }
        catch (Exception ex)
        {
            return Result<DomainEmailMessage>.Failure($"Failed to create domain email message: {ex.Message}");
        }
    }

    private async Task<Result> SendViaAzureAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken)
    {
        if (_azureEmailClient == null)
        {
            return Result.Failure("Azure Email Client is not configured. Check AzureConnectionString in EmailSettings.");
        }

        try
        {
            var senderAddress = _emailSettings.AzureSenderAddress;
            if (string.IsNullOrEmpty(senderAddress))
            {
                return Result.Failure("Azure sender address is not configured. Check AzureSenderAddress in EmailSettings.");
            }

            // Build email content
            var emailContent = new EmailContent(emailMessage.Subject);

            if (!string.IsNullOrWhiteSpace(emailMessage.HtmlBody))
            {
                emailContent.Html = emailMessage.HtmlBody;
            }

            if (!string.IsNullOrWhiteSpace(emailMessage.PlainTextBody))
            {
                emailContent.PlainText = emailMessage.PlainTextBody;
            }
            else if (!string.IsNullOrWhiteSpace(emailMessage.HtmlBody))
            {
                // Generate plain text from HTML if not provided
                emailContent.PlainText = StripHtml(emailMessage.HtmlBody);
            }

            // Build recipients
            var recipients = new EmailRecipients(new List<EmailAddress>
            {
                new EmailAddress(emailMessage.ToEmail, emailMessage.ToName)
            });

            // Create email message
            var azureEmailMessage = new EmailMessage(senderAddress, recipients, emailContent);

            // Add attachments if any
            // Phase 6A.35: Support CID inline attachments for embedded images
            if (emailMessage.Attachments?.Any() == true)
            {
                foreach (var attachment in emailMessage.Attachments)
                {
                    var azureAttachment = new AzureEmailAttachment(
                        attachment.FileName,
                        attachment.ContentType,
                        new BinaryData(attachment.Content));

                    // Phase 6A.35: Set ContentId for inline CID attachments
                    // This allows images to be embedded in email body using src="cid:{ContentId}"
                    if (!string.IsNullOrEmpty(attachment.ContentId))
                    {
                        azureAttachment.ContentId = attachment.ContentId;
                        _logger.LogInformation("Adding inline attachment {FileName} with ContentId: {ContentId}",
                            attachment.FileName, attachment.ContentId);
                    }

                    azureEmailMessage.Attachments.Add(azureAttachment);
                }
            }

            // Send email
            _logger.LogInformation("Sending email via Azure Communication Services to {ToEmail}", emailMessage.ToEmail);

            var operation = await _azureEmailClient.SendAsync(
                WaitUntil.Completed,
                azureEmailMessage,
                cancellationToken);

            if (operation.Value.Status == EmailSendStatus.Succeeded)
            {
                _logger.LogInformation("Azure email sent successfully. Operation ID: {OperationId}",
                    operation.Id);
                return Result.Success();
            }
            else
            {
                var error = $"Azure email send failed with status: {operation.Value.Status}";
                _logger.LogError(error);
                return Result.Failure(error);
            }
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Communication Services request failed. Status: {Status}, Error: {Error}",
                ex.Status, ex.ErrorCode);
            return Result.Failure($"Azure email send failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure email send failed for {ToEmail}", emailMessage.ToEmail);
            return Result.Failure($"Azure email send failed: {ex.Message}");
        }
    }

    private async Task<Result> SendViaSmtpAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken)
    {
        try
        {
            using var smtpClient = new System.Net.Mail.SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                Timeout = _emailSettings.TimeoutInSeconds * 1000
            };

            using var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = emailMessage.Subject,
                Body = emailMessage.HtmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new System.Net.Mail.MailAddress(emailMessage.ToEmail, emailMessage.ToName));

            // Add plain text alternative if provided
            if (!string.IsNullOrWhiteSpace(emailMessage.PlainTextBody))
            {
                var plainTextView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(
                    emailMessage.PlainTextBody, System.Text.Encoding.UTF8, "text/plain");
                mailMessage.AlternateViews.Add(plainTextView);
            }

            // Add attachments if any
            if (emailMessage.Attachments?.Any() == true)
            {
                foreach (var attachment in emailMessage.Attachments)
                {
                    var mailAttachment = new System.Net.Mail.Attachment(
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

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Simple HTML stripping - remove tags
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
        // Decode HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);
        // Normalize whitespace
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    #region IEmailTemplateService Implementation

    /// <summary>
    /// Phase 6A.43 Fix: Implements RenderTemplateAsync from IEmailTemplateService.
    /// Uses database-stored templates instead of filesystem templates for consistency.
    /// This method is called by PaymentCompletedEventHandler.
    /// </summary>
    public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(
        string templateName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Phase 6A.43 Fix] RenderTemplateAsync called for template '{TemplateName}' - using DATABASE template",
                templateName);

            // Get template from database
            var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
            if (template == null)
            {
                _logger.LogWarning("Email template '{TemplateName}' not found in database", templateName);
                return Result<RenderedEmailTemplate>.Failure($"Email template '{templateName}' not found");
            }

            if (!template.IsActive)
            {
                _logger.LogWarning("Email template '{TemplateName}' is not active", templateName);
                return Result<RenderedEmailTemplate>.Failure($"Email template '{templateName}' is not active");
            }

            // Render template directly from database content using existing RenderTemplateContent method
            var subject = RenderTemplateContent(template.SubjectTemplate.Value, parameters);
            var htmlBody = RenderTemplateContent(template.HtmlTemplate ?? string.Empty, parameters);
            var textBody = RenderTemplateContent(template.TextTemplate, parameters);

            _logger.LogInformation(
                "[Phase 6A.43 Fix] Template '{TemplateName}' rendered successfully from database",
                templateName);

            var renderedTemplate = new RenderedEmailTemplate
            {
                Subject = subject,
                HtmlBody = htmlBody,
                PlainTextBody = textBody
            };

            return Result<RenderedEmailTemplate>.Success(renderedTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template '{TemplateName}'", templateName);
            return Result<RenderedEmailTemplate>.Failure($"Failed to render template: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets available email templates from database.
    /// </summary>
    public async Task<Result<List<EmailTemplateInfo>>> GetAvailableTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _emailTemplateRepository.GetAllAsync(cancellationToken);
            var templateInfos = templates.Select(t => new EmailTemplateInfo
            {
                Name = t.Name,
                DisplayName = t.Name.Replace("-", " ").Replace("_", " "),
                Description = t.Description,
                RequiredParameters = new List<string>(),
                OptionalParameters = new List<string>(),
                Category = "General",
                IsActive = t.IsActive,
                LastModified = t.UpdatedAt ?? t.CreatedAt
            }).ToList();

            return Result<List<EmailTemplateInfo>>.Success(templateInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available templates");
            return Result<List<EmailTemplateInfo>>.Failure($"Failed to get available templates: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets template metadata from database.
    /// </summary>
    public async Task<Result<EmailTemplateInfo>> GetTemplateInfoAsync(
        string templateName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
            if (template == null)
            {
                return Result<EmailTemplateInfo>.Failure($"Template '{templateName}' not found");
            }

            var info = new EmailTemplateInfo
            {
                Name = template.Name,
                DisplayName = template.Name.Replace("-", " ").Replace("_", " "),
                Description = template.Description,
                RequiredParameters = new List<string>(),
                OptionalParameters = new List<string>(),
                Category = "General",
                IsActive = template.IsActive,
                LastModified = template.UpdatedAt ?? template.CreatedAt
            };

            return Result<EmailTemplateInfo>.Success(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template info for '{TemplateName}'", templateName);
            return Result<EmailTemplateInfo>.Failure($"Failed to get template info: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates template parameters against database template.
    /// </summary>
    public async Task<Result> ValidateTemplateParametersAsync(
        string templateName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        try
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

            if (parameters == null)
            {
                return Result.Failure("Parameters cannot be null");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate template parameters for '{TemplateName}'", templateName);
            return Result.Failure($"Failed to validate template parameters: {ex.Message}");
        }
    }

    #endregion
}
