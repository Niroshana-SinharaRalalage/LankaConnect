using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.RemoveFromWaitingList;

/// <summary>
/// Command to remove a user from an event's waiting list
/// Called when user no longer wants to be notified about available spots
/// </summary>
public record RemoveFromWaitingListCommand(Guid EventId, Guid UserId) : ICommand;
