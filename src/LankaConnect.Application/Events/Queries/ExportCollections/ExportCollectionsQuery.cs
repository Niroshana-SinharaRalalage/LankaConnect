using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;

namespace LankaConnect.Application.Events.Queries.ExportCollections;

/// <summary>
/// Query to export event collections (event fund contributions) to Excel or CSV format.
/// Reuses ExportFormat and ExportResult from ExportEventAttendees.
/// </summary>
public record ExportCollectionsQuery(
    Guid EventId,
    ExportFormat Format
) : IQuery<ExportResult>;
