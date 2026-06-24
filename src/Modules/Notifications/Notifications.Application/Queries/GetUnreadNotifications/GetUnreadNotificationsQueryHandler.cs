using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Notifications.Application.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Modules.Notifications.Domain;

namespace LankaConnect.Modules.Notifications.Application.Queries.GetUnreadNotifications;

/// <summary>
/// Handler for GetUnreadNotificationsQuery
/// Phase 6A.6: Returns unread notifications for the current authenticated user
/// </summary>
public class GetUnreadNotificationsQueryHandler : IQueryHandler<GetUnreadNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadNotificationsQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        if (currentUserId == Guid.Empty)
            return Result<IReadOnlyList<NotificationDto>>.Failure("User must be authenticated");

        var notifications = await _notificationRepository.GetUnreadByUserIdAsync(currentUserId, cancellationToken);

        var dtos = notifications
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt,
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType
            })
            .ToList();

        return Result<IReadOnlyList<NotificationDto>>.Success(dtos);
    }
}
