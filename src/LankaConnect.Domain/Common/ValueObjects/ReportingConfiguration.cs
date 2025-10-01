using System;
using System.Collections.Generic;
using System.Linq;

namespace LankaConnect.Domain.Common.ValueObjects;

/// <summary>
/// Value object representing reporting configuration with cultural intelligence awareness
/// for the LankaConnect platform. Provides enterprise-grade reporting capabilities
/// with Sri Lankan diaspora community context.
/// </summary>
public sealed class ReportingConfiguration : IEquatable<ReportingConfiguration>
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
    /// Gets whether to include charts in the report.
    /// </summary>
    public bool IncludeCharts { get; }

    /// <summary>
    /// Gets the cultural context for the report, if any.
    /// </summary>
    public string? CulturalContext { get; }

    /// <summary>
    /// Gets whether this configuration uses default values.
    /// </summary>
    public bool IsDefault { get; }

    /// <summary>
    /// Gets whether the report format is supported.
    /// </summary>
    public bool SupportedFormat => Enum.IsDefined(typeof(ReportFormat), Format);

    /// <summary>
    /// Gets whether the report has cultural metrics.
    /// </summary>
    public bool HasCulturalMetrics => !string.IsNullOrEmpty(CulturalContext) && IsCulturalContent();

    /// <summary>
    /// Private constructor for creating ReportingConfiguration instances.
    /// </summary>
    private ReportingConfiguration(string reportTitle, ReportFormat format, bool includeCharts, string? culturalContext, bool isDefault = false)
    {
        ReportTitle = reportTitle;
        Format = format;
        IncludeCharts = includeCharts;
        CulturalContext = culturalContext;
        IsDefault = isDefault;
    }

    /// <summary>
    /// Creates a new ReportingConfiguration with validation.
    /// </summary>
    /// <param name="reportTitle">The title of the report.</param>
    /// <param name="format">The format of the report.</param>
    /// <param name="includeCharts">Whether to include charts.</param>
    /// <param name="culturalContext">The cultural context, if any.</param>
    /// <returns>A new ReportingConfiguration instance.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    public static ReportingConfiguration Create(string reportTitle, ReportFormat format, bool includeCharts, string? culturalContext)
    {
        if (string.IsNullOrWhiteSpace(reportTitle))
            throw new ArgumentException("Report title cannot be null or empty", nameof(reportTitle));

        return new ReportingConfiguration(reportTitle, format, includeCharts, culturalContext);
    }

    /// <summary>
    /// Creates a default ReportingConfiguration with standard settings.
    /// </summary>
    /// <param name="reportTitle">The title of the report.</param>
    /// <returns>A new ReportingConfiguration with default settings.</returns>
    public static ReportingConfiguration CreateDefault(string reportTitle)
    {
        if (string.IsNullOrWhiteSpace(reportTitle))
            throw new ArgumentException("Report title cannot be null or empty", nameof(reportTitle));

        return new ReportingConfiguration(reportTitle, ReportFormat.JSON, false, null, true);
    }

    /// <summary>
    /// Gets the report parameters as a dictionary.
    /// </summary>
    /// <returns>A dictionary containing the report parameters.</returns>
    public Dictionary<string, object> GetReportParameters()
    {
        var parameters = new Dictionary<string, object>
        {
            ["Title"] = ReportTitle,
            ["Format"] = Format.ToString(),
            ["IncludeCharts"] = IncludeCharts
        };

        if (!string.IsNullOrEmpty(CulturalContext))
        {
            parameters["CulturalContext"] = CulturalContext;
        }

        parameters["IsDefault"] = IsDefault;
        parameters["SupportedFormat"] = SupportedFormat;
        parameters["HasCulturalMetrics"] = HasCulturalMetrics;

        return parameters;
    }

    /// <summary>
    /// Determines if the cultural context contains Sri Lankan or diaspora cultural content.
    /// </summary>
    private bool IsCulturalContent()
    {
        if (string.IsNullOrEmpty(CulturalContext))
            return false;

        var culturalKeywords = new[]
        {
            "vesak", "poyaday", "sinhala", "tamil", "buddhist", "hindu", "muslim",
            "diaspora", "sri lankan", "festival", "cultural", "community",
            "celebration", "tradition", "heritage"
        };

        return culturalKeywords.Any(keyword => 
            CulturalContext.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns a string representation of the reporting configuration.
    /// </summary>
    public override string ToString()
    {
        var result = $"{ReportTitle} ({Format})";
        
        if (IncludeCharts)
            result += " with charts";

        if (!string.IsNullOrEmpty(CulturalContext))
            result += $" - {CulturalContext}";

        if (IsDefault)
            result += " [Default]";

        return result;
    }

    #region Equality

    /// <summary>
    /// Determines equality with another ReportingConfiguration.
    /// </summary>
    public bool Equals(ReportingConfiguration? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return ReportTitle == other.ReportTitle &&
               Format == other.Format &&
               IncludeCharts == other.IncludeCharts &&
               CulturalContext == other.CulturalContext &&
               IsDefault == other.IsDefault;
    }

    /// <summary>
    /// Determines equality with another object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is ReportingConfiguration other && Equals(other);
    }

    /// <summary>
    /// Gets the hash code for this ReportingConfiguration.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(ReportTitle, Format, IncludeCharts, CulturalContext, IsDefault);
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(ReportingConfiguration? left, ReportingConfiguration? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(ReportingConfiguration? left, ReportingConfiguration? right)
    {
        return !Equals(left, right);
    }

    #endregion
}