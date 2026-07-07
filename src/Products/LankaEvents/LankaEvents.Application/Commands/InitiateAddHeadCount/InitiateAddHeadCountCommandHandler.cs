using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Commands.InitiateAddAttendees;
using LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.InitiateAddHeadCount;

/// <summary>
/// Phase 7F-D.3 (architect-approved 2026-04-30): handler for Mode-B add-headcount
/// initiation. Mirrors <c>InitiateAddAttendeesCommandHandler</c> at the contract level
/// but operates on the head-count axis. Free Mode-B additions short-circuit Stripe
/// (architect §2.5 — same code path as free Mode-A: AdditionalAmount = 0 → no checkout
/// session, merge happens directly).
///
/// Architect Q8 (single pending addition per registration): enforced by the existing
/// partial unique index <c>uq_registration_additions_one_pending_per_registration</c>
/// at the DB level + a fail-fast check in this handler so the user gets a clean 400
/// instead of a constraint violation.
/// </summary>
public class InitiateAddHeadCountCommandHandler
    : ICommandHandler<InitiateAddHeadCountCommand, InitiateAddAttendeesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationAdditionRepository _additionRepository;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InitiateAddHeadCountCommandHandler> _logger;

    public InitiateAddHeadCountCommandHandler(
        IApplicationDbContext context,
        IEventRepository eventRepository,
        IRegistrationAdditionRepository additionRepository,
        IStripePaymentService stripePaymentService,
        IUnitOfWork unitOfWork,
        ILogger<InitiateAddHeadCountCommandHandler> logger)
    {
        _context = context;
        _eventRepository = eventRepository;
        _additionRepository = additionRepository;
        _stripePaymentService = stripePaymentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<InitiateAddAttendeesResult>> Handle(
        InitiateAddHeadCountCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "InitiateAddHeadCount"))
        using (LogContext.PushProperty("RegistrationId", request.RegistrationId))
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("[7F-D] InitiateAddHeadCount START — RegId={RegId}", request.RegistrationId);

            try
            {
                // 1. Argument validation.
                if (request.HeadCountDelta == null)
                    return Ok(InitiateAddAttendeesResult.Failed("Head-count delta is required"));

                // 2. Load the registration + parent event.
                var registration = await _context.Registrations
                    .FirstOrDefaultAsync(r => r.Id == request.RegistrationId, cancellationToken);
                if (registration == null)
                    return Ok(InitiateAddAttendeesResult.Failed("Registration not found"));

                if (registration.Status != RegistrationStatus.Confirmed)
                    return Ok(InitiateAddAttendeesResult.Failed(
                        $"Registration must be Confirmed to add attendees. Current status: {registration.Status}"));

                if (registration.PaymentStatus != PaymentStatus.Completed)
                    return Ok(InitiateAddAttendeesResult.Failed(
                        $"Registration must have a completed payment. Current status: {registration.PaymentStatus}"));

                if (!IsHeadCountMode(registration.RegistrationMode))
                    return Ok(InitiateAddAttendeesResult.Failed(
                        $"This registration is in {registration.RegistrationMode}. Use /add-attendees for Mode A registrations."));

                if (registration.HeadCount == null)
                    return Ok(InitiateAddAttendeesResult.Failed(
                        "Registration has no head-count populated; cannot compute the merged shape."));

                // 3. Architect Q8: reject if a pending addition already exists.
                var existingPending = await _additionRepository.GetPendingByRegistrationIdAsync(
                    request.RegistrationId, cancellationToken);
                if (existingPending != null)
                    return Ok(InitiateAddAttendeesResult.Failed(
                        "You have a pending payment for this registration; complete or cancel it before starting a new addition."));

                var @event = await _eventRepository.GetByIdAsync(registration.EventId, trackChanges: false, cancellationToken);
                if (@event == null)
                    return Ok(InitiateAddAttendeesResult.Failed("Parent event not found"));

                // 4. Build the delta head-count using the same factories the RSVP path uses
                //    (mode-aware → invariant enforcement).
                var deltaResult = BuildDeltaHeadCount(registration.RegistrationMode, request.HeadCountDelta);
                if (deltaResult.IsFailure)
                    return Ok(InitiateAddAttendeesResult.Failed(deltaResult.Error));
                var delta = deltaResult.Value;

                // 5. Build the merged head-count + compute pricing via Event's pricing helper
                //    (architect §2.3 — single source of truth, no fork).
                var mergedResult = MergeHeadCount(registration.HeadCount!, delta);
                if (mergedResult.IsFailure)
                    return Ok(InitiateAddAttendeesResult.Failed(mergedResult.Error));
                var merged = mergedResult.Value;

                var newPriceResult = @event.CalculatePriceForHeadCount(merged);
                if (newPriceResult.IsFailure)
                    return Ok(InitiateAddAttendeesResult.Failed(newPriceResult.Error));
                var newTotal = newPriceResult.Value;

                var previousTotal = registration.TotalPrice ?? Money.Create(0m, newTotal.Currency).Value;
                var deltaAmount = newTotal.Amount - previousTotal.Amount;
                if (deltaAmount < 0)
                    return Ok(InitiateAddAttendeesResult.Failed(
                        "Computed delta amount is negative — refund-on-shrink is out of scope (Phase 7F-D §5)."));

                var deltaMoneyResult = Money.Create(deltaAmount, newTotal.Currency);
                if (deltaMoneyResult.IsFailure)
                    return Ok(InitiateAddAttendeesResult.Failed(deltaMoneyResult.Error));

                // 6. Create the RegistrationAddition (Mode-B factory).
                var additionResult = RegistrationAddition.CreateForHeadCountDelta(
                    registrationId: request.RegistrationId,
                    eventId: registration.EventId,
                    mode: registration.RegistrationMode,
                    headCountDelta: delta,
                    previousTotal: previousTotal,
                    newTotal: newTotal,
                    additionalAmount: deltaMoneyResult.Value);
                if (additionResult.IsFailure)
                    return Ok(InitiateAddAttendeesResult.Failed(additionResult.Error));
                var addition = additionResult.Value;

                // 7. Persist the addition row first so the FK in the Stripe metadata is valid.
                await _additionRepository.AddAsync(addition, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "[7F-D] Addition created — AdditionId={AdditionId} DeltaTotal={Delta} AdditionalAmount={Amount} {Currency}",
                    addition.Id, delta.Total, deltaAmount, newTotal.Currency);

                // 8. Free path (architect §2.5): zero amount → skip Stripe, complete payment
                //    + merge directly. Mirrors free Mode-A behaviour.
                if (deltaAmount == 0m)
                {
                    var completeResult = addition.CompletePayment("free-no-stripe-" + Guid.NewGuid());
                    if (completeResult.IsFailure)
                        return Ok(InitiateAddAttendeesResult.Failed(completeResult.Error));

                    var mergeResult = registration.MergeHeadCountAddition(
                        registration.RegistrationMode, delta, newTotal,
                        @event.MaxAttendeesPerRegistration);
                    if (mergeResult.IsFailure)
                    {
                        addition.MarkAsFailed();
                        _additionRepository.Update(addition);
                        await _unitOfWork.CommitAsync(cancellationToken);
                        return Ok(InitiateAddAttendeesResult.Failed(mergeResult.Error));
                    }
                    addition.MarkAsMerged();
                    _additionRepository.Update(addition);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Ok(InitiateAddAttendeesResult.Successful(
                        addition.Id, checkoutSessionId: "free-no-stripe", checkoutUrl: "",
                        expiresAt: DateTime.UtcNow,
                        additionalAmount: 0m, currency: newTotal.Currency.ToString(),
                        newAttendeesCount: delta.Total));
                }

                // 9. Paid path: create Stripe checkout session.
                var checkoutResult = await _stripePaymentService.CreateAdditionCheckoutSessionAsync(
                    new CreateAdditionCheckoutSessionRequest
                    {
                        RegistrationId = request.RegistrationId,
                        RegistrationAdditionId = addition.Id,
                        EventId = registration.EventId,
                        EventTitle = @event.Title.Value,
                        Amount = deltaAmount,
                        Currency = newTotal.Currency.ToString(),
                        NewAttendeesCount = delta.Total,
                        SuccessUrl = request.SuccessUrl,
                        CancelUrl = request.CancelUrl,
                        ContactEmail = registration.Contact?.Email,
                        UserId = request.UserId,
                    }, cancellationToken);

                if (checkoutResult.IsFailure)
                {
                    _logger.LogError("[7F-D] Stripe checkout creation failed — {Error}", checkoutResult.Error);
                    return Ok(InitiateAddAttendeesResult.Failed($"Failed to create payment session: {checkoutResult.Error}"));
                }

                addition.SetStripeCheckoutSession(checkoutResult.Value.SessionId, checkoutResult.Value.ExpiresAt);
                _additionRepository.Update(addition);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "[7F-D] InitiateAddHeadCount SUCCESS — AdditionId={AdditionId} Stripe={SessionId} Duration={Ms}ms",
                    addition.Id, checkoutResult.Value.SessionId, stopwatch.ElapsedMilliseconds);

                return Ok(InitiateAddAttendeesResult.Successful(
                    addition.Id,
                    checkoutResult.Value.SessionId,
                    checkoutResult.Value.CheckoutUrl,
                    checkoutResult.Value.ExpiresAt,
                    deltaAmount,
                    newTotal.Currency.ToString(),
                    delta.Total));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[7F-D] InitiateAddHeadCount FAILED — RegId={RegId} Duration={Ms}ms",
                    request.RegistrationId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private static Result<InitiateAddAttendeesResult> Ok(InitiateAddAttendeesResult result)
        => Result<InitiateAddAttendeesResult>.Success(result);

    private static bool IsHeadCountMode(RegistrationMode mode) =>
        mode == RegistrationMode.HeadCountOnly
        || mode == RegistrationMode.HeadCountByAge
        || mode == RegistrationMode.HeadCountByGender
        || mode == RegistrationMode.HeadCountByAgeAndGender;

    /// <summary>
    /// Builds the delta <see cref="HeadCountBreakdown"/> from the DTO using the mode-
    /// specific factory the RSVP path also uses — invariants enforced consistently.
    /// </summary>
    private static Result<HeadCountBreakdown> BuildDeltaHeadCount(
        RegistrationMode mode, HeadCountDto dto)
    {
        IReadOnlyList<TierCount>? tiers = null;
        if (dto.TierCounts != null && dto.TierCounts.Count > 0)
        {
            // The handler doesn't have access to live tiers (event isn't loaded for tier validation
            // here — that's the caller's job upstream). For the delta we trust the DTO; the
            // domain factories validate the count/sum invariants.
            tiers = dto.TierCounts
                .Select(tc => TierCount.Create(
                    tc.TierId, "tier-snapshot", tc.Count,
                    tc.AdultCount, tc.ChildCount,
                    // Phase 7F-E.7: forward optional per-tier 4-leaf demographic split.
                    tc.AdultMaleCount, tc.AdultFemaleCount,
                    tc.ChildMaleCount, tc.ChildFemaleCount))
                .Where(r => r.IsSuccess)
                .Select(r => r.Value)
                .ToList();
        }

        return mode switch
        {
            RegistrationMode.HeadCountOnly =>
                HeadCountBreakdown.ForTotalOnly(dto.Total ?? 0, tiers),
            RegistrationMode.HeadCountByAge =>
                HeadCountBreakdown.ForByAge(dto.Adults ?? 0, dto.Children ?? 0, tiers),
            RegistrationMode.HeadCountByGender =>
                HeadCountBreakdown.ForByGender(dto.Males ?? 0, dto.Females ?? 0, tiers),
            RegistrationMode.HeadCountByAgeAndGender =>
                HeadCountBreakdown.ForByAgeAndGender(
                    dto.AdultMales ?? 0, dto.AdultFemales ?? 0,
                    dto.ChildMales ?? 0, dto.ChildFemales ?? 0, tiers),
            _ => Result<HeadCountBreakdown>.Failure($"Unsupported registration mode: {mode}"),
        };
    }

    private static Result<HeadCountBreakdown> MergeHeadCount(
        HeadCountBreakdown existing, HeadCountBreakdown delta)
    {
        // Same merge logic the domain method uses — kept here for the pre-payment price
        // calculation. Keep it simple: total + demographics + tier counts.
        var newTotal = existing.Total + delta.Total;

        if (existing.Demographics == null && delta.Demographics == null)
            return HeadCountBreakdown.ForTotalOnly(newTotal, MergeTiers(existing.TierCounts, delta.TierCounts));

        // Pick the family from the existing registration (mode-match invariant ensures consistency).
        if (existing.Demographics?.Adults != null || existing.Demographics?.Children != null
            || delta.Demographics?.Adults != null || delta.Demographics?.Children != null)
        {
            return HeadCountBreakdown.ForByAge(
                adults: (existing.Demographics?.Adults ?? 0) + (delta.Demographics?.Adults ?? 0),
                children: (existing.Demographics?.Children ?? 0) + (delta.Demographics?.Children ?? 0),
                MergeTiers(existing.TierCounts, delta.TierCounts));
        }

        if (existing.Demographics?.Males != null || existing.Demographics?.Females != null
            || delta.Demographics?.Males != null || delta.Demographics?.Females != null)
        {
            return HeadCountBreakdown.ForByGender(
                males: (existing.Demographics?.Males ?? 0) + (delta.Demographics?.Males ?? 0),
                females: (existing.Demographics?.Females ?? 0) + (delta.Demographics?.Females ?? 0),
                MergeTiers(existing.TierCounts, delta.TierCounts));
        }

        return HeadCountBreakdown.ForByAgeAndGender(
            adultMales: (existing.Demographics?.AdultMales ?? 0) + (delta.Demographics?.AdultMales ?? 0),
            adultFemales: (existing.Demographics?.AdultFemales ?? 0) + (delta.Demographics?.AdultFemales ?? 0),
            childMales: (existing.Demographics?.ChildMales ?? 0) + (delta.Demographics?.ChildMales ?? 0),
            childFemales: (existing.Demographics?.ChildFemales ?? 0) + (delta.Demographics?.ChildFemales ?? 0),
            MergeTiers(existing.TierCounts, delta.TierCounts));
    }

    private static IReadOnlyList<TierCount>? MergeTiers(
        IReadOnlyList<TierCount>? a, IReadOnlyList<TierCount>? b)
    {
        if (a == null && b == null) return null;
        var byId = new Dictionary<Guid, TierCount>();
        foreach (var tc in a ?? Array.Empty<TierCount>()) byId[tc.TierId] = tc;
        foreach (var tc in b ?? Array.Empty<TierCount>())
        {
            if (byId.TryGetValue(tc.TierId, out var prior))
            {
                var sum = prior.Count + tc.Count;
                int? adult = (prior.AdultCount.HasValue || tc.AdultCount.HasValue)
                    ? (prior.AdultCount ?? prior.Count) + (tc.AdultCount ?? tc.Count)
                    : null;
                int? child = (prior.ChildCount.HasValue || tc.ChildCount.HasValue)
                    ? (prior.ChildCount ?? 0) + (tc.ChildCount ?? 0)
                    : null;
                if (!prior.AdultCount.HasValue && !tc.AdultCount.HasValue) { adult = null; child = null; }
                var rebuilt = TierCount.Create(tc.TierId, tc.TierName, sum, adult, child);
                if (rebuilt.IsSuccess) byId[tc.TierId] = rebuilt.Value;
            }
            else byId[tc.TierId] = tc;
        }
        return byId.Values.ToList();
    }
}
