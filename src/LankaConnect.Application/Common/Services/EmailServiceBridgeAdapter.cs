using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Common.Services;

/// <summary>
/// Phase 6A.87: Bridge adapter that connects TypedEmailServiceAdapter to the existing IEmailService.
///
/// Purpose:
/// - Implements IEmailServiceBridge (from Shared project)
/// - Wraps IEmailService (from Application project)
/// - Converts Result pattern to simple bool for adapter use
/// - Provides logging for traceability
///
/// Architecture:
/// TypedEmailServiceAdapter (Shared)
///     → IEmailServiceBridge (Shared interface)
///     → EmailServiceBridgeAdapter (Application implementation)
///     → IEmailService (Application interface)
///     → EmailService (Infrastructure implementation)
///
/// This separation ensures:
/// - No circular dependencies between projects
/// - Shared project remains independent of Application
/// - Testable at each layer
/// </summary>
public class EmailServiceBridgeAdapter : IEmailServiceBridge
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailServiceBridgeAdapter> _logger;

    public EmailServiceBridgeAdapter(
        IEmailService emailService,
        ILogger<EmailServiceBridgeAdapter> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> SendTemplatedEmailAsync(
        string templateName,
        string recipientEmail,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "EmailServiceBridgeAdapter: Sending email via IEmailService. Template={Template}, Recipient={Recipient}",
                templateName, recipientEmail);

            var result = await _emailService.SendTemplatedEmailAsync(
                templateName,
                recipientEmail,
                parameters,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogDebug(
                    "EmailServiceBridgeAdapter: Email sent successfully. Template={Template}",
                    templateName);
                return true;
            }
            else
            {
                _logger.LogWarning(
                    "EmailServiceBridgeAdapter: Email send failed. Template={Template}, Errors={Errors}",
                    templateName, string.Join(", ", result.Errors));
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "EmailServiceBridgeAdapter: Exception while sending email. Template={Template}, Recipient={Recipient}",
                templateName, recipientEmail);
            throw; // Let the adapter handle the exception
        }
    }
}
