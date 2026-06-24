using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using System.Text.Json;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Support;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Modules.Notifications.Domain;
using LankaConnect.Modules.Notifications.Domain.Enums;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Identity.Application.Commands.Users.ApproveRoleUpgrade;

/// <summary>
/// Handler for ApproveRoleUpgradeCommand
/// Phase 6A.5: Approves pending role upgrade and starts free trial for Event Organizers
/// Phase 6A.6: Creates in-app notification when role upgrade is approved
/// Phase 6A.75: Sends email notification when EventOrganizer role is approved
/// Phase 6A.X Issue #46: Added admin audit logging for traceability
/// Phase 6A.100: Migrated to ITypedEmailService with OrganizerRoleApprovalEmailParams
/// </summary>
public class ApproveRoleUpgradeCommandHandler : ICommandHandler<ApproveRoleUpgradeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IApplicationUrlsService _urlsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveRoleUpgradeCommandHandler> _logger;

    public ApproveRoleUpgradeCommandHandler(
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IAdminAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService,
        ITypedEmailService typedEmailService,
        IApplicationUrlsService urlsService,
        IUnitOfWork unitOfWork,
        ILogger<ApproveRoleUpgradeCommandHandler> logger)
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

    public async Task<Result> Handle(ApproveRoleUpgradeCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ApproveRoleUpgrade"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ApproveRoleUpgrade START: UserId={UserId}",
                request.UserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ApproveRoleUpgrade FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User not found");
                }

                _logger.LogInformation(
                    "ApproveRoleUpgrade: User loaded - UserId={UserId}, Email={Email}, CurrentRole={CurrentRole}, PendingUpgradeRole={PendingUpgradeRole}",
                    user.Id, user.Email.Value, user.Role, user.PendingUpgradeRole);

                // Capture the role being approved before the state changes
                var approvedRole = user.PendingUpgradeRole;

                _logger.LogInformation(
                    "ApproveRoleUpgrade: Approving role upgrade - UserId={UserId}, FromRole={FromRole}, ToRole={ToRole}",
                    user.Id, user.Role, approvedRole);

                // Approve the role upgrade (updates Role, clears PendingUpgradeRole)
                var approvalResult = user.ApproveRoleUpgrade();
                if (approvalResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ApproveRoleUpgrade FAILED: Domain validation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, approvalResult.Error, stopwatch.ElapsedMilliseconds);

                    return approvalResult;
                }

                _logger.LogInformation(
                    "ApproveRoleUpgrade: Domain method succeeded - UserId={UserId}, NewRole={NewRole}",
                    user.Id, user.Role);

                // Phase 6A.6: Create in-app notification for approved role upgrade
                // Phase 6A.X Issue #46: Added timestamp for traceability
                var approvalTimestamp = DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' h:mm tt 'UTC'");
                var notificationTitle = $"Role Upgrade Approved";
                var notificationMessage = user.Role == UserRole.EventOrganizer
                    ? $"Congratulations! Your request to become an Event Organizer has been approved on {approvalTimestamp}. You now have a 6-month free trial to explore all Event Organizer features."
                    : $"Congratulations! Your role has been upgraded to {user.Role} on {approvalTimestamp}.";

                _logger.LogInformation(
                    "ApproveRoleUpgrade: Creating notification - UserId={UserId}, NotificationType={NotificationType}",
                    user.Id, NotificationType.RoleUpgradeApproved);

                var notificationResult = Notification.Create(
                    user.Id,
                    notificationTitle,
                    notificationMessage,
                    NotificationType.RoleUpgradeApproved,
                    user.Id.ToString(),
                    "User"
                );

                if (notificationResult.IsSuccess)
                {
                    await _notificationRepository.AddAsync(notificationResult.Value, cancellationToken);

                    _logger.LogInformation(
                        "ApproveRoleUpgrade: Notification created successfully - NotificationId={NotificationId}",
                        notificationResult.Value.Id);
                }
                else
                {
                    _logger.LogWarning(
                        "ApproveRoleUpgrade: Notification creation failed - UserId={UserId}, Errors={Errors}",
                        user.Id, notificationResult.Error);
                }

                // Phase 6A.X Issue #46: Create admin audit log entry for role approval
                var auditDetails = JsonSerializer.Serialize(new
                {
                    TargetUserId = user.Id,
                    TargetUserEmail = user.Email.Value,
                    FromRole = approvedRole?.ToString() ?? "None",
                    ToRole = user.Role.ToString(),
                    ApprovedAt = DateTime.UtcNow
                });

                var auditLog = AdminAuditLog.CreateForUserAction(
                    _currentUserService.UserId,
                    AdminAuditActions.RoleUpgradeApproved,
                    user.Id,
                    auditDetails,
                    null,
                    null
                );

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);

                _logger.LogInformation(
                    "ApproveRoleUpgrade: Admin audit log created - AdminUserId={AdminUserId}, TargetUserId={TargetUserId}",
                    _currentUserService.UserId, user.Id);

                await _unitOfWork.CommitAsync(cancellationToken);

                // Phase 6A.75: Send email notification for EventOrganizer role approval
                if (user.Role == UserRole.EventOrganizer)
                {
                    _logger.LogInformation(
                        "ApproveRoleUpgrade: Sending EventOrganizer approval email - UserId={UserId}, Email={Email}",
                        user.Id, user.Email.Value);

                    await SendOrganizerApprovalEmailAsync(user, cancellationToken);

                    _logger.LogInformation(
                        "ApproveRoleUpgrade: Email method completed - UserId={UserId}",
                        user.Id);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "ApproveRoleUpgrade COMPLETE: UserId={UserId}, NewRole={NewRole}, NotificationCreated={NotificationCreated}, AdminUserId={AdminUserId}, Duration={ElapsedMs}ms",
                    request.UserId, user.Role, notificationResult.IsSuccess, _currentUserService.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "ApproveRoleUpgrade CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "ApproveRoleUpgrade FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.75: Sends organizer role approval email using database template.
    /// Phase 6A.100: Migrated to ITypedEmailService with OrganizerRoleApprovalEmailParams.
    /// Fail-silent pattern: Logs error but doesn't fail the command if email fails.
    /// </summary>
    private async Task SendOrganizerApprovalEmailAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var userName = $"{user.FirstName} {user.LastName}";
            var dashboardUrl = $"{_urlsService.FrontendBaseUrl}/dashboard";

            // Phase 6A.100: Use typed email params instead of dictionary
            var emailParams = OrganizerRoleApprovalEmailParams.Create(
                userId: user.Id,
                recipientEmail: user.Email.Value,
                userName: userName,
                dashboardUrl: dashboardUrl);

            _logger.LogInformation(
                "[Phase 6A.100] SendOrganizerApprovalEmailAsync: Preparing to send - Email={Email}, UserId={UserId}",
                user.Email.Value, user.Id);

            // Phase 6A.100: Use typed email service
            var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "[Phase 6A.100] SendOrganizerApprovalEmailAsync: SUCCESS - Email sent to {Email}",
                    user.Email.Value);
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 6A.100] SendOrganizerApprovalEmailAsync: FAILED - Email={Email}, Errors={Errors}",
                    user.Email.Value, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw to prevent command failure
            _logger.LogError(ex,
                "[Phase 6A.100] SendOrganizerApprovalEmailAsync: EXCEPTION - UserId={UserId}, Error={ErrorMessage}",
                user.Id, ex.Message);
        }
    }
}
