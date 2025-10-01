using LankaConnect.Infrastructure.Email.Models;

namespace LankaConnect.Infrastructure.Email.Interfaces;

/// <summary>
/// Simple email service interface for basic SMTP operations
/// </summary>
public interface ISimpleEmailService
{
    /// <summary>
    /// Send a simple text email
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an HTML email with optional text fallback
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string textBody, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email with full options
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string textBody, string? htmlBody = null, string? toDisplayName = null, string? fromEmail = null, string? fromDisplayName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an email using a template
    /// </summary>
    Task<bool> SendTemplatedEmailAsync(string to, string templateName, Dictionary<string, object> templateData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email message object
    /// </summary>
    Task<bool> SendEmailAsync(SimpleEmailMessage emailMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test email connection
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}