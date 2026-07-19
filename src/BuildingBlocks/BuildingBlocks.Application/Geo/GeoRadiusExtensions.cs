using LankaConnect.SharedKernel.Geo;

namespace LankaConnect.BuildingBlocks.Application.Geo;

/// <summary>
/// LINQ extensions for filtering an in-memory sequence by geographic radius.
/// Phase A adequacy primitive — pairs with <see cref="GeoCoordinate.DistanceKmTo"/>
/// (Haversine, spherical Earth) to answer "give me the items within N km of point P".
/// </summary>
/// <remarks>
/// <para>
/// Authored 2026-07-19 as part of Wave 8.5 GAP-6 (Geo capability cluster) per
/// <c>docs/architecture/COMMON_COMPONENTS_INVENTORY_2026_07_16.md</c>. Consumed by
/// Phase B products (LankaHomes, LankaMart, LankaBusiness, LankaSeyla) for their
/// "listings near me" browse UX.
/// </para>
/// <para>
/// <b>Scale ceiling.</b> This is intentionally an in-memory filter — the caller
/// pre-loads candidate rows from persistence (typically via a bounding-box
/// prefilter: <c>lat BETWEEN a AND b AND lon BETWEEN c AND d</c>) and then hands
/// the projection to <see cref="WithinRadiusKm"/>. Good up to a few thousand rows
/// per query at Phase-A LankaEvents scale. If a Product starts pushing tens of
/// thousands per query, upgrade to a PostGIS <c>ST_DWithin</c>-backed implementation
/// (deferred to Phase B if scale demands per COMMON_COMPONENTS_INVENTORY GAP-6).
/// </para>
/// <para>
/// <b>Design choice — extension over interface.</b> Contract briefs floated an
/// <c>IGeoRadiusQuery&lt;T&gt;</c> interface. Deferred: no consumer needs polymorphic
/// dispatch today, and the extension surface is trivially wrappable behind an
/// interface later if a future Product needs to swap the impl.
/// </para>
/// </remarks>
public static class GeoRadiusExtensions
{
    /// <summary>
    /// Filters <paramref name="source"/> to items whose selected location is within
    /// <paramref name="radiusKm"/> kilometers of <paramref name="center"/>. Items
    /// whose <paramref name="locationSelector"/> returns null are excluded.
    /// </summary>
    /// <typeparam name="T">The item type. No shape constraint — location is projected via <paramref name="locationSelector"/>.</typeparam>
    /// <param name="source">The candidate sequence (pre-loaded by the caller).</param>
    /// <param name="center">Center of the query radius.</param>
    /// <param name="radiusKm">Inclusive radius in kilometers. Must be non-negative.</param>
    /// <param name="locationSelector">Projects an item to its <see cref="GeoCoordinate"/>. Return null for items without a known location.</param>
    /// <returns>The filtered subsequence. Deferred execution (LINQ standard).</returns>
    /// <exception cref="ArgumentNullException">Any of <paramref name="source"/>, <paramref name="center"/>, or <paramref name="locationSelector"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radiusKm"/> is negative.</exception>
    public static IEnumerable<T> WithinRadiusKm<T>(
        this IEnumerable<T> source,
        GeoCoordinate center,
        double radiusKm,
        Func<T, GeoCoordinate?> locationSelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(center);
        ArgumentNullException.ThrowIfNull(locationSelector);
        if (radiusKm < 0)
            throw new ArgumentOutOfRangeException(nameof(radiusKm), radiusKm, "Radius must be non-negative.");

        return source.Where(item =>
        {
            var loc = locationSelector(item);
            return loc is not null && center.DistanceKmTo(loc) <= radiusKm;
        });
    }

    /// <summary>
    /// Projection variant — returns each qualifying item paired with its computed
    /// distance to <paramref name="center"/>. Useful when the caller wants to sort
    /// results by nearness ("nearest first").
    /// </summary>
    /// <returns>Deferred sequence of <c>(Item, DistanceKm)</c> tuples for items within radius.</returns>
    /// <exception cref="ArgumentNullException">Any of <paramref name="source"/>, <paramref name="center"/>, or <paramref name="locationSelector"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radiusKm"/> is negative.</exception>
    public static IEnumerable<(T Item, double DistanceKm)> WithinRadiusKmWithDistance<T>(
        this IEnumerable<T> source,
        GeoCoordinate center,
        double radiusKm,
        Func<T, GeoCoordinate?> locationSelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(center);
        ArgumentNullException.ThrowIfNull(locationSelector);
        if (radiusKm < 0)
            throw new ArgumentOutOfRangeException(nameof(radiusKm), radiusKm, "Radius must be non-negative.");

        foreach (var item in source)
        {
            var loc = locationSelector(item);
            if (loc is null) continue;
            var dist = center.DistanceKmTo(loc);
            if (dist <= radiusKm) yield return (item, dist);
        }
    }
}
