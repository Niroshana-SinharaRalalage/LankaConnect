using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.SharedKernel.Geo;

/// <summary>
/// Postal address value object. Pan-platform primitive per ADR-006: shared by
/// LankaEvents (venue address), LankaHomes (property address), LankaBusiness
/// (business address), Identity (user location), and any future product needing
/// physical-location data.
/// </summary>
/// <remarks>
/// <para>
/// Promoted from <c>LankaConnect.Products.LankaEvents.Domain.ValueObjects</c> in
/// Wave 8.5-cleanup (2026-07-18) per ExtractabilityAudit GAP-6 — the shape had
/// been living inside a Product namespace since Consult #12 wiped
/// <c>LankaConnect.BuildingBlocks.Domain.Shared</c>. Its home is SharedKernel.Geo,
/// matching the SharedKernel.Money extraction pattern.
/// </para>
/// <para>
/// Behavior-preserving move: same equality components, same ToString shape, same
/// factory + fields. Callers keep working via a new <c>using</c> directive.
/// </para>
/// </remarks>
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }
    public string Country { get; }

    private Address() { Street = City = State = ZipCode = Country = string.Empty; }

    public Address(string street, string city, string state, string zipCode, string country)
    {
        Street = street ?? string.Empty;
        City = city ?? string.Empty;
        State = state ?? string.Empty;
        ZipCode = zipCode ?? string.Empty;
        Country = country ?? string.Empty;
    }

    public static Result<Address> Create(string street, string city, string state, string zipCode, string country)
        => Result<Address>.Success(new Address(street, city, state, zipCode, country));

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
        yield return Country;
    }

    public override string ToString() => $"{Street}, {City}, {State}, {ZipCode}, {Country}";
}
