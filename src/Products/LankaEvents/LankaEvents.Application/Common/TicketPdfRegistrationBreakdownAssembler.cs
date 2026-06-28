using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;

namespace LankaConnect.Products.LankaEvents.Application.Common;

/// <summary>
/// Phase 7F-E.4a (architect-approved 2026-05-01): adapts a <see cref="Registration"/>
/// aggregate into the shared <see cref="RegistrationBreakdown"/> projection consumed by
/// the PDF ticket renderer.
///
/// Mirrors the projection logic used by the email pipeline (see
/// <c>HeadCountEmailFormatter</c>) so the PDF, email, and event-detail surfaces all
/// compute identical breakdown shapes from the same registration. Centralising this
/// dispatch in one helper keeps <c>TicketService</c>'s three PDF-build sites
/// (<c>GenerateTicketAsync</c>, <c>RegeneratePdfAsync</c>,
/// <c>RegenerateTicketPdfForRegistrationAsync</c>) DRY.
///
/// Returns <c>null</c> when the registration has neither detailed attendees nor a head-
/// count breakdown (a defensive case — should not occur for paid registrations that
/// reach the ticket generator). PDF renderer treats <c>null</c> as "skip the breakdown
/// section", preserving Mode A's existing per-attendee list as the only attendee
/// block.
/// </summary>
public static class TicketPdfRegistrationBreakdownAssembler
{
    public static RegistrationBreakdown? Build(Registration? registration)
    {
        if (registration is null)
            return null;

        // Mode A — Registration has per-attendee detail.
        if (registration.HasDetailedAttendees())
            return RegistrationBreakdownFormatter.FromAttendees(registration.Attendees);

        // Mode B1/B2/B3/B4 — Registration carries a HeadCountBreakdown snapshot.
        if (registration.HeadCount is not null)
            return RegistrationBreakdownFormatter.FromHeadCount(
                registration.HeadCount, registration.RegistrationMode);

        return null;
    }
}
