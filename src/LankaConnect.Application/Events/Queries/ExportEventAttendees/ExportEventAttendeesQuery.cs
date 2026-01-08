using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Queries.ExportEventAttendees;

/// <summary>
/// Query to export event attendees to Excel or CSV format.
/// </summary>
public record ExportEventAttendeesQuery(
    Guid EventId,
    ExportFormat Format
) : IQuery<ExportResult>;

/// <summary>
/// Export format options.
/// </summary>
public enum ExportFormat
{
    Excel,
    Csv,
    SignUpListsZip  // Phase 6A.69: ZIP archive with multiple CSV files (one per signup list category)
}

/// <summary>
/// Result containing file data for download.
/// </summary>
public class ExportResult
{
    public byte[] FileContent { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
}
