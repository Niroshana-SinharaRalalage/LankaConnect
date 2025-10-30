using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects.Recommendations;

namespace LankaConnect.Infrastructure.CulturalIntelligence;

/// <summary>
/// Stub implementation of IGeographicProximityService for MVP
/// TODO: Replace with real geographic clustering algorithms in Phase 2
/// </summary>
public class StubGeographicProximityService : IGeographicProximityService
{
    public bool IsDiasporaLocation(string location) => true; // Stub: Assume all locations are diaspora

    public double GetCommunityDensity(string location) => 0.5; // Stub: 50% default density

    public CommunityCluster[] AnalyzeCommunityCluster(string userLocation, IEnumerable<Event> events)
        => Array.Empty<CommunityCluster>(); // Stub: No clusters for MVP

    public Distance CalculateDistance(Coordinates from, Coordinates to)
        => new Distance(10, DistanceUnit.Miles); // Stub: Default 10 miles

    public RegionalPreferences GetRegionalPreferences(string location)
        => new RegionalPreferences("Default Region", Array.Empty<string>(), Array.Empty<string>(), 0);

    public RegionalMatchScore CalculateRegionalMatch(Event @event, RegionalPreferences preferences)
        => new RegionalMatchScore(0.7, "Stub regional match");

    public AccessibilityScore CalculateTransportationAccessibility(Event @event, TransportationPreferences preferences)
        => new AccessibilityScore(0.7);

    public MultiLocationProximity CalculateMultiLocationProximity(string userLocation, string[] eventLocations)
        => new MultiLocationProximity(10.0, 15.0, 20.0, eventLocations.Length);

    public ProximityScore CalculateProximityScore(MultiLocationProximity proximity)
        => new ProximityScore(0.7);

    public ProximityScore CalculateProximityScore(Event @event, string userLocation)
        => new ProximityScore(0.7);

    public LocationHandlingResult HandleLocationEdgeCase(string userLocation, Event @event)
        => new LocationHandlingResult(true, "Default handling", "No action needed", 0.7);

    public bool IsBorderLocation(string location) => false; // Stub: No border locations for MVP
}
