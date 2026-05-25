namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.148.D7: Email-only view of a single refund line item.
///
/// Keeps the domain entity (Domain/Events/Entities/RefundRequestLineItem.cs) outside the
/// Shared assembly — the email projection only needs the display fields, not the EF/Money
/// types. Shared cannot reference Domain (one-way dependency rule).
///
/// One <see cref="RefundLineItemView"/> represents one row in the per-line table rendered
/// into the body of the pending-review / decision / rejected emails.
/// </summary>
/// <param name="Type">Display label for the bucket: "Ticket", "Add-On", "Collection", "Sponsor".</param>
/// <param name="RequestedAmount">Amount the attendee requested for this line.</param>
/// <param name="ApprovedAmount">Organizer-approved amount. Null in pending/rejected stages. Zero means line declined (Decision template renders "Declined").</param>
/// <param name="Status">Lowercase status label for badge rendering: "requested" / "approved" / "rejected" / "processing" / "refunded" / "failed".</param>
public record RefundLineItemView(
    string Type,
    decimal RequestedAmount,
    decimal? ApprovedAmount,
    string Status);
