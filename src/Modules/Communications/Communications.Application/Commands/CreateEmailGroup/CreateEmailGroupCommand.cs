using LankaConnect.Application.Communications.Common;
using LankaConnect.Modules.Communications.Application.Common;
using LankaConnect.Domain.Common;
using MediatR;
namespace LankaConnect.Modules.Communications.Application.Commands.CreateEmailGroup;

/// <summary>
/// Command to create a new email group
/// Phase 6A.25: Email Groups Management
/// </summary>
public record CreateEmailGroupCommand : IRequest<Result<EmailGroupDto>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string EmailAddresses { get; init; } = string.Empty;
}
