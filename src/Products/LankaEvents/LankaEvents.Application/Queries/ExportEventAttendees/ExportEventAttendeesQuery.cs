using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Contracts;

namespace LankaConnect.Products.LankaEvents.Application.Queries.ExportEventAttendees;

/// <summary>
/// Query to export event attendees to Excel or CSV format.
///
/// Wave 6.a.1 (2026-07-01): ExportResult + ExportFormat moved to
/// Products.LankaEvents.Contracts so Forms.Application can consume without
/// importing Products.LankaEvents.Application (Rule 9 boundary).
/// </summary>
public record ExportEventAttendeesQuery(
    Guid EventId,
    ExportFormat Format
) : IQuery<ExportResult>;
