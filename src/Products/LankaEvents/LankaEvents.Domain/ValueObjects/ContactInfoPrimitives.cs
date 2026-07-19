using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

// Wave 8.5-cleanup (2026-07-18) ExtractabilityAudit GAP-6 progress:
//   - Address + GeoCoordinate promoted to LankaConnect.SharedKernel.Geo per ADR-006
//     (see SharedKernel/SharedKernel.Geo/Address.cs + GeoCoordinate.cs).
//   - Email + PhoneNumber still here transitionally (this file's residual scope);
//     promotion to SharedKernel.Contact follows in a separate small commit so the
//     history remains reversible per Consult #17 pattern.
//   - CulturalAppropriateness still here transitionally; slated to move to
//     CulturalIntelligence.Contracts alongside ICulturalCalendar promotion.
//
// Sprint Day 5 (2026-07-06) Consult #12 fallout note (preserved for history):
// the five VOs previously lived in the wiped LankaConnect.BuildingBlocks.Domain.Shared
// namespace; they were restored here as minimal stubs so LankaEvents.Domain compiles.
// This file drains as the promotions land.

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure("Email is required");
        var trimmed = value.Trim();
        if (!trimmed.Contains('@'))
            return Result<Email>.Failure("Email must contain '@'");
        return Result<Email>.Success(new Email(trimmed));
    }

    public override IEnumerable<object> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value;
}

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

// Address + GeoCoordinate promoted to LankaConnect.SharedKernel.Geo
// (Wave 8.5-cleanup 2026-07-18, ExtractabilityAudit GAP-6).
// See src/SharedKernel/SharedKernel.Geo/Address.cs and GeoCoordinate.cs.

public sealed class CulturalAppropriateness : ValueObject
{
    public double Value { get; }

    public CulturalAppropriateness(double value) { Value = value; }

    public override IEnumerable<object> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value.ToString("F2");
}
