using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.SharedKernel.Geo.MetroAreas.Common;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 6.5.f mirror: LankaEventsDbContext
namespace LankaConnect.SharedKernel.Geo.MetroAreas.Queries.GetMetroAreas;

/// <summary>
/// Query to get all active metro areas
/// Phase 5C: Metro Areas API
/// </summary>
public record GetMetroAreasQuery : IQuery<IReadOnlyList<MetroAreaDto>>
{
    /// <summary>
    /// Optional filter: Only return active metro areas (default: true)
    /// </summary>
    public bool? ActiveOnly { get; init; } = true;

    /// <summary>
    /// Optional filter: Filter by state code (e.g., "OH")
    /// </summary>
    public string? StateFilter { get; init; }
}
