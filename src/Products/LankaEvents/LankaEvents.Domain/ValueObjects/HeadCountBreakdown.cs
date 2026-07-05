using System.Text.Json.Serialization;
using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Phase 7E: Composite multi-axis head-count for B-mode registrations.
///
/// Always carries a <see cref="Total"/>. Optional axes:
/// - <see cref="Demographics"/> — present in B2/B3/B4. <c>null</c> in B1.
/// - <see cref="TierCounts"/> — present iff the event has ticket tiers configured. Independent
///   of the demographic axis (organisers can pick B1 + tier counts, B2 + tier counts, etc.).
///
/// Invariants enforced in factories:
/// 1. <see cref="Total"/> ≥ 1.
/// 2. If <see cref="Demographics"/> is set, its leaf sum equals <see cref="Total"/>.
/// 3. If <see cref="TierCounts"/> is set, the sum of counts equals <see cref="Total"/>.
///
/// Persisted as a flat JSONB string via a custom <c>ValueConverter</c> + deep-copy
/// <c>ValueComparer</c> in <see cref="LankaConnect.Infrastructure.Data.Configurations.RegistrationConfiguration"/>
/// — NOT via <c>OwnsOne(...).ToJson()</c>, which has the Phase 6A.130 IReadOnlyList rehydration trap.
///
/// Construction paths:
/// - <see cref="ForTotalOnly"/> (B1)
/// - <see cref="ForByAge"/> (B2)
/// - <see cref="ForByGender"/> (B3)
/// - <see cref="ForByAgeAndGender"/> (B4)
/// All factories optionally accept a <c>tierCounts</c> argument that is validated against the derived total.
/// </summary>
public sealed class HeadCountBreakdown : ValueObject
{
    public int Total { get; }
    public DemographicBreakdown? Demographics { get; }
    public IReadOnlyList<TierCount>? TierCounts { get; }

    /// <summary>
    /// JSON-deserialisation entry point. Validation is intentionally bypassed here because
    /// the JSONB column already contains validated data. New instances must go through one of
    /// the static factories.
    /// </summary>
    [JsonConstructor]
    public HeadCountBreakdown(int total, DemographicBreakdown? demographics, IReadOnlyList<TierCount>? tierCounts)
    {
        Total = total;
        Demographics = demographics;
        TierCounts = tierCounts;
    }

    /// <summary>Mode B1 factory. Total is the sole input.</summary>
    public static Result<HeadCountBreakdown> ForTotalOnly(int total, IReadOnlyList<TierCount>? tierCounts = null)
    {
        if (total <= 0)
            return Result<HeadCountBreakdown>.Failure("Total must be greater than 0");

        var tierResult = ValidateTierCounts(tierCounts, total, expectedAdults: null, expectedChildren: null,
            modeNameForAgeAxisRejection: "HeadCountOnly");
        if (tierResult.IsFailure)
            return Result<HeadCountBreakdown>.Failure(tierResult.Errors);

        return Result<HeadCountBreakdown>.Success(new HeadCountBreakdown(total, null, tierCounts));
    }

    /// <summary>Mode B2 factory. Total is auto-derived from Adults + Children.</summary>
    public static Result<HeadCountBreakdown> ForByAge(int adults, int children, IReadOnlyList<TierCount>? tierCounts = null)
    {
        if (adults < 0 || children < 0)
            return Result<HeadCountBreakdown>.Failure("Adults and Children counts must be non-negative");

        var total = adults + children;
        if (total <= 0)
            return Result<HeadCountBreakdown>.Failure("At least one attendee is required");

        var demographics = new DemographicBreakdown { Adults = adults, Children = children };

        var tierResult = ValidateTierCounts(tierCounts, total, expectedAdults: adults, expectedChildren: children,
            modeNameForAgeAxisRejection: null);
        if (tierResult.IsFailure)
            return Result<HeadCountBreakdown>.Failure(tierResult.Errors);

        return Result<HeadCountBreakdown>.Success(new HeadCountBreakdown(total, demographics, tierCounts));
    }

    /// <summary>Mode B3 factory. Total is auto-derived from Males + Females.</summary>
    public static Result<HeadCountBreakdown> ForByGender(int males, int females, IReadOnlyList<TierCount>? tierCounts = null)
    {
        if (males < 0 || females < 0)
            return Result<HeadCountBreakdown>.Failure("Males and Females counts must be non-negative");

        var total = males + females;
        if (total <= 0)
            return Result<HeadCountBreakdown>.Failure("At least one attendee is required");

        var demographics = new DemographicBreakdown { Males = males, Females = females };

        var tierResult = ValidateTierCounts(tierCounts, total, expectedAdults: null, expectedChildren: null,
            modeNameForAgeAxisRejection: "HeadCountByGender");
        if (tierResult.IsFailure)
            return Result<HeadCountBreakdown>.Failure(tierResult.Errors);

        return Result<HeadCountBreakdown>.Success(new HeadCountBreakdown(total, demographics, tierCounts));
    }

    /// <summary>Mode B4 factory. Total is auto-derived from the four leaf counts.</summary>
    public static Result<HeadCountBreakdown> ForByAgeAndGender(
        int adultMales, int adultFemales, int childMales, int childFemales,
        IReadOnlyList<TierCount>? tierCounts = null)
    {
        if (adultMales < 0 || adultFemales < 0 || childMales < 0 || childFemales < 0)
            return Result<HeadCountBreakdown>.Failure("All leaf counts must be non-negative");

        var total = adultMales + adultFemales + childMales + childFemales;
        if (total <= 0)
            return Result<HeadCountBreakdown>.Failure("At least one attendee is required");

        var demographics = new DemographicBreakdown
        {
            AdultMales = adultMales,
            AdultFemales = adultFemales,
            ChildMales = childMales,
            ChildFemales = childFemales,
        };

        // B4 cross-axis: tier-age sum must agree with the *aggregated* age split
        // (AdultMales + AdultFemales = adults; ChildMales + ChildFemales = children).
        // Architect §3 clarification: B4 doesn't expose Demographics.Adults directly;
        // we aggregate the 4-leaf cross here.
        var tierResult = ValidateTierCounts(tierCounts, total,
            expectedAdults: adultMales + adultFemales,
            expectedChildren: childMales + childFemales,
            modeNameForAgeAxisRejection: null);
        if (tierResult.IsFailure)
            return Result<HeadCountBreakdown>.Failure(tierResult.Errors);

        return Result<HeadCountBreakdown>.Success(new HeadCountBreakdown(total, demographics, tierCounts));
    }

    /// <summary>
    /// Validates the tier-counts axis against the total + (Phase 7F-C) the age axis when present.
    ///
    /// Two new architect-required invariants on top of the legacy <c>sum(Count) == total</c>:
    /// 1. <strong>B1 / B3 reject age splits</strong> — these modes don't capture age, so any
    ///    <see cref="TierCount"/> with <see cref="TierCount.HasAgeSplit"/> true is invalid.
    ///    <paramref name="modeNameForAgeAxisRejection"/> is non-null when this rule applies.
    /// 2. <strong>B2 / B4 cross-axis sum check</strong> — when at least one tier has age splits,
    ///    sum(<c>AdultCount</c>) must equal <paramref name="expectedAdults"/> AND
    ///    sum(<c>ChildCount</c>) must equal <paramref name="expectedChildren"/>. Architect Q1
    ///    default = strict.
    /// </summary>
    private static Result ValidateTierCounts(
        IReadOnlyList<TierCount>? tierCounts,
        int total,
        int? expectedAdults,
        int? expectedChildren,
        string? modeNameForAgeAxisRejection)
    {
        if (tierCounts == null || tierCounts.Count == 0)
            return Result.Success();

        var tierSum = tierCounts.Sum(t => t.Count);
        if (tierSum != total)
            return Result.Failure(
                $"Sum of tier counts ({tierSum}) must equal head-count total ({total}). " +
                $"Tiers: {string.Join(", ", tierCounts)}");

        // Phase 7F-C invariant #1: B1 / B3 must not carry an age axis.
        if (modeNameForAgeAxisRejection != null)
        {
            if (tierCounts.Any(t => t.HasAgeSplit))
                return Result.Failure(
                    $"{modeNameForAgeAxisRejection} mode does not capture an age axis — " +
                    $"TierCount.AdultCount and TierCount.ChildCount must both be null in this mode. " +
                    $"Use HeadCountByAge (B2) or HeadCountByAgeAndGender (B4) for per-tier-age pricing.");
            return Result.Success();
        }

        // Phase 7F-C invariant #2: B2 / B4 — if any tier has age splits, all relevant ones
        // must agree with the demographic-axis totals.
        var anySplit = tierCounts.Any(t => t.HasAgeSplit);
        if (!anySplit)
            return Result.Success(); // legacy path — null axis is valid even on B2/B4 (architect Q7).

        // All-or-nothing across the basket: if ANY tier has a split, every tier in the basket
        // must (otherwise the cross-axis sum can't be checked sensibly). This is a
        // policy choice — relax later if organisers ask for partial age tagging.
        if (tierCounts.Any(t => !t.HasAgeSplit))
            return Result.Failure(
                "Age-split inconsistency: some tiers have AdultCount/ChildCount set and others don't. " +
                "Specify the age split on every tier, or none of them.");

        var adultSum = tierCounts.Sum(t => t.AdultCount ?? 0);
        var childSum = tierCounts.Sum(t => t.ChildCount ?? 0);

        if (expectedAdults.HasValue && adultSum != expectedAdults.Value)
            return Result.Failure(
                $"Cross-axis mismatch: sum of tier AdultCount ({adultSum}) must equal demographic adults ({expectedAdults}). " +
                $"Tiers: {string.Join(", ", tierCounts)}");

        if (expectedChildren.HasValue && childSum != expectedChildren.Value)
            return Result.Failure(
                $"Cross-axis mismatch: sum of tier ChildCount ({childSum}) must equal demographic children ({expectedChildren}). " +
                $"Tiers: {string.Join(", ", tierCounts)}");

        return Result.Success();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Total;
        yield return Demographics ?? (object)"<none>";

        if (TierCounts == null)
        {
            yield return "<no-tiers>";
        }
        else
        {
            // Element-wise structural equality so two TierCount lists with identical content compare equal.
            yield return TierCounts.Count;
            foreach (var tc in TierCounts)
            {
                yield return tc;
            }
        }
    }

    public override string ToString()
    {
        var parts = new List<string> { $"Total={Total}" };
        if (Demographics != null) parts.Add($"Demographics={{Adults={Demographics.Adults},Children={Demographics.Children},Males={Demographics.Males},Females={Demographics.Females},AM={Demographics.AdultMales},AF={Demographics.AdultFemales},CM={Demographics.ChildMales},CF={Demographics.ChildFemales}}}");
        if (TierCounts is { Count: > 0 }) parts.Add($"Tiers=[{string.Join(", ", TierCounts)}]");
        return string.Join(" ", parts);
    }
}
