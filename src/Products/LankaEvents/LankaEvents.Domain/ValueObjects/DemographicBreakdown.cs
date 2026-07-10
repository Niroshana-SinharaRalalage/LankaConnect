using System.Text.Json.Serialization;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Phase 7E: Demographic axis of <see cref="HeadCountBreakdown"/>. Carries optional per-axis
/// counts depending on the event's <see cref="Enums.RegistrationMode"/>.
///
/// Population per mode:
/// - <c>HeadCountByAge</c> (B2): <see cref="Adults"/> + <see cref="Children"/> set, others null.
/// - <c>HeadCountByGender</c> (B3): <see cref="Males"/> + <see cref="Females"/> set, others null.
/// - <c>HeadCountByAgeAndGender</c> (B4): the four leaf counts (<see cref="AdultMales"/>,
///   <see cref="AdultFemales"/>, <see cref="ChildMales"/>, <see cref="ChildFemales"/>) set;
///   the aggregate accessors (<see cref="ComputedAdults"/>, etc.) return derived totals.
///
/// Used as a flat DTO inside the <c>head_count</c> JSONB column. <c>init</c> setters allow
/// <c>System.Text.Json</c> to populate via object-initialiser syntax without bypassing immutability.
/// </summary>
public sealed class DemographicBreakdown : ValueObject
{
    public int? Adults { get; init; }
    public int? Children { get; init; }
    public int? Males { get; init; }
    public int? Females { get; init; }
    public int? AdultMales { get; init; }
    public int? AdultFemales { get; init; }
    public int? ChildMales { get; init; }
    public int? ChildFemales { get; init; }

    /// <summary>JSON-friendly default constructor.</summary>
    public DemographicBreakdown() { }

    /// <summary>Total derived from whichever leaves are populated. Used to validate against <see cref="HeadCountBreakdown.Total"/>.</summary>
    [JsonIgnore]
    public int LeafSum
    {
        get
        {
            // B4: four leaves are the source of truth
            if (AdultMales.HasValue || AdultFemales.HasValue || ChildMales.HasValue || ChildFemales.HasValue)
            {
                return (AdultMales ?? 0) + (AdultFemales ?? 0) + (ChildMales ?? 0) + (ChildFemales ?? 0);
            }

            // B2: Adults + Children
            if (Adults.HasValue || Children.HasValue)
            {
                return (Adults ?? 0) + (Children ?? 0);
            }

            // B3: Males + Females
            if (Males.HasValue || Females.HasValue)
            {
                return (Males ?? 0) + (Females ?? 0);
            }

            return 0;
        }
    }

    /// <summary>Computed adult total (sum of AdultMales + AdultFemales) for B4; falls back to <see cref="Adults"/> for B2.</summary>
    [JsonIgnore]
    public int? ComputedAdults =>
        (AdultMales.HasValue || AdultFemales.HasValue)
            ? (AdultMales ?? 0) + (AdultFemales ?? 0)
            : Adults;

    /// <summary>Computed child total. B4 derives from leaves; B2 uses <see cref="Children"/> directly.</summary>
    [JsonIgnore]
    public int? ComputedChildren =>
        (ChildMales.HasValue || ChildFemales.HasValue)
            ? (ChildMales ?? 0) + (ChildFemales ?? 0)
            : Children;

    public override IEnumerable<object> GetEqualityComponents()
    {
        // -1 sentinel for null so equality across nullable ints is structural.
        yield return Adults ?? -1;
        yield return Children ?? -1;
        yield return Males ?? -1;
        yield return Females ?? -1;
        yield return AdultMales ?? -1;
        yield return AdultFemales ?? -1;
        yield return ChildMales ?? -1;
        yield return ChildFemales ?? -1;
    }
}
