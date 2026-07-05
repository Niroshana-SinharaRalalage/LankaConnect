using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetVenueLayout;

/// <summary>
/// Gets a venue layout by ID or by event ID (with zones and seats).
/// </summary>
public record GetVenueLayoutQuery(
    Guid? LayoutId,
    Guid? EventId
) : IQuery<VenueLayoutDto>;
