using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Modules.Communications.Application.Common;
namespace LankaConnect.Modules.Communications.Application.Queries.GetEmailGroupById;

/// <summary>
/// Query to get a single email group by ID
/// Phase 6A.25: Email Groups Management
/// </summary>
public record GetEmailGroupByIdQuery : IQuery<EmailGroupDto>
{
    public Guid Id { get; init; }
}
