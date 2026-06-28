using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.PublishEvent;

public record PublishEventCommand(Guid EventId) : ICommand;
