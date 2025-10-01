using LankaConnect.Domain.Common.Enums;
using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Result of language routing failover operations
/// </summary>
public record LanguageRoutingFailoverResult
{
    /// <summary>
    /// Whether failover was successful
    /// </summary>
    public bool IsSuccessful { get; init; }
    
    /// <summary>
    /// Original language that failed
    /// </summary>
    public SouthAsianLanguage OriginalLanguage { get; init; }
    
    /// <summary>
    /// Fallback language used
    /// </summary>
    public SouthAsianLanguage FallbackLanguage { get; init; }
    
    /// <summary>
    /// Reason for failover
    /// </summary>
    public string FailoverReason { get; init; } = string.Empty;
    
    /// <summary>
    /// Failover strategy used
    /// </summary>
    public string FailoverStrategy { get; init; } = string.Empty;
    
    /// <summary>
    /// Time taken for failover
    /// </summary>
    public TimeSpan FailoverDuration { get; init; }
    
    /// <summary>
    /// New routing endpoint after failover
    /// </summary>
    public string NewEndpoint { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence score for fallback route (0-100)
    /// </summary>
    public int FallbackConfidence { get; init; }
    
    /// <summary>
    /// Available alternative languages
    /// </summary>
    public List<SouthAsianLanguage> AlternativeLanguages { get; init; } = new();
    
    /// <summary>
    /// Failover timestamp
    /// </summary>
    public DateTime FailoverTimestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Error details if failover failed
    /// </summary>
    public string ErrorDetails { get; init; } = string.Empty;
    
    /// <summary>
    /// Failover metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}