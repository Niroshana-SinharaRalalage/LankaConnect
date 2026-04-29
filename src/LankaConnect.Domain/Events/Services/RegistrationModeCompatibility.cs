using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Services;

/// <summary>
/// Phase 7E.2: Pure-function domain helper that decides whether a given
/// <see cref="RegistrationMode"/> is compatible with a given event shape, and which
/// modes are allowed for a shape.
///
/// Single source of truth for the 14-row compatibility table from the Phase 7E plan §2.
/// Used by:
/// 1. <c>CreateEventCommandHandler</c> — validate the requested mode at creation time.
/// 2. <c>UpdateEventCommandHandler</c> — validate mode change against new event shape (the
///    "no mode change once registrations exist" rule lives separately on
///    <see cref="Event.SetRegistrationMode"/>).
/// 3. <c>GetAllowedRegistrationModesQueryHandler</c> — drive the frontend mode picker.
///
/// Architectural note: lives in the Domain layer (not Application) because the compatibility
/// rules ARE the domain semantics of registration modes; they would survive any application-
/// or transport-layer rewrite.
///
/// Scope discipline: enforces only the axes that are representable on the current
/// <see cref="Event"/> aggregate. The plan's "named seating", "identity-bound add-on",
/// and "tier × age matrix pricing" axes are not yet modelled — they are tracked as Phase 7F
/// and the corresponding context fields default to <c>false</c> here. When those fields
/// are added in a later slice, the compatibility table is the single point of update.
/// </summary>
public static class RegistrationModeCompatibility
{
    /// <summary>
    /// Returns success if the mode is allowed for the shape, otherwise a Failure with a
    /// user-facing reason string suitable for surfacing through API validation errors.
    /// </summary>
    public static Result Check(RegistrationMode mode, RegistrationModeContext context)
    {
        if (context == null)
            return Result.Failure("Registration mode context is required");

        switch (mode)
        {
            case RegistrationMode.NoRegistration:
                return CheckNoRegistration(context);

            case RegistrationMode.HeadCountOnly:
            case RegistrationMode.HeadCountByGender:
                return CheckHeadCountWithoutAge(mode, context);

            case RegistrationMode.HeadCountByAge:
            case RegistrationMode.HeadCountByAgeAndGender:
                return CheckHeadCountWithAge(mode, context);

            case RegistrationMode.DetailedAttendees:
                // Mode A is always allowed — it's the maximum-info capture and never excluded.
                return Result.Success();

            default:
                return Result.Failure($"Unknown registration mode: {mode}");
        }
    }

    /// <summary>
    /// Returns the full set of allowed modes for the given shape, in enum order.
    /// Always includes at least <see cref="RegistrationMode.DetailedAttendees"/>.
    /// </summary>
    public static IReadOnlyList<RegistrationMode> AllowedModes(RegistrationModeContext context)
    {
        if (context == null)
            return new[] { RegistrationMode.DetailedAttendees };

        var allowed = new List<RegistrationMode>();
        foreach (RegistrationMode mode in Enum.GetValues(typeof(RegistrationMode)))
        {
            if (Check(mode, context).IsSuccess)
                allowed.Add(mode);
        }
        return allowed;
    }

    // ---- private rules below ----

    private static Result CheckNoRegistration(RegistrationModeContext ctx)
    {
        // Mode C requires:
        //  - free attendance (no per-attendee fee — there'd be nothing to charge)
        //  - no seating (the seat block has to be bound to a Registration row, which
        //    Mode C does not produce)
        //  - no per-ticket attendee name (Mode C produces no tickets at all)
        //  - no identity-bound add-ons (no attendee identity to bind to)
        if (!ctx.IsFreeAttendance)
            return Result.Failure(
                "NoRegistration mode requires free attendance. " +
                "Paid attendance has nothing to charge against without a Registration record. " +
                "Either set the event as free, or choose a different registration mode.");

        if (ctx.HasSeating)
            return Result.Failure(
                "NoRegistration mode is not compatible with seating. " +
                "Seat allocations need a Registration row to bind to for cancel/refund. " +
                "Either remove seating, or choose a different registration mode.");

        if (ctx.RequiresAttendeeNameOnTicket)
            return Result.Failure(
                "NoRegistration mode produces no tickets, so 'attendee name required per ticket' " +
                "cannot be honoured. Either remove the per-ticket name requirement, " +
                "or choose a registration mode that issues tickets.");

        if (ctx.HasIdentityBoundAddOn)
            return Result.Failure(
                "NoRegistration mode is not compatible with identity-bound add-ons " +
                "(e.g. an engraved name plate per attendee). " +
                "Identity-bound add-ons require an attendee identity, which Mode C cannot capture.");

        return Result.Success();
    }

    private static Result CheckHeadCountWithoutAge(RegistrationMode mode, RegistrationModeContext ctx)
    {
        // B1 (HeadCountOnly) and B3 (HeadCountByGender) carry no age info.
        // They cannot price dual-pricing events (need adult/child counts).
        if (ctx.HasDualPricing)
            return Result.Failure(
                $"{mode} cannot be used with dual pricing (Adult/Child). " +
                "Adult and child counts cannot be derived from this mode's data. " +
                "Choose A (DetailedAttendees), B2 (HeadCountByAge), or B4 (HeadCountByAgeAndGender).");

        return CheckCommonHeadCountConstraints(mode, ctx);
    }

    private static Result CheckHeadCountWithAge(RegistrationMode mode, RegistrationModeContext ctx)
    {
        // B2 and B4 carry age info (B2 directly, B4 derived from leaves).
        // No additional constraint for dual pricing — both modes work.
        return CheckCommonHeadCountConstraints(mode, ctx);
    }

    private static Result CheckCommonHeadCountConstraints(RegistrationMode mode, RegistrationModeContext ctx)
    {
        // Constraints common to all B modes (B1-B4):

        // PHASE_7E_3B: remove this gate when paid B-mode + Stripe ships.
        // See docs/MASTER_TODO_PHASE_7E_FLEXIBLE_REGISTRATION.md (paid B-mode slice 7E.3b)
        // and docs/MASTER_TODO_PHASE_7E_PAID_BMODE_GATE.md for the full RCA + recovery plan.
        // The validator was written to the full plan §2 but slice 7E.3a only implements
        // FREE B-mode RSVP. Until 7E.3b lands the Stripe checkout path, paid + B is rejected
        // here so the mode picker, update handler, and EventDto.registrationModeStatus all
        // honour the implementation gate consistently. Frontend pattern-matches on the
        // RegistrationModeErrorCodes.PaidHeadCountDeferred constant (NOT this English copy)
        // to render its "coming soon" panel.
        if (!ctx.IsFreeAttendance)
            return Result.Failure(RegistrationModeErrorCodes.PaidHeadCountDeferred);

        if (ctx.RequiresAttendeeNameOnTicket)
            return Result.Failure(
                $"{mode} cannot be used when each ticket must carry a unique attendee name. " +
                "Choose A (DetailedAttendees), or remove the per-ticket name requirement.");

        if (ctx.HasNamedSeating)
            return Result.Failure(
                $"{mode} cannot be used with named seating (each seat assigned to a named attendee). " +
                "Choose A (DetailedAttendees), or switch to auto-allocated block seating.");

        if (ctx.HasIdentityBoundAddOn)
            return Result.Failure(
                $"{mode} cannot be used with identity-bound add-ons (e.g. engraved name plate per attendee). " +
                "Choose A (DetailedAttendees), or switch to count-based add-ons.");

        if (ctx.HasMatrixPricing)
            return Result.Failure(
                $"{mode} cannot capture (tier × age) matrix pricing. " +
                "Choose A (DetailedAttendees). Phase 7F will add matrix support to head-count modes.");

        return Result.Success();
    }
}
