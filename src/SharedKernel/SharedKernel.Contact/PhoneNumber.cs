using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.SharedKernel.Contact;

/// <summary>
/// Phone number value object. Pan-platform primitive per ADR-006 — consumed by
/// Identity (user profile phone), LankaEvents (attendee phone / anonymous
/// registrant phone), Communications (WhatsApp recipient phone), and any future
/// product needing phone as a first-class type.
/// </summary>
/// <remarks>
/// <para>
/// Promoted from <c>LankaConnect.Products.LankaEvents.Domain.ValueObjects</c> in
/// Wave 8.5-cleanup (2026-07-18) per ExtractabilityAudit GAP-6. Behavior-preserving
/// move: same trim rule, same <c>Value</c> accessor, same <c>ToDisplayFormat</c>.
/// </para>
/// </remarks>
public sealed class PhoneNumber : ValueObject
{
    public string Value { get; }

    private PhoneNumber(string value) { Value = value; }

    public static Result<PhoneNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PhoneNumber>.Failure("Phone number is required");
        return Result<PhoneNumber>.Success(new PhoneNumber(value.Trim()));
    }

    public string ToDisplayFormat() => Value;

    public override IEnumerable<object> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value;
}
