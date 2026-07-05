using LankaConnect.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.DeleteEvent;

public record DeleteEventCommand(Guid EventId, Guid UserId) : ICommand;
