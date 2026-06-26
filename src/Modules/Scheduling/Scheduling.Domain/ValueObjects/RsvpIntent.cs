using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Modules.Scheduling.Domain.ValueObjects;

/// <summary>
/// Reusable scheduling primitive: a user's intent to participate in a scheduled occurrence.
/// Wave 4.8.a (2026-06-26) NET-NEW type. LankaEvents <c>Registration</c> aggregate is a
/// SPECIALIZATION of this primitive (Registration adds payment, ticket, attendee details,
/// quantity per tier); future products (LankaTemples puja booking, LankaSeyla appointment
/// reservation) compose this VO directly without the LankaEvents payment lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="UserId"/> is the participant. <see cref="Status"/> tracks the intent
/// lifecycle (Pending → Confirmed → Cancelled). <see cref="CreatedAt"/> is the moment
/// the intent was recorded.
/// </para>
/// <para>
/// This VO deliberately omits payment / tier / ticket fields — those are LankaEvents
/// concerns and live on the Registration aggregate root. The split exists so that
/// future products' "I want to attend" semantics don't drag the payment machinery.
/// </para>
/// </remarks>
public sealed class RsvpIntent : ValueObject
{
    public Guid UserId { get; }
    public RsvpStatus Status { get; }
    public DateTime CreatedAt { get; }

    private RsvpIntent() { }

    private RsvpIntent(Guid userId, RsvpStatus status, DateTime createdAt)
    {
        UserId = userId;
        Status = status;
        CreatedAt = createdAt;
    }

    public static Result<RsvpIntent> Create(Guid userId, DateTime createdAt)
    {
        if (userId == Guid.Empty)
            return Result<RsvpIntent>.Failure(new Error("Scheduling.Rsvp.MissingUserId", "UserId is required"));
        return Result<RsvpIntent>.Success(new RsvpIntent(userId, RsvpStatus.Pending, createdAt));
    }

    public RsvpIntent WithStatus(RsvpStatus status) =>
        new RsvpIntent(UserId, status, CreatedAt);

    public bool IsActive => Status == RsvpStatus.Pending || Status == RsvpStatus.Confirmed;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return Status;
        yield return CreatedAt;
    }
}

public enum RsvpStatus : byte
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
}
