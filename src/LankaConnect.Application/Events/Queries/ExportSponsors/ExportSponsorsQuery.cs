using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;

namespace LankaConnect.Application.Events.Queries.ExportSponsors;

/// <summary>
/// Query to export event sponsors to Excel or CSV format.
/// Reuses ExportFormat and ExportResult from ExportEventAttendees.
/// </summary>
public record ExportSponsorsQuery(
    Guid EventId,
    ExportFormat Format
) : IQuery<ExportResult>;
