using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Notifications;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Notifications.Commands.MarkAllNotificationsAsRead;

/// <summary>
/// Handler for MarkAllNotificationsAsReadCommand
/// Phase 6A.6: Marks all notifications as read for the current user
/// </summary>
public class MarkAllNotificationsAsReadCommandHandler : ICommandHandler<MarkAllNotificationsAsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkAllNotificationsAsReadCommandHandler> _logger;

    public MarkAllNotificationsAsReadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<MarkAllNotificationsAsReadCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "MarkAllNotificationsAsRead"))
        using (LogContext.PushProperty("EntityType", "Notification"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("MarkAllNotificationsAsRead START");

            try
            {
                var currentUserId = _currentUserService.UserId;

                if (currentUserId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "MarkAllNotificationsAsRead FAILED: User not authenticated - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User must be authenticated");
                }

                using (LogContext.PushProperty("UserId", currentUserId))
                {
                    _logger.LogInformation(
                        "MarkAllNotificationsAsRead: Processing - UserId={UserId}",
                        currentUserId);

                    await _notificationRepository.MarkAllAsReadAsync(currentUserId, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "MarkAllNotificationsAsRead COMPLETE: UserId={UserId}, Duration={ElapsedMs}ms",
                        currentUserId, stopwatch.ElapsedMilliseconds);

                    return Result.Success();
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "MarkAllNotificationsAsRead FAILED: Exception occurred - Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
