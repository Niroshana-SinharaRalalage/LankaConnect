using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

// Sprint Day 5 (2026-07-06) Consult #12 fallout: Email/PhoneNumber/Address/GeoCoordinate/
// CulturalAppropriateness VOs previously lived in the wiped LankaConnect.BuildingBlocks.Domain.Shared
// namespace. Restored here as minimal stubs so LankaEvents.Domain compiles. Post-sprint
// (Consult #13 planned): promote to dedicated SharedKernel.ContactInfo + SharedKernel.Geography
// projects, mirroring the SharedKernel.Money extraction pattern used in the same session.

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

public sealed class GeoCoordinate : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }

    private GeoCoordinate() { }

    public GeoCoordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }

    public override string ToString() => $"{Latitude:F6},{Longitude:F6}";
}

public sealed class CulturalAppropriateness : ValueObject
{
    public double Value { get; }

    public CulturalAppropriateness(double value) { Value = value; }

    public override IEnumerable<object> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value.ToString("F2");
}
