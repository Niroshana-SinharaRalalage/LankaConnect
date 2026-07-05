using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using System.Text.Json;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Application.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain.Support;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Domain.Enums;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Identity.Application.Commands.Users.AdminDeactivateUser;

/// <summary>
/// Handler for AdminDeactivateUserCommand
/// Phase 6A.100: Deactivates a user by admin with role hierarchy protection and audit logging.
/// Uses ITypedEmailService with AccountDeactivatedEmailParams for compile-time type safety.
/// </summary>
public class AdminDeactivateUserCommandHandler : ICommandHandler<AdminDeactivateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminDeactivateUserCommandHandler> _logger;

    public AdminDeactivateUserCommandHandler(
        IUserRepository userRepository,
        IAdminAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService,
        ITypedEmailService typedEmailService,
        IUnitOfWork unitOfWork,
        ILogger<AdminDeactivateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
        _typedEmailService = typedEmailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AdminDeactivateUserCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AdminDeactivateUser"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("TargetUserId", request.TargetUserId))
        using (LogContext.PushProperty("AdminUserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AdminDeactivateUser START: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}",
                request.TargetUserId, _currentUserService.UserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get admin user to check role hierarchy
                var adminUser = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
                if (adminUser == null)
                {
                    _logger.LogWarning(
                        "AdminDeactivateUser FAILED: Admin user not found - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Admin user not found");
                }

                // Validate admin role
                if (adminUser.Role != UserRole.Admin && adminUser.Role != UserRole.AdminManager)
                {
                    _logger.LogWarning(
                        "AdminDeactivateUser FAILED: Insufficient permissions - AdminUserId={AdminUserId}, Role={Role}",
                        _currentUserService.UserId, adminUser.Role);
                    return Result.Failure("Insufficient permissions to perform this action");
                }

                // Get target user
                var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
                if (targetUser == null)
                {
                    _logger.LogWarning(
                        "AdminDeactivateUser FAILED: Target user not found - TargetUserId={TargetUserId}",
                        request.TargetUserId);
                    return Result.Failure("User not found");
                }

                // Self-prevention: Admin cannot deactivate themselves
                if (targetUser.Id == adminUser.Id)
                {
                    _logger.LogWarning(
                        "AdminDeactivateUser FAILED: Cannot deactivate own account - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Cannot deactivate your own account");
                }

                // Role hierarchy protection: Admin cannot manage AdminManager
                if (adminUser.Role == UserRole.Admin &&
                    (targetUser.Role == UserRole.AdminManager || targetUser.Role == UserRole.Admin))
                {
                    _logger.LogWarning(
                        "AdminDeactivateUser FAILED: Role hierarchy violation - AdminRole={AdminRole}, TargetRole={TargetRole}",
                        adminUser.Role, targetUser.Role);
                    return Result.Failure("Cannot perform actions on users with equal or higher role");
                }

                _logger.LogInformation(
                    "AdminDeactivateUser: Deactivating user - TargetUserId={TargetUserId}, TargetEmail={TargetEmail}, CurrentStatus={IsActive}",
                    targetUser.Id, targetUser.Email.Value, targetUser.IsActive);

                // Deactivate the user
                var result = targetUser.DeactivateByAdmin();
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "AdminDeactivateUser FAILED: Domain validation failed - TargetUserId={TargetUserId}, Error={Error}",
                        request.TargetUserId, result.Error);
                    return result;
                }

                // Create audit log
                var auditDetails = JsonSerializer.Serialize(new
                {
                    BeforeState = new { IsActive = true },
                    AfterState = new { IsActive = false },
                    TargetEmail = targetUser.Email.Value,
                    TargetName = targetUser.FullName,
                    TargetRole = targetUser.Role.ToString()
                });

                var auditLog = AdminAuditLog.CreateForUserAction(
                    _currentUserService.UserId,
                    AdminAuditActions.UserDeactivated,
                    targetUser.Id,
                    auditDetails,
                    request.IpAddress,
                    request.UserAgent);

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                // Send notification email (fail-silent)
                await SendDeactivationEmailAsync(targetUser, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AdminDeactivateUser COMPLETE: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}, Duration={ElapsedMs}ms",
                    request.TargetUserId, _currentUserService.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "AdminDeactivateUser CANCELED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "AdminDeactivateUser FAILED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private async Task SendDeactivationEmailAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            // Phase 6A.100: Create typed email parameters
            var emailParams = AccountDeactivatedEmailParams.Create(
                userId: user.Id,
                recipientEmail: user.Email.Value,
                userName: user.FullName);

            _logger.LogInformation(
                "[Phase 6A.100] Sending deactivation email to {Email}",
                user.Email.Value);

            var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "[Phase 6A.100] Deactivation email sent successfully to {Email}, CorrelationId={CorrelationId}",
                    user.Email.Value, result.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 6A.100] Failed to send deactivation email to {Email}: {Errors}",
                    user.Email.Value, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw
            _logger.LogError(ex,
                "[Phase 6A.100] Error sending deactivation email to user {UserId}",
                user.Id);
        }
    }
}
