using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LankaConnect.Infrastructure.Email.Configuration;
using LankaConnect.Infrastructure.Email.Interfaces;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Email.Models;

namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Simple SMTP-based email service for MailHog integration
/// </summary>
public class SimpleEmailService : ISimpleEmailService, IDisposable
{
    private readonly EmailSettings _emailSettings;
    private readonly IEmailTemplateService? _templateService;
    private readonly ILogger<SimpleEmailService> _logger;
    private readonly SmtpClient _smtpClient;

    public SimpleEmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<SimpleEmailService> logger,
        IEmailTemplateService? templateService = null)
    {
        _emailSettings = emailSettings.Value;
        _templateService = templateService;
        _logger = logger;
        _smtpClient = CreateSmtpClient();
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        return await SendEmailAsync(to, subject, body, null, null, null, null, cancellationToken);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string textBody, string htmlBody, CancellationToken cancellationToken = default)
    {
        return await SendEmailAsync(to, subject, textBody, htmlBody, null, null, null, cancellationToken);
    }

    public async Task<bool> SendEmailAsync(
        string to, 
        string subject, 
        string textBody, 
        string? htmlBody = null, 
        string? toDisplayName = null, 
        string? fromEmail = null, 
        string? fromDisplayName = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var emailMessage = new SimpleEmailMessage
            {
                To = to,
                ToDisplayName = toDisplayName,
                From = fromEmail ?? _emailSettings.SenderEmail,
                FromDisplayName = fromDisplayName ?? _emailSettings.SenderName,
                Subject = subject,
                TextBody = textBody,
                HtmlBody = htmlBody
            };

            return await SendEmailAsync(emailMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient} with subject {Subject}", to, subject);
            return false;
        }
    }

    public async Task<bool> SendTemplatedEmailAsync(string to, string templateName, Dictionary<string, object> templateData, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_templateService == null)
            {
                _logger.LogError("Template service not available for template {TemplateName}", templateName);
                return false;
            }

            var templateInfoResult = await _templateService.GetTemplateInfoAsync(templateName, cancellationToken);
            if (!templateInfoResult.IsSuccess)
            {
                _logger.LogError("Template {TemplateName} not found: {Error}", templateName, templateInfoResult.Error);
                return false;
            }

            var renderResult = await _templateService.RenderTemplateAsync(templateName, templateData, cancellationToken);
            if (!renderResult.IsSuccess)
            {
                _logger.LogError("Failed to render template {TemplateName}: {Error}", templateName, renderResult.Error);
                return false;
            }

            var renderedTemplate = renderResult.Value;
            var subject = renderedTemplate.Subject;
            var textBody = renderedTemplate.PlainTextBody;
            var htmlBody = renderedTemplate.HtmlBody;
            
            var emailMessage = new SimpleEmailMessage
            {
                To = to,
                ToDisplayName = templateData.TryGetValue("Name", out var name) ? name?.ToString() : null,
                From = _emailSettings.SenderEmail,
                FromDisplayName = _emailSettings.SenderName,
                Subject = subject,
                TextBody = textBody,
                HtmlBody = htmlBody
            };

            return await SendEmailAsync(emailMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send templated email to {Recipient} using template {TemplateName}", to, templateName);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(SimpleEmailMessage emailMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending email to {Recipient} with subject {Subject}", 
                emailMessage.To, emailMessage.Subject);

            // If in development mode and configured to save emails to file
            if (_emailSettings.IsDevelopment && _emailSettings.SaveEmailsToFile)
            {
                await SaveEmailToFileAsync(emailMessage, cancellationToken);
                emailMessage.MessageId = "file-saved";
                _logger.LogInformation("Email saved to file for {Recipient}", emailMessage.To);
                return true;
            }

            using var mailMessage = CreateMailMessage(emailMessage);
            
            await _smtpClient.SendMailAsync(mailMessage, cancellationToken);
            
            emailMessage.MessageId = mailMessage.Headers["Message-ID"];
            
            _logger.LogInformation("Email sent successfully to {Recipient}", emailMessage.To);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", emailMessage.To);
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing SMTP connection to {Server}:{Port}", 
                _emailSettings.SmtpServer, _emailSettings.SmtpPort);

            // Create a test email
            var testEmail = new SimpleEmailMessage
            {
                To = _emailSettings.SenderEmail,
                From = _emailSettings.SenderEmail,
                FromDisplayName = _emailSettings.SenderName,
                Subject = "LankaConnect Email Test - " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                TextBody = $"This is a test email from LankaConnect sent at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.\n\nIf you receive this email, your email configuration is working correctly!",
                HtmlBody = $@"
                    <html>
                    <body>
                        <h2>LankaConnect Email Test</h2>
                        <p>This is a test email sent at <strong>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</strong>.</p>
                        <p>If you receive this email, your email configuration is working correctly!</p>
                        <hr>
                        <p><em>Sent from LankaConnect Email Service</em></p>
                    </body>
                    </html>"
            };

            return await SendEmailAsync(testEmail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP connection test failed");
            return false;
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
        {
            EnableSsl = _emailSettings.EnableSsl,
            Timeout = _emailSettings.TimeoutInSeconds * 1000
        };

        if (!string.IsNullOrEmpty(_emailSettings.Username))
        {
            client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
        }

        _logger.LogDebug("Created SMTP client for {Server}:{Port}, SSL: {EnableSsl}", 
            _emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.EnableSsl);

        return client;
    }

    private static MailMessage CreateMailMessage(SimpleEmailMessage emailMessage)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(emailMessage.From!, emailMessage.FromDisplayName),
            Subject = emailMessage.Subject,
            Body = emailMessage.TextBody,
            IsBodyHtml = false // We'll set this up with alternate views if HTML is present
        };

        mailMessage.To.Add(new MailAddress(emailMessage.To, emailMessage.ToDisplayName));

        if (!string.IsNullOrEmpty(emailMessage.HtmlBody))
        {
            // Create alternate views for both text and HTML
            var textView = AlternateView.CreateAlternateViewFromString(emailMessage.TextBody, null, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(emailMessage.HtmlBody, null, "text/html");
            
            mailMessage.AlternateViews.Add(textView);
            mailMessage.AlternateViews.Add(htmlView);
        }

        // Set priority
        mailMessage.Priority = emailMessage.Priority switch
        {
            1 => MailPriority.High,
            2 => MailPriority.High,
            3 => MailPriority.Normal,
            4 => MailPriority.Low,
            5 => MailPriority.Low,
            _ => MailPriority.Normal
        };

        return mailMessage;
    }

    private async Task SaveEmailToFileAsync(SimpleEmailMessage emailMessage, CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.Combine(Directory.GetCurrentDirectory(), _emailSettings.EmailSaveDirectory);
            Directory.CreateDirectory(directory);

            var fileName = $"email_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.txt";
            var filePath = Path.Combine(directory, fileName);

            var content = $"""
                To: {emailMessage.To}
                {(string.IsNullOrEmpty(emailMessage.ToDisplayName) ? "" : $"To Name: {emailMessage.ToDisplayName}")}
                From: {emailMessage.From}
                {(string.IsNullOrEmpty(emailMessage.FromDisplayName) ? "" : $"From Name: {emailMessage.FromDisplayName}")}
                Subject: {emailMessage.Subject}
                Priority: {emailMessage.Priority}
                Sent: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                
                TEXT BODY:
                {emailMessage.TextBody}
                
                {(string.IsNullOrEmpty(emailMessage.HtmlBody) ? "" : $"HTML BODY:\n{emailMessage.HtmlBody}")}
                """;

            await File.WriteAllTextAsync(filePath, content, cancellationToken);
            
            _logger.LogDebug("Email saved to file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save email to file");
            // Don't throw - this is just for development
        }
    }

    public void Dispose()
    {
        _smtpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}