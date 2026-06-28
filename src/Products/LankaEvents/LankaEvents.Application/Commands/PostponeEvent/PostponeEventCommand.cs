using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.PostponeEvent;

public record PostponeEventCommand(Guid EventId, string PostponementReason) : ICommand;
