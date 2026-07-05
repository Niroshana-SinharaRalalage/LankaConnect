using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.RemoveTicketTier;

public record RemoveTicketTierCommand(
    Guid EventId,
    Guid TierId
) : ICommand;
