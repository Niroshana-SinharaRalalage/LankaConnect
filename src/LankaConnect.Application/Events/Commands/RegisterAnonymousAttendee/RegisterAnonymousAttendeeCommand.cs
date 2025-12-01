using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.RegisterAnonymousAttendee;

public record RegisterAnonymousAttendeeCommand(
    Guid EventId,
    string Name,
    int Age,
    string Address,
    string Email,
    string PhoneNumber,
    int Quantity = 1
) : ICommand;
