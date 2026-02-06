using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Commands.AdminActivateUser;

/// <summary>
/// Command to activate a user by an admin
/// Phase 6A.90: Admin User Management
/// </summary>
public record AdminActivateUserCommand : ICommand
{
    public Guid TargetUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }

    public AdminActivateUserCommand(Guid targetUserId, string? ipAddress = null, string? userAgent = null)
    {
        TargetUserId = targetUserId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
