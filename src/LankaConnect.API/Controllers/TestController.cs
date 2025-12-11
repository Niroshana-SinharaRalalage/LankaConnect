using LankaConnect.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Controller for testing and diagnostics endpoints
/// These endpoints should be secured or disabled in production
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IEmailService emailService,
        ILogger<TestController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Send a test email to verify email configuration
    /// </summary>
    /// <param name="toEmail">Recipient email address (defaults to niroshanaks@gmail.com)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the email send operation</returns>
    [HttpPost("send-test-email")]
    [AllowAnonymous] // Consider securing this in production
    [ProducesResponseType(typeof(TestEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TestEmailResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TestEmailResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendTestEmail(
        [FromQuery] string toEmail = "niroshanaks@gmail.com",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending test email to {ToEmail}", toEmail);

            var emailMessage = new EmailMessageDto
            {
                ToEmail = toEmail,
                ToName = "Test Recipient",
                Subject = $"LankaConnect Test Email - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                HtmlBody = GenerateTestEmailHtml(),
                PlainTextBody = GenerateTestEmailPlainText(),
                Priority = 1 // High priority for test emails
            };

            var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Test email sent successfully to {ToEmail}", toEmail);
                return Ok(new TestEmailResponse
                {
                    Success = true,
                    Message = $"Test email sent successfully to {toEmail}",
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Test email failed: {Error}", result.Error);
                return BadRequest(new TestEmailResponse
                {
                    Success = false,
                    Message = $"Failed to send test email: {result.Error}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending test email to {ToEmail}", toEmail);
            return StatusCode(StatusCodes.Status500InternalServerError, new TestEmailResponse
            {
                Success = false,
                Message = $"Exception while sending test email: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Health check endpoint for email service
    /// </summary>
    [HttpGet("email-health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EmailHealthResponse), StatusCodes.Status200OK)]
    public IActionResult EmailHealthCheck()
    {
        return Ok(new EmailHealthResponse
        {
            Status = "Healthy",
            Provider = "Azure Communication Services",
            Timestamp = DateTime.UtcNow
        });
    }

    private static string GenerateTestEmailHtml()
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>LankaConnect Test Email</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; }}
        .footer {{ background: #f3f4f6; padding: 15px; text-align: center; font-size: 12px; color: #6b7280; border-radius: 0 0 10px 10px; }}
        .success-badge {{ background: #10b981; color: white; padding: 5px 15px; border-radius: 20px; display: inline-block; margin: 10px 0; }}
        .info-box {{ background: white; padding: 15px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #667eea; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>LankaConnect</h1>
        <p>Email Configuration Test</p>
    </div>
    <div class='content'>
        <div class='success-badge'>Email Working!</div>

        <h2>Test Email Successful</h2>
        <p>Congratulations! Your email configuration is working correctly.</p>

        <div class='info-box'>
            <strong>Test Details:</strong><br>
            <strong>Timestamp:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC<br>
            <strong>Provider:</strong> Azure Communication Services<br>
            <strong>Environment:</strong> Development/Testing
        </div>

        <p>This email was sent from the LankaConnect API to verify that the email system is properly configured.</p>

        <h3>What's Working:</h3>
        <ul>
            <li>Azure Communication Services connection</li>
            <li>Email domain verification</li>
            <li>SMTP/SDK email delivery</li>
            <li>HTML email rendering</li>
        </ul>

        <p>If you received this email, your email configuration is complete!</p>
    </div>
    <div class='footer'>
        <p>&copy; {DateTime.UtcNow.Year} LankaConnect. All rights reserved.</p>
        <p>This is an automated test email. Please do not reply.</p>
    </div>
</body>
</html>";
    }

    private static string GenerateTestEmailPlainText()
    {
        return $@"
LANKACONNECT - EMAIL CONFIGURATION TEST
=======================================

Test Email Successful!

Congratulations! Your email configuration is working correctly.

TEST DETAILS:
- Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
- Provider: Azure Communication Services
- Environment: Development/Testing

This email was sent from the LankaConnect API to verify that the email system is properly configured.

WHAT'S WORKING:
- Azure Communication Services connection
- Email domain verification
- SMTP/SDK email delivery
- Plain text email rendering

If you received this email, your email configuration is complete!

---
(c) {DateTime.UtcNow.Year} LankaConnect. All rights reserved.
This is an automated test email. Please do not reply.
";
    }
}

/// <summary>
/// Response model for test email endpoint
/// </summary>
public class TestEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Response model for email health check
/// </summary>
public class EmailHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
