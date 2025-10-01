using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.CulturalIntelligence
{
    /// <summary>
    /// Sacred content request for validation
    /// </summary>
    public class SacredContentRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RequestId { get; set; } = string.Empty;
        public string ContentId { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string CulturalContext { get; set; } = string.Empty;
        public string RequestorId { get; set; } = string.Empty;
        public List<string> ValidationsRequired { get; set; } = new();
        public Dictionary<string, object> ContentMetadata { get; set; } = new();
        public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Result of sacred content validation
    /// </summary>
    public class SacredContentValidationResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ResultId { get; set; } = string.Empty;
        public SacredContentRequest Request { get; set; } = new();
        public bool IsValid { get; set; }
        public List<string> ValidationIssues { get; set; } = new();
        public string ValidationLevel { get; set; } = string.Empty;
        public Dictionary<string, object> ValidationMetrics { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Sacred content type classification
    /// </summary>
    public class SacredContentType
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TypeId { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string SacredLevel { get; set; } = string.Empty;
        public List<string> CulturalRestrictions { get; set; } = new();
        public Dictionary<string, object> AccessRules { get; set; } = new();
        public bool RequiresSpecialHandling { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cultural background information
    /// </summary>
    public class CulturalBackground
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string BackgroundId { get; set; } = string.Empty;
        public string CulturalIdentity { get; set; } = string.Empty;
        public List<string> Languages { get; set; } = new();
        public List<string> Traditions { get; set; } = new();
        public Dictionary<string, object> CulturalAttributes { get; set; } = new();
        public string Region { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Multi-language routing request
    /// </summary>
    public class MultiLanguageRoutingRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RequestId { get; set; } = string.Empty;
        public string SourceLanguage { get; set; } = string.Empty;
        public List<string> TargetLanguages { get; set; } = new();
        public string ContentType { get; set; } = string.Empty;
        public string RoutingStrategy { get; set; } = string.Empty;
        public Dictionary<string, object> RoutingParameters { get; set; } = new();
        public CulturalBackground CulturalContext { get; set; } = new();
        public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Multi-language routing response
    /// </summary>
    public class MultiLanguageRoutingResponse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ResponseId { get; set; } = string.Empty;
        public MultiLanguageRoutingRequest Request { get; set; } = new();
        public Dictionary<string, string> RoutedEndpoints { get; set; } = new();
        public List<string> SupportedLanguages { get; set; } = new();
        public string RoutingStatus { get; set; } = string.Empty;
        public Dictionary<string, object> RoutingMetrics { get; set; } = new();
        public bool IsSuccessful { get; set; }
        public DateTimeOffset RoutedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}