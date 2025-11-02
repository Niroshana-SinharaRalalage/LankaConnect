using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateEventCapacity;

public record UpdateEventCapacityCommand(Guid EventId, int NewCapacity) : ICommand;
