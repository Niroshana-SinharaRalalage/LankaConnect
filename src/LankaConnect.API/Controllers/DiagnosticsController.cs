using LankaConnect.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LankaConnect.API.Controllers;

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
}
