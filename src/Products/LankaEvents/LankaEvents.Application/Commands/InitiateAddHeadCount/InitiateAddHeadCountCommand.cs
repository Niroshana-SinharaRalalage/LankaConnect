using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent; // HeadCountDto
using LankaConnect.Products.LankaEvents.Application.Commands.InitiateAddAttendees; // InitiateAddAttendeesResult reuse
namespace LankaConnect.Products.LankaEvents.Application.Commands.InitiateAddHeadCount;

/// <summary>
/// Phase 7F-D (architect-approved 2026-04-30, plan §3 7F-D.3): initiate adding head-count
/// attendees to an existing paid Mode-B registration. Mirrors
/// <see cref="LankaConnect.Products.LankaEvents.Application.Commands.InitiateAddAttendees.InitiateAddAttendeesCommand"/>
/// at the contract level but works on the head-count axis instead of per-attendee rows.
///
/// Architect Q1 ratified: extends the existing add-attendees Stripe wiring rather than
/// introducing a parallel service. Both initiation paths share <c>IRegistrationCheckoutService.
/// InitiateAdditionCheckoutAsync</c> via the handler — anti-fork guard.
///
/// Architect Q2 ratified: distinct endpoint from <c>/add-attendees</c> because the body
/// shape is genuinely different (HeadCountDto vs per-attendee list); conflating them in
/// one endpoint creates a JSON-discriminator nightmare for the OpenAPI spec.
///
/// Architect Q5 ratified: head-count delta payload uses the same shape as RSVP
/// (HeadCountDto) so the FE form components from 7E.6 / 7F-C.3 can be reused without a
/// fork.
/// </summary>
public record InitiateAddHeadCountCommand(
    Guid RegistrationId,
    HeadCountDto HeadCountDelta,
    string SuccessUrl,
    string CancelUrl,
    Guid? UserId = null
) : ICommand<InitiateAddAttendeesResult>; // Reuse the existing result envelope.
