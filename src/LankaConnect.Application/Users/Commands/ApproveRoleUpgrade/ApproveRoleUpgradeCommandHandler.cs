using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Notifications;
using LankaConnect.Domain.Notifications.Enums;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Users.Commands.ApproveRoleUpgrade;

/// <summary>
/// Handler for ApproveRoleUpgradeCommand
/// Phase 6A.5: Approves pending role upgrade and starts free trial for Event Organizers
/// Phase 6A.6: Creates in-app notification when role upgrade is approved
/// Phase 6A.75: Sends email notification when EventOrganizer role is approved
/// </summary>
public class ApproveRoleUpgradeCommandHandler : ICommandHandler<ApproveRoleUpgradeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly IApplicationUrlsService _urlsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveRoleUpgradeCommandHandler> _logger;

    public ApproveRoleUpgradeCommandHandler(
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IEmailService emailService,
        IApplicationUrlsService urlsService,
        IUnitOfWork unitOfWork,
        ILogger<ApproveRoleUpgradeCommandHandler> logger)
    {
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _urlsService = urlsService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ApproveRoleUpgradeCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
            return Result.Failure("User not found");

        // Capture the role being approved before the state changes
        var approvedRole = user.PendingUpgradeRole;

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

        // Phase 6A.75: Send email notification for EventOrganizer role approval
        if (user.Role == UserRole.EventOrganizer)
        {
            await SendOrganizerApprovalEmailAsync(user, cancellationToken);
        }

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.75: Sends organizer role approval email using database template.
    /// Fail-silent pattern: Logs error but doesn't fail the command if email fails.
    /// </summary>
    private async Task SendOrganizerApprovalEmailAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var userName = $"{user.FirstName} {user.LastName}";
            var dashboardUrl = $"{_urlsService.FrontendBaseUrl}/dashboard";

            var parameters = new Dictionary<string, object>
            {
                { "UserName", userName },
                { "ApprovedAt", DateTime.UtcNow.ToString("MMMM dd, yyyy h:mm tt") },
                { "DashboardUrl", dashboardUrl }
            };

            _logger.LogInformation(
                "[Phase 6A.75] Sending organizer role approval email to {Email} for user {UserId}",
                user.Email.Value, user.Id);

            var result = await _emailService.SendTemplatedEmailAsync(
                "organizer-role-approved",
                user.Email.Value,
                parameters,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 6A.75] Organizer role approval email sent successfully to {Email}",
                    user.Email.Value);
            }
            else
            {
                _logger.LogError(
                    "[Phase 6A.75] Failed to send organizer role approval email to {Email}: {Errors}",
                    user.Email.Value, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw to prevent command failure
            _logger.LogError(ex,
                "[Phase 6A.75] Error sending organizer role approval email to user {UserId}",
                user.Id);
        }
    }
}
