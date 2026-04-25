using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Commands.CreateVenueLayout;

/// <summary>
/// Creates a new venue layout with zones.
/// </summary>
public record CreateVenueLayoutCommand(
    string Name,
    string LayoutType,
    Guid CreatedByUserId,
    Guid? EventId,
    bool IsTemplate,
    List<CreateVenueZoneRequest> Zones
) : ICommand<VenueLayoutDto>;

public record CreateVenueZoneRequest(
    string Name,
    string Color,
    Guid? TicketTierId,
    int SortOrder
);
