using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Users.Queries.GetPendingRoleUpgrades;

/// <summary>
/// Handler for GetPendingRoleUpgradesQuery
/// Phase 6A.5: Returns all users awaiting role upgrade approval
/// </summary>
public class GetPendingRoleUpgradesQueryHandler : IQueryHandler<GetPendingRoleUpgradesQuery, IReadOnlyList<PendingRoleUpgradeDto>>
{
    private readonly IUserRepository _userRepository;

    public GetPendingRoleUpgradesQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<PendingRoleUpgradeDto>>> Handle(GetPendingRoleUpgradesQuery request, CancellationToken cancellationToken)
    {
        var usersWithPendingUpgrades = await _userRepository.GetUsersWithPendingRoleUpgradesAsync(cancellationToken);

        var dtos = usersWithPendingUpgrades
            .Select(user => new PendingRoleUpgradeDto
            {
                UserId = user.Id,
                Email = user.Email.Value,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                CurrentRole = user.Role.ToString(),
                RequestedRole = user.PendingUpgradeRole!.Value.ToString(),
                RequestedAt = user.UpgradeRequestedAt!.Value
            })
            .OrderBy(dto => dto.RequestedAt)
            .ToList();

        return Result<IReadOnlyList<PendingRoleUpgradeDto>>.Success(dtos);
    }
}
