namespace LankaConnect.Domain.Common.Enums;

/// <summary>
/// Represents the format options for generating reports in the cultural intelligence platform
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// JSON format for API consumption and programmatic processing
    /// </summary>
    Json = 1,
    
    /// <summary>
    /// PDF format for formal documentation and presentations
    /// </summary>
    Pdf = 2,
    
    /// <summary>
    /// Excel format for data analysis and manipulation
    /// </summary>
    Excel = 3,
    
    /// <summary>
    /// CSV format for data export and integration
    /// </summary>
    Csv = 4,
    
    /// <summary>
    /// XML format for structured data exchange
    /// </summary>
    Xml = 5,
    
    /// <summary>
    /// HTML format for web-based reporting
    /// </summary>
    Html = 6
}