using System.Text.Json.Serialization;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Phase 7E: Captures the per-tier count for a head-count-mode registration when the event
/// has multiple ticket tiers (e.g. "VIP × 2, General × 3").
///
/// <see cref="TierName"/> is a SNAPSHOT — captured at registration time and never updated
/// even if the underlying <c>TicketTier</c> is renamed or deleted on the event. This keeps
/// historical email re-renders correct (architect requirement, mirrors the
/// <see cref="AttendeeDetails.TicketTierName"/> snapshot pattern from Slice 8 ticketing).
///
/// Public <see cref="JsonConstructorAttribute"/> exists for <c>System.Text.Json</c> deserialisation
/// from the <c>head_count</c> JSONB column; new instances should be built via <see cref="Create"/>
/// to enforce validation.
/// </summary>
public sealed class TierCount : ValueObject
{
    public Guid TierId { get; }
    public string TierName { get; }
    public int Count { get; }

    /// <summary>
    /// JSON-deserialisation entry point. Called by <c>System.Text.Json</c> with property names
    /// matching the JSONB shape. Validation is bypassed here because stored data is already
    /// validated; new instances must use <see cref="Create"/>.
    /// </summary>
    [JsonConstructor]
    public TierCount(Guid tierId, string tierName, int count)
    {
        TierId = tierId;
        TierName = tierName ?? string.Empty;
        Count = count;
    }

    /// <summary>Validated factory. Use this for all new instances; the public ctor is JSON-only.</summary>
    public static Result<TierCount> Create(Guid tierId, string? tierName, int count)
    {
        if (tierId == Guid.Empty)
            return Result<TierCount>.Failure("TierId is required");

        if (string.IsNullOrWhiteSpace(tierName))
            return Result<TierCount>.Failure("TierName is required (it is snapshotted at registration time)");

        if (count <= 0)
            return Result<TierCount>.Failure("Count must be greater than 0");

        return Result<TierCount>.Success(new TierCount(tierId, tierName.Trim(), count));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return TierId;
        yield return TierName;
        yield return Count;
    }

    public override string ToString() => $"{TierName} × {Count}";
}
