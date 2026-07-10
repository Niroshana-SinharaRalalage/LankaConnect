using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateRsvp;

public record UpdateRsvpCommand(Guid EventId, Guid UserId, int NewQuantity) : ICommand;
