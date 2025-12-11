using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Notifications;
using LankaConnect.Domain.Notifications.Enums;

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

    public RejectRoleUpgradeCommandHandler(
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RejectRoleUpgradeCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
            return Result.Failure("User not found");

        // Reject the role upgrade (clears PendingUpgradeRole)
        var rejectionResult = user.RejectRoleUpgrade(request.Reason);
        if (rejectionResult.IsFailure)
            return rejectionResult;

        // Phase 6A.6: Create in-app notification for rejected role upgrade
        var notificationTitle = "Role Upgrade Request Declined";
        var notificationMessage = string.IsNullOrWhiteSpace(request.Reason)
            ? "Your role upgrade request has been declined. Please contact support for more information."
            : $"Your role upgrade request has been declined. Reason: {request.Reason}";

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
        }

        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
