using System.Diagnostics;
using System.Text.RegularExpressions;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Communications.Application.Queries.GetEmailTemplateById;

/// <summary>
/// Phase 6A.89: Handler for retrieving a single email template by ID
/// Used for template management in admin dashboard
/// </summary>
public class GetEmailTemplateByIdQueryHandler : IRequestHandler<GetEmailTemplateByIdQuery, Result<EmailTemplateDetailDto>>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly ILogger<GetEmailTemplateByIdQueryHandler> _logger;

    public GetEmailTemplateByIdQueryHandler(
        IEmailTemplateRepository emailTemplateRepository,
        ILogger<GetEmailTemplateByIdQueryHandler> logger)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _logger = logger;
    }

    public async Task<Result<EmailTemplateDetailDto>> Handle(
        GetEmailTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEmailTemplateById"))
        using (LogContext.PushProperty("EntityType", "EmailTemplate"))
        using (LogContext.PushProperty("TemplateId", request.Id))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEmailTemplateById START: TemplateId={TemplateId}",
                request.Id);

            try
            {
                // Validate ID
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetEmailTemplateById FAILED: Invalid template ID - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailTemplateDetailDto>.Failure("Template ID is required");
                }

                // Get template from repository
                var template = await _emailTemplateRepository.GetByIdAsync(request.Id, cancellationToken);

                if (template == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetEmailTemplateById FAILED: Template not found - TemplateId={TemplateId}, Duration={ElapsedMs}ms",
                        request.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailTemplateDetailDto>.Failure("Email template not found");
                }

                // Extract parameters from template content
                var parameters = ExtractParameters(template.TextTemplate, template.HtmlTemplate);

                // Map to detailed DTO
                var dto = new EmailTemplateDetailDto
                {
                    Id = template.Id,
                    Name = template.Name,
                    Description = template.Description,
                    Subject = template.SubjectTemplate.Value,
                    TextTemplate = template.TextTemplate,
                    HtmlTemplate = template.HtmlTemplate,
                    Category = template.Category.Value,
                    Type = template.Type.ToString(),
                    IsActive = template.IsActive,
                    Tags = template.Tags,
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt,
                    RequiredParameters = parameters.Required,
                    OptionalParameters = parameters.Optional
                };

                stopwatch.Stop();
                _logger.LogInformation(
                    "GetEmailTemplateById COMPLETE: TemplateId={TemplateId}, Name={TemplateName}, Duration={ElapsedMs}ms",
                    template.Id,
                    template.Name,
                    stopwatch.ElapsedMilliseconds);

                return Result<EmailTemplateDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetEmailTemplateById FAILED: Exception - TemplateId={TemplateId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                return Result<EmailTemplateDetailDto>.Failure("An error occurred while retrieving the email template");
            }
        }
    }

    /// <summary>
    /// Extracts template parameters from the template content.
    /// Parameters are identified by {{ParameterName}} or {ParameterName} patterns.
    /// </summary>
    private static (List<string> Required, List<string> Optional) ExtractParameters(
        string textTemplate,
        string? htmlTemplate)
    {
        var allContent = textTemplate + (htmlTemplate ?? string.Empty);

        // Match both {{param}} and {param} patterns
        var pattern = @"\{\{?(\w+)\}?\}";
        var matches = Regex.Matches(allContent, pattern);

        var parameters = matches
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        // Common required parameters
        var commonRequired = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "recipientName", "recipientEmail", "userName", "userEmail",
            "eventName", "eventTitle", "businessName"
        };

        var required = parameters.Where(p => commonRequired.Contains(p)).ToList();
        var optional = parameters.Where(p => !commonRequired.Contains(p)).ToList();

        return (required, optional);
    }
}
