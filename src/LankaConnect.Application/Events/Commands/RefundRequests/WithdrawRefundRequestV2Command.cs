using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.RefundRequests;

/// <summary>
/// Phase 6A.148: Attendee withdraws their own Pending refund request.
/// Distinct from the legacy <c>WithdrawRefundRequestCommand</c> which operates on
/// the registration directly (legacy 6A.91 flow, kept behind feature flag for
/// in-flight Stripe rows).
/// </summary>
public record WithdrawRefundRequestV2Command(
    Guid EventId,
    Guid CallerUserId
) : ICommand;
