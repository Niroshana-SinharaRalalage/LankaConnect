using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Notifications;

namespace LankaConnect.Application.Notifications.Commands.MarkNotificationAsRead;

/// <summary>
/// Handler for MarkNotificationAsReadCommand
/// Phase 6A.6: Marks a notification as read for the current user
/// </summary>
public class MarkNotificationAsReadCommandHandler : ICommandHandler<MarkNotificationAsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationAsReadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        if (currentUserId == Guid.Empty)
            return Result.Failure("User must be authenticated");

        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification == null)
            return Result.Failure("Notification not found");

        // Ensure the notification belongs to the current user
        if (notification.UserId != currentUserId)
            return Result.Failure("You do not have permission to mark this notification as read");

        var markAsReadResult = notification.MarkAsRead();
        if (markAsReadResult.IsFailure)
            return markAsReadResult;

        _notificationRepository.Update(notification);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
