using LankaConnect.Domain.Common.Enums;
using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Result of cultural intelligence preservation operations
/// </summary>
public record CulturalIntelligencePreservationResult
{
    /// <summary>
    /// Whether preservation was successful
    /// </summary>
    public bool IsSuccessful { get; init; }
    
    /// <summary>
    /// Preservation operation identifier
    /// </summary>
    public Guid OperationId { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Content type that was preserved
    /// </summary>
    public ContentType ContentType { get; init; }
    
    /// <summary>
    /// Language of preserved content
    /// </summary>
    public SouthAsianLanguage Language { get; init; }
    
    /// <summary>
    /// Cultural context preserved
    /// </summary>
    public string CulturalContext { get; init; } = string.Empty;
    
    /// <summary>
    /// Preservation integrity score (0-100)
    /// </summary>
    public decimal IntegrityScore { get; init; }
    
    /// <summary>
    /// Preservation method used
    /// </summary>
    public string PreservationMethod { get; init; } = string.Empty;
    
    /// <summary>
    /// Storage location reference
    /// </summary>
    public string StorageReference { get; init; } = string.Empty;
    
    /// <summary>
    /// Preservation timestamp
    /// </summary>
    public DateTime PreservedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Expected retention period
    /// </summary>
    public TimeSpan RetentionPeriod { get; init; }
    
    /// <summary>
    /// Any warnings during preservation
    /// </summary>
    public List<string> Warnings { get; init; } = new();
    
    /// <summary>
    /// Error message if preservation failed
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;
    
    /// <summary>
    /// Preservation metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}