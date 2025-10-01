using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Common.Abstractions;

/// <summary>
/// Defines the interface for failover orchestration with cultural intelligence
/// </summary>
public interface IFailoverOrchestrator
{
    /// <summary>
    /// Initiates failover from primary to secondary region
    /// </summary>
    /// <param name="primaryLocation">Primary geographic location</param>
    /// <param name="secondaryLocation">Secondary geographic location</param>
    /// <param name="reason">Reason for failover</param>
    Task<bool> InitiateFailoverAsync(GeographicLocation primaryLocation, GeographicLocation secondaryLocation, string reason);

    /// <summary>
    /// Monitors failover progress
    /// </summary>
    Task<FailoverStatus> GetFailoverStatusAsync();

    /// <summary>
    /// Performs rollback to original configuration
    /// </summary>
    Task<bool> RollbackFailoverAsync();
}

/// <summary>
/// Failover status enumeration
/// </summary>
public enum FailoverStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    RolledBack
}

/// <summary>
/// Geographic location for failover operations
/// </summary>
public record GeographicLocation(string RegionCode, string Country, string City, double Latitude, double Longitude)
{
    public static GeographicLocation Create(string regionCode, string country, string city, double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(regionCode))
            throw new ArgumentException("Region code cannot be empty");
        
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty");

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty");

        return new GeographicLocation(regionCode, country, city, latitude, longitude);
    }
}