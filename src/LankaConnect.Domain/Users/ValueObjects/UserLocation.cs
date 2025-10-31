using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Users.ValueObjects;

/// <summary>
/// Value object representing a user's location
/// Privacy-focused: includes City, State, ZipCode, Country (no street address, no GPS coordinates)
/// Suitable for regional matching in diaspora community context
/// </summary>
public class UserLocation : ValueObject
{
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }
    public string Country { get; }

    // For EF Core
    private UserLocation()
    {
        City = null!;
        State = null!;
        ZipCode = null!;
        Country = null!;
    }

    private UserLocation(string city, string state, string zipCode, string country)
    {
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    /// <summary>
    /// Creates a new UserLocation with validation
    /// </summary>
    /// <param name="city">City name (required, max 100 chars)</param>
    /// <param name="state">State/Province name (required, max 100 chars)</param>
    /// <param name="zipCode">Zip/Postal code (required, max 20 chars)</param>
    /// <param name="country">Country name (required, max 100 chars)</param>
    /// <returns>Result containing UserLocation or error message</returns>
    public static Result<UserLocation> Create(string city, string state, string zipCode, string country)
    {
        // Validate city
        if (string.IsNullOrWhiteSpace(city))
            return Result<UserLocation>.Failure("City is required");

        if (city.Length > 100)
            return Result<UserLocation>.Failure("City cannot exceed 100 characters");

        // Validate state
        if (string.IsNullOrWhiteSpace(state))
            return Result<UserLocation>.Failure("State is required");

        if (state.Length > 100)
            return Result<UserLocation>.Failure("State cannot exceed 100 characters");

        // Validate zip code
        if (string.IsNullOrWhiteSpace(zipCode))
            return Result<UserLocation>.Failure("Zip code is required");

        if (zipCode.Length > 20)
            return Result<UserLocation>.Failure("Zip code cannot exceed 20 characters");

        // Validate country
        if (string.IsNullOrWhiteSpace(country))
            return Result<UserLocation>.Failure("Country is required");

        if (country.Length > 100)
            return Result<UserLocation>.Failure("Country cannot exceed 100 characters");

        return Result<UserLocation>.Success(new UserLocation(
            city.Trim(),
            state.Trim(),
            zipCode.Trim(),
            country.Trim()
        ));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return City;
        yield return State;
        yield return ZipCode;
        yield return Country;
    }

    public override string ToString() => $"{City}, {State} {ZipCode}, {Country}";
}
