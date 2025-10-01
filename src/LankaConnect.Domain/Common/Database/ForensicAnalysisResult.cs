namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Result of forensic analysis with cultural intelligence security context
/// </summary>
public class ForensicAnalysisResult
{
    /// <summary>
    /// Unique identifier for this forensic analysis
    /// </summary>
    public Guid AnalysisId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Scope of the forensic analysis performed
    /// </summary>
    public ForensicAnalysisScope Scope { get; set; }

    /// <summary>
    /// Number of compromised data elements found
    /// </summary>
    public int CompromisedDataCount { get; set; }

    /// <summary>
    /// Security risk level determined by analysis
    /// </summary>
    public string SecurityRiskLevel { get; set; } = "Unknown";

    /// <summary>
    /// Timestamp when analysis was performed
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cultural data elements that may be compromised
    /// </summary>
    public List<string> CulturalDataAffected { get; set; } = new();

    /// <summary>
    /// Recommended security actions
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();

    /// <summary>
    /// Whether this analysis found critical security issues
    /// </summary>
    public bool HasCriticalIssues { get; set; }
}