using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Commands.SendPasswordReset;

/// <summary>
/// Command to send password reset email to a user
/// </summary>
/// <param name="Email">The email address to send password reset to</param>
/// <param name="ForceResend">Whether to force resend even if recently sent</param>
public record SendPasswordResetCommand(
    string Email,
    bool ForceResend = false) : ICommand<SendPasswordResetResponse>;

/// <summary>
/// Response for send password reset command
/// </summary>
public class SendPasswordResetResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime TokenExpiresAt { get; init; }
    public bool WasRecentlySent { get; init; }
    public bool UserNotFound { get; init; }
    
    public SendPasswordResetResponse(Guid userId, string email, DateTime tokenExpiresAt, 
        bool wasRecentlySent = false, bool userNotFound = false)
    {
        UserId = userId;
        Email = email;
        TokenExpiresAt = tokenExpiresAt;
        WasRecentlySent = wasRecentlySent;
        UserNotFound = userNotFound;
    }
}