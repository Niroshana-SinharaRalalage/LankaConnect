using Microsoft.AspNetCore.Mvc;
using LankaConnect.Infrastructure.Email.Interfaces;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Email testing controller for development
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "dev")]
public class EmailController : ControllerBase
{
    private readonly ISimpleEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(ISimpleEmailService emailService, ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Test email connection
    /// </summary>
    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var result = await _emailService.TestConnectionAsync();
            
            if (result)
            {
                return Ok(new { success = true, message = "Email connection test successful" });
            }
            
            return BadRequest(new { success = false, message = "Email connection test failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing email connection");
            return StatusCode(500, new { success = false, message = "Internal server error during email connection test" });
        }
    }

    /// <summary>
    /// Send a test email
    /// </summary>
    [HttpPost("send-test")]
    public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _emailService.SendEmailAsync(
                request.To,
                request.Subject ?? "LankaConnect Test Email",
                request.TextBody ?? "This is a test email from LankaConnect API.",
                request.HtmlBody,
                request.ToName
            );

            if (result)
            {
                return Ok(new { success = true, message = "Email sent successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to send email" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test email");
            return StatusCode(500, new { success = false, message = "Internal server error during email send" });
        }
    }

    /// <summary>
    /// Send a templated email
    /// </summary>
    [HttpPost("send-template")]
    public async Task<IActionResult> SendTemplatedEmail([FromBody] TemplatedEmailRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _emailService.SendTemplatedEmailAsync(
                request.To,
                request.TemplateName,
                request.TemplateData ?? new Dictionary<string, object>()
            );

            if (result)
            {
                return Ok(new { success = true, message = "Templated email sent successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to send templated email" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending templated email");
            return StatusCode(500, new { success = false, message = "Internal server error during templated email send" });
        }
    }
}

/// <summary>
/// Test email request model
/// </summary>
public class TestEmailRequest
{
    public required string To { get; set; }
    public string? ToName { get; set; }
    public string? Subject { get; set; }
    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }
}

/// <summary>
/// Templated email request model
/// </summary>
public class TemplatedEmailRequest
{
    public required string To { get; set; }
    public required string TemplateName { get; set; }
    public Dictionary<string, object>? TemplateData { get; set; }
}