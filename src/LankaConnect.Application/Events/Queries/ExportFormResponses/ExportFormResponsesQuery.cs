using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;

namespace LankaConnect.Application.Events.Queries.ExportFormResponses;

/// <summary>
/// Query to export custom form responses to CSV or Excel format.
/// Phase 6A.110: Form response export functionality
/// </summary>
public record ExportFormResponsesQuery(
    Guid EventId,
    Guid FormId,
    ExportFormat Format
) : IQuery<ExportResult>;