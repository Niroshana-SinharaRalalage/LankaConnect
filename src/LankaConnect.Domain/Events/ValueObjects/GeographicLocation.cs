using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Events.ValueObjects;

public record GeographicLocation
{
    public string City { get; init; }
    public string StateProvince { get; init; }
    public string Country { get; init; }
    public GeographicRegion Region { get; init; }
    public TimeZoneInfo TimeZone { get; init; }

    public GeographicLocation(string city, string stateProvince, string country, 
        GeographicRegion? region = null, TimeZoneInfo? timeZone = null)
    {
        City = city ?? string.Empty;
        StateProvince = stateProvince ?? string.Empty;
        Country = country ?? string.Empty;
        Region = region ?? DetermineRegion(Country, StateProvince);
        TimeZone = timeZone ?? TimeZoneInfo.Local;
    }

    private static GeographicRegion DetermineRegion(string country, string stateProvince)
    {
        return country.ToUpperInvariant() switch
        {
            "SRI LANKA" => GeographicRegion.SriLanka,
            "USA" or "UNITED STATES" => stateProvince.ToUpperInvariant() == "CALIFORNIA" ? 
                GeographicRegion.BayArea : GeographicRegion.NorthAmerica,
            "CANADA" => GeographicRegion.NorthAmerica,
            "AUSTRALIA" => GeographicRegion.Australia,
            _ => GeographicRegion.Other
        };
    }

    public bool IsDiasporaLocation => Region != GeographicRegion.SriLanka;
}