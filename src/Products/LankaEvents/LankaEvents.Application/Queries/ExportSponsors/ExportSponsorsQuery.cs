using LankaConnect.Products.LankaEvents.Contracts;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Queries.ExportEventAttendees;

namespace LankaConnect.Products.LankaEvents.Application.Queries.ExportSponsors;

/// <summary>
/// Query to export event sponsors to Excel or CSV format.
/// Reuses ExportFormat and ExportResult from ExportEventAttendees.
/// </summary>
public record ExportSponsorsQuery(
    Guid EventId,
    ExportFormat Format
) : IQuery<ExportResult>;
