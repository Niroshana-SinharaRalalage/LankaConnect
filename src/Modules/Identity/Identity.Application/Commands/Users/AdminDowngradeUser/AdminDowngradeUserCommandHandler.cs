using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using System.Text.Json;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Support;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Identity.Application.Commands.Users.AdminDowngradeUser;

/// <summary>
/// Handler for AdminDowngradeUserCommand
/// Phase 6A.106: Downgrades a user's role to GeneralUser by admin with role hierarchy protection and audit logging.
/// </summary>
public class AdminDowngradeUserCommandHandler : ICommandHandler<AdminDowngradeUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventRepository _eventRepository;
    // TODO Phase 6A.106 Hotfix: Re-add IBillingRepository when implementation exists
    // private readonly IBillingRepository _billingRepository;
    private readonly ILogger<AdminDowngradeUserCommandHandler> _logger;

    public AdminDowngradeUserCommandHandler(
        IUserRepository userRepository,
        IAdminAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IEventRepository eventRepository,
        // TODO Phase 6A.106 Hotfix: Re-add IBillingRepository when implementation exists
        // IBillingRepository billingRepository,
        ILogger<AdminDowngradeUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _eventRepository = eventRepository;
        // TODO Phase 6A.106 Hotfix: Re-add when implementation exists
        // _billingRepository = billingRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(AdminDowngradeUserCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AdminDowngradeUser"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("TargetUserId", request.TargetUserId))
        using (LogContext.PushProperty("AdminUserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AdminDowngradeUser START: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}",
                request.TargetUserId, _currentUserService.UserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get admin user to check role hierarchy
                var adminUser = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
                if (adminUser == null)
                {
                    _logger.LogWarning(
                        "AdminDowngradeUser FAILED: Admin user not found - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Admin user not found");
                }

                // Validate admin role
                if (adminUser.Role != UserRole.Admin && adminUser.Role != UserRole.AdminManager)
                {
                    _logger.LogWarning(
                        "AdminDowngradeUser FAILED: Insufficient permissions - AdminUserId={AdminUserId}, Role={Role}",
                        _currentUserService.UserId, adminUser.Role);
                    return Result.Failure("Insufficient permissions to perform this action");
                }

                // Get target user
                var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
                if (targetUser == null)
                {
                    _logger.LogWarning(
                        "AdminDowngradeUser FAILED: Target user not found - TargetUserId={TargetUserId}",
                        request.TargetUserId);
                    return Result.Failure("User not found");
                }

                // Self-prevention: Admin cannot downgrade themselves
                if (targetUser.Id == adminUser.Id)
                {
                    _logger.LogWarning(
                        "AdminDowngradeUser FAILED: Cannot downgrade own account - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Cannot downgrade your own account");
                }

                // Role hierarchy protection: Admin cannot manage AdminManager
                if (adminUser.Role == UserRole.Admin &&
                    (targetUser.Role == UserRole.AdminManager || targetUser.Role == UserRole.Admin))
                {
                    _logger.LogWarning(
                        "AdminDowngradeUser FAILED: Role hierarchy violation - AdminRole={AdminRole}, TargetRole={TargetRole}",
                        adminUser.Role, targetUser.Role);
                    return Result.Failure("Cannot perform actions on users with equal or higher role");
                }

                // Store old role before downgrade
                var oldRole = targetUser.Role;

                _logger.LogInformation(
                    "AdminDowngradeUser: Downgrading user - TargetUserId={TargetUserId}, TargetEmail={TargetEmail}, CurrentRole={CurrentRole}",
                    targetUser.Id, targetUser.Email.Value, oldRole);

                // Downgrade the user to GeneralUser
                var result = targetUser.DowngradeToGeneralUserByAdmin();
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "AdminDowngradeUser FAILED: Domain validation failed - TargetUserId={TargetUserId}, Error={Error}",
                        request.TargetUserId, result.Error);
                    return result;
                }

                // Create audit log
                var auditDetails = JsonSerializer.Serialize(new
                {
                    TargetEmail = targetUser.Email.Value,
                    TargetName = targetUser.FullName,
                    OldRole = oldRole.ToString(),
                    NewRole = "GeneralUser",
                    Reason = request.Reason
                });

                var auditLog = AdminAuditLog.CreateForUserAction(
                    _currentUserService.UserId,
                    AdminAuditActions.UserRoleDowngraded,
                    targetUser.Id,
                    auditDetails,
                    request.IpAddress,
                    request.UserAgent);

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                // Fail-silent operations after role downgrade succeeds
                await UnpublishUserEventsAsync(targetUser.Id, cancellationToken);
                // TODO Phase 6A.106 Hotfix: Re-enable when IBillingRepository implementation exists
                // await CancelUserSubscriptionAsync(targetUser.Id, oldRole, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AdminDowngradeUser COMPLETE: TargetUserId={TargetUserId}, AdminUserId={AdminUserId}, Duration={ElapsedMs}ms",
                    request.TargetUserId, _currentUserService.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "AdminDowngradeUser CANCELED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "AdminDowngradeUser FAILED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.TargetUserId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private async Task UnpublishUserEventsAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var events = await _eventRepository.GetByOrganizerAsync(userId, cancellationToken);
            var publishedFutureEvents = events.Where(e =>
                e.Status == EventStatus.Published &&
                e.StartDate > DateTime.UtcNow).ToList();

            if (!publishedFutureEvents.Any())
            {
                _logger.LogInformation("No published future events to unpublish for user {UserId}", userId);
                return;
            }

            foreach (var evt in publishedFutureEvents)
            {
                var result = evt.Unpublish();
                if (result.IsFailure)
                {
                    _logger.LogWarning("Failed to unpublish event {EventId}: {Error}", evt.Id, result.Error);
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            _logger.LogInformation("Unpublished {Count} future events for downgraded user {UserId}", publishedFutureEvents.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unpublish events for downgraded user {UserId}", userId);
            // Fail-silent: role change already committed
        }
    }

    // TODO Phase 6A.106 Hotfix: Re-enable when IBillingRepository implementation exists and is registered in DI
    // private async Task CancelUserSubscriptionAsync(Guid userId, UserRole oldRole, CancellationToken cancellationToken)
    // {
    //     if (!oldRole.RequiresSubscription())
    //     {
    //         _logger.LogInformation("User {UserId} with role {Role} does not require subscription, skipping cancellation", userId, oldRole);
    //         return;
    //     }
    //
    //     try
    //     {
    //         var userIdResult = UserId.Create(userId);
    //         if (userIdResult.IsFailure)
    //         {
    //             _logger.LogWarning("Invalid user ID {UserId}: {Error}", userId, userIdResult.Error);
    //             return;
    //         }
    //
    //         var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(userIdResult.Value, cancellationToken);
    //
    //         if (subscription == null)
    //         {
    //             _logger.LogInformation("No subscription found for user {UserId}", userId);
    //             return;
    //         }
    //
    //         if (!subscription.IsActive)
    //         {
    //             _logger.LogInformation("Subscription for user {UserId} is already inactive", userId);
    //             return;
    //         }
    //
    //         subscription.Cancel();
    //         await _billingRepository.SaveSubscriptionAsync(subscription, cancellationToken);
    //
    //         _logger.LogWarning("Stripe subscription {SubscriptionId} for user {UserId} cancelled locally. " +
    //                           "Stripe API cancellation not yet implemented - manual cancellation required via Stripe dashboard.",
    //                           subscription.StripeSubscriptionId, userId);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Failed to cancel subscription for downgraded user {UserId}", userId);
    //         // Fail-silent: role change already committed
    //     }
    // }
}
