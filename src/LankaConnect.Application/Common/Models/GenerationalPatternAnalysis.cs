using LankaConnect.Domain.Common.Enums;
using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Analysis of generational patterns in cultural content usage
/// </summary>
public record GenerationalPatternAnalysis
{
    /// <summary>
    /// Generation category
    /// </summary>
    public string Generation { get; init; } = string.Empty;
    
    /// <summary>
    /// Age range for this generation
    /// </summary>
    public (int MinAge, int MaxAge) AgeRange { get; init; }
    
    /// <summary>
    /// Preferred languages for this generation
    /// </summary>
    public List<SouthAsianLanguage> PreferredLanguages { get; init; } = new();
    
    /// <summary>
    /// Content types most engaged with
    /// </summary>
    public List<ContentType> PreferredContentTypes { get; init; } = new();
    
    /// <summary>
    /// Cultural engagement score (0-100)
    /// </summary>
    public decimal EngagementScore { get; init; }
    
    /// <summary>
    /// Digital adoption rate (0-100)
    /// </summary>
    public decimal DigitalAdoptionRate { get; init; }
    
    /// <summary>
    /// Traditional cultural adherence score (0-100)
    /// </summary>
    public decimal TraditionalAdherence { get; init; }
    
    /// <summary>
    /// Analysis period
    /// </summary>
    public DateTimeOffset AnalysisPeriod { get; init; }
    
    /// <summary>
    /// Sample size for this analysis
    /// </summary>
    public int SampleSize { get; init; }
    
    /// <summary>
    /// Statistical confidence level
    /// </summary>
    public decimal ConfidenceLevel { get; init; }
    
    /// <summary>
    /// Analysis metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}