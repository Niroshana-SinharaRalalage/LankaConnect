using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.WithdrawRefundRequest;

/// <summary>
/// Phase 6A.91: Command to withdraw a pending refund request.
/// This allows users to cancel their refund request and keep their registration.
/// Only valid when registration is in RefundRequested status and event hasn't started.
/// </summary>
/// <param name="EventId">The event ID</param>
/// <param name="UserId">The user withdrawing their refund request</param>
public record WithdrawRefundRequestCommand(
    Guid EventId,
    Guid UserId
) : ICommand;
