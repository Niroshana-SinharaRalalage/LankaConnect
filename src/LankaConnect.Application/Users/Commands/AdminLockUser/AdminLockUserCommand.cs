using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Commands.AdminLockUser;

/// <summary>
/// Command to lock a user account by an admin
/// Phase 6A.90: Admin User Management
/// </summary>
public record AdminLockUserCommand : ICommand
{
    public Guid TargetUserId { get; init; }
    public DateTime LockUntil { get; init; }
    public string? Reason { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }

    public AdminLockUserCommand(
        Guid targetUserId,
        DateTime lockUntil,
        string? reason = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        TargetUserId = targetUserId;
        LockUntil = lockUntil;
        Reason = reason;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
