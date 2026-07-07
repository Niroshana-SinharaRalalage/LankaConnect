using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Queries.CalculateAdditionPrice;

/// <summary>
/// Handler for calculating the price of adding new attendees to an existing registration.
/// Supports Single, AgeDual (Adult/Child), and GroupTiered pricing.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public class CalculateAdditionPriceQueryHandler
    : IQueryHandler<CalculateAdditionPriceQuery, AdditionPriceResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationAdditionRepository _additionRepository;
    private readonly ILogger<CalculateAdditionPriceQueryHandler> _logger;

    public CalculateAdditionPriceQueryHandler(
        IApplicationDbContext context,
        IEventRepository eventRepository,
        IRegistrationAdditionRepository additionRepository,
        ILogger<CalculateAdditionPriceQueryHandler> logger)
    {
        _context = context;
        _eventRepository = eventRepository;
        _additionRepository = additionRepository;
        _logger = logger;
    }

    public async Task<Result<AdditionPriceResultDto>> Handle(
        CalculateAdditionPriceQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CalculateAdditionPrice"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("RegistrationId", request.RegistrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CalculateAdditionPrice START: RegistrationId={RegistrationId}, NewAttendeesCount={NewAttendeesCount}",
                request.RegistrationId, request.NewAttendees?.Count ?? 0);

            try
            {
                // Validate request
                if (request.RegistrationId == Guid.Empty)
                {
                    return CreateErrorResult(
                        request.RegistrationId,
                        "Registration ID is required",
                        stopwatch);
                }

                if (request.NewAttendees == null || !request.NewAttendees.Any())
                {
                    return CreateErrorResult(
                        request.RegistrationId,
                        "At least one new attendee is required",
                        stopwatch);
                }

                // Get registration with current details
                var registration = await _context.Registrations
                    .Where(r => r.Id == request.RegistrationId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (registration == null)
                {
                    return CreateErrorResult(
                        request.RegistrationId,
                        "Registration not found",
                        stopwatch);
                }

                // Get event with pricing details
                var @event = await _eventRepository.GetByIdAsync(
                    registration.EventId,
                    trackChanges: false,
                    cancellationToken);

                if (@event == null)
                {
                    return CreateErrorResult(
                        request.RegistrationId,
                        "Event not found",
                        stopwatch);
                }

                // Get current registration count for capacity check
                var currentRegistrationCount = await _context.Registrations
                    .Where(r => r.EventId == @event.Id && r.Status == RegistrationStatus.Confirmed)
                    .SumAsync(r => r.Quantity, cancellationToken);

                // Create anonymous object with event details for consistent usage below
                // Note: Convert value objects to primitives and handle capacity (0 means unlimited)
                var eventDetails = new
                {
                    @event.Id,
                    Title = @event.Title.Value,
                    @event.Pricing,
                    @event.TicketPrice,
                    Capacity = @event.Capacity > 0 ? (int?)@event.Capacity : null,
                    MaxAttendeesPerRegistration = @event.MaxAttendeesPerRegistration,
                    @event.Status,
                    CurrentRegistrationCount = currentRegistrationCount
                };

                // Validate registration status
                if (registration.Status != RegistrationStatus.Confirmed)
                {
                    return CreateErrorResult(
                        request.RegistrationId,
                        "Only confirmed registrations can add attendees",
                        stopwatch,
                        eventDetails.Id,
                        eventDetails.Title,
                        registration.Quantity);
                }

                // Validate payment status
                if (registration.PaymentStatus != PaymentStatus.Completed)
                {
                    return CreateErrorResult(
                        request.RegistrationId,
                        "Only paid registrations can add attendees",
                        stopwatch,
                        eventDetails.Id,
                        eventDetails.Title,
                        registration.Quantity);
                }

                // Check for pending addition
                var hasPendingAddition = await _additionRepository.HasPendingAdditionAsync(
                    request.RegistrationId,
                    cancellationToken);

                if (hasPendingAddition)
                {
                    return CreateErrorResult(
                        request.RegistrationId,
                        "A pending addition already exists for this registration. Please complete or cancel it first.",
                        stopwatch,
                        eventDetails.Id,
                        eventDetails.Title,
                        registration.Quantity,
                        hasPendingAddition: true);
                }

                // Get max attendees per registration
                var maxAttendeesPerRegistration = eventDetails.MaxAttendeesPerRegistration;
                var currentAttendeeCount = registration.Quantity;
                var newAttendeesCount = request.NewAttendees.Count;
                var totalAttendeeCount = currentAttendeeCount + newAttendeesCount;

                // Check max attendees per registration
                if (totalAttendeeCount > maxAttendeesPerRegistration)
                {
                    return CreateErrorResult(
                        request.RegistrationId,
                        $"Cannot add {newAttendeesCount} attendees. Maximum attendees per registration is {maxAttendeesPerRegistration}. You currently have {currentAttendeeCount}.",
                        stopwatch,
                        eventDetails.Id,
                        eventDetails.Title,
                        currentAttendeeCount,
                        maxAttendeesPerRegistration: maxAttendeesPerRegistration);
                }

                // Check event capacity if set
                int? remainingCapacity = null;
                if (eventDetails.Capacity.HasValue)
                {
                    remainingCapacity = eventDetails.Capacity.Value - eventDetails.CurrentRegistrationCount;
                    if (newAttendeesCount > remainingCapacity)
                    {
                        return CreateErrorResult(
                            request.RegistrationId,
                            $"Cannot add {newAttendeesCount} attendees. Only {remainingCapacity} spots remaining for this event.",
                            stopwatch,
                            eventDetails.Id,
                            eventDetails.Title,
                            currentAttendeeCount,
                            remainingCapacity: remainingCapacity);
                    }
                }

                // Get current total paid
                var currentTotalPaid = registration.TotalPrice?.Amount ?? 0;
                var currency = registration.TotalPrice?.Currency.ToString() ?? "USD";

                // Calculate new total price based on pricing type
                var (newTotalPrice, attendeeBreakdown) = CalculateNewTotalPrice(
                    eventDetails.Pricing,
                    eventDetails.TicketPrice,
                    registration.Attendees.ToList(),
                    request.NewAttendees,
                    totalAttendeeCount);

                // Calculate delta
                var additionalAmount = newTotalPrice - currentTotalPaid;

                stopwatch.Stop();

                var result = new AdditionPriceResultDto
                {
                    RegistrationId = request.RegistrationId,
                    EventId = eventDetails.Id,
                    EventTitle = eventDetails.Title,
                    CurrentAttendeeCount = currentAttendeeCount,
                    NewAttendeesCount = newAttendeesCount,
                    TotalAttendeeCount = totalAttendeeCount,
                    MaxAttendeesPerRegistration = maxAttendeesPerRegistration,
                    CurrentTotalPaid = currentTotalPaid,
                    NewTotalPrice = newTotalPrice,
                    AdditionalAmount = additionalAmount,
                    Currency = currency,
                    IsValid = true,
                    HasPendingAddition = false,
                    AttendeeBreakdown = attendeeBreakdown,
                    RemainingCapacity = remainingCapacity
                };

                _logger.LogInformation(
                    "CalculateAdditionPrice COMPLETE: RegistrationId={RegistrationId}, CurrentTotal={CurrentTotal}, NewTotal={NewTotal}, AdditionalAmount={AdditionalAmount}, Currency={Currency}, Duration={ElapsedMs}ms",
                    request.RegistrationId,
                    currentTotalPaid,
                    newTotalPrice,
                    additionalAmount,
                    currency,
                    stopwatch.ElapsedMilliseconds);

                return Result<AdditionPriceResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CalculateAdditionPrice FAILED: RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.RegistrationId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    private (decimal TotalPrice, List<AttendeePrice> Breakdown) CalculateNewTotalPrice(
        LankaConnect.Products.LankaEvents.Domain.ValueObjects.TicketPricing? pricing,
        LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects.Money? ticketPrice,
        List<LankaConnect.Products.LankaEvents.Domain.ValueObjects.AttendeeDetails> existingAttendees,
        List<NewAttendeeDto> newAttendees,
        int totalAttendeeCount)
    {
        var breakdown = new List<AttendeePrice>();

        // If no pricing configured, event is free
        if (pricing == null && ticketPrice == null)
        {
            return (0, breakdown);
        }

        // Legacy single price (from TicketPrice property)
        if (pricing == null && ticketPrice != null)
        {
            var price = ticketPrice.Amount;
            foreach (var attendee in newAttendees)
            {
                breakdown.Add(new AttendeePrice(attendee.Name, attendee.AgeCategory, price));
            }
            return (totalAttendeeCount * price, breakdown);
        }

        // Modern pricing (Single, AgeDual, or GroupTiered)
        switch (pricing!.Type)
        {
            case PricingType.Single:
                var singlePrice = pricing.AdultPrice.Amount;
                foreach (var attendee in newAttendees)
                {
                    breakdown.Add(new AttendeePrice(attendee.Name, attendee.AgeCategory, singlePrice));
                }
                return (totalAttendeeCount * singlePrice, breakdown);

            case PricingType.AgeDual:
                // Calculate for all attendees (existing + new) with age-based pricing
                decimal totalForExisting = 0;
                foreach (var attendee in existingAttendees)
                {
                    var attendeePrice = pricing.CalculateForCategory(attendee.AgeCategory);
                    totalForExisting += attendeePrice.Amount;
                }

                decimal totalForNew = 0;
                foreach (var attendee in newAttendees)
                {
                    var attendeePrice = pricing.CalculateForCategory(attendee.AgeCategory);
                    totalForNew += attendeePrice.Amount;
                    breakdown.Add(new AttendeePrice(attendee.Name, attendee.AgeCategory, attendeePrice.Amount));
                }

                return (totalForExisting + totalForNew, breakdown);

            case PricingType.GroupTiered:
                // Group tiered pricing recalculates based on total count
                var groupPriceResult = pricing.CalculateGroupPrice(totalAttendeeCount);
                if (groupPriceResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to calculate group price: {Error}",
                        groupPriceResult.Error);
                    return (0, breakdown);
                }

                var tierResult = pricing.FindTierForAttendeeCount(totalAttendeeCount);
                var pricePerPerson = tierResult.IsSuccess
                    ? tierResult.Value.PricePerPerson.Amount
                    : 0;

                foreach (var attendee in newAttendees)
                {
                    breakdown.Add(new AttendeePrice(attendee.Name, attendee.AgeCategory, pricePerPerson));
                }

                return (groupPriceResult.Value.Amount, breakdown);

            default:
                _logger.LogWarning("Unknown pricing type: {PricingType}", pricing.Type);
                return (0, breakdown);
        }
    }

    private Result<AdditionPriceResultDto> CreateErrorResult(
        Guid registrationId,
        string errorMessage,
        Stopwatch stopwatch,
        Guid? eventId = null,
        string? eventTitle = null,
        int currentAttendeeCount = 0,
        int maxAttendeesPerRegistration = 10,
        int? remainingCapacity = null,
        bool hasPendingAddition = false)
    {
        stopwatch.Stop();

        _logger.LogWarning(
            "CalculateAdditionPrice FAILED: RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
            registrationId, errorMessage, stopwatch.ElapsedMilliseconds);

        var result = new AdditionPriceResultDto
        {
            RegistrationId = registrationId,
            EventId = eventId ?? Guid.Empty,
            EventTitle = eventTitle ?? string.Empty,
            CurrentAttendeeCount = currentAttendeeCount,
            NewAttendeesCount = 0,
            TotalAttendeeCount = currentAttendeeCount,
            MaxAttendeesPerRegistration = maxAttendeesPerRegistration,
            CurrentTotalPaid = 0,
            NewTotalPrice = 0,
            AdditionalAmount = 0,
            Currency = "USD",
            IsValid = false,
            ErrorMessage = errorMessage,
            HasPendingAddition = hasPendingAddition,
            RemainingCapacity = remainingCapacity
        };

        return Result<AdditionPriceResultDto>.Success(result);
    }
}
