using System.Text.Json.Serialization;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Phase 7E: Captures the per-tier count for a head-count-mode registration when the event
/// has multiple ticket tiers (e.g. "VIP × 2, General × 3").
///
/// <see cref="TierName"/> is a SNAPSHOT — captured at registration time and never updated
/// even if the underlying <c>TicketTier</c> is renamed or deleted on the event. This keeps
/// historical email re-renders correct (architect requirement, mirrors the
/// <see cref="AttendeeDetails.TicketTierName"/> snapshot pattern from Slice 8 ticketing).
///
/// Phase 7F-C (architect-approved 2026-04-30): adds an optional per-tier-by-age axis
/// (<see cref="AdultCount"/> + <see cref="ChildCount"/>) so B2 / B4 mode events with tiered
/// pricing can split a tier's count into adults vs children and let the pricing helper bill
/// <c>tier.AdultPrice × adults + tier.ChildPrice × children</c>. Both nullable: legacy 7E.3c
/// payloads (B1 / B3 / B-mode-without-age-split) leave both null and pricing falls back to
/// <c>AdultPrice × Count</c> per the architect Q7 default ("legacy null-axis stays a valid
/// choice indefinitely").
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
    /// Phase 7F-C: optional per-tier-by-age count of adults. Nullable; if set, <see cref="ChildCount"/>
    /// must also be set (no half-set) and they must sum to <see cref="Count"/>.
    /// </summary>
    public int? AdultCount { get; }

    /// <summary>
    /// Phase 7F-C: optional per-tier-by-age count of children. See <see cref="AdultCount"/>.
    /// </summary>
    public int? ChildCount { get; }

    /// <summary>
    /// Phase 7F-E.7 (architect-approved 2026-05-04, re-opens §2.2 #4 deferred decision):
    /// optional per-tier 4-leaf demographic split. All-or-nothing per tier (any of 4
    /// set → all 4 set + sum equals <see cref="Count"/>). When set, age split is
    /// auto-derived for back-compat with the 7F-C pricing helper.
    /// </summary>
    public int? AdultMaleCount { get; }

    /// <summary>Phase 7F-E.7: see <see cref="AdultMaleCount"/>.</summary>
    public int? AdultFemaleCount { get; }

    /// <summary>Phase 7F-E.7: see <see cref="AdultMaleCount"/>.</summary>
    public int? ChildMaleCount { get; }

    /// <summary>Phase 7F-E.7: see <see cref="AdultMaleCount"/>.</summary>
    public int? ChildFemaleCount { get; }

    /// <summary>
    /// Phase 7F-C: derived — true iff this tier carries an age split. Used by call-sites
    /// to switch between the legacy <c>AdultPrice × Count</c> path and the new
    /// per-age path. Both <see cref="AdultCount"/> and <see cref="ChildCount"/> are
    /// always either both set or both null (factory invariant), so this single check suffices.
    /// </summary>
    [JsonIgnore]
    public bool HasAgeSplit => AdultCount.HasValue;

    /// <summary>
    /// Phase 7F-E.7: derived — true iff this tier carries the per-tier 4-leaf demographic
    /// split. Renderers use this to switch from the legacy "N/A on per-tier rows" display
    /// to the captured 4-leaf display.
    /// </summary>
    [JsonIgnore]
    public bool HasFourLeafSplit => AdultMaleCount.HasValue;

    /// <summary>
    /// JSON-deserialisation entry point. Called by <c>System.Text.Json</c> with property names
    /// matching the JSONB shape. Validation is bypassed here because stored data is already
    /// validated; new instances must use <see cref="Create"/>.
    ///
    /// Phase 7F-C: <paramref name="adultCount"/> + <paramref name="childCount"/> are nullable
    /// so legacy JSONB rows (which lack these fields entirely) deserialise cleanly with both null.
    ///
    /// Phase 7F-E.7: 4-leaf params likewise nullable for legacy-row deserialisation.
    /// </summary>
    [JsonConstructor]
    public TierCount(
        Guid tierId, string tierName, int count,
        int? adultCount = null, int? childCount = null,
        int? adultMaleCount = null, int? adultFemaleCount = null,
        int? childMaleCount = null, int? childFemaleCount = null)
    {
        TierId = tierId;
        TierName = tierName ?? string.Empty;
        Count = count;
        AdultCount = adultCount;
        ChildCount = childCount;
        AdultMaleCount = adultMaleCount;
        AdultFemaleCount = adultFemaleCount;
        ChildMaleCount = childMaleCount;
        ChildFemaleCount = childFemaleCount;
    }

    /// <summary>
    /// Validated factory. Use this for all new instances; the public ctor is JSON-only.
    ///
    /// Phase 7F-C invariants (architect edits #1, #2):
    /// - Both <paramref name="adultCount"/> and <paramref name="childCount"/> must be set, or
    ///   both null. Half-set is rejected — eliminates ambiguity between "no age split" and
    ///   "zero of that category."
    /// - When both are set, <paramref name="adultCount"/> + <paramref name="childCount"/> must
    ///   equal <paramref name="count"/>.
    /// - Both must be non-negative when set.
    /// </summary>
    public static Result<TierCount> Create(
        Guid tierId,
        string? tierName,
        int count,
        int? adultCount = null,
        int? childCount = null,
        int? adultMaleCount = null,
        int? adultFemaleCount = null,
        int? childMaleCount = null,
        int? childFemaleCount = null)
    {
        if (tierId == Guid.Empty)
            return Result<TierCount>.Failure("TierId is required");

        if (string.IsNullOrWhiteSpace(tierName))
            return Result<TierCount>.Failure("TierName is required (it is snapshotted at registration time)");

        if (count <= 0)
            return Result<TierCount>.Failure("Count must be greater than 0");

        // Phase 7F-E.7 invariant: the 4-leaf is all-or-nothing per tier.
        var fourLeafSetCount = (adultMaleCount.HasValue ? 1 : 0)
                             + (adultFemaleCount.HasValue ? 1 : 0)
                             + (childMaleCount.HasValue ? 1 : 0)
                             + (childFemaleCount.HasValue ? 1 : 0);
        if (fourLeafSetCount > 0 && fourLeafSetCount < 4)
            return Result<TierCount>.Failure(
                "TierCount.Create: per-tier 4-leaf demographic split is all-or-nothing. " +
                "Either set all of AdultMaleCount / AdultFemaleCount / ChildMaleCount / " +
                "ChildFemaleCount, or leave all null. Half-set is ambiguous.");

        // Phase 7F-E.7: when 4-leaf is fully set, derive age split for back-compat with
        // the 7F-C pricing helper. If the caller ALSO passed in age-split, validate that
        // the two axes agree — mismatch is rejected as ambiguous source of truth.
        if (fourLeafSetCount == 4)
        {
            if (adultMaleCount!.Value < 0 || adultFemaleCount!.Value < 0
                || childMaleCount!.Value < 0 || childFemaleCount!.Value < 0)
                return Result<TierCount>.Failure(
                    "TierCount.Create: 4-leaf counts must be non-negative.");

            var derivedAdults = adultMaleCount.Value + adultFemaleCount.Value;
            var derivedChildren = childMaleCount.Value + childFemaleCount.Value;
            if (derivedAdults + derivedChildren != count)
                return Result<TierCount>.Failure(
                    $"TierCount.Create: 4-leaf sum ({adultMaleCount}+{adultFemaleCount}+" +
                    $"{childMaleCount}+{childFemaleCount}={derivedAdults + derivedChildren}) " +
                    $"must equal Count ({count}).");

            // Cross-axis check: if caller passed age-split, it must agree with the 4-leaf.
            if (adultCount.HasValue && adultCount.Value != derivedAdults)
                return Result<TierCount>.Failure(
                    $"TierCount.Create: 4-leaf adult sum ({derivedAdults}) doesn't match " +
                    $"AdultCount ({adultCount}). Either drop the explicit age split (it'll be " +
                    "derived from the 4-leaf) or correct the values so they agree.");
            if (childCount.HasValue && childCount.Value != derivedChildren)
                return Result<TierCount>.Failure(
                    $"TierCount.Create: 4-leaf child sum ({derivedChildren}) doesn't match " +
                    $"ChildCount ({childCount}). Either drop the explicit age split or correct.");

            // Auto-derive age split for back-compat with the existing 7F-C pricing helper.
            adultCount ??= derivedAdults;
            childCount ??= derivedChildren;
        }

        // Phase 7F-C invariant #1: both-or-neither (age split).
        if (adultCount.HasValue ^ childCount.HasValue)
            return Result<TierCount>.Failure(
                "TierCount.Create: both AdultCount and ChildCount must be set, or both null. " +
                "Half-set is ambiguous — pick one shape per tier.");

        if (adultCount.HasValue)
        {
            // Both are set (XOR ruled out above).
            if (adultCount.Value < 0 || childCount!.Value < 0)
                return Result<TierCount>.Failure("AdultCount and ChildCount must be non-negative");

            if (adultCount.Value + childCount.Value != count)
                return Result<TierCount>.Failure(
                    $"TierCount.Create: AdultCount ({adultCount}) + ChildCount ({childCount}) " +
                    $"must equal Count ({count}).");
        }

        return Result<TierCount>.Success(new TierCount(
            tierId, tierName.Trim(), count,
            adultCount, childCount,
            adultMaleCount, adultFemaleCount, childMaleCount, childFemaleCount));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return TierId;
        yield return TierName;
        yield return Count;
        // Phase 7F-C: include the age axis in equality so two TierCounts with identical
        // counts but different age splits are distinct.
        yield return AdultCount ?? -1;
        yield return ChildCount ?? -1;
        // Phase 7F-E.7: include the 4-leaf axis as well. Two TierCounts with the same
        // age split but different 4-leaf splits (e.g. {2 AM, 0 AF, 0 CM, 1 CF} vs
        // {1 AM, 1 AF, 1 CM, 0 CF}) are semantically distinct.
        yield return AdultMaleCount ?? -1;
        yield return AdultFemaleCount ?? -1;
        yield return ChildMaleCount ?? -1;
        yield return ChildFemaleCount ?? -1;
    }

    public override string ToString()
    {
        if (HasFourLeafSplit)
            return $"{TierName} × {Count} (AM:{AdultMaleCount}, AF:{AdultFemaleCount}, " +
                   $"CM:{ChildMaleCount}, CF:{ChildFemaleCount})";
        if (HasAgeSplit)
            return $"{TierName} × {Count} ({AdultCount} adults, {ChildCount} children)";
        return $"{TierName} × {Count}";
    }
}
