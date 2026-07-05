using LankaConnect.Products.LankaEvents.Contracts;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Queries.ExportEventAttendees;
namespace LankaConnect.Products.LankaEvents.Application.Queries.ExportDonations;

/// <summary>
/// Query to export event donations to Excel or CSV format.
/// Reuses ExportFormat and ExportResult from ExportEventAttendees.
/// </summary>
public record ExportDonationsQuery(
    Guid EventId,
    ExportFormat Format
) : IQuery<ExportResult>;
