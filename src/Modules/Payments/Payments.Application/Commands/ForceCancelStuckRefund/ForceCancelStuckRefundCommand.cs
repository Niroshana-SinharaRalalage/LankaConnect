using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Modules.Payments.Application.Commands.ForceCancelStuckRefund;

/// <summary>
/// Phase 7E follow-up: Organiser-initiated cancellation of a registration that is stuck in
/// <c>RefundRequested</c> status because the Stripe webhook never confirmed the refund.
///
/// Why this exists: a stuck row consumes capacity until Stripe confirms; if Stripe never does
/// (very old events, manually-processed off-platform refunds), the row blocks
/// <c>Event.SetRegistrationMode</c> and clutters the dashboard. The registrant's
/// <c>WithdrawRefundRequest</c> path can't help here — that requires their account.
///
/// Authorization happens at the controller layer (must be the event organiser — owner or
/// co-organizer). The domain method itself enforces the status-transition invariant
/// (<c>RefundRequested → Cancelled</c> only).
/// </summary>
/// <param name="EventId">Event the registration belongs to.</param>
/// <param name="RegistrationId">Registration row to force-cancel.</param>
public record ForceCancelStuckRefundCommand(
    Guid EventId,
    Guid RegistrationId
) : ICommand;
