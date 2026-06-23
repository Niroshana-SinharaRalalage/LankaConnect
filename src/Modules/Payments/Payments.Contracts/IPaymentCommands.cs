namespace LankaConnect.Modules.Payments.Contracts;

/// <summary>
/// Cross-module mutator API for the Payments module.
/// SCOPE: only operations invoked FROM other modules. All within-Payments
/// mutators (Create/Approve/Reject/Withdraw RefundRequest, Stripe webhook
/// handling, etc.) move into Payments.Application in Wave 4.4.c.1-c.4 and are
/// NOT exposed via this contract - they remain MediatR commands dispatched by
/// the controllers that already live inside the module.
/// </summary>
/// <remarks>
/// Wave 4.4.a (2026-06-23). The surface is intentionally empty at launch -
/// concrete methods are added in Wave 4.4.d.1 after the
/// <c>LankaConnect.Application</c> audit identifies which cross-module mutator
/// paths (if any) need to go through Contracts. Mirrors the Forms 5.3a +
/// Communications 5.4.a precedent. If the 4.4.d.1 audit surfaces zero
/// cross-module mutators, this interface is removed in 4.4.d.3 along with the
/// unused DI registration.
/// <para>
/// Likely 4.4.d.1 additions (per the plan's consumer survey): the
/// <c>EventCancellationEmailJob</c> currently invokes
/// <c>RefundExecutionService</c> directly across the module boundary - that
/// call site is a candidate for an <c>IPaymentCommands.InitiateBulkEventRefundsAsync</c>
/// method once the audit confirms the shape.
/// </para>
/// </remarks>
public interface IPaymentCommands
{
    // Concrete methods added in Wave 4.4.d.1 after audit. Empty marker interface
    // for now so consumers can take the dependency in 4.4.d.1 without
    // additional Contracts ripple.
}
