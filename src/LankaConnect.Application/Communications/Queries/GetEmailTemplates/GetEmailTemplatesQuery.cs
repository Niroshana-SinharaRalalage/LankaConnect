using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Application.Communications.Queries.GetEmailTemplates;

/// <summary>
/// Query to get available email templates using domain value objects
/// </summary>
/// <param name="Category">Optional category filter using domain value object</param>
/// <param name="IsActive">Optional filter for active templates only</param>
/// <param name="SearchTerm">Optional search term to filter by name or description</param>
/// <param name="PageNumber">Page number for pagination</param>
/// <param name="PageSize">Page size for pagination</param>
public record GetEmailTemplatesQuery(
    LankaConnect.Domain.Communications.ValueObjects.EmailTemplateCategory? Category = null,
    bool? IsActive = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<GetEmailTemplatesResponse>;

/// <summary>
/// Response containing email template information
/// </summary>
public class GetEmailTemplatesResponse
{
    public List<EmailTemplateDto> Templates { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public Dictionary<LankaConnect.Domain.Communications.ValueObjects.EmailTemplateCategory, int> CategoryCounts { get; init; } = new();

    public GetEmailTemplatesResponse(List<EmailTemplateDto> templates, int totalCount, 
        int pageNumber, int pageSize, Dictionary<LankaConnect.Domain.Communications.ValueObjects.EmailTemplateCategory, int> categoryCounts)
    {
        Templates = templates;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        CategoryCounts = categoryCounts;
    }
}