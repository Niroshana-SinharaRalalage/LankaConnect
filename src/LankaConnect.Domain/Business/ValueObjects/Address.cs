using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business.ValueObjects;

public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }
    public string Country { get; }

    private Address(string street, string city, string state, string zipCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    public static Result<Address> Create(string street, string city, string state, string zipCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            return Result<Address>.Failure("Street address is required");

        if (string.IsNullOrWhiteSpace(city))
            return Result<Address>.Failure("City is required");

        if (string.IsNullOrWhiteSpace(state))
            return Result<Address>.Failure("State is required");

        if (string.IsNullOrWhiteSpace(zipCode))
            return Result<Address>.Failure("Zip code is required");

        if (string.IsNullOrWhiteSpace(country))
            return Result<Address>.Failure("Country is required");

        if (street.Length > 255)
            return Result<Address>.Failure("Street address cannot exceed 255 characters");

        if (city.Length > 100)
            return Result<Address>.Failure("City cannot exceed 100 characters");

        if (state.Length > 100)
            return Result<Address>.Failure("State cannot exceed 100 characters");

        if (zipCode.Length > 20)
            return Result<Address>.Failure("Zip code cannot exceed 20 characters");

        if (country.Length > 100)
            return Result<Address>.Failure("Country cannot exceed 100 characters");

        return Result<Address>.Success(new Address(
            street.Trim(),
            city.Trim(),
            state.Trim(),
            zipCode.Trim(),
            country.Trim()
        ));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
        yield return Country;
    }

    public override string ToString() => $"{Street}, {City}, {State} {ZipCode}, {Country}";
}