using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Queries.GetEmailTemplates;

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
        try
        {
            // Validate pagination parameters
            if (request.PageNumber < 1)
            {
                return Result<GetEmailTemplatesResponse>.Failure("Page number must be greater than 0");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
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
            var categoryCounts = await _emailTemplateRepository.GetCategoryCountsAsync(
                request.IsActive,
                cancellationToken);

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

            _logger.LogInformation("Retrieved {Count} email templates for query with filters: Category={Category}, IsActive={IsActive}, SearchTerm={SearchTerm}", 
                templates.Count, request.Category, request.IsActive, request.SearchTerm);

            return Result<GetEmailTemplatesResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving email templates for query: Category={Category}, SearchTerm={SearchTerm}", 
                request.Category, request.SearchTerm);
            return Result<GetEmailTemplatesResponse>.Failure("An error occurred while retrieving email templates");
        }
    }

    private static EmailTemplateDto MapToDto(LankaConnect.Domain.Communications.Entities.EmailTemplate template)
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
    private static EmailTemplateCategory MapToApplicationCategory(LankaConnect.Domain.Communications.ValueObjects.EmailTemplateCategory domainCategory)
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