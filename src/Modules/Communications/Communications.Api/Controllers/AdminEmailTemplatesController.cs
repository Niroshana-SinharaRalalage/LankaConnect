using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.API.Extensions;
using LankaConnect.Application.Communications.Queries.GetEmailTemplates;
using LankaConnect.Application.Communications.Queries.GetEmailTemplateById;
using LankaConnect.Application.Communications.Commands.UpdateEmailTemplate;
namespace LankaConnect.Modules.Communications.Api.Controllers;

/// <summary>
/// Phase 6A.89: Admin email template management endpoints
/// Provides CRUD operations for email templates in admin dashboard
/// </summary>
[ApiController]
[Route("api/admin/email-templates")]
[Produces("application/json")]
[Authorize(Policy = "RequireAdmin")]
public class AdminEmailTemplatesController : BaseController<AdminEmailTemplatesController>
{
    public AdminEmailTemplatesController(IMediator mediator, ILogger<AdminEmailTemplatesController> logger)
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get paginated list of email templates
    /// Phase 6A.89: Returns templates with filtering and pagination
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="search">Optional search term for name/description</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <returns>Paginated list of email templates</returns>
    [HttpGet]
    [ProducesResponseType(typeof(GetEmailTemplatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        Logger.LogInformation(
            "Admin {AdminUserId} retrieving email templates - Category={Category}, IsActive={IsActive}, Search={Search}, Page={Page}",
            User.TryGetUserId(), category, isActive, search, page);

        // Parse category if provided
        LankaConnect.Domain.Communications.ValueObjects.EmailTemplateCategory? domainCategory = null;
        if (!string.IsNullOrEmpty(category))
        {
            var categoryResult = LankaConnect.Domain.Communications.ValueObjects.EmailTemplateCategory.FromValue(category);
            if (categoryResult.IsSuccess)
            {
                domainCategory = categoryResult.Value;
            }
        }

        var query = new GetEmailTemplatesQuery(
            Category: domainCategory,
            IsActive: isActive,
            SearchTerm: search,
            PageNumber: page,
            PageSize: pageSize);

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get a single email template by ID
    /// Phase 6A.89: Returns detailed template including full content for editing
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Detailed email template</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmailTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTemplate(Guid id)
    {
        Logger.LogInformation(
            "Admin {AdminUserId} retrieving email template - TemplateId={TemplateId}",
            User.TryGetUserId(), id);

        var query = new GetEmailTemplateByIdQuery(id);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get a single email template by name
    /// Phase 6A.89: Alternative lookup by template name
    /// </summary>
    /// <param name="name">Template name</param>
    /// <returns>Detailed email template</returns>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(typeof(EmailTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTemplateByName(string name)
    {
        Logger.LogInformation(
            "Admin {AdminUserId} retrieving email template by name - TemplateName={TemplateName}",
            User.TryGetUserId(), name);

        // First get templates filtered by name, then return the first match
        var query = new GetEmailTemplatesQuery(
            SearchTerm: name,
            PageNumber: 1,
            PageSize: 1);

        var result = await Mediator.Send(query);

        if (!result.IsSuccess)
        {
            return HandleResult(result);
        }

        var template = result.Value?.Templates.FirstOrDefault();
        if (template == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Email template with name '{name}' not found",
                Status = 404
            });
        }

        // Get full details using the template name to find the ID
        // For now, return the list result - in a real implementation, we'd get the full template by ID
        return Ok(template);
    }

    /// <summary>
    /// Update an email template
    /// Phase 6A.89: Updates template content (subject, text, HTML)
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Update request with new content</param>
    /// <returns>Updated template details</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EmailTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateEmailTemplateRequest request)
    {
        Logger.LogInformation(
            "Admin {AdminUserId} updating email template - TemplateId={TemplateId}",
            User.TryGetUserId(), id);

        var command = new UpdateEmailTemplateCommand
        {
            Id = id,
            Subject = request.Subject,
            TextTemplate = request.TextTemplate,
            HtmlTemplate = request.HtmlTemplate,
            Description = request.Description,
            Tags = request.Tags
        };

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Toggle email template active status
    /// Phase 6A.89: Activates or deactivates a template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Toggle request</param>
    /// <returns>Updated template details</returns>
    [HttpPatch("{id:guid}/toggle-active")]
    [ProducesResponseType(typeof(EmailTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ToggleActive(Guid id, [FromBody] ToggleActiveRequest request)
    {
        Logger.LogInformation(
            "Admin {AdminUserId} toggling email template active status - TemplateId={TemplateId}, IsActive={IsActive}",
            User.TryGetUserId(), id, request.IsActive);

        var command = new ToggleEmailTemplateActiveCommand
        {
            Id = id,
            IsActive = request.IsActive
        };

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Preview an email template with sample data
    /// Phase 6A.89: Renders template with provided parameters
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Preview request with sample parameters</param>
    /// <returns>Rendered template preview</returns>
    [HttpPost("{id:guid}/preview")]
    [ProducesResponseType(typeof(TemplatePreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PreviewTemplate(Guid id, [FromBody] TemplatePreviewRequest request)
    {
        Logger.LogInformation(
            "Admin {AdminUserId} previewing email template - TemplateId={TemplateId}",
            User.TryGetUserId(), id);

        // Get the template
        var query = new GetEmailTemplateByIdQuery(id);
        var result = await Mediator.Send(query);

        if (!result.IsSuccess)
        {
            return HandleResult(result);
        }

        var template = result.Value!;

        // Simple parameter replacement for preview
        var previewSubject = ReplaceParameters(template.Subject, request.Parameters);
        var previewText = ReplaceParameters(template.TextTemplate, request.Parameters);
        var previewHtml = template.HtmlTemplate != null
            ? ReplaceParameters(template.HtmlTemplate, request.Parameters)
            : null;

        return Ok(new TemplatePreviewResponse
        {
            Subject = previewSubject,
            TextBody = previewText,
            HtmlBody = previewHtml,
            TemplateName = template.Name
        });
    }

    private static string ReplaceParameters(string template, Dictionary<string, string>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return template;

        var result = template;
        foreach (var param in parameters)
        {
            // Replace both {{param}} and {param} patterns
            result = result.Replace($"{{{{{param.Key}}}}}", param.Value);
            result = result.Replace($"{{{param.Key}}}", param.Value);
        }
        return result;
    }
}

/// <summary>
/// Request model for updating an email template
/// </summary>
public class UpdateEmailTemplateRequest
{
    /// <summary>
    /// The email subject template
    /// </summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>
    /// The plain text email body template
    /// </summary>
    public string TextTemplate { get; init; } = string.Empty;

    /// <summary>
    /// The HTML email body template (optional)
    /// </summary>
    public string? HtmlTemplate { get; init; }

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Template tags (comma-separated)
    /// </summary>
    public string? Tags { get; init; }
}

/// <summary>
/// Request model for toggling template active status
/// </summary>
public class ToggleActiveRequest
{
    public bool IsActive { get; init; }
}

/// <summary>
/// Request model for template preview
/// </summary>
public class TemplatePreviewRequest
{
    /// <summary>
    /// Sample parameters to substitute in the template
    /// </summary>
    public Dictionary<string, string>? Parameters { get; init; }
}

/// <summary>
/// Response model for template preview
/// </summary>
public class TemplatePreviewResponse
{
    public string Subject { get; init; } = string.Empty;
    public string TextBody { get; init; } = string.Empty;
    public string? HtmlBody { get; init; }
    public string TemplateName { get; init; } = string.Empty;
}
