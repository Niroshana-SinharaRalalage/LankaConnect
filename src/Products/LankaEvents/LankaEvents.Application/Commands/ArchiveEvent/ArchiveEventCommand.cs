using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.ArchiveEvent;

public record ArchiveEventCommand(Guid EventId) : ICommand;
