using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.VerifyEmail;

/// <summary>
/// Command to verify a user's email address using verification token
/// </summary>
/// <param name="UserId">The ID of the user verifying their email</param>
/// <param name="Token">The email verification token</param>
public record VerifyEmailCommand(
    Guid UserId,
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