using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Modules.Scheduling.Domain.ValueObjects;

/// <summary>
/// Reusable scheduling primitive: encodes whether waitlist signups are accepted when
/// capacity is exhausted, and how many slots the waitlist can hold. Wave 4.8.a
/// (2026-06-26) extracted from the Event aggregate's inline waitlist logic (the
/// <c>_waitingList</c> collection + Epic 2 waiting-list methods).
/// </summary>
/// <remarks>
/// <para>
/// Policy = <see cref="WaitlistMode.NotAccepted"/> means "do not queue overflow; reject
/// the registration outright". <see cref="WaitlistMode.Accepted"/> means "queue overflow
/// up to <see cref="MaxSize"/> slots". MaxSize = 0 with Accepted mode is unbounded.
/// </para>
/// <para>
/// The waitlist ENTRIES themselves (who is queued, in what position) remain on the
/// aggregate that composes this VO — Event.cs keeps the <c>_waitingList</c> collection
/// for LankaEvents; LankaTemples puja-slot will own its own collection. This VO is the
/// POLICY only.
/// </para>
/// </remarks>
public sealed class WaitlistPolicy : ValueObject
{
    public WaitlistMode Mode { get; }
    public int MaxSize { get; }

    private WaitlistPolicy() { }

    private WaitlistPolicy(WaitlistMode mode, int maxSize)
    {
        Mode = mode;
        MaxSize = maxSize;
    }

    /// <summary>
    /// Default policy: overflow registrations are rejected.
    /// </summary>
    public static WaitlistPolicy NotAccepted => new WaitlistPolicy(WaitlistMode.NotAccepted, 0);

    public static Result<WaitlistPolicy> Accepted(int maxSize = 0)
    {
        if (maxSize < 0)
            return Result<WaitlistPolicy>.Failure(new Error("Scheduling.Waitlist.NegativeMaxSize", "MaxSize cannot be negative"));
        return Result<WaitlistPolicy>.Success(new WaitlistPolicy(WaitlistMode.Accepted, maxSize));
    }

    public bool AcceptsNewEntries(int currentWaitlistSize)
    {
        if (Mode == WaitlistMode.NotAccepted) return false;
        if (MaxSize == 0) return true; // unbounded
        return currentWaitlistSize < MaxSize;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Mode;
        yield return MaxSize;
    }
}

public enum WaitlistMode : byte
{
    NotAccepted = 0,
    Accepted = 1,
}
