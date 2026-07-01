namespace LankaConnect.Products.LankaEvents.Contracts;

/// <summary>
/// Cross-boundary result container for file exports. Shared by
/// <c>Products.LankaEvents.Application</c> (attendee/signup-list exports) and
/// <c>Modules.Forms.Application</c> (form-response exports).
///
/// Wave 6.a.1 (2026-07-01): moved from
/// <c>Products.LankaEvents.Application.Queries.ExportEventAttendees</c> to
/// <c>Products.LankaEvents.Contracts</c> so Forms.Application can consume
/// without violating Rule 9 (Capability modules reach Products only via
/// Domain / Contracts, never via Application internals).
/// </summary>
public class ExportResult
{
    public byte[] FileContent { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
}

/// <summary>
/// Export format options. Kept alongside ExportResult because Forms exporters
/// call into the same file-writer utility (CSV / Excel) that consumes this enum.
///
/// Wave 6.a.1 (2026-07-01): moved from
/// <c>Products.LankaEvents.Application.Queries.ExportEventAttendees</c>.
/// </summary>
public enum ExportFormat
{
    Excel,
    Csv,
    SignUpListsZip,   // Phase 6A.69: ZIP archive with multiple CSV files (one per signup list category)
    SignUpListsExcel, // Phase 6A.73: Excel file with signup lists (one sheet per category)
    VolunteersZip,    // Phase 7D.1 Step 16: ZIP with CSV files, volunteer labels, Kind=Volunteers lists only
    VolunteersExcel   // Phase 7D.1 Step 16: ZIP with Excel files, volunteer labels, Kind=Volunteers lists only
}
