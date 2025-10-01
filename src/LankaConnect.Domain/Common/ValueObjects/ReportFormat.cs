namespace LankaConnect.Domain.Common.ValueObjects;

/// <summary>
/// Enumeration representing supported report formats for the LankaConnect platform.
/// Includes enterprise-grade formats for cultural intelligence reporting.
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// JavaScript Object Notation format for API consumption.
    /// </summary>
    JSON = 0,

    /// <summary>
    /// Portable Document Format for professional reporting.
    /// </summary>
    PDF = 1,

    /// <summary>
    /// Microsoft Excel format for data analysis.
    /// </summary>
    Excel = 2,

    /// <summary>
    /// Extensible Markup Language format for structured data.
    /// </summary>
    XML = 3,

    /// <summary>
    /// Comma-Separated Values format for data export.
    /// </summary>
    CSV = 4,

    /// <summary>
    /// HyperText Markup Language format for web display.
    /// </summary>
    HTML = 5
}