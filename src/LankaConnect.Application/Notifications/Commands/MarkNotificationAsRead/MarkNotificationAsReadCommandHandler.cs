using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Notifications;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
    private readonly ILogger<MarkNotificationAsReadCommandHandler> _logger;

    public MarkNotificationAsReadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<MarkNotificationAsReadCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "MarkNotificationAsRead"))
        using (LogContext.PushProperty("EntityType", "Notification"))
        using (LogContext.PushProperty("NotificationId", request.NotificationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "MarkNotificationAsRead START: NotificationId={NotificationId}",
                request.NotificationId);

            try
            {
                var currentUserId = _currentUserService.UserId;

                if (currentUserId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "MarkNotificationAsRead FAILED: User not authenticated - NotificationId={NotificationId}, Duration={ElapsedMs}ms",
                        request.NotificationId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User must be authenticated");
                }

                using (LogContext.PushProperty("UserId", currentUserId))
                {
                    if (request.NotificationId == Guid.Empty)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "MarkNotificationAsRead FAILED: Invalid NotificationId - NotificationId={NotificationId}, UserId={UserId}, Duration={ElapsedMs}ms",
                            request.NotificationId, currentUserId, stopwatch.ElapsedMilliseconds);

                        return Result.Failure("Notification ID is required");
                    }

                    var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);

                    if (notification == null)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "MarkNotificationAsRead FAILED: Notification not found - NotificationId={NotificationId}, UserId={UserId}, Duration={ElapsedMs}ms",
                            request.NotificationId, currentUserId, stopwatch.ElapsedMilliseconds);

                        return Result.Failure("Notification not found");
                    }

                    // Ensure the notification belongs to the current user
                    if (notification.UserId != currentUserId)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "MarkNotificationAsRead FAILED: Access denied - NotificationId={NotificationId}, RequestingUserId={UserId}, NotificationUserId={NotificationUserId}, Duration={ElapsedMs}ms",
                            request.NotificationId, currentUserId, notification.UserId, stopwatch.ElapsedMilliseconds);

                        return Result.Failure("You do not have permission to mark this notification as read");
                    }

                    var markAsReadResult = notification.MarkAsRead();
                    if (markAsReadResult.IsFailure)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "MarkNotificationAsRead FAILED: MarkAsRead failed - NotificationId={NotificationId}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                            request.NotificationId, currentUserId, markAsReadResult.Error, stopwatch.ElapsedMilliseconds);

                        return markAsReadResult;
                    }

                    _notificationRepository.Update(notification);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "MarkNotificationAsRead COMPLETE: NotificationId={NotificationId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.NotificationId, currentUserId, stopwatch.ElapsedMilliseconds);

                    return Result.Success();
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "MarkNotificationAsRead FAILED: Exception occurred - NotificationId={NotificationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.NotificationId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
