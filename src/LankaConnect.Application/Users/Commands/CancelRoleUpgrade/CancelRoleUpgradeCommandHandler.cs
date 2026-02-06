using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.CancelRoleUpgrade;

/// <summary>
/// Handler for CancelRoleUpgradeCommand
/// Phase 6A.7: User Upgrade Workflow - Allows users to cancel their pending upgrade request
/// </summary>
public class CancelRoleUpgradeCommandHandler : ICommandHandler<CancelRoleUpgradeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelRoleUpgradeCommandHandler> _logger;

    public CancelRoleUpgradeCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<CancelRoleUpgradeCommandHandler> logger)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelRoleUpgradeCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        using (LogContext.PushProperty("Operation", "CancelRoleUpgrade"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", currentUserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CancelRoleUpgrade START: UserId={UserId}",
                currentUserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get current user
                if (currentUserId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelRoleUpgrade FAILED: User not authenticated - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User must be authenticated");
                }

                var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelRoleUpgrade FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        currentUserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User not found");
                }

                _logger.LogInformation(
                    "CancelRoleUpgrade: User loaded - UserId={UserId}, Email={Email}, CurrentRole={CurrentRole}, PendingUpgradeRole={PendingUpgradeRole}",
                    user.Id, user.Email.Value, user.Role, user.PendingUpgradeRole);

                // Cancel the role upgrade (domain validates business rules)
                var result = user.CancelRoleUpgrade();
                if (result.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelRoleUpgrade FAILED: Domain validation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        user.Id, result.Error, stopwatch.ElapsedMilliseconds);

                    return result;
                }

                _logger.LogInformation(
                    "CancelRoleUpgrade: Domain method succeeded - UserId={UserId}",
                    user.Id);

                // Save changes
                _userRepository.Update(user);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CancelRoleUpgrade COMPLETE: UserId={UserId}, Duration={ElapsedMs}ms",
                    user.Id, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "CancelRoleUpgrade CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    currentUserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CancelRoleUpgrade FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    currentUserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
