using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.PromoteFromWaitingList;

/// <summary>
/// Command to promote a user from waiting list to confirmed registration
/// Called when a spot becomes available and user accepts the promotion
/// </summary>
public record PromoteFromWaitingListCommand(Guid EventId, Guid UserId) : ICommand;
