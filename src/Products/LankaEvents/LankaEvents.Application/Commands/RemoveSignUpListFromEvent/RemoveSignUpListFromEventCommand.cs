using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.RemoveSignUpListFromEvent;

public record RemoveSignUpListFromEventCommand(
    Guid EventId,
    Guid SignUpListId
) : ICommand;
