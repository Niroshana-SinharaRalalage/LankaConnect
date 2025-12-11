using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.AddToWaitingList;

/// <summary>
/// Command to add a user to an event's waiting list
/// Called when event is at full capacity and user wants to be notified if spots become available
/// </summary>
public record AddToWaitingListCommand(Guid EventId, Guid UserId) : ICommand;
