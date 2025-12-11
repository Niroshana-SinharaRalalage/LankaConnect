using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.PublishEvent;

public record PublishEventCommand(Guid EventId) : ICommand;
