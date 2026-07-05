using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Contracts;
using LankaConnect.Products.LankaEvents.Application.Queries.ExportEventAttendees;
namespace LankaConnect.Products.LankaEvents.Application.Queries.ExportCollections;

/// <summary>
/// Query to export event collections (event fund contributions) to Excel or CSV format.
/// Reuses ExportFormat and ExportResult from ExportEventAttendees.
/// </summary>
public record ExportCollectionsQuery(
    Guid EventId,
    ExportFormat Format
) : IQuery<ExportResult>;
