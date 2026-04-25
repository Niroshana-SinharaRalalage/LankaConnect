using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.SetSeatingMode;

/// <summary>
/// Command to set the seating mode for an event (GeneralAdmission or AssignedSeating).
/// AssignedSeating requires the event to already be in TicketingMode.Tiered.
/// In Slice 1 this command only flips the enum — venue layout creation comes in Slice 2+3.
/// </summary>
public record SetSeatingModeCommand(
    Guid EventId,
    SeatingMode SeatingMode
) : ICommand;
