using LankaConnect.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LankaConnect.Host.AllInOne.Controllers;

/// <summary>
/// Temporary diagnostic endpoints for troubleshooting email template issues
/// TODO: Remove after fixing email template status
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // TEMP: For quick diagnostic access
public class DiagnosticsController : ControllerBase
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        IEmailTemplateRepository emailTemplateRepository,
        ILogger<DiagnosticsController> logger)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _logger = logger;
    }

    [HttpGet("email-templates/status")]
    public async Task<IActionResult> GetEmailTemplateStatus(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[DIAGNOSTICS] Checking email template status");

            // Get all templates
            var allTemplates = await _emailTemplateRepository.GetAllAsync(cancellationToken);

            // Get signup list templates specifically
            var signupTemplates = allTemplates
                .Where(t => t.Name.Contains("signup-list-commitment"))
                .ToList();

            var result = new
            {
                TotalTemplates = allTemplates.Count,
                ActiveTemplates = allTemplates.Count(t => t.IsActive),
                InactiveTemplates = allTemplates.Count(t => !t.IsActive),
                SignupListTemplates = signupTemplates.Select(t => new
                {
                    Name = t.Name,
                    IsActive = t.IsActive,
                    Category = t.Category,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                }),
                AllTemplates = allTemplates.Select(t => new
                {
                    Name = t.Name,
                    IsActive = t.IsActive,
                    Category = t.Category,
                    HasHtml = !string.IsNullOrEmpty(t.HtmlTemplate),
                    HasSubject = t.SubjectTemplate?.Value != null,
                    CreatedAt = t.CreatedAt
                }).OrderByDescending(t => t.IsActive).ThenBy(t => t.Name)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAGNOSTICS] Failed to retrieve email template status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("email-templates/inactive")]
    public async Task<IActionResult> GetInactiveTemplates(CancellationToken cancellationToken)
    {
        try
        {
            var allTemplates = await _emailTemplateRepository.GetAllAsync(cancellationToken);
            var inactiveTemplates = allTemplates
                .Where(t => !t.IsActive)
                .Select(t => new
                {
                    Name = t.Name,
                    Category = t.Category,
                    UpdatedAt = t.UpdatedAt
                })
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToList();

            return Ok(new
            {
                Count = inactiveTemplates.Count,
                Templates = inactiveTemplates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAGNOSTICS] Failed to retrieve inactive templates");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Phase 6A.129b: Check if specific Handlebars blocks exist in email templates
    /// Used to verify that migration correctly added {{#HasSignUpLists}} and {{#HasSignupForms}} blocks
    /// </summary>
    [HttpGet("email-templates/check-blocks")]
    public async Task<IActionResult> CheckTemplateBlocks(CancellationToken cancellationToken)
    {
        try
        {
            var allTemplates = await _emailTemplateRepository.GetAllAsync(cancellationToken);
            var results = allTemplates
                .Where(t => t.IsActive)
                .Select(t => new
                {
                    Name = t.Name,
                    HasSignUpLists = t.HtmlTemplate?.Contains("HasSignUpLists") ?? false,
                    HasSignupForms = t.HtmlTemplate?.Contains("HasSignupForms") ?? false,
                    HasOrganizerContact = t.HtmlTemplate?.Contains("HasOrganizerContact") ?? false,
                    HasEventImage = t.HtmlTemplate?.Contains("HasEventImage") ?? false,
                    TemplateLength = t.HtmlTemplate?.Length ?? 0,
                    UpdatedAt = t.UpdatedAt
                })
                .OrderBy(t => t.Name)
                .ToList();

            return Ok(new
            {
                TotalChecked = results.Count,
                WithSignUpLists = results.Count(r => r.HasSignUpLists),
                WithSignupForms = results.Count(r => r.HasSignupForms),
                Templates = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAGNOSTICS] Failed to check template blocks");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Phase 6A.133: Fetch raw HTML of a specific email template for debugging.
    /// Returns a snippet around <!-- CLOSING --> to verify template structure.
    /// </summary>
    [HttpGet("email-templates/{templateName}/html-snippet")]
    public async Task<IActionResult> GetTemplateHtmlSnippet(string templateName, [FromQuery] string? around = "<!-- CLOSING -->", [FromQuery] int context = 2000, CancellationToken cancellationToken = default)
    {
        try
        {
            var allTemplates = await _emailTemplateRepository.GetAllAsync(cancellationToken);
            var template = allTemplates.FirstOrDefault(t => t.Name == templateName);
            if (template == null)
                return NotFound(new { error = $"Template '{templateName}' not found" });

            var html = template.HtmlTemplate ?? "";
            if (string.IsNullOrEmpty(around))
                return Ok(new { name = templateName, length = html.Length, html = html.Length > 10000 ? html[..10000] + "...(truncated)" : html });

            var idx = html.IndexOf(around, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return Ok(new { name = templateName, length = html.Length, searchTerm = around, found = false, snippet = "" });

            var start = Math.Max(0, idx - context);
            var end = Math.Min(html.Length, idx + around.Length + context);
            return Ok(new { name = templateName, length = html.Length, searchTerm = around, found = true, position = idx, snippet = html[start..end] });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAGNOSTICS] Failed to get template snippet");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test endpoint to trigger a diagnostic log entry for signup commitment
    /// </summary>
    [HttpPost("test-signup-commitment-logging")]
    public IActionResult TestSignupCommitmentLogging([FromBody] TestSignupRequest request)
    {
        try
        {
            _logger.LogInformation("=== DIAGNOSTIC TEST: Signup Commitment Logging ===");
            _logger.LogInformation("[DIAG-TEST] Event ID: {EventId}", request.EventId);
            _logger.LogInformation("[DIAG-TEST] User Email: {UserEmail}", request.UserEmail);
            _logger.LogInformation("[DIAG-TEST] This log entry confirms logging is working");
            _logger.LogInformation("[DIAG-TEST] If you don't see UserCommittedToSignUp logs, domain events aren't being raised");
            _logger.LogInformation("=== END DIAGNOSTIC TEST ===");

            return Ok(new
            {
                success = true,
                message = "Diagnostic log written. Check Azure logs for '[DIAG-TEST]' entries.",
                eventId = request.EventId,
                userEmail = request.UserEmail
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAGNOSTICS] Test logging failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public record TestSignupRequest(string EventId, string UserEmail);
