using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Modules.Identity.Application.Commands.Users.AdminDowngradeUser;

/// <summary>
/// Command to downgrade a user's role to GeneralUser by an admin
/// Phase 6A.106: Admin User Role Downgrade
/// </summary>
public record AdminDowngradeUserCommand : ICommand
{
    public Guid TargetUserId { get; init; }
    public string Reason { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }

    public AdminDowngradeUserCommand(Guid targetUserId, string reason, string? ipAddress = null, string? userAgent = null)
    {
        TargetUserId = targetUserId;
        Reason = reason;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
