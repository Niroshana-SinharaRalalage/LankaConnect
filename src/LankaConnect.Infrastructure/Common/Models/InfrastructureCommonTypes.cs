using LankaConnect.Domain.Common;

namespace LankaConnect.Infrastructure.Common.Models;

/// <summary>
/// Infrastructure-specific validation result (single source of truth)
/// TDD GREEN Phase: Consolidation of ValidationResult across Infrastructure layer
/// </summary>
public class InfrastructureValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime ValidationTimestamp { get; set; }
    public string ValidatorName { get; set; } = string.Empty;
    public ValidationSeverity Severity { get; set; }
}

/// <summary>
/// Infrastructure-specific date range (single source of truth)
/// </summary>
public class InfrastructureDateRange
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan Duration => EndDate - StartDate;
    public bool IsValid => EndDate >= StartDate;
    public bool Contains(DateTime date) => date >= StartDate && date <= EndDate;
    public bool Overlaps(InfrastructureDateRange other) => StartDate <= other.EndDate && EndDate >= other.StartDate;
}

/// <summary>
/// Cultural event type enumeration for Infrastructure layer
/// </summary>
public enum InfrastructureCulturalEventType
{
    Religious = 0,
    Cultural = 1,
    Community = 2,
    Educational = 3,
    Business = 4,
    Entertainment = 5,
    Social = 6,
    Charity = 7
}

/// <summary>
/// System health status for Infrastructure monitoring
/// </summary>
public enum InfrastructureSystemHealthStatus
{
    Optimal = 0,
    Good = 1,
    Warning = 2,
    Critical = 3,
    Emergency = 4,
    Maintenance = 5
}

/// <summary>
/// Validation severity levels
/// </summary>
public enum ValidationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

/// <summary>
/// Cultural event context for Infrastructure operations
/// </summary>
public class InfrastructureCulturalEventContext
{
    public string EventId { get; set; } = string.Empty;
    public InfrastructureCulturalEventType EventType { get; set; }
    public string Region { get; set; } = string.Empty;
    public string CulturalGroup { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public DateTime EventEndTime { get; set; }
    public bool IsActive => DateTime.UtcNow >= EventStartTime && DateTime.UtcNow <= EventEndTime;
    public TimeSpan Duration => EventEndTime - EventStartTime;
    public Dictionary<string, object> Properties { get; set; } = new();
}