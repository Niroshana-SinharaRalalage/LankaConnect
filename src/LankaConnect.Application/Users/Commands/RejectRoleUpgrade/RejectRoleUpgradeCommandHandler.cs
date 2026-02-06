using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Notifications;
using LankaConnect.Domain.Notifications.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.RejectRoleUpgrade;

/// <summary>
/// Handler for RejectRoleUpgradeCommand
/// Phase 6A.5: Rejects pending role upgrade with optional reason
/// Phase 6A.6: Creates in-app notification when role upgrade is rejected
/// </summary>
public class RejectRoleUpgradeCommandHandler : ICommandHandler<RejectRoleUpgradeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectRoleUpgradeCommandHandler> _logger;

    public RejectRoleUpgradeCommandHandler(
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<RejectRoleUpgradeCommandHandler> logger)
    {
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RejectRoleUpgradeCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RejectRoleUpgrade"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RejectRoleUpgrade START: UserId={UserId}, HasReason={HasReason}",
                request.UserId, !string.IsNullOrWhiteSpace(request.Reason));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RejectRoleUpgrade FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User not found");
                }

                _logger.LogInformation(
                    "RejectRoleUpgrade: User loaded - UserId={UserId}, Email={Email}, CurrentRole={CurrentRole}, PendingUpgradeRole={PendingUpgradeRole}",
                    user.Id, user.Email.Value, user.Role, user.PendingUpgradeRole);

                // Reject the role upgrade (clears PendingUpgradeRole)
                var rejectionResult = user.RejectRoleUpgrade(request.Reason);
                if (rejectionResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RejectRoleUpgrade FAILED: Domain validation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, rejectionResult.Error, stopwatch.ElapsedMilliseconds);

                    return rejectionResult;
                }

                _logger.LogInformation(
                    "RejectRoleUpgrade: Domain method succeeded - UserId={UserId}, Reason={Reason}",
                    user.Id, request.Reason ?? "No reason provided");

                // Phase 6A.6: Create in-app notification for rejected role upgrade
                var notificationTitle = "Role Upgrade Request Declined";
                var notificationMessage = string.IsNullOrWhiteSpace(request.Reason)
                    ? "Your role upgrade request has been declined. Please contact support for more information."
                    : $"Your role upgrade request has been declined. Reason: {request.Reason}";

                _logger.LogInformation(
                    "RejectRoleUpgrade: Creating notification - UserId={UserId}, NotificationType={NotificationType}",
                    user.Id, NotificationType.RoleUpgradeRejected);

                var notificationResult = Notification.Create(
                    user.Id,
                    notificationTitle,
                    notificationMessage,
                    NotificationType.RoleUpgradeRejected,
                    user.Id.ToString(),
                    "User"
                );

                if (notificationResult.IsSuccess)
                {
                    await _notificationRepository.AddAsync(notificationResult.Value, cancellationToken);

                    _logger.LogInformation(
                        "RejectRoleUpgrade: Notification created successfully - NotificationId={NotificationId}",
                        notificationResult.Value.Id);
                }
                else
                {
                    _logger.LogWarning(
                        "RejectRoleUpgrade: Notification creation failed - UserId={UserId}, Errors={Errors}",
                        user.Id, string.Join(", ", notificationResult.Errors));
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RejectRoleUpgrade COMPLETE: UserId={UserId}, NotificationCreated={NotificationCreated}, Duration={ElapsedMs}ms",
                    request.UserId, notificationResult.IsSuccess, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "RejectRoleUpgrade CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RejectRoleUpgrade FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
