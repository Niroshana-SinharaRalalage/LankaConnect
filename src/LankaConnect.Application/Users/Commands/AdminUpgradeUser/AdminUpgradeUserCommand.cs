using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Commands.AdminUpgradeUser;

/// <summary>
/// Command to upgrade a user's role from GeneralUser to EventOrganizer by an admin (admin-initiated, no user request needed).
/// Phase 6A.139: Symmetric counterpart to AdminDowngradeUserCommand (Phase 6A.106).
/// </summary>
public record AdminUpgradeUserCommand : ICommand
{
    public Guid TargetUserId { get; init; }
    public string Reason { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }

    public AdminUpgradeUserCommand(Guid targetUserId, string reason, string? ipAddress = null, string? userAgent = null)
    {
        TargetUserId = targetUserId;
        Reason = reason;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
