namespace LankaConnect.Modules.Payments.Contracts;

/// <summary>
/// Cross-module mirror of <c>LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType</c>.
/// One line item per underlying Stripe charge. Donations are intentionally excluded
/// (non-refundable per product decision). Deliberately duplicated at the Contracts
/// boundary per the Forms / Communications precedent.
/// </summary>
/// <remarks>
/// Wave 4.4.a (2026-06-23). 1:1 cast at the Payments.Application mapping seam.
/// </remarks>
public enum RefundLineItemTypeDto : byte
{
    Ticket = 0,
    AddOn = 1,
    Collection = 2,
    Sponsor = 3,
}
