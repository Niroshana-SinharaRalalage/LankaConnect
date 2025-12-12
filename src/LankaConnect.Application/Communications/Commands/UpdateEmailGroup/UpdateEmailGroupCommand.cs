using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Communications.Commands.UpdateEmailGroup;

/// <summary>
/// Command to update an existing email group
/// Phase 6A.25: Email Groups Management
/// </summary>
public record UpdateEmailGroupCommand : IRequest<Result<EmailGroupDto>>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string EmailAddresses { get; init; } = string.Empty;
}
