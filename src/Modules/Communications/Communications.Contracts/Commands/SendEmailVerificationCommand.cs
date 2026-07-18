using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Modules.Communications.Contracts.Commands; // Wave 8.5.d (2026-07-18): split from LegacyPromotions/ per Consult #17 Q2 Day 10 debt. Originally promoted from Communications.Application/Commands/ (4C.h Day 5, 2026-07-10) per Consult #15 PASS C.

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
