using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Notifications;
using LankaConnect.Domain.Notifications.Enums;

namespace LankaConnect.Application.Users.Commands.ApproveRoleUpgrade;

/// <summary>
/// Handler for ApproveRoleUpgradeCommand
/// Phase 6A.5: Approves pending role upgrade and starts free trial for Event Organizers
/// Phase 6A.6: Creates in-app notification when role upgrade is approved
/// </summary>
public class ApproveRoleUpgradeCommandHandler : ICommandHandler<ApproveRoleUpgradeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveRoleUpgradeCommandHandler(
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ApproveRoleUpgradeCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
            return Result.Failure("User not found");

        // Approve the role upgrade (updates Role, clears PendingUpgradeRole)
        var approvalResult = user.ApproveRoleUpgrade();
        if (approvalResult.IsFailure)
            return approvalResult;

        // Phase 6A.6: Create in-app notification for approved role upgrade
        var notificationTitle = $"Role Upgrade Approved";
        var notificationMessage = user.Role == UserRole.EventOrganizer
            ? $"Congratulations! Your request to become an Event Organizer has been approved. You now have a 6-month free trial to explore all Event Organizer features."
            : $"Congratulations! Your role has been upgraded to {user.Role}.";

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
        }

        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
