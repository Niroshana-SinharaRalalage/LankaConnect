using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.Business.ValueObjects;

public class GeoCoordinateTests
{
    [Fact]
    public void Create_WithValidCoordinates_ShouldReturnSuccess()
    {
        var latitude = 6.9271m; // Colombo latitude
        var longitude = 79.8612m; // Colombo longitude

        var result = GeoCoordinate.Create(latitude, longitude);

        Assert.True(result.IsSuccess);
        var coordinate = result.Value;
        Assert.Equal(latitude, coordinate.Latitude);
        Assert.Equal(longitude, coordinate.Longitude);
    }

    [Theory]
    [InlineData(6.9271, 79.8612)] // Colombo, Sri Lanka
    [InlineData(40.7128, -74.0060)] // New York, USA
    [InlineData(-33.8688, 151.2093)] // Sydney, Australia
    [InlineData(51.5074, -0.1278)] // London, UK
    [InlineData(35.6762, 139.6503)] // Tokyo, Japan
    public void Create_WithValidWorldCoordinates_ShouldReturnSuccess(double lat, double lng)
    {
        var latitude = (decimal)lat;
        var longitude = (decimal)lng;

        var result = GeoCoordinate.Create(latitude, longitude);

        Assert.True(result.IsSuccess);
        Assert.Equal(latitude, result.Value.Latitude);
        Assert.Equal(longitude, result.Value.Longitude);
    }

    [Theory]
    [InlineData(-90.0)] // South Pole
    [InlineData(90.0)]  // North Pole
    [InlineData(0.0)]   // Equator
    public void Create_WithValidLatitudeBoundaries_ShouldReturnSuccess(double lat)
    {
        var latitude = (decimal)lat;
        var longitude = 0m;

        var result = GeoCoordinate.Create(latitude, longitude);

        Assert.True(result.IsSuccess);
        Assert.Equal(latitude, result.Value.Latitude);
    }

    [Theory]
    [InlineData(-180.0)] // International Date Line (West)
    [InlineData(180.0)]  // International Date Line (East)
    [InlineData(0.0)]    // Prime Meridian
    public void Create_WithValidLongitudeBoundaries_ShouldReturnSuccess(double lng)
    {
        var latitude = 0m;
        var longitude = (decimal)lng;

        var result = GeoCoordinate.Create(latitude, longitude);

        Assert.True(result.IsSuccess);
        Assert.Equal(longitude, result.Value.Longitude);
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(-91)]
    [InlineData(-100)]
    public void Create_WithLatitudeBelowMinimum_ShouldReturnFailure(double lat)
    {
        var latitude = (decimal)lat;
        var longitude = 0m;

        var result = GeoCoordinate.Create(latitude, longitude);

        Assert.True(result.IsFailure);
        Assert.Contains("Latitude must be between -90 and 90 degrees", result.Errors);
    }

    [Theory]
    [InlineData(90.1)]
    [InlineData(91)]
    [InlineData(100)]
    public void Create_WithLatitudeAboveMaximum_ShouldReturnFailure(double lat)
    {
        var latitude = (decimal)lat;
        var longitude = 0m;

        var result = GeoCoordinate.Create(latitude, longitude);

        Assert.True(result.IsFailure);
        Assert.Contains("Latitude must be between -90 and 90 degrees", result.Errors);
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(-181)]
    [InlineData(-200)]
    public void Create_WithLongitudeBelowMinimum_ShouldReturnFailure(double lng)
    {
        var latitude = 0m;
        var longitude = (decimal)lng;

        var result = GeoCoordinate.Create(latitude, longitude);

        Assert.True(result.IsFailure);
        Assert.Contains("Longitude must be between -180 and 180 degrees", result.Errors);
    }

    [Theory]
    [InlineData(180.1)]
    [InlineData(181)]
    [InlineData(200)]
    public void Create_WithLongitudeAboveMaximum_ShouldReturnFailure(double lng)
    {
        var latitude = 0m;
        var longitude = (decimal)lng;

        var result = GeoCoordinate.Create(latitude, longitude);

        Assert.True(result.IsFailure);
        Assert.Contains("Longitude must be between -180 and 180 degrees", result.Errors);
    }

    [Fact]
    public void Create_WithHighPrecisionCoordinates_ShouldReturnSuccess()
    {
        var latitude = 6.927079m;
        var longitude = 79.861244m;

        var result = GeoCoordinate.Create(latitude, longitude);

        Assert.True(result.IsSuccess);
        Assert.Equal(latitude, result.Value.Latitude);
        Assert.Equal(longitude, result.Value.Longitude);
    }

    [Fact]
    public void DistanceTo_WithSameCoordinates_ShouldReturnZero()
    {
        var coordinate1 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
        var coordinate2 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        var distance = coordinate1.DistanceTo(coordinate2);

        Assert.Equal(0.0, distance, 0.001); // Allow small floating point differences
    }

    [Fact]
    public void DistanceTo_WithKnownCoordinates_ShouldCalculateCorrectDistance()
    {
        // US cities relevant to Sri Lankan American community
        var newYork = GeoCoordinate.Create(40.7128m, -74.0060m).Value;  // New York City
        var philadelphia = GeoCoordinate.Create(39.9526m, -75.1652m).Value;  // Philadelphia

        var distance = newYork.DistanceTo(philadelphia);

        // Approximate distance between NYC and Philadelphia is about 95 miles (130 km)
        Assert.True(distance > 125 && distance < 135, $"Expected distance between 125-135 km, got {distance}");
    }

    [Fact]
    public void DistanceTo_WithOppositeEndsOfEarth_ShouldCalculateMaxDistance()
    {
        var northPole = GeoCoordinate.Create(90m, 0m).Value;
        var southPole = GeoCoordinate.Create(-90m, 0m).Value;

        var distance = northPole.DistanceTo(southPole);

        // Distance from North Pole to South Pole should be approximately half the Earth's circumference
        // Earth's circumference is approximately 40,075 km, so half is about 20,037 km
        Assert.True(distance > 19900 && distance < 20100, $"Expected distance around 20,000 km, got {distance}");
    }

    [Fact]
    public void DistanceTo_WithEastWestCoordinates_ShouldCalculateCorrectDistance()
    {
        var london = GeoCoordinate.Create(51.5074m, -0.1278m).Value;
        var newYork = GeoCoordinate.Create(40.7128m, -74.0060m).Value;

        var distance = london.DistanceTo(newYork);

        // Distance between London and New York is approximately 5,585 km
        Assert.True(distance > 5500 && distance < 5650, $"Expected distance between 5500-5650 km, got {distance}");
    }

    [Fact]
    public void DistanceTo_WithNorthSouthCoordinates_ShouldCalculateCorrectDistance()
    {
        var equator = GeoCoordinate.Create(0m, 0m).Value;
        var tropicOfCancer = GeoCoordinate.Create(23.5m, 0m).Value;

        var distance = equator.DistanceTo(tropicOfCancer);

        // Distance from equator to Tropic of Cancer should be approximately 2,609 km
        Assert.True(distance > 2500 && distance < 2700, $"Expected distance between 2500-2700 km, got {distance}");
    }

    [Fact]
    public void Equality_WithSameCoordinates_ShouldBeEqual()
    {
        var coordinate1 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
        var coordinate2 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        Assert.Equal(coordinate1, coordinate2);
    }

    [Fact]
    public void Equality_WithDifferentLatitudes_ShouldNotBeEqual()
    {
        var coordinate1 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
        var coordinate2 = GeoCoordinate.Create(7.0000m, 79.8612m).Value;

        Assert.NotEqual(coordinate1, coordinate2);
    }

    [Fact]
    public void Equality_WithDifferentLongitudes_ShouldNotBeEqual()
    {
        var coordinate1 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
        var coordinate2 = GeoCoordinate.Create(6.9271m, 80.0000m).Value;

        Assert.NotEqual(coordinate1, coordinate2);
    }

    [Fact]
    public void GetHashCode_WithSameCoordinates_ShouldBeEqual()
    {
        var coordinate1 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
        var coordinate2 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        Assert.Equal(coordinate1.GetHashCode(), coordinate2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var coordinate = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        var result = coordinate.ToString();

        Assert.Equal("6.9271, 79.8612", result);
    }

    [Theory]
    [InlineData(0, 0, "0, 0")]
    [InlineData(-90, -180, "-90, -180")]
    [InlineData(90, 180, "90, 180")]
    [InlineData(6.927079, 79.861244, "6.927079, 79.861244")]
    public void ToString_WithVariousCoordinates_ShouldFormatCorrectly(double lat, double lng, string expected)
    {
        var coordinate = GeoCoordinate.Create((decimal)lat, (decimal)lng).Value;

        var result = coordinate.ToString();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void DistanceTo_ShouldBeSymmetric()
    {
        var coordinate1 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
        var coordinate2 = GeoCoordinate.Create(7.2906m, 80.6337m).Value;

        var distance1to2 = coordinate1.DistanceTo(coordinate2);
        var distance2to1 = coordinate2.DistanceTo(coordinate1);

        Assert.Equal(distance1to2, distance2to1, 0.001); // Allow small floating point differences
    }

    [Fact]
    public void Create_WithVerySmallCoordinates_ShouldReturnSuccess()
    {
        var latitude = 0.000001m;
        var longitude = 0.000001m;

        var result = GeoCoordinate.Create(latitude, longitude);

        Assert.True(result.IsSuccess);
        Assert.Equal(latitude, result.Value.Latitude);
        Assert.Equal(longitude, result.Value.Longitude);
    }

    [Fact]
    public void Create_WithNegativeCoordinates_ShouldReturnSuccess()
    {
        var latitude = -33.8688m; // Sydney, Australia (Southern Hemisphere)
        var longitude = -74.0060m; // New York, USA (Western Hemisphere)

        var result = GeoCoordinate.Create(latitude, longitude);

        Assert.True(result.IsSuccess);
        Assert.Equal(latitude, result.Value.Latitude);
        Assert.Equal(longitude, result.Value.Longitude);
    }

    [Fact]
    public void DistanceTo_WithVeryCloseCoordinates_ShouldCalculateSmallDistance()
    {
        var coordinate1 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
        var coordinate2 = GeoCoordinate.Create(6.9272m, 79.8613m).Value; // Very close coordinates

        var distance = coordinate1.DistanceTo(coordinate2);

        // Distance should be very small (less than 1 km)
        Assert.True(distance < 1.0, $"Expected distance < 1 km, got {distance} km");
        Assert.True(distance > 0.0, "Expected distance > 0 km");
    }

    [Fact]
    public void Create_WithExtremeValidValues_ShouldReturnSuccess()
    {
        var extremeCoordinates = new[]
        {
            (90m, 180m),   // Northeast extreme
            (90m, -180m),  // Northwest extreme
            (-90m, 180m),  // Southeast extreme
            (-90m, -180m)  // Southwest extreme
        };

        foreach (var (lat, lng) in extremeCoordinates)
        {
            var result = GeoCoordinate.Create(lat, lng);
            Assert.True(result.IsSuccess, $"Failed to create coordinate ({lat}, {lng})");
        }
    }
}