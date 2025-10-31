using FluentAssertions;
using LankaConnect.Domain.Users.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Domain;

/// <summary>
/// TDD RED Phase - Tests for UserLocation value object
/// Following architect guidance: City, State, ZipCode, Country (no street, no coordinates)
/// </summary>
public class UserLocationTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var city = "Los Angeles";
        var state = "California";
        var zipCode = "90001";
        var country = "United States";

        // Act
        var result = UserLocation.Create(city, state, zipCode, country);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.City.Should().Be(city);
        result.Value.State.Should().Be(state);
        result.Value.ZipCode.Should().Be(zipCode);
        result.Value.Country.Should().Be(country);
    }

    [Theory]
    [InlineData(null, "California", "90001", "United States")]
    [InlineData("", "California", "90001", "United States")]
    [InlineData("   ", "California", "90001", "United States")]
    public void Create_WithInvalidCity_ShouldReturnFailure(string? city, string state, string zipCode, string country)
    {
        // Act
        var result = UserLocation.Create(city!, state, zipCode, country);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("City is required");
    }

    [Theory]
    [InlineData("Los Angeles", null, "90001", "United States")]
    [InlineData("Los Angeles", "", "90001", "United States")]
    [InlineData("Los Angeles", "   ", "90001", "United States")]
    public void Create_WithInvalidState_ShouldReturnFailure(string city, string? state, string zipCode, string country)
    {
        // Act
        var result = UserLocation.Create(city, state!, zipCode, country);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("State is required");
    }

    [Theory]
    [InlineData("Los Angeles", "California", null, "United States")]
    [InlineData("Los Angeles", "California", "", "United States")]
    [InlineData("Los Angeles", "California", "   ", "United States")]
    public void Create_WithInvalidZipCode_ShouldReturnFailure(string city, string state, string? zipCode, string country)
    {
        // Act
        var result = UserLocation.Create(city, state, zipCode!, country);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Zip code is required");
    }

    [Theory]
    [InlineData("Los Angeles", "California", "90001", null)]
    [InlineData("Los Angeles", "California", "90001", "")]
    [InlineData("Los Angeles", "California", "90001", "   ")]
    public void Create_WithInvalidCountry_ShouldReturnFailure(string city, string state, string zipCode, string? country)
    {
        // Act
        var result = UserLocation.Create(city, state, zipCode, country!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Country is required");
    }

    [Fact]
    public void Create_WithCityExceedingMaxLength_ShouldReturnFailure()
    {
        // Arrange
        var city = new string('A', 101); // Exceeds 100 character limit
        var state = "California";
        var zipCode = "90001";
        var country = "United States";

        // Act
        var result = UserLocation.Create(city, state, zipCode, country);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("City cannot exceed 100 characters");
    }

    [Fact]
    public void Create_WithStateExceedingMaxLength_ShouldReturnFailure()
    {
        // Arrange
        var city = "Los Angeles";
        var state = new string('A', 101); // Exceeds 100 character limit
        var zipCode = "90001";
        var country = "United States";

        // Act
        var result = UserLocation.Create(city, state, zipCode, country);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("State cannot exceed 100 characters");
    }

    [Fact]
    public void Create_WithZipCodeExceedingMaxLength_ShouldReturnFailure()
    {
        // Arrange
        var city = "Los Angeles";
        var state = "California";
        var zipCode = new string('9', 21); // Exceeds 20 character limit
        var country = "United States";

        // Act
        var result = UserLocation.Create(city, state, zipCode, country);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Zip code cannot exceed 20 characters");
    }

    [Fact]
    public void Create_WithCountryExceedingMaxLength_ShouldReturnFailure()
    {
        // Arrange
        var city = "Los Angeles";
        var state = "California";
        var zipCode = "90001";
        var country = new string('A', 101); // Exceeds 100 character limit

        // Act
        var result = UserLocation.Create(city, state, zipCode, country);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Country cannot exceed 100 characters");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        var city = "  Los Angeles  ";
        var state = "  California  ";
        var zipCode = "  90001  ";
        var country = "  United States  ";

        // Act
        var result = UserLocation.Create(city, state, zipCode, country);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.City.Should().Be("Los Angeles");
        result.Value.State.Should().Be("California");
        result.Value.ZipCode.Should().Be("90001");
        result.Value.Country.Should().Be("United States");
    }

    [Fact]
    public void TwoLocationsWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var location1 = UserLocation.Create("Los Angeles", "California", "90001", "United States").Value;
        var location2 = UserLocation.Create("Los Angeles", "California", "90001", "United States").Value;

        // Act & Assert
        location1.Should().Be(location2);
        (location1 == location2).Should().BeTrue();
    }

    [Fact]
    public void TwoLocationsWithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var location1 = UserLocation.Create("Los Angeles", "California", "90001", "United States").Value;
        var location2 = UserLocation.Create("San Francisco", "California", "94102", "United States").Value;

        // Act & Assert
        location1.Should().NotBe(location2);
        (location1 != location2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var location = UserLocation.Create("Los Angeles", "California", "90001", "United States").Value;

        // Act
        var result = location.ToString();

        // Assert
        result.Should().Be("Los Angeles, California 90001, United States");
    }

    [Fact]
    public void Create_WithInternationalAddress_ShouldSucceed()
    {
        // Arrange - Testing with Toronto, Canada
        var city = "Toronto";
        var state = "Ontario";
        var zipCode = "M5H 2N2";
        var country = "Canada";

        // Act
        var result = UserLocation.Create(city, state, zipCode, country);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.City.Should().Be(city);
        result.Value.State.Should().Be(state);
        result.Value.ZipCode.Should().Be(zipCode);
        result.Value.Country.Should().Be(country);
    }

    [Fact]
    public void Create_WithUKAddress_ShouldSucceed()
    {
        // Arrange - Testing with London, UK
        var city = "London";
        var state = "England";
        var zipCode = "SW1A 1AA";
        var country = "United Kingdom";

        // Act
        var result = UserLocation.Create(city, state, zipCode, country);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.City.Should().Be(city);
        result.Value.State.Should().Be(state);
        result.Value.ZipCode.Should().Be(zipCode);
        result.Value.Country.Should().Be(country);
    }
}
