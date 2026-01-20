using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Queries.GetPendingRoleUpgrades;

/// <summary>
/// Handler for GetPendingRoleUpgradesQuery
/// Phase 6A.5: Returns all users awaiting role upgrade approval
/// </summary>
public class GetPendingRoleUpgradesQueryHandler : IQueryHandler<GetPendingRoleUpgradesQuery, IReadOnlyList<PendingRoleUpgradeDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetPendingRoleUpgradesQueryHandler> _logger;

    public GetPendingRoleUpgradesQueryHandler(
        IUserRepository userRepository,
        ILogger<GetPendingRoleUpgradesQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<PendingRoleUpgradeDto>>> Handle(GetPendingRoleUpgradesQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetPendingRoleUpgrades"))
        using (LogContext.PushProperty("EntityType", "User"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("GetPendingRoleUpgrades START");

            try
            {
                var usersWithPendingUpgrades = await _userRepository.GetUsersWithPendingRoleUpgradesAsync(cancellationToken);

                _logger.LogInformation(
                    "GetPendingRoleUpgrades: Users loaded - PendingCount={PendingCount}",
                    usersWithPendingUpgrades.Count());

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

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetPendingRoleUpgrades COMPLETE: PendingCount={PendingCount}, Duration={ElapsedMs}ms",
                    dtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<PendingRoleUpgradeDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetPendingRoleUpgrades FAILED: Exception occurred - Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
