using LankaConnect.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateEventCapacity;

public record UpdateEventCapacityCommand(Guid EventId, int NewCapacity) : ICommand;
