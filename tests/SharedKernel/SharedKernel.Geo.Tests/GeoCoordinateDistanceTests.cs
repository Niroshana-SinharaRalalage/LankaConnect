namespace LankaConnect.SharedKernel.Geo.Tests;

/// <summary>
/// Unit tests for <see cref="GeoCoordinate.DistanceKmTo"/> — Haversine formula.
/// Wave 8.5 GAP-6 (2026-07-19). Reference distances derived from published
/// great-circle calculators (e.g. movable-type.co.uk/scripts/latlong.html) so
/// the assertions are anchored to third-party ground truth, not to the impl.
/// </summary>
public sealed class GeoCoordinateDistanceTests
{
    // Colombo, Sri Lanka (rough BMICH latitude/longitude).
    private const double ColomboLat = 6.9271;
    private const double ColomboLon = 79.8612;

    // Kandy, Sri Lanka (rough Temple of the Tooth).
    private const double KandyLat = 7.2906;
    private const double KandyLon = 80.6337;

    // Toronto, Canada (CN Tower).
    private const double TorontoLat = 43.6426;
    private const double TorontoLon = -79.3871;

    // London, UK (Big Ben).
    private const double LondonLat = 51.5007;
    private const double LondonLon = -0.1246;

    [Fact]
    public void DistanceKmTo_SamePoint_ReturnsZero()
    {
        var a = new GeoCoordinate(ColomboLat, ColomboLon);
        var b = new GeoCoordinate(ColomboLat, ColomboLon);

        var distance = a.DistanceKmTo(b);

        distance.Should().BeApproximately(0.0, 0.001);
    }

    [Fact]
    public void DistanceKmTo_ColomboToKandy_ReturnsApproximately95Km()
    {
        // Ground truth per movable-type.co.uk Haversine calc: ~94 km.
        // Tolerance = 2 km (spherical-Earth vs WGS-84 ellipsoid drift).
        var colombo = new GeoCoordinate(ColomboLat, ColomboLon);
        var kandy = new GeoCoordinate(KandyLat, KandyLon);

        var distance = colombo.DistanceKmTo(kandy);

        distance.Should().BeApproximately(94, 2);
    }

    [Fact]
    public void DistanceKmTo_TorontoToLondon_ReturnsApproximately5700Km()
    {
        // Ground truth: 5711 km. Tolerance 30 km at intercontinental scale.
        var toronto = new GeoCoordinate(TorontoLat, TorontoLon);
        var london = new GeoCoordinate(LondonLat, LondonLon);

        var distance = toronto.DistanceKmTo(london);

        distance.Should().BeApproximately(5711, 30);
    }

    [Fact]
    public void DistanceKmTo_IsSymmetric()
    {
        // Haversine is symmetric — a→b == b→a. Guard against accidental
        // formula asymmetry from a future edit.
        var colombo = new GeoCoordinate(ColomboLat, ColomboLon);
        var toronto = new GeoCoordinate(TorontoLat, TorontoLon);

        var ab = colombo.DistanceKmTo(toronto);
        var ba = toronto.DistanceKmTo(colombo);

        ab.Should().BeApproximately(ba, 0.001);
    }

    [Fact]
    public void DistanceKmTo_NullOther_Throws()
    {
        var colombo = new GeoCoordinate(ColomboLat, ColomboLon);

        var act = () => colombo.DistanceKmTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DistanceKmTo_AntipodalPoints_ReturnsApproximatelyHalfEarthCircumference()
    {
        // Half circumference ≈ π · R = π · 6371 ≈ 20015 km.
        // Antipode of (0, 0) is (0, 180).
        var a = new GeoCoordinate(0, 0);
        var b = new GeoCoordinate(0, 180);

        var distance = a.DistanceKmTo(b);

        distance.Should().BeApproximately(20015, 5);
    }

    [Fact]
    public void DistanceKmTo_CrossEquator_UsesSphericalGeometry()
    {
        // Simple sanity check: 1 degree of latitude ≈ 111 km at any longitude.
        var north = new GeoCoordinate(1, 0);
        var south = new GeoCoordinate(-1, 0);

        var distance = north.DistanceKmTo(south);

        // 2 degrees ≈ 222 km.
        distance.Should().BeApproximately(222, 1);
    }
}
