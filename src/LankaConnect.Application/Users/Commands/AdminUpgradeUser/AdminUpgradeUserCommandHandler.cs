using System.Diagnostics;
using System.Text.Json;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Notifications;
using LankaConnect.Domain.Notifications.Enums;
using LankaConnect.Domain.Support;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.AdminUpgradeUser;

/// <summary>
/// Phase 6A.139: Handler for AdminUpgradeUserCommand.
/// Admin-initiated upgrade of a GeneralUser to EventOrganizer (no user-request needed).
/// Symmetric counterpart to AdminDowngradeUserCommandHandler.
/// Side effects mirror ApproveRoleUpgradeCommandHandler: in-app notification + organizer-approval email.
/// Email send is fail-silent — role change must not be reverted if Azure ACS is down.
/// </summary>
public class AdminUpgradeUserCommandHandler : ICommandHandler<AdminUpgradeUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IApplicationUrlsService _urlsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminUpgradeUserCommandHandler> _logger;

    public AdminUpgradeUserCommandHandler(
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IAdminAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService,
        ITypedEmailService typedEmailService,
        IApplicationUrlsService urlsService,
        IUnitOfWork unitOfWork,
        ILogger<AdminUpgradeUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
        _typedEmailService = typedEmailService;
        _urlsService = urlsService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AdminUpgradeUserCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AdminUpgradeUser"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("TargetUserId", request.TargetUserId))
        using (LogContext.PushProperty("AdminUserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AdminUpgradeUser START: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}",
                request.TargetUserId, _currentUserService.UserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Load admin and validate role
                var adminUser = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
                if (adminUser == null)
                {
                    _logger.LogWarning(
                        "AdminUpgradeUser FAILED: Admin user not found - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Admin user not found");
                }

                if (adminUser.Role != UserRole.Admin && adminUser.Role != UserRole.AdminManager)
                {
                    _logger.LogWarning(
                        "AdminUpgradeUser FAILED: Insufficient permissions - AdminUserId={AdminUserId}, Role={Role}",
                        _currentUserService.UserId, adminUser.Role);
                    return Result.Failure("Insufficient permissions to perform this action");
                }

                // 2. Self-action guard (target may equal admin even before target lookup)
                if (request.TargetUserId == adminUser.Id)
                {
                    _logger.LogWarning(
                        "AdminUpgradeUser FAILED: Cannot upgrade own account - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Cannot upgrade your own account");
                }

                // 3. Load target
                var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
                if (targetUser == null)
                {
                    _logger.LogWarning(
                        "AdminUpgradeUser FAILED: Target user not found - TargetUserId={TargetUserId}",
                        request.TargetUserId);
                    return Result.Failure("User not found");
                }

                // Capture pre-upgrade state for audit + short-circuit detection
                var oldRole = targetUser.Role;
                var hadPendingUpgrade = targetUser.PendingUpgradeRole.HasValue;

                _logger.LogInformation(
                    "AdminUpgradeUser: Upgrading user - TargetUserId={TargetUserId}, TargetEmail={TargetEmail}, CurrentRole={CurrentRole}, HadPendingUpgrade={HadPendingUpgrade}",
                    targetUser.Id, targetUser.Email.Value, oldRole, hadPendingUpgrade);

                // 4. Domain method (enforces invariants)
                var domainResult = targetUser.UpgradeToEventOrganizerByAdmin();
                if (domainResult.IsFailure)
                {
                    _logger.LogWarning(
                        "AdminUpgradeUser FAILED: Domain validation failed - TargetUserId={TargetUserId}, Error={Error}",
                        request.TargetUserId, domainResult.Error);
                    return domainResult;
                }

                // 5. In-app notification (parity with ApproveRoleUpgradeCommandHandler)
                var approvalTimestamp = DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' h:mm tt 'UTC'");
                const string notificationTitle = "Role Upgrade Approved";
                var notificationMessage =
                    $"Congratulations! Your account has been upgraded to Event Organizer on {approvalTimestamp}. You now have a 6-month free trial to explore all Event Organizer features.";

                var notificationResult = Notification.Create(
                    targetUser.Id,
                    notificationTitle,
                    notificationMessage,
                    NotificationType.RoleUpgradeApproved,
                    targetUser.Id.ToString(),
                    "User");

                if (notificationResult.IsSuccess)
                {
                    await _notificationRepository.AddAsync(notificationResult.Value, cancellationToken);
                    _logger.LogInformation(
                        "AdminUpgradeUser: Notification created - NotificationId={NotificationId}",
                        notificationResult.Value.Id);
                }
                else
                {
                    _logger.LogWarning(
                        "AdminUpgradeUser: Notification creation failed - UserId={UserId}, Errors={Errors}",
                        targetUser.Id, string.Join(", ", notificationResult.Errors));
                }

                // 6. Audit log (with ShortCircuitedPendingRequest flag if applicable)
                var auditDetails = JsonSerializer.Serialize(new
                {
                    TargetEmail = targetUser.Email.Value,
                    TargetName = targetUser.FullName,
                    OldRole = oldRole.ToString(),
                    NewRole = "EventOrganizer",
                    Reason = request.Reason,
                    ShortCircuitedPendingRequest = hadPendingUpgrade
                });

                var auditLog = AdminAuditLog.CreateForUserAction(
                    _currentUserService.UserId,
                    AdminAuditActions.UserRoleUpgraded,
                    targetUser.Id,
                    auditDetails,
                    request.IpAddress,
                    request.UserAgent);

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);

                // 7. Commit role + notification + audit atomically
                await _unitOfWork.CommitAsync(cancellationToken);

                // 8. Send approval email (fail-silent: do not roll back the role change)
                await SendOrganizerApprovalEmailAsync(targetUser, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AdminUpgradeUser COMPLETE: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}, OldRole={OldRole}, NewRole={NewRole}, Duration={ElapsedMs}ms",
                    request.TargetUserId, _currentUserService.UserId, oldRole, targetUser.Role, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "AdminUpgradeUser CANCELED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "AdminUpgradeUser FAILED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Sends the organizer-approval email. Fail-silent: never throws or returns failure;
    /// any error is logged so the role upgrade itself is not rolled back.
    /// Mirrors ApproveRoleUpgradeCommandHandler.SendOrganizerApprovalEmailAsync.
    /// </summary>
    private async Task SendOrganizerApprovalEmailAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var userName = $"{user.FirstName} {user.LastName}";
            var dashboardUrl = $"{_urlsService.FrontendBaseUrl}/dashboard";

            var emailParams = OrganizerRoleApprovalEmailParams.Create(
                userId: user.Id,
                recipientEmail: user.Email.Value,
                userName: userName,
                dashboardUrl: dashboardUrl);

            _logger.LogInformation(
                "AdminUpgradeUser.SendOrganizerApprovalEmailAsync: Preparing - Email={Email}, UserId={UserId}",
                user.Email.Value, user.Id);

            var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "AdminUpgradeUser.SendOrganizerApprovalEmailAsync: SUCCESS - Email={Email}",
                    user.Email.Value);
            }
            else
            {
                _logger.LogWarning(
                    "AdminUpgradeUser.SendOrganizerApprovalEmailAsync: FAILED - Email={Email}, Errors={Errors}",
                    user.Email.Value, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: role change is already committed; do not propagate email errors.
            _logger.LogError(ex,
                "AdminUpgradeUser.SendOrganizerApprovalEmailAsync: EXCEPTION - UserId={UserId}, Error={ErrorMessage}",
                user.Id, ex.Message);
        }
    }
}
