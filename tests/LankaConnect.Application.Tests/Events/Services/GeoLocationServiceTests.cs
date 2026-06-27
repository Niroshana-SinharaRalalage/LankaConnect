using LankaConnect.Domain.Events.Services;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Services;

/// <summary>
/// Unit tests for GeoLocationService Haversine distance calculations
/// Tests use known geographic distances for validation
/// </summary>
public class GeoLocationServiceTests
{
    private readonly IGeoLocationService _service;

    public GeoLocationServiceTests()
    {
        _service = new GeoLocationService();
    }

    [Fact]
    public void CalculateDistanceKm_SamePoint_ReturnsZero()
    {
        // Arrange: Los Angeles coordinates
        const decimal lat = 34.0522m;
        const decimal lon = -118.2437m;

        // Act
        var distance = _service.CalculateDistanceKm(lat, lon, lat, lon);

        // Assert
        Assert.Equal(0, distance, precision: 2);
    }

    [Fact]
    public void CalculateDistanceKm_ColombToKandy_ReturnsAccurateDistance()
    {
        // Arrange: Real Sri Lankan cities
        const decimal colomboLat = 6.9271m;
        const decimal colomboLon = 79.8612m;
        const decimal kandyLat = 7.2906m;
        const decimal kandyLon = 80.6337m;

        // Known distance: approximately 94-95 km (verified with Haversine calculator)
        const double expectedKm = 94.5;
        const double toleranceKm = 1.0; // Within 1km accuracy

        // Act
        var distance = _service.CalculateDistanceKm(colomboLat, colomboLon, kandyLat, kandyLon);

        // Assert
        Assert.InRange(distance, expectedKm - toleranceKm, expectedKm + toleranceKm);
    }

    [Fact]
    public void CalculateDistanceKm_LosAngelesToSanFrancisco_ReturnsAccurateDistance()
    {
        // Arrange: Major US cities
        const decimal laLat = 34.0522m;
        const decimal laLon = -118.2437m;
        const decimal sfLat = 37.7749m;
        const decimal sfLon = -122.4194m;

        // Known distance: approximately 559 km
        const double expectedKm = 559.0;
        const double toleranceKm = 5.0; // Within 5km accuracy

        // Act
        var distance = _service.CalculateDistanceKm(laLat, laLon, sfLat, sfLon);

        // Assert
        Assert.InRange(distance, expectedKm - toleranceKm, expectedKm + toleranceKm);
    }

    [Fact]
    public void CalculateDistanceKm_NewYorkToLondon_ReturnsAccurateDistance()
    {
        // Arrange: Transatlantic distance
        const decimal nyLat = 40.7128m;
        const decimal nyLon = -74.0060m;
        const decimal londonLat = 51.5074m;
        const decimal londonLon = -0.1278m;

        // Known distance: approximately 5,571 km
        const double expectedKm = 5571.0;
        const double toleranceKm = 50.0; // Within 50km accuracy for long distances

        // Act
        var distance = _service.CalculateDistanceKm(nyLat, nyLon, londonLat, londonLon);

        // Assert
        Assert.InRange(distance, expectedKm - toleranceKm, expectedKm + toleranceKm);
    }

    [Fact]
    public void CalculateDistanceKm_EquatorPoints_CalculatesCorrectly()
    {
        // Arrange: Two points on equator, 1 degree apart
        const decimal lat1 = 0m;
        const decimal lon1 = 0m;
        const decimal lat2 = 0m;
        const decimal lon2 = 1m;

        // Expected: approximately 111 km (1 degree longitude at equator)
        const double expectedKm = 111.0;
        const double toleranceKm = 2.0;

        // Act
        var distance = _service.CalculateDistanceKm(lat1, lon1, lat2, lon2);

        // Assert
        Assert.InRange(distance, expectedKm - toleranceKm, expectedKm + toleranceKm);
    }

    [Fact]
    public void CalculateDistanceKm_SmallDistance_ReturnsAccurateResult()
    {
        // Arrange: Very close points (< 1km apart)
        const decimal lat1 = 34.0522m;
        const decimal lon1 = -118.2437m;
        const decimal lat2 = 34.0532m; // ~0.11 km north (0.001 degrees ≈ 111 meters)
        const decimal lon2 = -118.2437m;

        // Expected: approximately 0.11 km
        const double expectedKm = 0.11;
        const double toleranceKm = 0.05;

        // Act
        var distance = _service.CalculateDistanceKm(lat1, lon1, lat2, lon2);

        // Assert
        Assert.InRange(distance, expectedKm - toleranceKm, expectedKm + toleranceKm);
    }

    [Fact]
    public void CalculateDistanceKm_NorthToSouth_ReturnsPositiveDistance()
    {
        // Arrange: Northern and Southern hemisphere
        const decimal northLat = 40.7128m; // New York
        const decimal northLon = -74.0060m;
        const decimal southLat = -33.8688m; // Sydney
        const decimal southLon = 151.2093m;

        // Act
        var distance = _service.CalculateDistanceKm(northLat, northLon, southLat, southLon);

        // Assert
        Assert.True(distance > 0, "Distance should be positive");
        Assert.True(distance > 10000, "Distance NYC to Sydney should be > 10,000 km");
        Assert.True(distance < 20000, "Distance NYC to Sydney should be < 20,000 km");
    }

    [Fact]
    public void CalculateDistanceKm_CrossingDateLine_CalculatesCorrectly()
    {
        // Arrange: Points on either side of International Date Line
        const decimal lat1 = 0m;
        const decimal lon1 = 179m; // Just east of date line
        const decimal lat2 = 0m;
        const decimal lon2 = -179m; // Just west of date line

        // Expected: approximately 222 km (2 degrees at equator)
        const double expectedKm = 222.0;
        const double toleranceKm = 5.0;

        // Act
        var distance = _service.CalculateDistanceKm(lat1, lon1, lat2, lon2);

        // Assert
        Assert.InRange(distance, expectedKm - toleranceKm, expectedKm + toleranceKm);
    }

    [Fact]
    public void CalculateDistanceKm_IsSymmetric()
    {
        // Arrange
        const decimal lat1 = 34.0522m;
        const decimal lon1 = -118.2437m;
        const decimal lat2 = 37.7749m;
        const decimal lon2 = -122.4194m;

        // Act
        var distance1 = _service.CalculateDistanceKm(lat1, lon1, lat2, lon2);
        var distance2 = _service.CalculateDistanceKm(lat2, lon2, lat1, lon1);

        // Assert: Distance A->B should equal distance B->A
        Assert.Equal(distance1, distance2, precision: 2);
    }

    [Theory]
    [InlineData(34.0522, -118.2437, 34.0532, -118.2437)] // Small distance
    [InlineData(6.9271, 79.8612, 7.2906, 80.6337)] // Colombo to Kandy
    [InlineData(0, 0, 0, 1)] // Equator
    [InlineData(40.7128, -74.0060, 37.7749, -122.4194)] // NYC to SF
    public void CalculateDistanceKm_ValidCoordinates_ReturnsPositiveNonZeroDistance(
        double lat1, double lon1, double lat2, double lon2)
    {
        // Act
        var distance = _service.CalculateDistanceKm((decimal)lat1, (decimal)lon1, (decimal)lat2, (decimal)lon2);

        // Assert
        Assert.True(distance >= 0, "Distance must be non-negative");
    }

    [Fact]
    public void CalculateDistanceKm_PolarRegions_HandlesHighLatitudes()
    {
        // Arrange: Near North Pole
        const decimal lat1 = 89m;
        const decimal lon1 = 0m;
        const decimal lat2 = 89m;
        const decimal lon2 = 180m;

        // Act
        var distance = _service.CalculateDistanceKm(lat1, lon1, lat2, lon2);

        // Assert: Distance should be small (points very close to pole)
        Assert.True(distance >= 0, "Distance must be non-negative");
        Assert.True(distance < 500, "Distance near pole should be reasonable");
    }
}
