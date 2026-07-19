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
}
