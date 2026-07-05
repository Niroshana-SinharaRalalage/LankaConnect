using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Queries.CalculateAdditionPrice;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.InitiateAddAttendees;

/// <summary>
/// Handler for InitiateAddAttendeesCommand.
/// Creates a RegistrationAddition entity and Stripe checkout session.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public class InitiateAddAttendeesCommandHandler
    : ICommandHandler<InitiateAddAttendeesCommand, InitiateAddAttendeesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IRegistrationAdditionRepository _additionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IMediator _mediator;
    private readonly ILogger<InitiateAddAttendeesCommandHandler> _logger;

    public InitiateAddAttendeesCommandHandler(
        IApplicationDbContext context,
        IRegistrationAdditionRepository additionRepository,
        IEventRepository eventRepository,
        IStripePaymentService stripePaymentService,
        IMediator mediator,
        ILogger<InitiateAddAttendeesCommandHandler> logger)
    {
        _context = context;
        _additionRepository = additionRepository;
        _eventRepository = eventRepository;
        _stripePaymentService = stripePaymentService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<InitiateAddAttendeesResult>> Handle(
        InitiateAddAttendeesCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "InitiateAddAttendees"))
        using (LogContext.PushProperty("EntityType", "RegistrationAddition"))
        using (LogContext.PushProperty("RegistrationId", request.RegistrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[AddOnlyAttendees] InitiateAddAttendees START - RegistrationId={RegistrationId}, NewAttendeesCount={NewAttendeesCount}",
                request.RegistrationId, request.NewAttendees?.Count ?? 0);

            try
            {
                // Early validation - ensure we have attendees to add
                if (request.NewAttendees == null || request.NewAttendees.Count == 0)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "[AddOnlyAttendees] No attendees provided - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);
                    return Result<InitiateAddAttendeesResult>.Success(
                        InitiateAddAttendeesResult.Failed("At least one attendee is required"));
                }

                // Phase 8 S8.2.C: Reject add-attendees on AssignedSeating events upfront.
                // The buyer's existing seat assignments are bound on payment completion via
                // RegistrationWebhookHandler.HandleCheckoutCompletedAsync; adding new attendees
                // would require new seat selections + holds + reservations — that lifecycle
                // is delivered in Slice S9 per ADR-011. Reject before pricing/checkout work
                // so the buyer doesn't burn a Stripe session.
                var registrationForSeatingCheck = await _context.Registrations
                    .Where(r => r.Id == request.RegistrationId)
                    .Select(r => new { r.Id, r.EventId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (registrationForSeatingCheck != null)
                {
                    var eventForSeatingCheck = await _eventRepository.GetByIdAsync(
                        registrationForSeatingCheck.EventId,
                        trackChanges: false,
                        cancellationToken);

                    if (eventForSeatingCheck != null && eventForSeatingCheck.SeatingMode == SeatingMode.AssignedSeating)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "[AddOnlyAttendees] [S8.2.C] Rejected - AssignedSeating events cannot add attendees yet (Slice S9). RegistrationId={RegistrationId}, EventId={EventId}, Duration={ElapsedMs}ms",
                            request.RegistrationId, registrationForSeatingCheck.EventId, stopwatch.ElapsedMilliseconds);
                        return Result<InitiateAddAttendeesResult>.Success(
                            InitiateAddAttendeesResult.Failed(
                                "Add-attendees not yet supported for seated events — coming in Slice S9."));
                    }
                }

                // Step 1: Calculate pricing using the CalculateAdditionPrice query
                var priceQuery = new CalculateAdditionPriceQuery(request.RegistrationId, request.NewAttendees);
                var priceResult = await _mediator.Send(priceQuery, cancellationToken);

                if (priceResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "[AddOnlyAttendees] Price calculation failed - RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.RegistrationId, priceResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<InitiateAddAttendeesResult>.Success(
                        InitiateAddAttendeesResult.Failed(priceResult.Error));
                }

                var pricing = priceResult.Value;

                // Check if pricing result indicates validation errors
                if (!pricing.IsValid)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "[AddOnlyAttendees] Pricing validation failed - RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.RegistrationId, pricing.ErrorMessage, stopwatch.ElapsedMilliseconds);
                    return Result<InitiateAddAttendeesResult>.Success(
                        InitiateAddAttendeesResult.Failed(pricing.ErrorMessage ?? "Validation failed"));
                }

                // Step 2: Get registration for contact info
                var registration = await _context.Registrations
                    .Where(r => r.Id == request.RegistrationId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (registration == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "[AddOnlyAttendees] Registration not found - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);
                    return Result<InitiateAddAttendeesResult>.Success(
                        InitiateAddAttendeesResult.Failed("Registration not found"));
                }

                // Step 3: Create AttendeeDetails from NewAttendeeDto
                var newAttendees = request.NewAttendees
                    .Select(a => AttendeeDetails.Create(a.Name, a.AgeCategory, a.Gender))
                    .Where(r => r.IsSuccess)
                    .Select(r => r.Value)
                    .ToList();

                if (newAttendees.Count != request.NewAttendees.Count)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "[AddOnlyAttendees] Some attendee details failed validation - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);
                    return Result<InitiateAddAttendeesResult>.Success(
                        InitiateAddAttendeesResult.Failed("One or more attendee details are invalid"));
                }

                // Step 4: Create Money value objects
                var currency = Enum.Parse<LankaConnect.Domain.Shared.Enums.Currency>(pricing.Currency, true);
                var previousTotalPriceResult = Money.Create(pricing.CurrentTotalPaid, currency);
                var newTotalPriceResult = Money.Create(pricing.NewTotalPrice, currency);
                var additionalAmountResult = Money.Create(pricing.AdditionalAmount, currency);

                if (previousTotalPriceResult.IsFailure || newTotalPriceResult.IsFailure || additionalAmountResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        "[AddOnlyAttendees] Failed to create Money objects - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);
                    return Result<InitiateAddAttendeesResult>.Success(
                        InitiateAddAttendeesResult.Failed("Failed to process pricing"));
                }

                // Step 5: Create RegistrationAddition entity
                var additionResult = RegistrationAddition.Create(
                    request.RegistrationId,
                    pricing.EventId,
                    newAttendees,
                    previousTotalPriceResult.Value,
                    newTotalPriceResult.Value,
                    additionalAmountResult.Value);

                if (additionResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "[AddOnlyAttendees] Failed to create RegistrationAddition - RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.RegistrationId, additionResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<InitiateAddAttendeesResult>.Success(
                        InitiateAddAttendeesResult.Failed(additionResult.Error));
                }

                var addition = additionResult.Value;

                // Step 6: Create Stripe Checkout Session
                var checkoutRequest = new CreateAdditionCheckoutSessionRequest
                {
                    RegistrationId = request.RegistrationId,
                    RegistrationAdditionId = addition.Id,
                    EventId = pricing.EventId,
                    EventTitle = pricing.EventTitle,
                    Amount = pricing.AdditionalAmount,
                    Currency = pricing.Currency,
                    NewAttendeesCount = newAttendees.Count,
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    ContactEmail = registration.Contact?.Email,
                    UserId = request.UserId
                };

                var checkoutResult = await _stripePaymentService.CreateAdditionCheckoutSessionAsync(
                    checkoutRequest, cancellationToken);

                if (checkoutResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        "[AddOnlyAttendees] Failed to create Stripe checkout - RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.RegistrationId, checkoutResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<InitiateAddAttendeesResult>.Success(
                        InitiateAddAttendeesResult.Failed($"Failed to create payment session: {checkoutResult.Error}"));
                }

                var checkout = checkoutResult.Value;

                // Step 7: Update RegistrationAddition with checkout session info
                var setSessionResult = addition.SetStripeCheckoutSession(
                    checkout.SessionId,
                    checkout.ExpiresAt);

                if (setSessionResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        "[AddOnlyAttendees] Failed to set checkout session on addition - RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.RegistrationId, setSessionResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<InitiateAddAttendeesResult>.Success(
                        InitiateAddAttendeesResult.Failed("Failed to link payment session"));
                }

                // Step 8: Save RegistrationAddition to database
                await _additionRepository.AddAsync(addition, cancellationToken);
                await _context.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "[AddOnlyAttendees] InitiateAddAttendees SUCCESS - RegistrationId={RegistrationId}, AdditionId={AdditionId}, " +
                    "CheckoutSessionId={CheckoutSessionId}, AdditionalAmount={Amount} {Currency}, Duration={ElapsedMs}ms",
                    request.RegistrationId,
                    addition.Id,
                    checkout.SessionId,
                    pricing.AdditionalAmount,
                    pricing.Currency,
                    stopwatch.ElapsedMilliseconds);

                return Result<InitiateAddAttendeesResult>.Success(
                    InitiateAddAttendeesResult.Successful(
                        addition.Id,
                        checkout.SessionId,
                        checkout.CheckoutUrl,
                        checkout.ExpiresAt,
                        pricing.AdditionalAmount,
                        pricing.Currency,
                        newAttendees.Count));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "[AddOnlyAttendees] InitiateAddAttendees FAILED - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.RegistrationId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
