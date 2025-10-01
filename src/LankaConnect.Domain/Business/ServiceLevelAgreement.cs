using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business;

/// <summary>
/// Domain entity representing a Service Level Agreement with cultural intelligence features.
/// Used for enterprise-grade SLA management across the LankaConnect platform.
/// </summary>
public class ServiceLevelAgreement : BaseEntity
{
    /// <summary>
    /// Gets the SLA level (e.g., Gold, Silver, Bronze, Cultural_Premium).
    /// </summary>
    public string SlaLevel { get; private set; }

    /// <summary>
    /// Gets the performance targets dictionary (metric name -> target value).
    /// </summary>
    public Dictionary<string, double> PerformanceTargets { get; private set; }

    /// <summary>
    /// Gets the compliance measurement period.
    /// </summary>
    public TimeSpan CompliancePeriod { get; private set; }

    /// <summary>
    /// Gets or sets the cultural event context for specialized SLA monitoring.
    /// </summary>
    public string? CulturalEventContext { get; private set; }

    /// <summary>
    /// Gets or sets the diaspora region for geographic SLA enforcement.
    /// </summary>
    public string? DiasporaRegion { get; private set; }

    /// <summary>
    /// Private constructor for Entity Framework and domain reconstruction.
    /// </summary>
    private ServiceLevelAgreement()
    {
        SlaLevel = string.Empty;
        PerformanceTargets = new Dictionary<string, double>();
        CompliancePeriod = TimeSpan.Zero;
    }

    /// <summary>
    /// Private constructor for creating ServiceLevelAgreement instances.
    /// </summary>
    private ServiceLevelAgreement(string slaLevel, Dictionary<string, double> performanceTargets, TimeSpan compliancePeriod)
    {
        SlaLevel = slaLevel;
        PerformanceTargets = new Dictionary<string, double>(performanceTargets);
        CompliancePeriod = compliancePeriod;
    }

    /// <summary>
    /// Factory method to create a new ServiceLevelAgreement with validation.
    /// </summary>
    /// <param name="slaLevel">The SLA level identifier.</param>
    /// <param name="performanceTargets">Dictionary of performance targets.</param>
    /// <param name="compliancePeriod">The compliance measurement period.</param>
    /// <returns>A new ServiceLevelAgreement instance.</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static ServiceLevelAgreement Create(string slaLevel, Dictionary<string, double> performanceTargets, TimeSpan compliancePeriod)
    {
        if (string.IsNullOrWhiteSpace(slaLevel))
            throw new ArgumentException("SLA level cannot be null or empty", nameof(slaLevel));

        if (performanceTargets == null || !performanceTargets.Any())
            throw new ArgumentException("Performance targets cannot be null or empty", nameof(performanceTargets));

        if (compliancePeriod <= TimeSpan.Zero)
            throw new ArgumentException("Compliance period must be positive", nameof(compliancePeriod));

        return new ServiceLevelAgreement(slaLevel, performanceTargets, compliancePeriod);
    }

    /// <summary>
    /// Sets cultural intelligence context for specialized SLA monitoring.
    /// </summary>
    /// <param name="culturalEventContext">The cultural event context.</param>
    /// <param name="diasporaRegion">The affected diaspora region.</param>
    public void SetCulturalContext(string culturalEventContext, string diasporaRegion)
    {
        CulturalEventContext = culturalEventContext;
        DiasporaRegion = diasporaRegion;
    }

    /// <summary>
    /// Checks compliance against actual performance metrics.
    /// </summary>
    /// <param name="actualMetrics">Dictionary of actual performance metrics.</param>
    /// <returns>Compliance result with violation details.</returns>
    public SlaComplianceResult CheckCompliance(Dictionary<string, double> actualMetrics)
    {
        if (actualMetrics == null)
            throw new ArgumentNullException(nameof(actualMetrics));

        var violations = new List<string>();
        var complianceScores = new Dictionary<string, double>();
        
        foreach (var target in PerformanceTargets)
        {
            if (actualMetrics.TryGetValue(target.Key, out var actualValue))
            {
                var complianceScore = (actualValue / target.Value) * 100.0;
                complianceScores[target.Key] = complianceScore;
                
                if (actualValue < target.Value)
                {
                    violations.Add($"{target.Key}: Expected {target.Value}, got {actualValue}");
                }
            }
            else
            {
                violations.Add($"{target.Key}: No metric data provided");
                complianceScores[target.Key] = 0.0;
            }
        }

        var overallCompliance = complianceScores.Values.Any() 
            ? complianceScores.Values.Average() 
            : 0.0;

        return new SlaComplianceResult
        {
            IsCompliant = !violations.Any(),
            CompliancePercentage = overallCompliance,
            Violations = violations,
            ComplianceScores = complianceScores,
            CheckedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Result of SLA compliance checking.
/// </summary>
public class SlaComplianceResult
{
    /// <summary>
    /// Gets or sets whether the SLA is compliant.
    /// </summary>
    public bool IsCompliant { get; set; }

    /// <summary>
    /// Gets or sets the overall compliance percentage.
    /// </summary>
    public double CompliancePercentage { get; set; }

    /// <summary>
    /// Gets or sets the list of compliance violations.
    /// </summary>
    public List<string> Violations { get; set; } = new();

    /// <summary>
    /// Gets or sets the compliance scores by metric.
    /// </summary>
    public Dictionary<string, double> ComplianceScores { get; set; } = new();

    /// <summary>
    /// Gets or sets when the compliance check was performed.
    /// </summary>
    public DateTime CheckedAt { get; set; }
}