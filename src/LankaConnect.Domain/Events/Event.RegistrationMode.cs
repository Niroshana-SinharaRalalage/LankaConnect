using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
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
        // Architect rule (Phase 7E plan §3.2): forbid mode change while ACTIVE registrations
        // exist — those are the rows that would need attendee backfill on A↔B conversion.
        // Cancelled / Refunded / Abandoned registrations are historical-only: they don't
        // consume capacity and have nothing to backfill. Counting them caused the bug where
        // an event showing "0 registered / 75 spots left" on the dashboard refused mode-change
        // with "Existing registrations: 30" — the dashboard's CurrentRegistrations and the
        // mode-change guard were using different definitions of "exists".
        //
        // Active = anything not in {Cancelled, Refunded, Abandoned}.
        // - Confirmed / Waitlisted / CheckedIn / Attended: live, would need backfill.
        // - Preliminary: awaiting payment; if it completes, it'd carry the wrong mode shape.
        // - RefundRequested: still consumes capacity until refund completes.
        // - Pending (deprecated): treat like Preliminary for safety.
#pragma warning disable CS0618 // Pending is deprecated but still excluded for back-compat.
        var activeRegistrations = _registrations
            .Where(r => r.Status != RegistrationStatus.Cancelled &&
                        r.Status != RegistrationStatus.Refunded &&
                        r.Status != RegistrationStatus.Abandoned)
            .ToList();
#pragma warning restore CS0618

        if (activeRegistrations.Count > 0)
        {
            // Phase 7E follow-up: surface the blocking status breakdown so the organiser can act.
            // The dashboard's CurrentRegistrations only shows Confirmed → if the dashboard says
            // "0 registered" but this guard fires, the row is in an intermediate state
            // (Preliminary stuck Stripe checkout / Waitlisted / RefundRequested awaiting webhook).
            var byStatus = string.Join(", ",
                activeRegistrations
                    .GroupBy(r => r.Status)
                    .OrderBy(g => g.Key.ToString())
                    .Select(g => $"{g.Count()} {g.Key}"));

            return Result.Failure(
                $"Cannot change registration mode while active registrations exist " +
                $"({byStatus}). Cancel or wait for these to resolve before changing the mode. " +
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

        // 7. Per-tier capacity reservation (Phase 7E.3c, architect edit #2): for tiered events
        //    with TierCounts, reserve capacity per tier BEFORE pricing. Mirrors Mode A's
        //    behaviour at Event.cs:444-451. Atomic — if any tier reservation fails the whole
        //    RSVP rejects (no partial reserve held). Applies to free + paid tiered events:
        //    even free tiered events need to prevent over-selling a tier.
        if (TicketingMode == Enums.TicketingMode.Tiered)
        {
            if (headCount.TierCounts == null || headCount.TierCounts.Count == 0)
                return Result.Failure(
                    "Tiered events require TierCounts on the head-count payload. " +
                    "Specify counts per tier (e.g. VIP × 2, General × 3).");

            // Validate all tier IDs exist before reserving — fail-fast prevents partial state.
            // Phase 7F-C: also reject any tier whose ChildPrice isn't configured but the
            // payload claims ChildCount > 0 — otherwise CalculatePriceForAttendee(Child) would
            // silently fall back to AdultPrice and the user would be UNDER-charged.
            foreach (var tc in headCount.TierCounts)
            {
                var matchedTier = _ticketTiers.FirstOrDefault(t => t.Id == tc.TierId);
                if (matchedTier == null)
                    return Result.Failure(
                        $"Ticket tier {tc.TierId} not found on this event. RegistrationId not yet created — no rollback needed.");

                if (tc.HasAgeSplit && (tc.ChildCount ?? 0) > 0 && !matchedTier.HasChildPricing)
                    return Result.Failure(
                        $"Tier '{matchedTier.Name}' has no child pricing configured but the registration " +
                        $"claims {tc.ChildCount} children in this tier. Either configure a ChildPrice on the tier " +
                        $"or remove the age split from this tier's count (children would otherwise be billed at AdultPrice).");
            }

            // Reserve atomically. If any reserve fails, prior reserves on this call are lost
            // (no rollback path exists today — same limitation Mode A has). For 7E.3c the
            // pre-validation above + the fail-fast on the first failure keeps blast radius small.
            foreach (var tc in headCount.TierCounts)
            {
                var tier = _ticketTiers.First(t => t.Id == tc.TierId);
                var reserveResult = tier.Reserve(tc.Count);
                if (reserveResult.IsFailure)
                    return Result.Failure(reserveResult.Errors);
            }
        }

        // 8. Pricing — Phase 7E.3b paid B-mode + Phase 7E.3c TierCounts. Free events get
        //    Money.Zero(USD); paid events get the appropriate pricing (single, dual, or
        //    tiered) based on the event's pricing configuration.
        var priceResult = CalculateHeadCountPrice(headCount);
        if (priceResult.IsFailure)
            return Result.Failure(priceResult.Errors);
        var totalPrice = priceResult.Value;

        var isPaidEvent = !IsFree();

        var registrationResult = Registration.CreateWithHeadCount(
            Id, userId, RegistrationMode,
            leadAttendeeName.Trim(),
            headCount, contact,
            totalPrice,
            isPaidEvent: isPaidEvent);

        if (registrationResult.IsFailure)
            return Result.Failure(registrationResult.Errors);

        _registrations.Add(registrationResult.Value);
        MarkAsUpdated();

        // 8. Raise domain events. For free events the registration goes straight to Confirmed
        //    and we raise the confirmation event here. For paid events the row is Preliminary
        //    until the Stripe webhook fires; CompletePayment() raises the confirmation event
        //    at that point. Mirrors Mode A's behaviour exactly (see RegisterWithAttendees).
        if (registrationResult.Value.Status == RegistrationStatus.Confirmed)
        {
            var attendeeCount = headCount.Total;
            if (userId.HasValue)
            {
                RaiseDomainEvent(new RegistrationConfirmedEvent(Id, userId.Value, attendeeCount, DateTime.UtcNow));
            }
            else
            {
                RaiseDomainEvent(new AnonymousRegistrationConfirmedEvent(Id, contact.Email, attendeeCount, DateTime.UtcNow));
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Phase 7E.3b: pricing helper for head-count (Mode B) registrations. Mirrors Mode A's
    /// <see cref="CalculatePriceForAttendees"/> shape but operates on the head-count breakdown
    /// instead of a per-attendee list.
    ///
    /// Pricing logic (matches plan §3 + architect review iteration 1):
    /// <list type="bullet">
    /// <item>Free event → <c>Money.Zero(USD)</c> (currency informational).</item>
    /// <item>Tiered ticketing → REJECT (<see cref="RegistrationModeErrorCodes.PaidHeadCountTiersDeferred"/>) until 7E.3c.</item>
    /// <item>TierCounts axis present → REJECT (same gate).</item>
    /// <item>GroupTiered pricing → <c>Pricing.CalculateGroupPrice(headCount.Total)</c> (per-architect: parity with Mode A).</item>
    /// <item>AgeDual + B2 (HeadCountByAge) → adults × adultPrice + children × childPrice.</item>
    /// <item>AgeDual + B4 (HeadCountByAgeAndGender) → (AM+AF) × adultPrice + (CM+CF) × childPrice.</item>
    /// <item>AgeDual + B1/B3 → REJECT defensively (validator already excludes; defence-in-depth).</item>
    /// <item>Standard / single price + any B → Total × ticketPrice.</item>
    /// </list>
    /// </summary>
    private Result<Money> CalculateHeadCountPrice(HeadCountBreakdown headCount)
    {
        if (headCount == null)
            return Result<Money>.Failure("Head-count breakdown is required");

        // Free → zero. Currency is informational on a $0 registration.
        if (IsFree())
        {
            var zero = Money.Create(0m, Currency.USD);
            return zero.IsSuccess ? Result<Money>.Success(zero.Value) : Result<Money>.Failure(zero.Errors);
        }

        // Defensive: paid event must have pricing configured.
        if (!IsFreeEvent && Pricing == null && TicketPrice == null)
            return Result<Money>.Failure(
                "Paid event pricing is not configured. Use SetPricing(), SetDualPricing(), or SetGroupPricing().");

        // Phase 7E.3c (2026-04-29): lifted the PaidHeadCountTiersDeferred gates. Tiered
        // ticketing now uses TierCounts pricing; standalone TierCounts (without tiered
        // ticketing) is still rejected because there's no tier price to look up.
        if (headCount.TierCounts != null && headCount.TierCounts.Count > 0
            && TicketingMode != Enums.TicketingMode.Tiered)
            return Result<Money>.Failure(
                "TierCounts can only be used with TicketingMode.Tiered events. " +
                "Remove TierCounts from the head-count payload, or configure tiered ticketing on the event.");

        // Tiered ticketing → use TierCounts pricing path. Free + tiered short-circuited
        // earlier (IsFree() returned zero); reaching here means paid + tiered.
        if (TicketingMode == Enums.TicketingMode.Tiered)
            return CalculateTierCountsPrice(headCount.TierCounts);

        // Group-tiered pricing — single tier price covers everyone in the basket; calculator
        // chooses the correct tier from Total.
        if (Pricing != null && Pricing.Type == Enums.PricingType.GroupTiered)
        {
            var groupResult = Pricing.CalculateGroupPrice(headCount.Total);
            return groupResult.IsSuccess
                ? Result<Money>.Success(groupResult.Value)
                : Result<Money>.Failure(groupResult.Error);
        }

        // Dual pricing (AgeDual): adults / children counts must be derivable from this mode.
        if (Pricing != null && Pricing.Type == Enums.PricingType.AgeDual)
        {
            int adults;
            int children;
            switch (RegistrationMode)
            {
                case RegistrationMode.HeadCountByAge:
                    var demoB2 = headCount.Demographics;
                    if (demoB2 == null)
                        return Result<Money>.Failure(
                            "HeadCountByAge requires a demographic breakdown with adults/children counts.");
                    adults = demoB2.Adults ?? 0;
                    children = demoB2.Children ?? 0;
                    break;

                case RegistrationMode.HeadCountByAgeAndGender:
                    var demoB4 = headCount.Demographics;
                    if (demoB4 == null)
                        return Result<Money>.Failure(
                            "HeadCountByAgeAndGender requires the 4-leaf demographic breakdown.");
                    adults = (demoB4.AdultMales ?? 0) + (demoB4.AdultFemales ?? 0);
                    children = (demoB4.ChildMales ?? 0) + (demoB4.ChildFemales ?? 0);
                    break;

                case RegistrationMode.HeadCountOnly:
                case RegistrationMode.HeadCountByGender:
                    // Defensive — validator excludes these combos. If reached, it's a bug.
                    return Result<Money>.Failure(
                        $"{RegistrationMode} cannot be used with dual pricing — adult/child counts are not " +
                        "captured by this mode. The compatibility validator should have rejected this combination.");

                default:
                    return Result<Money>.Failure(
                        $"Unsupported RegistrationMode for head-count pricing: {RegistrationMode}");
            }

            var adultMoney = Pricing.CalculateForCategory(Enums.AgeCategory.Adult);
            var childMoney = Pricing.CalculateForCategory(Enums.AgeCategory.Child);

            // Build the total: adults × adultPrice + children × childPrice.
            var adultsTotalResult = adultMoney.Multiply(adults);
            if (adultsTotalResult.IsFailure)
                return Result<Money>.Failure(adultsTotalResult.Errors);

            var childrenTotalResult = childMoney.Multiply(children);
            if (childrenTotalResult.IsFailure)
                return Result<Money>.Failure(childrenTotalResult.Errors);

            var sumResult = adultsTotalResult.Value.Add(childrenTotalResult.Value);
            return sumResult.IsSuccess
                ? Result<Money>.Success(sumResult.Value)
                : Result<Money>.Failure(sumResult.Errors);
        }

        // Standard / single price (Pricing.Type == Standard OR legacy TicketPrice fallback).
        var unitPrice = Pricing?.AdultPrice ?? TicketPrice;
        if (unitPrice == null)
            return Result<Money>.Failure("Event pricing is not configured");

        var multiplyResult = unitPrice.Multiply(headCount.Total);
        return multiplyResult.IsSuccess
            ? Result<Money>.Success(multiplyResult.Value)
            : Result<Money>.Failure(multiplyResult.Errors);
    }

    /// <summary>
    /// Phase 7F-C (architect-approved single-shape refactor 2026-04-30): TierCounts pricing
    /// for paid Mode-B RSVP. Mirrors Mode A's
    /// <see cref="Event.CalculateTieredPriceForAttendees"/> — routes through
    /// <see cref="TicketTier.CalculatePriceForAttendee"/> per attendee category, so adults
    /// pay <c>tier.AdultPrice</c> and children pay <c>tier.ChildPrice</c> (when the tier has
    /// child pricing configured).
    ///
    /// Single-shape derivation:
    /// <code>
    /// adultCount = tc.AdultCount ?? tc.Count   // legacy null-axis path → all "adults" for pricing
    /// childCount = tc.ChildCount ?? 0
    /// lineTotal  = tier.AdultPrice × adultCount + tier.CalculatePriceForAttendee(Child) × childCount
    /// </code>
    /// Legacy 7E.3c (B1 / B3 / B-without-age-split) registrations — where both
    /// <see cref="TierCount.AdultCount"/> and <see cref="TierCount.ChildCount"/> are null —
    /// keep producing <c>AdultPrice × Count</c> per architect Q7 ("legacy null-axis stays
    /// a valid choice indefinitely"). The architect-required Mode A vs Mode B parity test
    /// in <c>Phase7FCTierAgeMatrixPricingTests</c> asserts that the new shape produces the
    /// same Money for the same basket as Mode A.
    ///
    /// Pre-condition (architect edit #8 in 7F-C plan): <see cref="RegisterWithHeadCount"/>
    /// already rejected any (tier, ChildCount > 0) where the tier has no <c>ChildPrice</c>,
    /// so this method does not need to re-check — the silent under-charge cannot reach here.
    /// </summary>
    private Result<Money> CalculateTierCountsPrice(IReadOnlyList<TierCount>? tierCounts)
    {
        if (tierCounts == null || tierCounts.Count == 0)
            return Result<Money>.Failure(
                "TierCounts is required when TicketingMode is Tiered. " +
                "Specify counts per tier (e.g. VIP × 2, General × 3).");

        Money? total = null;
        foreach (var tc in tierCounts)
        {
            var tier = _ticketTiers.FirstOrDefault(t => t.Id == tc.TierId);
            if (tier == null)
                return Result<Money>.Failure(
                    $"Ticket tier {tc.TierId} not found on this event. " +
                    "TierCount has gone stale — verify the event's tier list before retrying.");

            var adultCount = tc.AdultCount ?? tc.Count; // legacy null-axis falls back to all-adults pricing
            var childCount = tc.ChildCount ?? 0;

            var adultLineResult = tier.AdultPrice.Multiply(adultCount);
            if (adultLineResult.IsFailure)
                return Result<Money>.Failure(adultLineResult.Errors);

            var childUnitPrice = tier.CalculatePriceForAttendee(Enums.AgeCategory.Child);
            var childLineResult = childUnitPrice.Multiply(childCount);
            if (childLineResult.IsFailure)
                return Result<Money>.Failure(childLineResult.Errors);

            var lineTotalResult = adultLineResult.Value.Add(childLineResult.Value);
            if (lineTotalResult.IsFailure)
                return Result<Money>.Failure(lineTotalResult.Errors);

            if (total == null)
            {
                total = lineTotalResult.Value;
            }
            else
            {
                var addResult = total.Add(lineTotalResult.Value);
                if (addResult.IsFailure)
                    return Result<Money>.Failure(addResult.Errors);
                total = addResult.Value;
            }
        }

        return Result<Money>.Success(total!);
    }
}
