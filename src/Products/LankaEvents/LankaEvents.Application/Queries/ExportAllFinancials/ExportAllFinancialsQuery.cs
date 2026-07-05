using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Contracts;
using LankaConnect.Products.LankaEvents.Application.Queries.ExportEventAttendees;
namespace LankaConnect.Products.LankaEvents.Application.Queries.ExportAllFinancials;

/// <summary>
/// Query to export all financial data for an event.
/// Excel: Multi-sheet workbook (Attendees, Donations, Collections, Sponsors, Add-Ons).
/// CSV: ZIP archive containing 5 CSV files.
/// </summary>
public record ExportAllFinancialsQuery(
    Guid EventId,
    ExportFormat Format
) : IQuery<ExportResult>;
