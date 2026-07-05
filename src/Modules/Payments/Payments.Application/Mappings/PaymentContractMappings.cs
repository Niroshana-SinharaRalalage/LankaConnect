using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Modules.Payments.Contracts;
namespace LankaConnect.Modules.Payments.Application.Mappings;

/// <summary>
/// Domain -> Contracts projections for the Payments module. Wave 4.4.b (2026-06-23).
/// Mirrors the W5.4.b <c>EmailGroupContractMappings</c> pattern. Lives in
/// Payments.Application because the source types
/// (<see cref="RefundRequest"/>, <see cref="RefundRequestLineItem"/>) live in
/// <c>LankaConnect.Products.LankaEvents.Domain.Entities</c> per the architect Risk #1
/// Option A ruling. The transitional <c>Payments.Application -&gt; LankaConnect.Domain</c>
/// ProjectReference is what makes this wiring compile.
/// </summary>
internal static class PaymentContractMappings
{
    public static RefundRequestSummaryDto ToSummaryDto(this RefundRequest src) =>
        new(
            Id: src.Id,
            RegistrationId: src.RegistrationId,
            RequestedByUserId: src.RequestedByUserId,
            IsOrganizerInitiated: src.IsOrganizerInitiated,
            RequestedAt: src.RequestedAt,
            Status: (RefundRequestStatusDto)(byte)src.Status,
            RequesterReason: src.RequesterReason,
            ReviewedByUserId: src.ReviewedByUserId,
            ReviewedAt: src.ReviewedAt,
            OrganizerNotes: src.OrganizerNotes,
            RejectionReason: src.RejectionReason,
            CompletedAt: src.CompletedAt,
            LineItemCount: src.LineItems.Count,
            IsActive: src.IsActive);

    public static RefundRequestDetailDto ToDetailDto(this RefundRequest src) =>
        new(
            Id: src.Id,
            RegistrationId: src.RegistrationId,
            RequestedByUserId: src.RequestedByUserId,
            IsOrganizerInitiated: src.IsOrganizerInitiated,
            RequestedAt: src.RequestedAt,
            Status: (RefundRequestStatusDto)(byte)src.Status,
            RequesterReason: src.RequesterReason,
            ReviewedByUserId: src.ReviewedByUserId,
            ReviewedAt: src.ReviewedAt,
            OrganizerNotes: src.OrganizerNotes,
            RejectionReason: src.RejectionReason,
            CompletedAt: src.CompletedAt,
            IsActive: src.IsActive,
            LineItems: src.LineItems.Select(li => li.ToLineItemDto()).ToList());

    public static RefundLineItemDto ToLineItemDto(this RefundRequestLineItem src) =>
        new(
            Id: src.Id,
            Type: (RefundLineItemTypeDto)(byte)src.Type,
            ReferenceId: src.ReferenceId,
            RequestedAmount: src.RequestedAmount.Amount,
            RequestedCurrency: src.RequestedAmount.Currency.ToString(),
            ApprovedAmount: src.ApprovedAmount?.Amount,
            ApprovedCurrency: src.ApprovedAmount?.Currency.ToString(),
            LineStatus: src.Status.ToString(),
            StripeRefundId: src.StripeRefundId,
            CreatedAt: src.CreatedAt);

    public static RefundRequestStatus FromDto(this RefundRequestStatusDto dto) =>
        (RefundRequestStatus)(byte)dto;
}
