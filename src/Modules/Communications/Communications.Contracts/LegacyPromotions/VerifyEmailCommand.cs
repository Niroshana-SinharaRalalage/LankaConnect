using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Modules.Communications.Contracts.LegacyPromotions; // 4C.h Day 5: promoted per Consult #15 PASS C.

/// <summary>
/// Command to verify a user's email address using verification token
/// Phase 6A.53: Changed to token-only verification (removed UserId parameter)
/// Token is sufficient to uniquely identify the user via GetByEmailVerificationTokenAsync
/// </summary>
/// <param name="Token">The email verification token</param>
public record VerifyEmailCommand(
    string Token) : ICommand<VerifyEmailResponse>;

/// <summary>
/// Response for verify email command
/// </summary>
public class VerifyEmailResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime VerifiedAt { get; init; }
    public bool WasAlreadyVerified { get; init; }
    
    public VerifyEmailResponse(Guid userId, string email, DateTime verifiedAt, bool wasAlreadyVerified = false)
    {
        UserId = userId;
        Email = email;
        VerifiedAt = verifiedAt;
        WasAlreadyVerified = wasAlreadyVerified;
    }
}
