using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;

namespace LankaConnect.Application.Events.Queries.ExportDonations;

/// <summary>
/// Query to export event donations to Excel or CSV format.
/// Reuses ExportFormat and ExportResult from ExportEventAttendees.
/// </summary>
public record ExportDonationsQuery(
    Guid EventId,
    ExportFormat Format
) : IQuery<ExportResult>;
