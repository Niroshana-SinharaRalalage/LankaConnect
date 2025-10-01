using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.Business.ValueObjects;

public class BusinessLocationTests
{
    [Fact]
    public void Create_WithAddressAndCoordinates_ShouldReturnSuccess()
    {
        var address = Address.Create("123 Galle Road", "Colombo", "Western", "00100", "Sri Lanka").Value;
        var coordinates = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        var result = BusinessLocation.Create(address, coordinates);

        Assert.True(result.IsSuccess);
        var location = result.Value;
        Assert.Equal(address, location.Address);
        Assert.Equal(coordinates, location.Coordinates);
    }

    [Fact]
    public void Create_WithAddressOnly_ShouldReturnSuccess()
    {
        var address = Address.Create("123 Galle Road", "Colombo", "Western", "00100", "Sri Lanka").Value;

        var result = BusinessLocation.Create(address);

        Assert.True(result.IsSuccess);
        var location = result.Value;
        Assert.Equal(address, location.Address);
        Assert.Null(location.Coordinates);
    }

    [Fact]
    public void Create_WithNullAddress_ShouldReturnFailure()
    {
        var coordinates = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        var result = BusinessLocation.Create(null!, coordinates);

        Assert.True(result.IsFailure);
        Assert.Contains("Address is required", result.Errors);
    }

    [Fact]
    public void Create_WithStringParameters_ShouldReturnSuccess()
    {
        var street = "456 Kandy Road";
        var city = "Kandy";
        var state = "Central";
        var zipCode = "20000";
        var country = "Sri Lanka";
        var latitude = 7.2906m;
        var longitude = 80.6337m;

        var result = BusinessLocation.Create(street, city, state, zipCode, country, latitude, longitude);

        Assert.True(result.IsSuccess);
        var location = result.Value;
        Assert.Equal(street, location.Address.Street);
        Assert.Equal(city, location.Address.City);
        Assert.Equal(state, location.Address.State);
        Assert.Equal(zipCode, location.Address.ZipCode);
        Assert.Equal(country, location.Address.Country);
        Assert.NotNull(location.Coordinates);
        Assert.Equal(latitude, location.Coordinates.Latitude);
        Assert.Equal(longitude, location.Coordinates.Longitude);
    }

    [Fact]
    public void Create_WithStringParametersWithoutCoordinates_ShouldReturnSuccess()
    {
        var street = "456 Kandy Road";
        var city = "Kandy";
        var state = "Central";
        var zipCode = "20000";
        var country = "Sri Lanka";

        var result = BusinessLocation.Create(street, city, state, zipCode, country);

        Assert.True(result.IsSuccess);
        var location = result.Value;
        Assert.Equal(street, location.Address.Street);
        Assert.Equal(city, location.Address.City);
        Assert.Equal(state, location.Address.State);
        Assert.Equal(zipCode, location.Address.ZipCode);
        Assert.Equal(country, location.Address.Country);
        Assert.Null(location.Coordinates);
    }

    [Fact]
    public void Create_WithStringParametersAndPartialCoordinates_ShouldReturnSuccess()
    {
        var street = "456 Kandy Road";
        var city = "Kandy";
        var state = "Central";
        var zipCode = "20000";
        var country = "Sri Lanka";
        var latitude = 7.2906m;
        // longitude is null

        var result = BusinessLocation.Create(street, city, state, zipCode, country, latitude, null);

        Assert.True(result.IsSuccess);
        var location = result.Value;
        Assert.Equal(street, location.Address.Street);
        Assert.Null(location.Coordinates);
    }

    [Fact]
    public void Create_WithInvalidAddressFields_ShouldReturnFailure()
    {
        var result = BusinessLocation.Create("", "Colombo", "Western", "00100", "Sri Lanka", 6.9271m, 79.8612m);

        Assert.True(result.IsFailure);
        Assert.Contains("Street address is required", result.Errors.First());
    }

    [Fact]
    public void Create_WithInvalidCoordinates_ShouldReturnFailure()
    {
        var latitude = 91m; // Invalid latitude
        var longitude = 79.8612m;

        var result = BusinessLocation.Create("123 Street", "Colombo", "Western", "00100", "Sri Lanka", latitude, longitude);

        Assert.True(result.IsFailure);
        Assert.Contains("Latitude must be between -90 and 90 degrees", result.Errors.First());
    }

    [Fact]
    public void DistanceTo_WithBothCoordinates_ShouldCalculateDistance()
    {
        var location1 = BusinessLocation.Create(
            "123 Main St", "New York", "NY", "10001", "USA", 40.7128m, -74.0060m).Value;
        var location2 = BusinessLocation.Create(
            "456 Market St", "Philadelphia", "PA", "19107", "USA", 39.9526m, -75.1652m).Value;

        var distance = location1.DistanceTo(location2);

        Assert.NotNull(distance);
        Assert.True(distance > 0);
        // Distance between NYC and Philadelphia is approximately 95 miles (130 km)
        Assert.True(distance > 125 && distance < 135, $"Expected distance between 125-135 km, got {distance}");
    }

    [Fact]
    public void DistanceTo_WithMissingCoordinatesInFirst_ShouldReturnNull()
    {
        var location1 = BusinessLocation.Create(
            "123 Galle Road", "Colombo", "Western", "00100", "Sri Lanka").Value;
        var location2 = BusinessLocation.Create(
            "456 Kandy Road", "Kandy", "Central", "20000", "Sri Lanka", 7.2906m, 80.6337m).Value;

        var distance = location1.DistanceTo(location2);

        Assert.Null(distance);
    }

    [Fact]
    public void DistanceTo_WithMissingCoordinatesInSecond_ShouldReturnNull()
    {
        var location1 = BusinessLocation.Create(
            "123 Galle Road", "Colombo", "Western", "00100", "Sri Lanka", 6.9271m, 79.8612m).Value;
        var location2 = BusinessLocation.Create(
            "456 Kandy Road", "Kandy", "Central", "20000", "Sri Lanka").Value;

        var distance = location1.DistanceTo(location2);

        Assert.Null(distance);
    }

    [Fact]
    public void DistanceTo_WithBothMissingCoordinates_ShouldReturnNull()
    {
        var location1 = BusinessLocation.Create(
            "123 Galle Road", "Colombo", "Western", "00100", "Sri Lanka").Value;
        var location2 = BusinessLocation.Create(
            "456 Kandy Road", "Kandy", "Central", "20000", "Sri Lanka").Value;

        var distance = location1.DistanceTo(location2);

        Assert.Null(distance);
    }

    [Fact]
    public void DistanceTo_WithSameCoordinates_ShouldReturnZero()
    {
        var coordinates = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
        var location1 = BusinessLocation.Create(
            "123 Galle Road", "Colombo", "Western", "00100", "Sri Lanka", 6.9271m, 79.8612m).Value;
        var location2 = BusinessLocation.Create(
            "456 Different Street", "Colombo", "Western", "00200", "Sri Lanka", 6.9271m, 79.8612m).Value;

        var distance = location1.DistanceTo(location2);

        Assert.NotNull(distance);
        Assert.Equal(0.0, distance.Value, 0.001); // Allow small floating point differences
    }

    [Fact]
    public void Equality_WithSameAddressAndCoordinates_ShouldBeEqual()
    {
        var address = Address.Create("123 Street", "City", "State", "12345", "Country").Value;
        var coordinates = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        var location1 = BusinessLocation.Create(address, coordinates).Value;
        var location2 = BusinessLocation.Create(address, coordinates).Value;

        Assert.Equal(location1, location2);
    }

    [Fact]
    public void Equality_WithSameAddressNoCoordinates_ShouldBeEqual()
    {
        var address = Address.Create("123 Street", "City", "State", "12345", "Country").Value;

        var location1 = BusinessLocation.Create(address).Value;
        var location2 = BusinessLocation.Create(address).Value;

        Assert.Equal(location1, location2);
    }

    [Fact]
    public void Equality_WithDifferentAddresses_ShouldNotBeEqual()
    {
        var address1 = Address.Create("123 Street", "City", "State", "12345", "Country").Value;
        var address2 = Address.Create("456 Street", "City", "State", "12345", "Country").Value;
        var coordinates = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        var location1 = BusinessLocation.Create(address1, coordinates).Value;
        var location2 = BusinessLocation.Create(address2, coordinates).Value;

        Assert.NotEqual(location1, location2);
    }

    [Fact]
    public void Equality_WithDifferentCoordinates_ShouldNotBeEqual()
    {
        var address = Address.Create("123 Street", "City", "State", "12345", "Country").Value;
        var coordinates1 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
        var coordinates2 = GeoCoordinate.Create(7.2906m, 80.6337m).Value;

        var location1 = BusinessLocation.Create(address, coordinates1).Value;
        var location2 = BusinessLocation.Create(address, coordinates2).Value;

        Assert.NotEqual(location1, location2);
    }

    [Fact]
    public void Equality_WithOneHavingCoordinatesOtherNot_ShouldNotBeEqual()
    {
        var address = Address.Create("123 Street", "City", "State", "12345", "Country").Value;
        var coordinates = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        var location1 = BusinessLocation.Create(address, coordinates).Value;
        var location2 = BusinessLocation.Create(address).Value;

        Assert.NotEqual(location1, location2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        var address = Address.Create("123 Street", "City", "State", "12345", "Country").Value;
        var coordinates = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        var location1 = BusinessLocation.Create(address, coordinates).Value;
        var location2 = BusinessLocation.Create(address, coordinates).Value;

        Assert.Equal(location1.GetHashCode(), location2.GetHashCode());
    }

    [Fact]
    public void ToString_WithCoordinates_ShouldIncludeGPS()
    {
        var location = BusinessLocation.Create(
            "123 Galle Road", "Colombo", "Western", "00100", "Sri Lanka", 6.9271m, 79.8612m).Value;

        var result = location.ToString();

        Assert.Contains("123 Galle Road, Colombo, Western 00100, Sri Lanka", result);
        Assert.Contains("GPS:", result);
        Assert.Contains("6.9271, 79.8612", result);
    }

    [Fact]
    public void ToString_WithoutCoordinates_ShouldNotIncludeGPS()
    {
        var location = BusinessLocation.Create(
            "123 Galle Road", "Colombo", "Western", "00100", "Sri Lanka").Value;

        var result = location.ToString();

        Assert.Equal("123 Galle Road, Colombo, Western 00100, Sri Lanka", result);
        Assert.DoesNotContain("GPS:", result);
    }

    [Fact]
    public void Create_WithRealWorldSriLankanLocations_ShouldReturnSuccess()
    {
        var locations = new[]
        {
            // Colombo Fort Railway Station
            ("Fort Railway Station", "Colombo 01", "Western Province", "00100", "Sri Lanka", 6.9344m, 79.8428m),
            // Galle Face Green
            ("Galle Face Green", "Colombo 03", "Western Province", "00300", "Sri Lanka", 6.9218m, 79.8441m),
            // Temple of the Sacred Tooth Relic, Kandy
            ("Sri Dalada Maligawa", "Kandy", "Central Province", "20000", "Sri Lanka", 7.2935m, 80.6405m),
            // Sigiriya Rock Fortress
            ("Sigiriya", "Dambulla", "Central Province", "21100", "Sri Lanka", 7.9570m, 80.7603m)
        };

        foreach (var (street, city, state, zipCode, country, lat, lng) in locations)
        {
            var result = BusinessLocation.Create(street, city, state, zipCode, country, lat, lng);
            Assert.True(result.IsSuccess, $"Failed to create location for {street}, {city}");
            
            var location = result.Value;
            Assert.NotNull(location.Coordinates);
            Assert.Equal(lat, location.Coordinates.Latitude);
            Assert.Equal(lng, location.Coordinates.Longitude);
        }
    }

    [Fact]
    public void Create_WithInternationalLocations_ShouldReturnSuccess()
    {
        var locations = new[]
        {
            // Times Square, New York
            ("Times Square", "New York", "New York", "10036", "USA", 40.7580m, -73.9855m),
            // Big Ben, London
            ("Westminster", "London", "England", "SW1A 0AA", "UK", 51.4994m, -0.1245m),
            // Sydney Opera House
            ("Bennelong Point", "Sydney", "New South Wales", "2000", "Australia", -33.8568m, 151.2153m)
        };

        foreach (var (street, city, state, zipCode, country, lat, lng) in locations)
        {
            var result = BusinessLocation.Create(street, city, state, zipCode, country, lat, lng);
            Assert.True(result.IsSuccess, $"Failed to create location for {street}, {city}");
        }
    }

    [Fact]
    public void DistanceTo_WithInternationalLocations_ShouldCalculateCorrectDistance()
    {
        var colombo = BusinessLocation.Create(
            "Galle Face", "Colombo", "Western", "00100", "Sri Lanka", 6.9218m, 79.8441m).Value;
        var london = BusinessLocation.Create(
            "Westminster", "London", "England", "SW1", "UK", 51.4994m, -0.1245m).Value;

        var distance = colombo.DistanceTo(london);

        Assert.NotNull(distance);
        // Distance between Colombo and London is approximately 8,700 km
        Assert.True(distance > 8500 && distance < 9000, $"Expected distance between 8500-9000 km, got {distance}");
    }

    [Fact]
    public void Create_WithComplexAddressStructures_ShouldReturnSuccess()
    {
        var complexAddresses = new[]
        {
            ("Building A, Level 3, Unit 301, No. 45 Duplication Road", "Colombo 04", "Western Province", "00400", "Sri Lanka"),
            ("Apartment 5B, Tower 2, Ocean View Complex", "Mount Lavinia", "Western Province", "10370", "Sri Lanka"),
            ("Shop No. 15, Ground Floor, Commercial Complex", "Kandy", "Central Province", "20000", "Sri Lanka")
        };

        foreach (var (street, city, state, zipCode, country) in complexAddresses)
        {
            var result = BusinessLocation.Create(street, city, state, zipCode, country);
            Assert.True(result.IsSuccess, $"Failed to create location for complex address: {street}");
            Assert.Equal(street, result.Value.Address.Street);
        }
    }

    [Fact]
    public void DistanceTo_ShouldBeSymmetric()
    {
        var location1 = BusinessLocation.Create(
            "Location 1", "City 1", "State", "12345", "Country", 6.9271m, 79.8612m).Value;
        var location2 = BusinessLocation.Create(
            "Location 2", "City 2", "State", "54321", "Country", 7.2906m, 80.6337m).Value;

        var distance1to2 = location1.DistanceTo(location2);
        var distance2to1 = location2.DistanceTo(location1);

        Assert.NotNull(distance1to2);
        Assert.NotNull(distance2to1);
        Assert.Equal(distance1to2.Value, distance2to1.Value, 0.001);
    }
}