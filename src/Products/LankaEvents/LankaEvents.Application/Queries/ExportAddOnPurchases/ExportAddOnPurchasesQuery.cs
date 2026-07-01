using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Contracts;
using LankaConnect.Products.LankaEvents.Application.Queries.ExportEventAttendees;

namespace LankaConnect.Products.LankaEvents.Application.Queries.ExportAddOnPurchases;

/// <summary>
/// Query to export event add-on purchases to Excel or CSV format.
/// Reuses ExportFormat and ExportResult from ExportEventAttendees.
/// </summary>
public record ExportAddOnPurchasesQuery(
    Guid EventId,
    ExportFormat Format
) : IQuery<ExportResult>;
