using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.PostponeEvent;

public record PostponeEventCommand(Guid EventId, string PostponementReason) : ICommand;
