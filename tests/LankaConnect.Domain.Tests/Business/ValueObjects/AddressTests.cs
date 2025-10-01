using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.Business.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        var street = "123 Galle Road";
        var city = "Colombo";
        var state = "Western Province";
        var zipCode = "00100";
        var country = "Sri Lanka";

        var result = Address.Create(street, city, state, zipCode, country);

        Assert.True(result.IsSuccess);
        var address = result.Value;
        Assert.Equal(street, address.Street);
        Assert.Equal(city, address.City);
        Assert.Equal(state, address.State);
        Assert.Equal(zipCode, address.ZipCode);
        Assert.Equal(country, address.Country);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidStreet_ShouldReturnFailure(string street)
    {
        var result = Address.Create(street, "Colombo", "Western", "00100", "Sri Lanka");

        Assert.True(result.IsFailure);
        Assert.Contains("Street address is required", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidCity_ShouldReturnFailure(string city)
    {
        var result = Address.Create("123 Galle Road", city, "Western", "00100", "Sri Lanka");

        Assert.True(result.IsFailure);
        Assert.Contains("City is required", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidState_ShouldReturnFailure(string state)
    {
        var result = Address.Create("123 Galle Road", "Colombo", state, "00100", "Sri Lanka");

        Assert.True(result.IsFailure);
        Assert.Contains("State is required", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidZipCode_ShouldReturnFailure(string zipCode)
    {
        var result = Address.Create("123 Galle Road", "Colombo", "Western", zipCode, "Sri Lanka");

        Assert.True(result.IsFailure);
        Assert.Contains("Zip code is required", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidCountry_ShouldReturnFailure(string country)
    {
        var result = Address.Create("123 Galle Road", "Colombo", "Western", "00100", country);

        Assert.True(result.IsFailure);
        Assert.Contains("Country is required", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongStreet_ShouldReturnFailure()
    {
        var street = new string('a', 256);

        var result = Address.Create(street, "Colombo", "Western", "00100", "Sri Lanka");

        Assert.True(result.IsFailure);
        Assert.Contains("Street address cannot exceed 255 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthStreet_ShouldReturnSuccess()
    {
        var street = new string('a', 255);

        var result = Address.Create(street, "Colombo", "Western", "00100", "Sri Lanka");

        Assert.True(result.IsSuccess);
        Assert.Equal(street, result.Value.Street);
    }

    [Fact]
    public void Create_WithTooLongCity_ShouldReturnFailure()
    {
        var city = new string('a', 101);

        var result = Address.Create("123 Galle Road", city, "Western", "00100", "Sri Lanka");

        Assert.True(result.IsFailure);
        Assert.Contains("City cannot exceed 100 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthCity_ShouldReturnSuccess()
    {
        var city = new string('a', 100);

        var result = Address.Create("123 Galle Road", city, "Western", "00100", "Sri Lanka");

        Assert.True(result.IsSuccess);
        Assert.Equal(city, result.Value.City);
    }

    [Fact]
    public void Create_WithTooLongState_ShouldReturnFailure()
    {
        var state = new string('a', 101);

        var result = Address.Create("123 Galle Road", "Colombo", state, "00100", "Sri Lanka");

        Assert.True(result.IsFailure);
        Assert.Contains("State cannot exceed 100 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthState_ShouldReturnSuccess()
    {
        var state = new string('a', 100);

        var result = Address.Create("123 Galle Road", "Colombo", state, "00100", "Sri Lanka");

        Assert.True(result.IsSuccess);
        Assert.Equal(state, result.Value.State);
    }

    [Fact]
    public void Create_WithTooLongZipCode_ShouldReturnFailure()
    {
        var zipCode = new string('1', 21);

        var result = Address.Create("123 Galle Road", "Colombo", "Western", zipCode, "Sri Lanka");

        Assert.True(result.IsFailure);
        Assert.Contains("Zip code cannot exceed 20 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthZipCode_ShouldReturnSuccess()
    {
        var zipCode = new string('1', 20);

        var result = Address.Create("123 Galle Road", "Colombo", "Western", zipCode, "Sri Lanka");

        Assert.True(result.IsSuccess);
        Assert.Equal(zipCode, result.Value.ZipCode);
    }

    [Fact]
    public void Create_WithTooLongCountry_ShouldReturnFailure()
    {
        var country = new string('a', 101);

        var result = Address.Create("123 Galle Road", "Colombo", "Western", "00100", country);

        Assert.True(result.IsFailure);
        Assert.Contains("Country cannot exceed 100 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthCountry_ShouldReturnSuccess()
    {
        var country = new string('a', 100);

        var result = Address.Create("123 Galle Road", "Colombo", "Western", "00100", country);

        Assert.True(result.IsSuccess);
        Assert.Equal(country, result.Value.Country);
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromAllFields()
    {
        var street = "  123 Galle Road  ";
        var city = "  Colombo  ";
        var state = "  Western Province  ";
        var zipCode = "  00100  ";
        var country = "  Sri Lanka  ";

        var result = Address.Create(street, city, state, zipCode, country);

        Assert.True(result.IsSuccess);
        var address = result.Value;
        Assert.Equal("123 Galle Road", address.Street);
        Assert.Equal("Colombo", address.City);
        Assert.Equal("Western Province", address.State);
        Assert.Equal("00100", address.ZipCode);
        Assert.Equal("Sri Lanka", address.Country);
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        var address1 = Address.Create("123 Main St", "Colombo", "Western", "00100", "Sri Lanka").Value;
        var address2 = Address.Create("123 Main St", "Colombo", "Western", "00100", "Sri Lanka").Value;

        Assert.Equal(address1, address2);
    }

    [Fact]
    public void Equality_WithDifferentStreets_ShouldNotBeEqual()
    {
        var address1 = Address.Create("123 Main St", "Colombo", "Western", "00100", "Sri Lanka").Value;
        var address2 = Address.Create("456 Main St", "Colombo", "Western", "00100", "Sri Lanka").Value;

        Assert.NotEqual(address1, address2);
    }

    [Fact]
    public void Equality_WithDifferentCities_ShouldNotBeEqual()
    {
        var address1 = Address.Create("123 Main St", "Colombo", "Western", "00100", "Sri Lanka").Value;
        var address2 = Address.Create("123 Main St", "Kandy", "Central", "00100", "Sri Lanka").Value;

        Assert.NotEqual(address1, address2);
    }

    [Fact]
    public void Equality_WithDifferentStates_ShouldNotBeEqual()
    {
        var address1 = Address.Create("123 Main St", "Colombo", "Western", "00100", "Sri Lanka").Value;
        var address2 = Address.Create("123 Main St", "Colombo", "Central", "00100", "Sri Lanka").Value;

        Assert.NotEqual(address1, address2);
    }

    [Fact]
    public void Equality_WithDifferentZipCodes_ShouldNotBeEqual()
    {
        var address1 = Address.Create("123 Main St", "Colombo", "Western", "00100", "Sri Lanka").Value;
        var address2 = Address.Create("123 Main St", "Colombo", "Western", "00200", "Sri Lanka").Value;

        Assert.NotEqual(address1, address2);
    }

    [Fact]
    public void Equality_WithDifferentCountries_ShouldNotBeEqual()
    {
        var address1 = Address.Create("123 Main St", "Colombo", "Western", "00100", "Sri Lanka").Value;
        var address2 = Address.Create("123 Main St", "Colombo", "Western", "00100", "India").Value;

        Assert.NotEqual(address1, address2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        var address1 = Address.Create("123 Main St", "Colombo", "Western", "00100", "Sri Lanka").Value;
        var address2 = Address.Create("123 Main St", "Colombo", "Western", "00100", "Sri Lanka").Value;

        Assert.Equal(address1.GetHashCode(), address2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var address = Address.Create("123 Galle Road", "Colombo", "Western Province", "00100", "Sri Lanka").Value;

        var result = address.ToString();

        Assert.Equal("123 Galle Road, Colombo, Western Province 00100, Sri Lanka", result);
    }

    [Theory]
    [InlineData("123", "City", "State", "12345", "Country", "123, City, State 12345, Country")]
    [InlineData("Long Street Name", "Long City Name", "Long State Name", "12345-6789", "Long Country Name", 
        "Long Street Name, Long City Name, Long State Name 12345-6789, Long Country Name")]
    public void ToString_WithVariousInputs_ShouldFormatCorrectly(string street, string city, string state, string zipCode, string country, string expected)
    {
        var address = Address.Create(street, city, state, zipCode, country).Value;

        var result = address.ToString();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Create_WithSriLankanAddress_ShouldReturnSuccess()
    {
        var result = Address.Create(
            "No. 45, Duplication Road",
            "Colombo 04",
            "Western Province", 
            "00400",
            "Sri Lanka");

        Assert.True(result.IsSuccess);
        var address = result.Value;
        Assert.Equal("No. 45, Duplication Road", address.Street);
        Assert.Equal("Colombo 04", address.City);
        Assert.Equal("Western Province", address.State);
        Assert.Equal("00400", address.ZipCode);
        Assert.Equal("Sri Lanka", address.Country);
    }

    [Fact]
    public void Create_WithInternationalAddress_ShouldReturnSuccess()
    {
        var result = Address.Create(
            "1600 Pennsylvania Avenue NW",
            "Washington",
            "District of Columbia",
            "20500",
            "United States");

        Assert.True(result.IsSuccess);
        var address = result.Value;
        Assert.Equal("1600 Pennsylvania Avenue NW", address.Street);
        Assert.Equal("Washington", address.City);
        Assert.Equal("District of Columbia", address.State);
        Assert.Equal("20500", address.ZipCode);
        Assert.Equal("United States", address.Country);
    }

    [Theory]
    [InlineData("123 Main St", "Apt 4B", "Suite 200")]
    [InlineData("Building A, Unit 15", "Complex Name", "Floor 3")]
    public void Create_WithComplexStreetAddresses_ShouldReturnSuccess(params string[] streetParts)
    {
        var street = string.Join(", ", streetParts);

        var result = Address.Create(street, "Test City", "Test State", "12345", "Test Country");

        Assert.True(result.IsSuccess);
        Assert.Equal(street, result.Value.Street);
    }

    [Fact]
    public void Create_WithSpecialCharactersInZipCode_ShouldReturnSuccess()
    {
        var zipCode = "SW1A 1AA"; // UK postal code format

        var result = Address.Create("10 Downing St", "London", "England", zipCode, "United Kingdom");

        Assert.True(result.IsSuccess);
        Assert.Equal(zipCode, result.Value.ZipCode);
    }

    [Fact]
    public void Create_WithNumericPostalCode_ShouldReturnSuccess()
    {
        var zipCode = "12345-6789"; // US ZIP+4 format

        var result = Address.Create("123 Main St", "Anytown", "Anystate", zipCode, "United States");

        Assert.True(result.IsSuccess);
        Assert.Equal(zipCode, result.Value.ZipCode);
    }
}