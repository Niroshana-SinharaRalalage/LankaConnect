using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.RequestRoleUpgrade;

/// <summary>
/// Handler for RequestRoleUpgradeCommand
/// Phase 6A.7: User Upgrade Workflow - Allows users to request role upgrade
/// </summary>
public class RequestRoleUpgradeCommandHandler : ICommandHandler<RequestRoleUpgradeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RequestRoleUpgradeCommandHandler> _logger;

    public RequestRoleUpgradeCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<RequestRoleUpgradeCommandHandler> logger)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RequestRoleUpgradeCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        using (LogContext.PushProperty("Operation", "RequestRoleUpgrade"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", currentUserId))
        using (LogContext.PushProperty("TargetRole", request.TargetRole))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RequestRoleUpgrade START: UserId={UserId}, TargetRole={TargetRole}",
                currentUserId, request.TargetRole);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get current user
                if (currentUserId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RequestRoleUpgrade FAILED: User not authenticated - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User must be authenticated");
                }

                var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RequestRoleUpgrade FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        currentUserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User not found");
                }

                _logger.LogInformation(
                    "RequestRoleUpgrade: User loaded - UserId={UserId}, Email={Email}, CurrentRole={CurrentRole}, PendingUpgradeRole={PendingUpgradeRole}",
                    user.Id, user.Email.Value, user.Role, user.PendingUpgradeRole);

                // Validate reason is provided
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RequestRoleUpgrade FAILED: Reason is required - UserId={UserId}, Duration={ElapsedMs}ms",
                        currentUserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Reason is required for role upgrade request");
                }

                _logger.LogInformation(
                    "RequestRoleUpgrade: Reason provided - ReasonLength={ReasonLength}",
                    request.Reason.Length);

                // Request the role upgrade (domain validates business rules)
                var result = user.SetPendingUpgradeRole(request.TargetRole);
                if (result.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RequestRoleUpgrade FAILED: Domain validation failed - UserId={UserId}, TargetRole={TargetRole}, Error={Error}, Duration={ElapsedMs}ms",
                        user.Id, request.TargetRole, result.Error, stopwatch.ElapsedMilliseconds);

                    return result;
                }

                _logger.LogInformation(
                    "RequestRoleUpgrade: Domain method succeeded - UserId={UserId}, TargetRole={TargetRole}",
                    user.Id, request.TargetRole);

                // Save changes
                _userRepository.Update(user);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RequestRoleUpgrade COMPLETE: UserId={UserId}, TargetRole={TargetRole}, Duration={ElapsedMs}ms",
                    user.Id, request.TargetRole, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "RequestRoleUpgrade CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    currentUserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RequestRoleUpgrade FAILED: Exception occurred - UserId={UserId}, TargetRole={TargetRole}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    currentUserId, request.TargetRole, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
