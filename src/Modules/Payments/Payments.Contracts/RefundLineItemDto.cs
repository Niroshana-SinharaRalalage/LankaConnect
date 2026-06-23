namespace LankaConnect.Modules.Payments.Contracts;

/// <summary>
/// Cross-module read-only projection of a single refund request line item.
/// Returned as part of <see cref="RefundRequestDetailDto.LineItems"/>.
/// </summary>
/// <remarks>
/// Wave 4.4.a (2026-06-23). Money is projected to (decimal Amount + string Currency)
/// instead of carrying <c>LankaConnect.Domain.Shared.ValueObjects.Money</c> directly -
/// the Contracts layer must not pull <c>LankaConnect.Domain</c>. Mapping happens in
/// <c>Payments.Application.Mappings.PaymentContractMappings</c> at 4.4.b.
/// <para>
/// <see cref="ApprovedAmount"/> is null while the line is still <see cref="LineStatus"/>
/// = Requested (organizer has not yet approved per-line amounts).
/// </para>
/// </remarks>
public sealed record RefundLineItemDto(
    Guid Id,
    RefundLineItemTypeDto Type,
    Guid ReferenceId,
    decimal RequestedAmount,
    string RequestedCurrency,
    decimal? ApprovedAmount,
    string? ApprovedCurrency,
    string LineStatus,
    string? StripeRefundId,
    DateTime CreatedAt);
