using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Shared.Email.Contracts;

namespace LankaConnect.Modules.Payments.Application.EventHandlers.RefundRequests;

/// <summary>
/// Phase 6A.148.D8: Single source of truth for mapping a domain
/// <see cref="RefundRequestLineItem"/> into the email-only
/// <see cref="RefundLineItemView"/>. Shared cannot reference Domain, so the
/// conversion lives in Application where all four lifecycle handlers can reuse it.
///
/// Keep the type/status display strings in this one place — the
/// <see cref="RefundLineItemsHtmlBuilder.BuildDecisionListHtml"/> branch on
/// the status string for badge colouring, so drift between the handler-side
/// mapping and the helper-side colour table would silently render every line
/// in the fallback grey "Pending" badge.
/// </summary>
internal static class RefundLineItemViewMapper
{
    public static RefundLineItemView ToView(this RefundRequestLineItem line)
    {
        return new RefundLineItemView(
            Type: TypeDisplay(line.Type),
            RequestedAmount: line.RequestedAmount.Amount,
            ApprovedAmount: line.ApprovedAmount?.Amount,
            Status: StatusDisplay(line.Status));
    }

    private static string TypeDisplay(RefundLineItemType type) => type switch
    {
        RefundLineItemType.Ticket => "Ticket",
        RefundLineItemType.AddOn => "Add-On",
        RefundLineItemType.Collection => "Collection",
        RefundLineItemType.Sponsor => "Sponsor",
        _ => type.ToString()
    };

    private static string StatusDisplay(RefundLineItemStatus status) => status switch
    {
        RefundLineItemStatus.Requested => "requested",
        RefundLineItemStatus.Approved => "approved",
        RefundLineItemStatus.Rejected => "rejected",
        RefundLineItemStatus.Processing => "processing",
        RefundLineItemStatus.Refunded => "refunded",
        RefundLineItemStatus.Failed => "failed",
        _ => status.ToString().ToLowerInvariant()
    };
}
