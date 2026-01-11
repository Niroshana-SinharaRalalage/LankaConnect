using LankaConnect.Application.Communications.DTOs;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Communications.Commands.CreateNewsletter;

/// <summary>
/// Command to create a new newsletter
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public record CreateNewsletterCommand : IRequest<Result<Guid>>
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<Guid> EmailGroupIds { get; init; } = new();
    public bool IncludeNewsletterSubscribers { get; init; } = true;
    public Guid? EventId { get; init; }
}
