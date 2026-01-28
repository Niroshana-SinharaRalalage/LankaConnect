using System.Diagnostics;
using System.Text.Json;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Support;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.AdminUnlockUser;

/// <summary>
/// Handler for AdminUnlockUserCommand
/// Phase 6A.90: Unlocks a user account by admin with role hierarchy protection and audit logging
/// </summary>
public class AdminUnlockUserCommandHandler : ICommandHandler<AdminUnlockUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IApplicationUrlsService _urlsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminUnlockUserCommandHandler> _logger;

    public AdminUnlockUserCommandHandler(
        IUserRepository userRepository,
        IAdminAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IApplicationUrlsService urlsService,
        IUnitOfWork unitOfWork,
        ILogger<AdminUnlockUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _urlsService = urlsService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AdminUnlockUserCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AdminUnlockUser"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("TargetUserId", request.TargetUserId))
        using (LogContext.PushProperty("AdminUserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AdminUnlockUser START: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}",
                request.TargetUserId, _currentUserService.UserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get admin user to check role hierarchy
                var adminUser = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
                if (adminUser == null)
                {
                    _logger.LogWarning(
                        "AdminUnlockUser FAILED: Admin user not found - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Admin user not found");
                }

                // Validate admin role
                if (adminUser.Role != UserRole.Admin && adminUser.Role != UserRole.AdminManager)
                {
                    _logger.LogWarning(
                        "AdminUnlockUser FAILED: Insufficient permissions - AdminUserId={AdminUserId}, Role={Role}",
                        _currentUserService.UserId, adminUser.Role);
                    return Result.Failure("Insufficient permissions to perform this action");
                }

                // Get target user
                var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
                if (targetUser == null)
                {
                    _logger.LogWarning(
                        "AdminUnlockUser FAILED: Target user not found - TargetUserId={TargetUserId}",
                        request.TargetUserId);
                    return Result.Failure("User not found");
                }

                // Self-prevention: Admin cannot unlock themselves
                if (targetUser.Id == adminUser.Id)
                {
                    _logger.LogWarning(
                        "AdminUnlockUser FAILED: Cannot unlock own account - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Cannot unlock your own account");
                }

                // Role hierarchy protection: Admin cannot manage AdminManager
                if (adminUser.Role == UserRole.Admin &&
                    (targetUser.Role == UserRole.AdminManager || targetUser.Role == UserRole.Admin))
                {
                    _logger.LogWarning(
                        "AdminUnlockUser FAILED: Role hierarchy violation - AdminRole={AdminRole}, TargetRole={TargetRole}",
                        adminUser.Role, targetUser.Role);
                    return Result.Failure("Cannot perform actions on users with equal or higher role");
                }

                _logger.LogInformation(
                    "AdminUnlockUser: Unlocking user - TargetUserId={TargetUserId}, TargetEmail={TargetEmail}, CurrentLockUntil={AccountLockedUntil}",
                    targetUser.Id, targetUser.Email.Value, targetUser.AccountLockedUntil);

                // Unlock the user account
                var result = targetUser.UnlockAccountByAdmin();
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "AdminUnlockUser FAILED: Domain validation failed - TargetUserId={TargetUserId}, Error={Error}",
                        request.TargetUserId, result.Error);
                    return result;
                }

                // Create audit log
                var auditDetails = JsonSerializer.Serialize(new
                {
                    TargetEmail = targetUser.Email.Value,
                    TargetName = targetUser.FullName,
                    TargetRole = targetUser.Role.ToString()
                });

                var auditLog = AdminAuditLog.CreateForUserAction(
                    _currentUserService.UserId,
                    AdminAuditActions.UserUnlocked,
                    targetUser.Id,
                    auditDetails,
                    request.IpAddress,
                    request.UserAgent);

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                // Send notification email (fail-silent)
                await SendUnlockEmailAsync(targetUser, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AdminUnlockUser COMPLETE: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}, Duration={ElapsedMs}ms",
                    request.TargetUserId, _currentUserService.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "AdminUnlockUser CANCELED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "AdminUnlockUser FAILED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private async Task SendUnlockEmailAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var loginUrl = $"{_urlsService.FrontendBaseUrl}/auth/signin";

            var parameters = new Dictionary<string, object>
            {
                { "UserName", user.FullName },
                { "LoginUrl", loginUrl }
            };

            _logger.LogInformation(
                "[Phase 6A.90] Sending unlock notification email to {Email}",
                user.Email.Value);

            var result = await _emailService.SendTemplatedEmailAsync(
                "template-account-unlocked",
                user.Email.Value,
                parameters,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 6A.90] Unlock notification email sent successfully to {Email}",
                    user.Email.Value);
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 6A.90] Failed to send unlock notification email to {Email}: {Errors}",
                    user.Email.Value, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw
            _logger.LogError(ex,
                "[Phase 6A.90] Error sending unlock notification email to user {UserId}",
                user.Id);
        }
    }
}
