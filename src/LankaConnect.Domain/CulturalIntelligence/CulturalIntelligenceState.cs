using LankaConnect.Domain.Common.Enums;
using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.CulturalIntelligence;

/// <summary>
/// Represents the state of cultural intelligence processing
/// </summary>
public record CulturalIntelligenceState
{
    /// <summary>
    /// Unique identifier for the cultural intelligence state
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Cultural context information
    /// </summary>
    public string CulturalContext { get; init; } = string.Empty;
    
    /// <summary>
    /// Primary language for cultural processing
    /// </summary>
    public SouthAsianLanguage PrimaryLanguage { get; init; }
    
    /// <summary>
    /// Secondary languages for cultural processing
    /// </summary>
    public List<SouthAsianLanguage> SecondaryLanguages { get; init; } = new();
    
    /// <summary>
    /// Content type being processed
    /// </summary>
    public ContentType ContentType { get; init; }
    
    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; init; }
    
    /// <summary>
    /// Processing metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// Cultural sensitivity score (0-100)
    /// </summary>
    public int SensitivityScore { get; init; }
    
    /// <summary>
    /// Processing status
    /// </summary>
    public string Status { get; init; } = "Initialized";
}