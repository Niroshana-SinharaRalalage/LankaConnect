using LankaConnect.Domain.Common.Enums;
using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Represents language boost configuration for cultural events
/// </summary>
public record CulturalEventLanguageBoost
{
    /// <summary>
    /// Primary language for the boost
    /// </summary>
    public SouthAsianLanguage Language { get; init; }
    
    /// <summary>
    /// Boost multiplier (1.0 = no boost, >1.0 = boost)
    /// </summary>
    public decimal BoostMultiplier { get; init; } = 1.0m;
    
    /// <summary>
    /// Content type this boost applies to
    /// </summary>
    public ContentType ContentType { get; init; }
    
    /// <summary>
    /// Cultural event category
    /// </summary>
    public string EventCategory { get; init; } = string.Empty;
    
    /// <summary>
    /// Geographic regions where boost applies
    /// </summary>
    public List<string> ApplicableRegions { get; init; } = new();
    
    /// <summary>
    /// Time period when boost is active
    /// </summary>
    public DateTimeOffset BoostStartTime { get; init; }
    
    /// <summary>
    /// Time period when boost expires
    /// </summary>
    public DateTimeOffset BoostEndTime { get; init; }
    
    /// <summary>
    /// Whether the boost is currently active
    /// </summary>
    public bool IsActive => DateTime.UtcNow >= BoostStartTime && DateTime.UtcNow <= BoostEndTime;
    
    /// <summary>
    /// Boost configuration metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}