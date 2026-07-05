using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
namespace LankaConnect.Modules.Communications.Application.Queries.GetEmailTemplateById;

/// <summary>
/// Phase 6A.89: Query to get a single email template by ID
/// Used for template management in admin dashboard
/// </summary>
/// <param name="Id">The template ID</param>
public record GetEmailTemplateByIdQuery(Guid Id) : IQuery<EmailTemplateDetailDto>;

/// <summary>
/// Phase 6A.89: Detailed email template DTO including full content for editing
/// </summary>
public class EmailTemplateDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string TextTemplate { get; init; } = string.Empty;
    public string? HtmlTemplate { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string? Tags { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<string> RequiredParameters { get; init; } = new();
    public List<string> OptionalParameters { get; init; } = new();
}
