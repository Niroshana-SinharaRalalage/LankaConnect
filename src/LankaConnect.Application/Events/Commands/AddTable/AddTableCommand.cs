using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Application.Events.Commands.AddTable;

/// <summary>
/// Slice 5 Chunk 6: POST /api/venue-layouts/{id}/tables. Creates a new banquet /
/// dining table inside a layout and radially (round) or side-distributed
/// (square / rect) generates its seats in one operation so the client does not
/// have to chain two calls. <c>ExpectedRowVersion</c> sourced from the
/// <c>If-Match</c> header for optimistic concurrency.
/// </summary>
public record AddTableCommand(
    Guid LayoutId,
    uint ExpectedRowVersion,
    string Label,
    TableShape Shape,
    int Capacity,
    int SortOrder,
    Guid? ZoneId,
    string? Geometry,
    double StartAngleDeg
) : ICommand<Guid>;
