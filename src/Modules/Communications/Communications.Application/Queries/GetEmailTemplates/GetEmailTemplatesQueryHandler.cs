using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Application.Common;
using LankaConnect.BuildingBlocks.Domain;
using Serilog.Context;
namespace LankaConnect.Modules.Communications.Application.Queries.GetEmailTemplates;

/// <summary>
/// Handler for retrieving email templates
/// </summary>
public class GetEmailTemplatesQueryHandler : IRequestHandler<GetEmailTemplatesQuery, Result<GetEmailTemplatesResponse>>
{
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly ILogger<GetEmailTemplatesQueryHandler> _logger;

    public GetEmailTemplatesQueryHandler(
        IEmailTemplateService emailTemplateService,
        IEmailTemplateRepository emailTemplateRepository,
        ILogger<GetEmailTemplatesQueryHandler> logger)
    {
        _emailTemplateService = emailTemplateService;
        _emailTemplateRepository = emailTemplateRepository;
        _logger = logger;
    }

    public async Task<Result<GetEmailTemplatesResponse>> Handle(GetEmailTemplatesQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEmailTemplates"))
        using (LogContext.PushProperty("EntityType", "EmailTemplate"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEmailTemplates START: Category={Category}, IsActive={IsActive}, SearchTerm={SearchTerm}, Page={Page}, PageSize={PageSize}",
                request.Category, request.IsActive, request.SearchTerm, request.PageNumber, request.PageSize);

            try
            {
                // Validate pagination parameters
                if (request.PageNumber < 1)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEmailTemplates FAILED: Invalid page number - PageNumber={PageNumber}, Duration={ElapsedMs}ms",
                        request.PageNumber, stopwatch.ElapsedMilliseconds);

                    return Result<GetEmailTemplatesResponse>.Failure("Page number must be greater than 0");
                }

                if (request.PageSize < 1 || request.PageSize > 100)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEmailTemplates FAILED: Invalid page size - PageSize={PageSize}, Duration={ElapsedMs}ms",
                        request.PageSize, stopwatch.ElapsedMilliseconds);

                    return Result<GetEmailTemplatesResponse>.Failure("Page size must be between 1 and 100");
                }

                // Get templates with filters using domain types
                var templates = await _emailTemplateRepository.GetTemplatesAsync(
                    request.Category,
                    null, // emailType filter - can be added to query if needed
                    request.IsActive,
                    request.SearchTerm,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                // Get total count for pagination
                var totalCount = await _emailTemplateRepository.GetTemplatesCountAsync(
                    request.Category,
                    null, // emailType filter
                    request.IsActive,
                    request.SearchTerm,
                    cancellationToken);

                // Get category counts for filtering
                var categoryCountsRaw = await _emailTemplateRepository.GetCategoryCountsAsync(
                    request.IsActive,
                    cancellationToken);

                // Phase 6A.89 Fix: Convert to Dictionary<string, int> for proper JSON serialization
                var categoryCounts = categoryCountsRaw.ToDictionary(
                    kvp => kvp.Key.Value,
                    kvp => kvp.Value);

                // Map to DTOs
                var templateDtos = new List<EmailTemplateDto>();
                foreach (var template in templates)
                {
                    var dto = MapToDto(template);
                    templateDtos.Add(dto);
                }

                var response = new GetEmailTemplatesResponse(
                    templateDtos,
                    totalCount,
                    request.PageNumber,
                    request.PageSize,
                    categoryCounts);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEmailTemplates COMPLETE: Category={Category}, IsActive={IsActive}, ReturnedCount={Count}, TotalCount={TotalCount}, Duration={ElapsedMs}ms",
                    request.Category, request.IsActive, templates.Count, totalCount, stopwatch.ElapsedMilliseconds);

                return Result<GetEmailTemplatesResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Phase 6A.89 Enhancement: Log detailed exception info for better diagnostics
                _logger.LogError(ex,
                    "GetEmailTemplates FAILED: Exception occurred - Category={Category}, SearchTerm={SearchTerm}, Duration={ElapsedMs}ms, ExceptionType={ExceptionType}, Error={ErrorMessage}, InnerError={InnerError}",
                    request.Category, request.SearchTerm, stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name, ex.Message, ex.InnerException?.Message ?? "None");

                return Result<GetEmailTemplatesResponse>.Failure($"An error occurred while retrieving email templates: {ex.Message}");
            }
        }
    }

    private static EmailTemplateDto MapToDto(LankaConnect.Modules.Communications.Domain.Entities.EmailTemplate template)
    {
        return new EmailTemplateDto
        {
            Name = template.Name,
            DisplayName = template.Name, // Use Name as DisplayName
            Description = template.Description,
            Subject = template.SubjectTemplate.Value,
            HtmlTemplate = template.HtmlTemplate ?? string.Empty,
            PlainTextTemplate = template.TextTemplate,
            Category = MapToApplicationCategory(template.Category), // Use domain category directly
            RequiredParameters = ExtractParameters(template.TextTemplate, true),
            OptionalParameters = ExtractParameters(template.TextTemplate, false),
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            LastModified = template.UpdatedAt ?? template.CreatedAt
        };
    }

    /// <summary>
    /// Maps domain EmailTemplateCategory value object to application DTO enum
    /// </summary>
    private static EmailTemplateCategory MapToApplicationCategory(LankaConnect.Modules.Communications.Domain.ValueObjects.EmailTemplateCategory domainCategory)
    {
        return domainCategory.Value switch
        {
            "Authentication" => EmailTemplateCategory.Authentication,
            "Business" => EmailTemplateCategory.Business,
            "Marketing" => EmailTemplateCategory.Marketing,
            "Notification" => EmailTemplateCategory.Notification,
            "System" => EmailTemplateCategory.System,
            _ => EmailTemplateCategory.System
        };
    }

    private static List<string> ExtractParameters(string template, bool required)
    {
        // Simple parameter extraction - in real implementation, this would parse template variables
        // For now, return common parameters based on template type
        var commonRequired = new List<string> { "recipientName", "recipientEmail" };
        var commonOptional = new List<string> { "companyName", "supportUrl", "unsubscribeUrl" };
        
        return required ? commonRequired : commonOptional;
    }
}