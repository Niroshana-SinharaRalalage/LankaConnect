using System.Diagnostics;
using System.Text.Json;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Support;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.AdminActivateUser;

/// <summary>
/// Handler for AdminActivateUserCommand
/// Phase 6A.90: Activates a user by admin with role hierarchy protection and audit logging
/// </summary>
public class AdminActivateUserCommandHandler : ICommandHandler<AdminActivateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IApplicationUrlsService _urlsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminActivateUserCommandHandler> _logger;

    public AdminActivateUserCommandHandler(
        IUserRepository userRepository,
        IAdminAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IApplicationUrlsService urlsService,
        IUnitOfWork unitOfWork,
        ILogger<AdminActivateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _urlsService = urlsService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AdminActivateUserCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AdminActivateUser"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("TargetUserId", request.TargetUserId))
        using (LogContext.PushProperty("AdminUserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AdminActivateUser START: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}",
                request.TargetUserId, _currentUserService.UserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get admin user to check role hierarchy
                var adminUser = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
                if (adminUser == null)
                {
                    _logger.LogWarning(
                        "AdminActivateUser FAILED: Admin user not found - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Admin user not found");
                }

                // Validate admin role
                if (adminUser.Role != UserRole.Admin && adminUser.Role != UserRole.AdminManager)
                {
                    _logger.LogWarning(
                        "AdminActivateUser FAILED: Insufficient permissions - AdminUserId={AdminUserId}, Role={Role}",
                        _currentUserService.UserId, adminUser.Role);
                    return Result.Failure("Insufficient permissions to perform this action");
                }

                // Get target user
                var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
                if (targetUser == null)
                {
                    _logger.LogWarning(
                        "AdminActivateUser FAILED: Target user not found - TargetUserId={TargetUserId}",
                        request.TargetUserId);
                    return Result.Failure("User not found");
                }

                // Self-prevention: Admin cannot activate themselves (they're already active if they're here)
                if (targetUser.Id == adminUser.Id)
                {
                    _logger.LogWarning(
                        "AdminActivateUser FAILED: Cannot activate own account - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Cannot activate your own account");
                }

                // Role hierarchy protection: Admin cannot manage AdminManager
                if (adminUser.Role == UserRole.Admin &&
                    (targetUser.Role == UserRole.AdminManager || targetUser.Role == UserRole.Admin))
                {
                    _logger.LogWarning(
                        "AdminActivateUser FAILED: Role hierarchy violation - AdminRole={AdminRole}, TargetRole={TargetRole}",
                        adminUser.Role, targetUser.Role);
                    return Result.Failure("Cannot perform actions on users with equal or higher role");
                }

                _logger.LogInformation(
                    "AdminActivateUser: Activating user - TargetUserId={TargetUserId}, TargetEmail={TargetEmail}, CurrentStatus={IsActive}",
                    targetUser.Id, targetUser.Email.Value, targetUser.IsActive);

                // Activate the user
                var result = targetUser.ActivateByAdmin();
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "AdminActivateUser FAILED: Domain validation failed - TargetUserId={TargetUserId}, Error={Error}",
                        request.TargetUserId, result.Error);
                    return result;
                }

                // Create audit log
                var auditDetails = JsonSerializer.Serialize(new
                {
                    BeforeState = new { IsActive = false },
                    AfterState = new { IsActive = true },
                    TargetEmail = targetUser.Email.Value,
                    TargetName = targetUser.FullName,
                    TargetRole = targetUser.Role.ToString()
                });

                var auditLog = AdminAuditLog.CreateForUserAction(
                    _currentUserService.UserId,
                    AdminAuditActions.UserActivated,
                    targetUser.Id,
                    auditDetails,
                    request.IpAddress,
                    request.UserAgent);

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                // Send notification email (fail-silent)
                await SendActivationEmailAsync(targetUser, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AdminActivateUser COMPLETE: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}, Duration={ElapsedMs}ms",
                    request.TargetUserId, _currentUserService.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "AdminActivateUser CANCELED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "AdminActivateUser FAILED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private async Task SendActivationEmailAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var loginUrl = $"{_urlsService.FrontendBaseUrl}/auth/signin";

            var parameters = new Dictionary<string, object>
            {
                { "UserName", user.FullName },
                { "LoginUrl", loginUrl },
                { "SupportEmail", "support@lankaconnect.com" },
                { "Year", DateTime.UtcNow.Year.ToString() }
            };

            _logger.LogInformation(
                "[Phase 6A.90] Sending activation email to {Email}",
                user.Email.Value);

            var result = await _emailService.SendTemplatedEmailAsync(
                EmailTemplateNames.AccountActivatedByAdmin,
                user.Email.Value,
                parameters,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 6A.90] Activation email sent successfully to {Email}",
                    user.Email.Value);
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 6A.90] Failed to send activation email to {Email}: {Errors}",
                    user.Email.Value, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw
            _logger.LogError(ex,
                "[Phase 6A.90] Error sending activation email to user {UserId}",
                user.Id);
        }
    }
}
