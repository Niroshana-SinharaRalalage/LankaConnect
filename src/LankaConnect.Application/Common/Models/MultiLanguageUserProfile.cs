using LankaConnect.Domain.Common.Enums;
using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Multi-language user profile for cultural content routing
/// </summary>
public record MultiLanguageUserProfile
{
    /// <summary>
    /// User identifier
    /// </summary>
    public Guid UserId { get; init; }
    
    /// <summary>
    /// Primary language preference
    /// </summary>
    public SouthAsianLanguage PrimaryLanguage { get; init; }
    
    /// <summary>
    /// Secondary language preferences ranked by preference
    /// </summary>
    public List<SouthAsianLanguage> SecondaryLanguages { get; init; } = new();
    
    /// <summary>
    /// Language proficiency levels (Language -> Proficiency 0-100)
    /// </summary>
    public Dictionary<SouthAsianLanguage, int> LanguageProficiency { get; init; } = new();
    
    /// <summary>
    /// Content type preferences
    /// </summary>
    public List<ContentType> PreferredContentTypes { get; init; } = new();
    
    /// <summary>
    /// Cultural region preferences
    /// </summary>
    public List<string> CulturalRegions { get; init; } = new();
    
    /// <summary>
    /// User's generation category
    /// </summary>
    public string GenerationCategory { get; init; } = string.Empty;
    
    /// <summary>
    /// User's location
    /// </summary>
    public string Location { get; init; } = string.Empty;
    
    /// <summary>
    /// Time zone preference
    /// </summary>
    public string TimeZone { get; init; } = string.Empty;
    
    /// <summary>
    /// Cultural engagement score (0-100)
    /// </summary>
    public decimal CulturalEngagementScore { get; init; }
    
    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; init; }
    
    /// <summary>
    /// Profile metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}