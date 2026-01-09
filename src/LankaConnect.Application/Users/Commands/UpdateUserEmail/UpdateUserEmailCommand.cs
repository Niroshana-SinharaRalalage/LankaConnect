using MediatR;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Users.Commands.UpdateUserEmail;

/// <summary>
/// Command to update user's email address
/// Phase 6A.70: Profile Basic Info Section with Email Verification
///
/// Changes email and triggers verification flow (reuses Phase 6A.53 infrastructure)
/// User must verify new email before it becomes fully active
/// </summary>
public record UpdateUserEmailCommand : IRequest<Result<UpdateUserEmailResponse>>
{
    public Guid UserId { get; init; }
    public string NewEmail { get; init; } = null!;
}

/// <summary>
/// Response for email update operation
/// Includes verification details
/// </summary>
public record UpdateUserEmailResponse
{
    public string Email { get; init; } = null!;
    public bool IsVerified { get; init; }
    public DateTime? VerificationSentAt { get; init; }
    public string Message { get; init; } = null!;
}
