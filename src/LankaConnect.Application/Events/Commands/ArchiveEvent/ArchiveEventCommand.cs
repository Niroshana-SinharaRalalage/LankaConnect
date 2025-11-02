using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.ArchiveEvent;

public record ArchiveEventCommand(Guid EventId) : ICommand;
