using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.RsvpToEvent;

public record RsvpToEventCommand(Guid EventId, Guid UserId, int Quantity = 1) : ICommand;
