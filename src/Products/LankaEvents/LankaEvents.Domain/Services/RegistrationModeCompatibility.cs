using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Domain.Services;

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

        // Phase 8X.11 — ExternalPaid events accept ONLY RegistrationMode.External.
        // Every other mode (DetailedAttendees / HeadCount* / NoRegistration) is rejected
        // for ExternalPaid because they all imply internal Registration rows that
        // ExternalPaid events don't have.
        if (context.PaymentMode == EventPaymentMode.ExternalPaid && mode != RegistrationMode.External)
        {
            return Result.Failure(
                "External-paid events use the External registration mode (registration handled " +
                "by the external site). Other registration modes capture internal attendee data " +
                "which doesn't apply here.");
        }

        // Conversely, External mode requires PaymentMode = ExternalPaid.
        if (mode == RegistrationMode.External && context.PaymentMode != EventPaymentMode.ExternalPaid)
        {
            return Result.Failure(
                "External registration mode requires PaymentMode = ExternalPaid. " +
                "Set the event to 'Paid — external registration link' first, then choose External mode.");
        }

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
                // Mode A is always allowed for non-ExternalPaid events (the early-return above
                // already handled the ExternalPaid case).
                return Result.Success();

            case RegistrationMode.External:
                // External is allowed iff PaymentMode == ExternalPaid (early-return above
                // already handled the negative case).
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

        // 2026-04-29: PaidHeadCountDeferred gate LIFTED in Phase 7E.3b — paid B-mode + Stripe
        // checkout now ships single-price + dual-price (Adult/Child) paths. TierCounts axis
        // is still gated; that gate lives in Event.RegisterWithHeadCount + the pricing helper
        // (search for PaidHeadCountTiersDeferred). The RegistrationModeErrorCodes.PaidHeadCountDeferred
        // constant remains for one release as a no-op (caller back-compat).

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
