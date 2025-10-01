using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.SendEmailVerification;

/// <summary>
/// Command to send email verification to a user
/// </summary>
/// <param name="UserId">The ID of the user to send verification email to</param>
/// <param name="Email">Optional email override. If not provided, uses user's current email</param>
/// <param name="ForceResend">Whether to force resend even if recently sent</param>
public record SendEmailVerificationCommand(
    Guid UserId,
    string? Email = null,
    bool ForceResend = false) : ICommand<SendEmailVerificationResponse>;

/// <summary>
/// Response for send email verification command
/// </summary>
public class SendEmailVerificationResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime TokenExpiresAt { get; init; }
    public bool WasRecentlySent { get; init; }
    
    public SendEmailVerificationResponse(Guid userId, string email, DateTime tokenExpiresAt, bool wasRecentlySent = false)
    {
        UserId = userId;
        Email = email;
        TokenExpiresAt = tokenExpiresAt;
        WasRecentlySent = wasRecentlySent;
    }
}