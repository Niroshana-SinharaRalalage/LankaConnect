using LankaConnect.Domain.Common.Enums;
using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Request model for sacred content operations
/// </summary>
public record SacredContentRequest
{
    /// <summary>
    /// Unique request identifier
    /// </summary>
    public Guid RequestId { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Content to be processed
    /// </summary>
    public string Content { get; init; } = string.Empty;
    
    /// <summary>
    /// Content type
    /// </summary>
    public ContentType ContentType { get; init; }
    
    /// <summary>
    /// Language of the content
    /// </summary>
    public SouthAsianLanguage Language { get; init; }
    
    /// <summary>
    /// Cultural context for validation
    /// </summary>
    public string CulturalContext { get; init; } = string.Empty;
    
    /// <summary>
    /// Validation level required
    /// </summary>
    public string ValidationLevel { get; init; } = "Standard";
    
    /// <summary>
    /// User identifier making the request
    /// </summary>
    public Guid UserId { get; init; }
    
    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional validation parameters
    /// </summary>
    public Dictionary<string, object> ValidationParameters { get; init; } = new();
    
    /// <summary>
    /// Request metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}