using System.Diagnostics;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Infrastructure.Data;
using LankaConnect.Modules.Notifications.Domain;
using LankaConnect.Modules.Notifications.Domain.Enums;
using LankaConnect.Modules.Notifications.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Identity.Application.Commands.Users.RejectRoleUpgrade;

/// <summary>
/// Handler for RejectRoleUpgradeCommand
/// Phase 6A.5: Rejects pending role upgrade with optional reason
/// Phase 6A.6: Creates in-app notification when role upgrade is rejected
/// </summary>
public class RejectRoleUpgradeCommandHandler : ICommandHandler<RejectRoleUpgradeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IdentityDbContext _identityContext;
    private readonly NotificationsDbContext _notificationsContext;
    private readonly ILogger<RejectRoleUpgradeCommandHandler> _logger;

    public RejectRoleUpgradeCommandHandler(
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IdentityDbContext identityContext,
        NotificationsDbContext notificationsContext,
        ILogger<RejectRoleUpgradeCommandHandler> logger)
    {
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _identityContext = identityContext;
        _notificationsContext = notificationsContext;
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
                        user.Id, notificationResult.Error);
                }

                // Wave 8.5.h (D-01): retire IMultiContextUnitOfWork.CommitAsync(DbContext[]).
                // Per-context direct SaveChanges per Consult #25 Q6. Pre-retire the multi-
                // context UoW routed AppDbContext.CommitAsync (audit-log context) + the
                // NotificationsDbContext explicitly, but silently DROPPED User changes
                // because User is tracked by IdentityDbContext (moved there in 4C.e, Consult
                // #16). This mirrors the split-brain fixes shipped for CreateEventCommandHandler
                // (Sprint-Day 7) and RegisterUserHandler (Sprint-Day 9). Domain-event dispatch
                // continues via the per-module DomainEventSaveChangesInterceptor wired on
                // both contexts (Wave 8.5.f).
                await _identityContext.SaveChangesAsync(cancellationToken);
                await _notificationsContext.SaveChangesAsync(cancellationToken);

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
