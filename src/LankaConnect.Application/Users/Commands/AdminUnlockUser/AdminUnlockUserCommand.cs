using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Commands.AdminUnlockUser;

/// <summary>
/// Command to unlock a user account by an admin
/// Phase 6A.90: Admin User Management
/// </summary>
public record AdminUnlockUserCommand : ICommand
{
    public Guid TargetUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }

    public AdminUnlockUserCommand(Guid targetUserId, string? ipAddress = null, string? userAgent = null)
    {
        TargetUserId = targetUserId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
