using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.AssignLayoutToEvent;

/// <summary>
/// Assigns a venue layout to an event and sets the seating mode to AssignedSeating.
/// Validates zone→tier mapping and capacity constraints.
/// </summary>
public record AssignLayoutToEventCommand(
    Guid EventId,
    Guid LayoutId
) : ICommand;
