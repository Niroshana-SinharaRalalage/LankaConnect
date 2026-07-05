using LankaConnect.BuildingBlocks.Domain;
using MediatR;
namespace LankaConnect.Modules.Communications.Application.Commands.DeleteEmailGroup;

/// <summary>
/// Command to delete an email group (soft delete)
/// Phase 6A.25: Email Groups Management
/// </summary>
public record DeleteEmailGroupCommand : IRequest<Result>
{
    public Guid Id { get; init; }
}
