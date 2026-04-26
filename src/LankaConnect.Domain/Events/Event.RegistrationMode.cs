using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Phase 7E: Per-event registration capture mode (DetailedAttendees / HeadCount* / NoRegistration).
/// Default <see cref="RegistrationMode.DetailedAttendees"/> preserves all pre-7E event behaviour.
/// </summary>
public partial class Event
{
    /// <summary>
    /// Phase 7E: Organiser-selected registration capture shape.
    /// - <see cref="RegistrationMode.DetailedAttendees"/> = today's per-attendee registration (default).
    /// - <see cref="RegistrationMode.HeadCountOnly"/> through <c>HeadCountByAgeAndGender"/> = head-count modes.
    /// - <see cref="RegistrationMode.NoRegistration"/> = drop-in event; standalone donations/sponsors/add-ons/collections still work.
    ///
    /// Persisted with DB-level <c>DEFAULT 0</c> so legacy events materialise as <see cref="RegistrationMode.DetailedAttendees"/>
    /// automatically (Phase 6A.123 lesson).
    /// </summary>
    public RegistrationMode RegistrationMode { get; private set; } = RegistrationMode.DetailedAttendees;

    /// <summary>
    /// Phase 7E: Sets the registration mode for this event.
    ///
    /// Business rules:
    /// 1. Mode change is forbidden once <see cref="Registrations"/>.Any() — protects historical data.
    ///    Mode A↔B conversion with attendee backfill is deferred to Phase 7F.
    /// 2. Standalone contributions (donations / sponsors / add-on purchases / collections) are
    ///    intentionally NOT considered by this guard. They are mode-agnostic by design — their
    ///    aggregates live outside the <c>Event.Registrations</c> collection (verified in 7E.0
    ///    audit §6: Event has no <c>Donations</c>/<c>Sponsors</c>/<c>AddOnPurchases</c>/<c>Collections</c>
    ///    navigation collections; only nullable <c>*Configuration</c> value-objects).
    /// 3. Compatibility with pricing / seating / add-on shapes is enforced at the application layer
    ///    via <c>FluentValidation</c> in 7E.2 (the 14-row compatibility table). This domain method
    ///    only enforces the registration-locking rule.
    /// </summary>
    /// <param name="mode">The new registration mode.</param>
    /// <returns>Result indicating success or failure with a clear message.</returns>
    public Result SetRegistrationMode(RegistrationMode mode)
    {
        // Architect rule (Phase 7E plan §3.2): forbid mode change once registrations exist.
        if (_registrations.Any())
        {
            return Result.Failure(
                $"Cannot change registration mode while registrations exist. " +
                $"Existing registrations: {_registrations.Count}. " +
                $"Mode change with attendee backfill is deferred to Phase 7F. " +
                $"EventId={Id}, CurrentMode={RegistrationMode}, RequestedMode={mode}");
        }

        if (RegistrationMode == mode)
        {
            return Result.Success(); // Idempotent — no change to make.
        }

        RegistrationMode = mode;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Phase 7E.3a: Registers a head-count (B-mode) RSVP for this event.
    ///
    /// Mirrors <see cref="RegisterWithAttendees"/> but builds a <see cref="HeadCountBreakdown"/>-backed
    /// registration instead of a per-attendee one. Performs the same status/date/duplicate guards
    /// and uses <see cref="HeadCountBreakdown.Total"/> for capacity checks.
    ///
    /// Scope discipline (7E.3a): free events ONLY. Paid B-mode RSVP — including Stripe checkout
    /// session creation and per-tier amount calculation — lands in 7E.3b. Calling this method
    /// against a paid event returns a clear failure pointing at 7E.3b.
    /// </summary>
    /// <param name="userId">Authenticated user ID, or null for anonymous.</param>
    /// <param name="leadAttendeeName">Name of the lead attendee — used in emails.</param>
    /// <param name="headCount">Composite head-count breakdown built via <see cref="HeadCountBreakdown"/> factories.</param>
    /// <param name="contact">Shared contact info — required for emails.</param>
    /// <returns>Success if the registration was created; failure with a clear message otherwise.</returns>
    public Result RegisterWithHeadCount(
        Guid? userId,
        string leadAttendeeName,
        HeadCountBreakdown headCount,
        RegistrationContact contact)
    {
        // 1. Status & date guards (same as RegisterWithAttendees).
        if (Status != EventStatus.Published)
            return Result.Failure("Cannot register for unpublished event");

        if (StartDate <= DateTime.UtcNow)
            return Result.Failure("Cannot register for an event that has already started");

        // 2. Mode guard (defensive — the handler also dispatches by mode, but we enforce here too).
        if (RegistrationMode == RegistrationMode.DetailedAttendees)
            return Result.Failure(
                "This event uses detailed-attendee registration. Use the per-attendee RSVP path.");

        if (RegistrationMode == RegistrationMode.NoRegistration)
            return Result.Failure(
                "Registration is not required for this event. Standalone donations / sponsors / " +
                "add-on purchases / collections are still accepted via their own endpoints.");

        // 3. Argument validation.
        if (string.IsNullOrWhiteSpace(leadAttendeeName))
            return Result.Failure("Lead attendee name is required for head-count registrations");

        if (headCount == null)
            return Result.Failure("Head-count breakdown is required");

        if (contact == null)
            return Result.Failure("Contact information is required");

        // 4. Duplicate registration check — mirror RegisterWithAttendees logic.
        // Phase 6A.XXX FIX: cross-path dup detection (UserId + email).
#pragma warning disable CS0618 // Pending is deprecated but still excluded for back-compat dup check.
        if (userId.HasValue)
        {
            var existingByUserId = _registrations.FirstOrDefault(r =>
                r.UserId == userId &&
                r.Status != RegistrationStatus.Cancelled &&
                r.Status != RegistrationStatus.Refunded &&
                r.Status != RegistrationStatus.RefundRequested &&
                r.Status != RegistrationStatus.Preliminary &&
                r.Status != RegistrationStatus.Abandoned &&
                r.Status != RegistrationStatus.Pending);
            if (existingByUserId != null)
                return Result.Failure(
                    "You are already registered for this event. To change your registration, " +
                    "please cancel the existing one first.");

            var existingByEmail = _registrations.FirstOrDefault(r =>
                r.Contact != null &&
                r.Contact.Email.Equals(contact.Email, StringComparison.OrdinalIgnoreCase) &&
                r.Status != RegistrationStatus.Cancelled &&
                r.Status != RegistrationStatus.Refunded &&
                r.Status != RegistrationStatus.RefundRequested &&
                r.Status != RegistrationStatus.Preliminary &&
                r.Status != RegistrationStatus.Abandoned &&
                r.Status != RegistrationStatus.Pending);
            if (existingByEmail != null)
                return Result.Failure(
                    "This email is already registered for this event. Each email can only register once.");
        }
        else
        {
            var existingByEmail = _registrations.FirstOrDefault(r =>
                ((r.Contact != null && r.Contact.Email.Equals(contact.Email, StringComparison.OrdinalIgnoreCase)) ||
                 (r.AttendeeInfo != null && r.AttendeeInfo.Email != null &&
                  r.AttendeeInfo.Email.Value.Equals(contact.Email, StringComparison.OrdinalIgnoreCase))) &&
                r.Status != RegistrationStatus.Cancelled &&
                r.Status != RegistrationStatus.Refunded &&
                r.Status != RegistrationStatus.RefundRequested &&
                r.Status != RegistrationStatus.Preliminary &&
                r.Status != RegistrationStatus.Abandoned &&
                r.Status != RegistrationStatus.Pending);
            if (existingByEmail != null)
                return Result.Failure(
                    "This email is already registered for this event. Each email can only register once.");
        }
#pragma warning restore CS0618

        // 5. MaxAttendeesPerRegistration guard — applies to head-count Total just as it does
        //    to Attendees.Count (per-architect: cap applies to both).
        var effectiveMax = Math.Min(MaxAttendeesPerRegistration, SYSTEM_MAX_ATTENDEES_PER_REGISTRATION);
        if (headCount.Total > effectiveMax)
            return Result.Failure(
                $"Maximum {effectiveMax} attendees per registration. Requested: {headCount.Total}.");

        // 6. Capacity guard — capacity check uses HeadCount.Total via Registration.GetAttendeeCount.
        if (!HasCapacityFor(headCount.Total))
            return Result.Failure(
                $"Event does not have enough capacity for {headCount.Total} attendees. " +
                $"Available: {Capacity - ReservedCapacity}.");

        // 7. Phase 7E.3a scope: free events ONLY. Paid path lands in 7E.3b alongside Stripe
        //    amount-calc tests for HeadCountByAge / TierCounts.
        if (!IsFree())
            return Result.Failure(
                "Paid B-mode RSVP arrives in Phase 7E.3b (Stripe amount-calc tests required). " +
                "For now, switch the event to free attendance to use head-count registration.");

        // 8. Build the registration via the dedicated factory.
        // Currency on a free registration's TotalPrice is informational only; default to USD.
        var freePriceResult = Money.Create(0m, Currency.USD);
        if (freePriceResult.IsFailure)
            return Result.Failure(freePriceResult.Errors); // unreachable but type-safe.

        var registrationResult = Registration.CreateWithHeadCount(
            Id, userId, RegistrationMode,
            leadAttendeeName.Trim(),
            headCount, contact,
            freePriceResult.Value,
            isPaidEvent: false);

        if (registrationResult.IsFailure)
            return Result.Failure(registrationResult.Errors);

        _registrations.Add(registrationResult.Value);
        MarkAsUpdated();

        // 9. Raise the same domain events as RegisterWithAttendees so downstream email / WhatsApp
        //    handlers continue to fire identically — they read RegistrationMode + HeadCount from
        //    the loaded registration when rendering content (lands in 7E.4 templates).
        var attendeeCount = headCount.Total;
        if (userId.HasValue)
        {
            RaiseDomainEvent(new RegistrationConfirmedEvent(Id, userId.Value, attendeeCount, DateTime.UtcNow));
        }
        else
        {
            RaiseDomainEvent(new AnonymousRegistrationConfirmedEvent(Id, contact.Email, attendeeCount, DateTime.UtcNow));
        }

        return Result.Success();
    }
}
