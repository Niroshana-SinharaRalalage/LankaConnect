using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
namespace LankaConnect.Modules.Communications.Application.Commands.ResetPassword;

/// <summary>
/// Command to reset user password using reset token
/// Phase 6A.101: Email is now optional - user can be looked up by token
/// </summary>
/// <param name="Token">The password reset token</param>
/// <param name="NewPassword">The new password</param>
/// <param name="Email">The email address of the user (optional - for backward compatibility)</param>
public record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string? Email = null) : ICommand<ResetPasswordResponse>;

/// <summary>
/// Response for reset password command
/// </summary>
public class ResetPasswordResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime PasswordChangedAt { get; init; }
    public bool RequiresLogin { get; init; } = true;
    
    public ResetPasswordResponse(Guid userId, string email, DateTime passwordChangedAt, bool requiresLogin = true)
    {
        UserId = userId;
        Email = email;
        PasswordChangedAt = passwordChangedAt;
        RequiresLogin = requiresLogin;
    }
}