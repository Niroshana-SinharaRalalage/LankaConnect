using System;
using System.Collections.Generic;
using System.Linq;

namespace LankaConnect.Domain.Common.ValueObjects;

/// <summary>
/// Specialized value object for SLA reporting configuration with enterprise compliance
/// and cultural intelligence awareness for the LankaConnect platform.
/// </summary>
public sealed class SLAReportingConfiguration : IEquatable<SLAReportingConfiguration>
{
    /// <summary>
    /// Gets the report title.
    /// </summary>
    public string ReportTitle { get; }

    /// <summary>
    /// Gets the report format.
    /// </summary>
    public ReportFormat Format { get; }

    /// <summary>
    /// Gets whether to include metrics in the report.
    /// </summary>
    public bool IncludeMetrics { get; }

    /// <summary>
    /// Gets the SLA threshold percentage.
    /// </summary>
    public double SLAThreshold { get; }

    /// <summary>
    /// Gets the cultural context for the SLA report, if any.
    /// </summary>
    public string? CulturalContext { get; }

    /// <summary>
    /// Gets whether this configuration meets enterprise-grade requirements (≥99.5%).
    /// </summary>
    public bool IsEnterpriseGrade => SLAThreshold >= 99.5;

    /// <summary>
    /// Gets whether this configuration meets Fortune 500 requirements (≥99.9%).
    /// </summary>
    public bool IsFortune500Compliant => SLAThreshold >= 99.9;

    /// <summary>
    /// Gets whether this configuration has cultural adjustments.
    /// </summary>
    public bool HasCulturalAdjustments => !string.IsNullOrEmpty(CulturalContext) && IsCulturalEvent();

    /// <summary>
    /// Private constructor for creating SLAReportingConfiguration instances.
    /// </summary>
    private SLAReportingConfiguration(string reportTitle, ReportFormat format, bool includeMetrics, double slaThreshold, string? culturalContext)
    {
        ReportTitle = reportTitle;
        Format = format;
        IncludeMetrics = includeMetrics;
        SLAThreshold = slaThreshold;
        CulturalContext = culturalContext;
    }

    /// <summary>
    /// Creates a new SLAReportingConfiguration with validation.
    /// </summary>
    /// <param name="reportTitle">The title of the report.</param>
    /// <param name="format">The format of the report.</param>
    /// <param name="includeMetrics">Whether to include metrics.</param>
    /// <param name="slaThreshold">The SLA threshold percentage.</param>
    /// <param name="culturalContext">The cultural context, if any.</param>
    /// <returns>A new SLAReportingConfiguration instance.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    public static SLAReportingConfiguration Create(string reportTitle, ReportFormat format, bool includeMetrics, double slaThreshold, string? culturalContext)
    {
        if (string.IsNullOrWhiteSpace(reportTitle))
            throw new ArgumentException("Report title cannot be null or empty", nameof(reportTitle));

        if (slaThreshold < 0 || slaThreshold > 100)
            throw new ArgumentException("SLA threshold must be between 0 and 100", nameof(slaThreshold));

        return new SLAReportingConfiguration(reportTitle, format, includeMetrics, slaThreshold, culturalContext);
    }

    /// <summary>
    /// Creates a default SLAReportingConfiguration with enterprise-grade settings.
    /// </summary>
    /// <param name="reportTitle">The title of the report.</param>
    /// <returns>A new SLAReportingConfiguration with default enterprise settings.</returns>
    public static SLAReportingConfiguration CreateDefault(string reportTitle)
    {
        if (string.IsNullOrWhiteSpace(reportTitle))
            throw new ArgumentException("Report title cannot be null or empty", nameof(reportTitle));

        return new SLAReportingConfiguration(reportTitle, ReportFormat.PDF, true, 99.5, null);
    }

    /// <summary>
    /// Creates a Fortune 500 compliant SLAReportingConfiguration.
    /// </summary>
    /// <param name="reportTitle">The title of the report.</param>
    /// <returns>A new SLAReportingConfiguration meeting Fortune 500 standards.</returns>
    public static SLAReportingConfiguration CreateFortune500(string reportTitle)
    {
        if (string.IsNullOrWhiteSpace(reportTitle))
            throw new ArgumentException("Report title cannot be null or empty", nameof(reportTitle));

        return new SLAReportingConfiguration(reportTitle, ReportFormat.PDF, true, 99.9, null);
    }

    /// <summary>
    /// Gets the culturally adjusted SLA threshold for events and festivals.
    /// </summary>
    /// <returns>The adjusted SLA threshold considering cultural context.</returns>
    public double GetCulturallyAdjustedThreshold()
    {
        if (!HasCulturalAdjustments)
            return SLAThreshold;

        // During cultural events, we may relax SLA requirements slightly
        var culturalAdjustment = GetCulturalAdjustmentFactor();
        return Math.Max(95.0, SLAThreshold - culturalAdjustment);
    }

    /// <summary>
    /// Gets the compliance level based on the SLA threshold.
    /// </summary>
    /// <returns>A string representing the compliance level.</returns>
    public string GetComplianceLevel()
    {
        return SLAThreshold switch
        {
            >= 99.99 => "UltraHighAvailability",
            >= 99.9 => "Fortune500",
            >= 99.5 => "Enterprise",
            >= 99.0 => "Standard",
            >= 95.0 => "Basic",
            _ => "BelowStandard"
        };
    }

    /// <summary>
    /// Generates comprehensive report metadata including SLA context.
    /// </summary>
    /// <returns>A dictionary containing the report metadata.</returns>
    public Dictionary<string, object> GenerateReportMetadata()
    {
        var metadata = new Dictionary<string, object>
        {
            ["ReportTitle"] = ReportTitle,
            ["Format"] = Format.ToString(),
            ["IncludeMetrics"] = IncludeMetrics,
            ["SLAThreshold"] = SLAThreshold,
            ["ComplianceLevel"] = GetComplianceLevel(),
            ["IsEnterpriseGrade"] = IsEnterpriseGrade,
            ["IsFortune500Compliant"] = IsFortune500Compliant,
            ["ReportGeneratedAt"] = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(CulturalContext))
        {
            metadata["CulturalContext"] = CulturalContext;
            metadata["HasCulturalAdjustments"] = HasCulturalAdjustments;
            metadata["CulturallyAdjustedThreshold"] = GetCulturallyAdjustedThreshold();
        }

        return metadata;
    }

    /// <summary>
    /// Determines if the cultural context represents a cultural event that may affect SLA.
    /// </summary>
    private bool IsCulturalEvent()
    {
        if (string.IsNullOrEmpty(CulturalContext))
            return false;

        var culturalEventKeywords = new[]
        {
            "vesak", "poyaday", "sinhala new year", "tamil new year", "diwali",
            "ramadan", "christmas", "festival", "celebration", "cultural event",
            "traffic spike", "high volume", "seasonal"
        };

        return culturalEventKeywords.Any(keyword => 
            CulturalContext.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the cultural adjustment factor for SLA thresholds.
    /// </summary>
    private double GetCulturalAdjustmentFactor()
    {
        if (string.IsNullOrEmpty(CulturalContext))
            return 0.0;

        var context = CulturalContext.ToLowerInvariant();
        
        return context switch
        {
            var c when c.Contains("vesak") || c.Contains("sinhala new year") => 1.5,
            var c when c.Contains("diwali") || c.Contains("tamil new year") => 1.2,
            var c when c.Contains("ramadan") || c.Contains("christmas") => 1.0,
            var c when c.Contains("festival") || c.Contains("celebration") => 0.8,
            var c when c.Contains("traffic spike") || c.Contains("high volume") => 2.0,
            _ => 0.5
        };
    }

    /// <summary>
    /// Returns a string representation of the SLA reporting configuration.
    /// </summary>
    public override string ToString()
    {
        var result = $"{ReportTitle} (SLA: {SLAThreshold:F1}%, {Format})";
        
        if (IncludeMetrics)
            result += " with metrics";

        if (IsFortune500Compliant)
            result += " [Fortune 500]";
        else if (IsEnterpriseGrade)
            result += " [Enterprise]";

        if (!string.IsNullOrEmpty(CulturalContext))
            result += $" - {CulturalContext}";

        return result;
    }

    #region Equality

    /// <summary>
    /// Determines equality with another SLAReportingConfiguration.
    /// </summary>
    public bool Equals(SLAReportingConfiguration? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return ReportTitle == other.ReportTitle &&
               Format == other.Format &&
               IncludeMetrics == other.IncludeMetrics &&
               SLAThreshold.Equals(other.SLAThreshold) &&
               CulturalContext == other.CulturalContext;
    }

    /// <summary>
    /// Determines equality with another object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is SLAReportingConfiguration other && Equals(other);
    }

    /// <summary>
    /// Gets the hash code for this SLAReportingConfiguration.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(ReportTitle, Format, IncludeMetrics, SLAThreshold, CulturalContext);
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(SLAReportingConfiguration? left, SLAReportingConfiguration? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(SLAReportingConfiguration? left, SLAReportingConfiguration? right)
    {
        return !Equals(left, right);
    }

    #endregion
}