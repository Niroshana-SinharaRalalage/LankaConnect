using LankaConnect.Domain.Common.Enums;
using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Response model for multi-language routing operations
/// </summary>
public record MultiLanguageRoutingResponse
{
    /// <summary>
    /// Selected language for routing
    /// </summary>
    public SouthAsianLanguage SelectedLanguage { get; init; }
    
    /// <summary>
    /// Route endpoint URL
    /// </summary>
    public string RouteEndpoint { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence score for language selection (0-100)
    /// </summary>
    public int ConfidenceScore { get; init; }
    
    /// <summary>
    /// Response time for routing operation
    /// </summary>
    public TimeSpan ResponseTime { get; init; }
    
    /// <summary>
    /// Alternative language options
    /// </summary>
    public List<SouthAsianLanguage> AlternativeLanguages { get; init; } = new();
    
    /// <summary>
    /// Routing metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// Whether routing was successful
    /// </summary>
    public bool IsSuccessful { get; init; }
    
    /// <summary>
    /// Error message if routing failed
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;
}