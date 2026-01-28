using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Queries.GetAdminUserStatistics;

/// <summary>
/// Handler for GetAdminUserStatisticsQuery
/// Phase 6A.90: Returns user statistics for admin dashboard
/// </summary>
public class GetAdminUserStatisticsQueryHandler : IQueryHandler<GetAdminUserStatisticsQuery, AdminUserStatisticsDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetAdminUserStatisticsQueryHandler> _logger;

    public GetAdminUserStatisticsQueryHandler(
        IUserRepository userRepository,
        ILogger<GetAdminUserStatisticsQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<AdminUserStatisticsDto>> Handle(
        GetAdminUserStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetAdminUserStatistics"))
        using (LogContext.PushProperty("EntityType", "User"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("GetAdminUserStatistics START");

            try
            {
                // Get counts by role
                var countsByRole = await _userRepository.GetUserCountsByRoleAsync(cancellationToken);

                // Get active users count
                var activeUsersCount = await _userRepository.GetActiveUsersCountAsync(cancellationToken);

                // Get locked accounts count
                var lockedAccountsCount = await _userRepository.GetLockedAccountsCountAsync(cancellationToken);

                // Get users with pending role upgrades
                var pendingUpgrades = await _userRepository.GetUsersWithPendingRoleUpgradesAsync(cancellationToken);

                // Calculate totals
                var totalUsers = countsByRole.Values.Sum();
                var inactiveUsers = totalUsers - activeUsersCount;

                // For unverified emails, we need to query this separately
                // For now, we'll use a placeholder - this can be optimized with a dedicated repository method
                var unverifiedEmails = 0; // TODO: Add GetUnverifiedEmailsCountAsync to repository

                var dto = new AdminUserStatisticsDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsersCount,
                    InactiveUsers = inactiveUsers,
                    LockedAccounts = lockedAccountsCount,
                    UnverifiedEmails = unverifiedEmails,
                    UsersByRole = countsByRole.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => kvp.Value),
                    PendingUpgradeRequests = pendingUpgrades.Count
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetAdminUserStatistics COMPLETE: TotalUsers={TotalUsers}, ActiveUsers={ActiveUsers}, LockedAccounts={LockedAccounts}, Duration={ElapsedMs}ms",
                    totalUsers, activeUsersCount, lockedAccountsCount, stopwatch.ElapsedMilliseconds);

                return Result<AdminUserStatisticsDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetAdminUserStatistics FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
