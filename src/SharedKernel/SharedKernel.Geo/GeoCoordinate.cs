using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.SharedKernel.Geo;

/// <summary>
/// Latitude/longitude pair value object. Pan-platform primitive per ADR-006 —
/// consumed by LankaEvents (event venue GPS), LankaHomes (property location),
/// LankaBusiness (business geolocation), LankaNivasa (accommodation location),
/// and Identity (user preferred location).
/// </summary>
/// <remarks>
/// <para>
/// Promoted from <c>LankaConnect.Products.LankaEvents.Domain.ValueObjects</c> in
/// Wave 8.5-cleanup (2026-07-18) per ExtractabilityAudit GAP-6. Behavior-preserving
/// move: same double-precision fields, same equality components, same ToString.
/// </para>
/// <para>
/// Migration snapshot strings still spell the historical owning namespace
/// (<c>LankaConnect.Domain.Business.ValueObjects.GeoCoordinate</c>). Those are
/// EF-serialized model shape references, not compile-time symbols; they resolve
/// via runtime name matching and do not block the physical namespace move.
/// </para>
/// </remarks>
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

    public static Result<GeoCoordinate> Create(double latitude, double longitude)
        => Result<GeoCoordinate>.Success(new GeoCoordinate(latitude, longitude));

    public static Result<GeoCoordinate> Create(decimal latitude, decimal longitude)
        => Result<GeoCoordinate>.Success(new GeoCoordinate((double)latitude, (double)longitude));

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }

    public override string ToString() => $"{Latitude:F6},{Longitude:F6}";

    /// <summary>
    /// Great-circle distance in kilometers to <paramref name="other"/> via the
    /// Haversine formula. Assumes a spherical Earth with radius 6371 km — accurate
    /// to ~0.5% at continental scale (matches Google-Maps-grade for diaspora radius
    /// queries: "temples within 25 km", "properties within 10 mi", etc.).
    /// </summary>
    /// <remarks>
    /// Wave 8.5 GAP-6 (2026-07-19) — Phase A adequacy primitive. In-memory only.
    /// A PostGIS <c>ST_Distance</c>-backed impl is deferred to Phase B if a Product
    /// (LankaHomes, LankaMart, LankaBusiness, LankaSeyla) starts pushing >10k rows
    /// per query where in-process filtering stops being cheap. Until then, expect
    /// Products to load candidate rows via a bounding-box SQL prefilter, then call
    /// <c>WithinRadiusKm</c> to refine.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null.</exception>
    public double DistanceKmTo(GeoCoordinate other)
    {
        ArgumentNullException.ThrowIfNull(other);

        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians(other.Latitude - Latitude);
        var dLon = ToRadians(other.Longitude - Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians(Latitude)) * Math.Cos(ToRadians(other.Latitude))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
