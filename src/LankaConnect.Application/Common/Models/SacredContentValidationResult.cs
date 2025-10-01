using LankaConnect.Domain.Common.Enums;
using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Result of sacred content validation
/// </summary>
public record SacredContentValidationResult
{
    /// <summary>
    /// Whether the content is valid
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Validation message
    /// </summary>
    public string ValidationMessage { get; init; } = string.Empty;
    
    /// <summary>
    /// List of validation violations
    /// </summary>
    public List<string> Violations { get; init; } = new();
    
    /// <summary>
    /// Content type being validated
    /// </summary>
    public ContentType ContentType { get; init; }
    
    /// <summary>
    /// Cultural sensitivity score (0-100)
    /// </summary>
    public int SensitivityScore { get; init; }
    
    /// <summary>
    /// Language of the content
    /// </summary>
    public SouthAsianLanguage Language { get; init; }
    
    /// <summary>
    /// Validation timestamp
    /// </summary>
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Validation metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}