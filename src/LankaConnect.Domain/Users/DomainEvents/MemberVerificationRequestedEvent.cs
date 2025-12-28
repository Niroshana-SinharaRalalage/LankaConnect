using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Users.DomainEvents;

/// <summary>
/// Raised when a member requests email verification (signup or resend).
/// Triggers MemberVerificationRequestedEventHandler to send verification email.
/// Phase 6A.53: Member Email Verification System
/// </summary>
public sealed class MemberVerificationRequestedEvent : IDomainEvent
{
    public DateTime OccurredAt { get; }
    public Guid UserId { get; }
    public string Email { get; }
    public string VerificationToken { get; }
    public DateTimeOffset RequestedAt { get; }

    public MemberVerificationRequestedEvent(
        Guid userId,
        string email,
        string verificationToken,
        DateTimeOffset requestedAt)
    {
        OccurredAt = DateTime.UtcNow;
        UserId = userId;
        Email = email;
        VerificationToken = verificationToken;
        RequestedAt = requestedAt;
    }
}
