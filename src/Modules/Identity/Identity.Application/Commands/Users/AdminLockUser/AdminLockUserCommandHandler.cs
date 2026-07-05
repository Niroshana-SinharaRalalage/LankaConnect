using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using System.Text.Json;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Support;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Domain.Enums;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Identity.Application.Commands.Users.AdminLockUser;

/// <summary>
/// Handler for AdminLockUserCommand
/// Phase 6A.90: Locks a user account by admin with role hierarchy protection and audit logging
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class AdminLockUserCommandHandler : ICommandHandler<AdminLockUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminLockUserCommandHandler> _logger;

    public AdminLockUserCommandHandler(
        IUserRepository userRepository,
        IAdminAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService,
        ITypedEmailService typedEmailService,
        IUnitOfWork unitOfWork,
        ILogger<AdminLockUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
        _typedEmailService = typedEmailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AdminLockUserCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AdminLockUser"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("TargetUserId", request.TargetUserId))
        using (LogContext.PushProperty("AdminUserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AdminLockUser START: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}, LockUntil={LockUntil}",
                request.TargetUserId, _currentUserService.UserId, request.LockUntil);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get admin user to check role hierarchy
                var adminUser = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
                if (adminUser == null)
                {
                    _logger.LogWarning(
                        "AdminLockUser FAILED: Admin user not found - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Admin user not found");
                }

                // Validate admin role
                if (adminUser.Role != UserRole.Admin && adminUser.Role != UserRole.AdminManager)
                {
                    _logger.LogWarning(
                        "AdminLockUser FAILED: Insufficient permissions - AdminUserId={AdminUserId}, Role={Role}",
                        _currentUserService.UserId, adminUser.Role);
                    return Result.Failure("Insufficient permissions to perform this action");
                }

                // Get target user
                var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
                if (targetUser == null)
                {
                    _logger.LogWarning(
                        "AdminLockUser FAILED: Target user not found - TargetUserId={TargetUserId}",
                        request.TargetUserId);
                    return Result.Failure("User not found");
                }

                // Self-prevention: Admin cannot lock themselves
                if (targetUser.Id == adminUser.Id)
                {
                    _logger.LogWarning(
                        "AdminLockUser FAILED: Cannot lock own account - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Cannot lock your own account");
                }

                // Role hierarchy protection: Admin cannot manage AdminManager
                if (adminUser.Role == UserRole.Admin &&
                    (targetUser.Role == UserRole.AdminManager || targetUser.Role == UserRole.Admin))
                {
                    _logger.LogWarning(
                        "AdminLockUser FAILED: Role hierarchy violation - AdminRole={AdminRole}, TargetRole={TargetRole}",
                        adminUser.Role, targetUser.Role);
                    return Result.Failure("Cannot perform actions on users with equal or higher role");
                }

                _logger.LogInformation(
                    "AdminLockUser: Locking user - TargetUserId={TargetUserId}, TargetEmail={TargetEmail}, LockUntil={LockUntil}",
                    targetUser.Id, targetUser.Email.Value, request.LockUntil);

                // Lock the user account
                var result = targetUser.LockAccountByAdmin(request.LockUntil, request.Reason);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "AdminLockUser FAILED: Domain validation failed - TargetUserId={TargetUserId}, Error={Error}",
                        request.TargetUserId, result.Error);
                    return result;
                }

                // Create audit log
                var auditDetails = JsonSerializer.Serialize(new
                {
                    LockUntil = request.LockUntil,
                    Reason = request.Reason,
                    TargetEmail = targetUser.Email.Value,
                    TargetName = targetUser.FullName,
                    TargetRole = targetUser.Role.ToString()
                });

                var auditLog = AdminAuditLog.CreateForUserAction(
                    _currentUserService.UserId,
                    AdminAuditActions.UserLocked,
                    targetUser.Id,
                    auditDetails,
                    request.IpAddress,
                    request.UserAgent);

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                // Send notification email (fail-silent)
                await SendLockEmailAsync(targetUser, request.LockUntil, request.Reason, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AdminLockUser COMPLETE: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}, LockUntil={LockUntil}, Duration={ElapsedMs}ms",
                    request.TargetUserId, _currentUserService.UserId, request.LockUntil, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "AdminLockUser CANCELED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "AdminLockUser FAILED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private async Task SendLockEmailAsync(User user, DateTime lockUntil, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            // Phase 6A.87: Use typed email parameters for compile-time safety
            var emailParams = AdminUserEmailParams.CreateAccountLocked(
                userId: user.Id,
                userName: user.FullName,
                userEmail: user.Email.Value,
                adminId: _currentUserService.UserId,
                adminName: "Administrator",
                reason: reason ?? "Violation of terms of service",
                actionDate: DateTime.UtcNow,
                canAppeal: true
            );

            _logger.LogInformation(
                "[Phase 6A.87] Sending lock notification email to {Email}",
                user.Email.Value);

            // Phase 6A.100: Send via typed email service
            var typedResult = await _typedEmailService.SendEmailAsync(
                emailParams,
                cancellationToken);

            if (typedResult.Success)
            {
                _logger.LogInformation(
                    "[Phase 6A.100] Lock notification email sent successfully to {Email}",
                    user.Email.Value);
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 6A.87] Failed to send lock notification email to {Email}: {Errors}",
                    user.Email.Value, string.Join(", ", typedResult.Errors));
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw
            _logger.LogError(ex,
                "[Phase 6A.87] Error sending lock notification email to user {UserId}",
                user.Id);
        }
    }
}
