using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Modules.Scheduling.Domain.ValueObjects;

/// <summary>
/// Reusable scheduling primitive: wraps the total-capacity integer plus the predicates
/// that check whether N additional reservations fit. Wave 4.8.a (2026-06-26) extracted
/// from the Event aggregate's inline <c>Capacity</c> int + <c>HasCapacityFor(int)</c> /
/// <c>IsAtCapacity()</c> methods.
/// </summary>
/// <remarks>
/// <para>
/// Capacity is an absolute integer (no per-tier semantics — those stay in the
/// LankaEvents-specific <c>TicketTier</c>). The VO answers "do N more fit given M
/// currently reserved?" — it does not own the reservation count, which lives on the
/// aggregate that composes it.
/// </para>
/// <para>
/// LankaTemples (capacity per puja slot) and LankaSeyla (capacity per appointment block)
/// reuse this primitive so they do not reinvent the off-by-one bug that the original
/// Event.HasCapacityFor had to be patched for in Phase 6A.136C.
/// </para>
/// </remarks>
public sealed class CapacityRule : ValueObject
{
    public int Total { get; }

    private CapacityRule() { }

    private CapacityRule(int total)
    {
        Total = total;
    }

    public static Result<CapacityRule> Create(int total)
    {
        if (total <= 0)
            return Result<CapacityRule>.Failure(new Error("Scheduling.Capacity.NotPositive", "Capacity must be greater than 0"));
        return Result<CapacityRule>.Success(new CapacityRule(total));
    }

    /// <summary>
    /// Does <paramref name="additional"/> more reservations fit given <paramref name="currentlyReserved"/>?
    /// </summary>
    public bool HasRoomFor(int currentlyReserved, int additional) =>
        currentlyReserved + additional <= Total;

    public bool IsFull(int currentlyReserved) =>
        currentlyReserved >= Total;

    public int Remaining(int currentlyReserved) =>
        Math.Max(0, Total - currentlyReserved);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Total;
    }
}
