using LankaConnect.Application.Communications.Queries.GetEmailTemplateById;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Communications.Commands.UpdateEmailTemplate;

/// <summary>
/// Phase 6A.89: Command to update an email template
/// Used by admin dashboard for template management
/// </summary>
public record UpdateEmailTemplateCommand : IRequest<Result<EmailTemplateDetailDto>>
{
    /// <summary>
    /// The ID of the template to update
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The email subject template (supports parameter placeholders like {{recipientName}})
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
    /// Optional description update
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional tags update (comma-separated)
    /// </summary>
    public string? Tags { get; init; }
}

/// <summary>
/// Phase 6A.89: Command to toggle email template active status
/// </summary>
public record ToggleEmailTemplateActiveCommand : IRequest<Result<EmailTemplateDetailDto>>
{
    public Guid Id { get; init; }
    public bool IsActive { get; init; }
}
