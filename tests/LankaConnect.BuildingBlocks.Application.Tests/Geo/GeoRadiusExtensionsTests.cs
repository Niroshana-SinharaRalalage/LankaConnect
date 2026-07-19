using LankaConnect.BuildingBlocks.Application.Geo;
using LankaConnect.SharedKernel.Geo;

namespace LankaConnect.BuildingBlocks.Application.Tests.Geo;

/// <summary>
/// Unit tests for <see cref="GeoRadiusExtensions.WithinRadiusKm"/> +
/// <see cref="GeoRadiusExtensions.WithinRadiusKmWithDistance"/>. Wave 8.5 GAP-6
/// (2026-07-19).
/// </summary>
public sealed class GeoRadiusExtensionsTests
{
    // Anchor point: Colombo (BMICH area) — used as the query center everywhere.
    private static readonly GeoCoordinate Colombo = new(6.9271, 79.8612);

    // Sample locations at known-ish distances from Colombo:
    private static readonly GeoCoordinate Kandy = new(7.2906, 80.6337);       // ~94 km
    private static readonly GeoCoordinate Galle = new(6.0329, 80.2168);       // ~120 km
    private static readonly GeoCoordinate Jaffna = new(9.6615, 80.0255);      // ~305 km
    private static readonly GeoCoordinate Toronto = new(43.6426, -79.3871);   // ~13500 km

    private sealed record Venue(string Name, GeoCoordinate? Location);

    private static readonly Venue[] Sample =
    {
        new("BMICH", Colombo),
        new("Kandy Temple", Kandy),
        new("Galle Fort", Galle),
        new("Jaffna Library", Jaffna),
        new("CN Tower", Toronto),
        new("Missing Location Venue", null),
    };

    [Fact]
    public void WithinRadiusKm_100Km_ReturnsColomboAndKandy()
    {
        var results = Sample.WithinRadiusKm(Colombo, 100, v => v.Location).ToList();

        results.Select(v => v.Name).Should().BeEquivalentTo(new[] { "BMICH", "Kandy Temple" });
    }

    [Fact]
    public void WithinRadiusKm_150Km_AlsoIncludesGalle()
    {
        var results = Sample.WithinRadiusKm(Colombo, 150, v => v.Location).ToList();

        results.Select(v => v.Name).Should().BeEquivalentTo(new[] { "BMICH", "Kandy Temple", "Galle Fort" });
    }

    [Fact]
    public void WithinRadiusKm_ZeroRadius_ReturnsOnlyExactMatch()
    {
        var results = Sample.WithinRadiusKm(Colombo, 0, v => v.Location).ToList();

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("BMICH");
    }

    [Fact]
    public void WithinRadiusKm_ExcludesItemsWithNullLocation()
    {
        // "Missing Location Venue" has Location = null; the extension must not
        // NullReferenceException on it, must simply skip it.
        var results = Sample.WithinRadiusKm(Colombo, 20000, v => v.Location).ToList();

        results.Select(v => v.Name).Should().NotContain("Missing Location Venue");
        results.Should().HaveCount(5); // 6 total - 1 null-location
    }

    [Fact]
    public void WithinRadiusKm_EmptySource_ReturnsEmpty()
    {
        var results = Array.Empty<Venue>().WithinRadiusKm(Colombo, 100, v => v.Location).ToList();

        results.Should().BeEmpty();
    }

    [Fact]
    public void WithinRadiusKm_NullSource_Throws()
    {
        IEnumerable<Venue> src = null!;

        var act = () => src.WithinRadiusKm(Colombo, 100, v => v.Location).ToList();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithinRadiusKm_NullCenter_Throws()
    {
        var act = () => Sample.WithinRadiusKm(null!, 100, v => v.Location).ToList();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithinRadiusKm_NullSelector_Throws()
    {
        var act = () => Sample.WithinRadiusKm(Colombo, 100, (Func<Venue, GeoCoordinate?>)null!).ToList();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithinRadiusKm_NegativeRadius_Throws()
    {
        var act = () => Sample.WithinRadiusKm(Colombo, -1, v => v.Location).ToList();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithinRadiusKmWithDistance_PairsEachItemWithComputedDistance()
    {
        var results = Sample.WithinRadiusKmWithDistance(Colombo, 200, v => v.Location).ToList();

        results.Should().HaveCount(3); // Colombo (0), Kandy (~94), Galle (~120)
        results.Should().Contain(x => x.Item.Name == "BMICH" && x.DistanceKm < 0.1);
        results.Should().Contain(x => x.Item.Name == "Kandy Temple" && x.DistanceKm > 90 && x.DistanceKm < 100);
        results.Should().Contain(x => x.Item.Name == "Galle Fort" && x.DistanceKm > 100 && x.DistanceKm < 115);
    }

    [Fact]
    public void WithinRadiusKmWithDistance_CanBeSortedForNearestFirst()
    {
        // Common consumer pattern: order results by distance ascending.
        var sorted = Sample
            .WithinRadiusKmWithDistance(Colombo, 20000, v => v.Location)
            .OrderBy(x => x.DistanceKm)
            .Select(x => x.Item.Name)
            .ToList();

        sorted.Should().Equal("BMICH", "Kandy Temple", "Galle Fort", "Jaffna Library", "CN Tower");
    }
}
