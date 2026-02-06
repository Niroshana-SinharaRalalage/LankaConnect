using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetUserRegistrationForEvent;

public class GetUserRegistrationForEventQueryHandler
    : IQueryHandler<GetUserRegistrationForEventQuery, RegistrationDetailsDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly ILogger<GetUserRegistrationForEventQueryHandler> _logger;

    public GetUserRegistrationForEventQueryHandler(
        IApplicationDbContext context,
        IStripePaymentService stripePaymentService,
        ILogger<GetUserRegistrationForEventQueryHandler> logger)
    {
        _context = context;
        _stripePaymentService = stripePaymentService;
        _logger = logger;
    }

    public async Task<Result<RegistrationDetailsDto?>> Handle(
        GetUserRegistrationForEventQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetUserRegistrationForEvent"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetUserRegistrationForEvent START: EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetUserRegistrationForEvent FAILED: Invalid EventId - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<RegistrationDetailsDto?>.Failure("Event ID is required");
                }

                if (request.UserId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetUserRegistrationForEvent FAILED: Invalid UserId - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<RegistrationDetailsDto?>.Failure("User ID is required");
                }

                // Only return active registrations (exclude cancelled and refunded)
                // This fixes the multi-attendee re-registration issue (Session 30)
                // Phase 6A.41: Fixed to return NEWEST registration (OrderByDescending)
                // Phase 6A.47: Added AsNoTracking() to fix JSON projection error
                // Phase 6A.81 Part 3: Include Preliminary for payment pending UI
                var registration = await _context.Registrations
                    .AsNoTracking()
                    .Where(r => r.EventId == request.EventId &&
                               r.UserId == request.UserId &&
                               r.Status != RegistrationStatus.Cancelled &&
                               r.Status != RegistrationStatus.Refunded)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new RegistrationDetailsDto
                    {
                        Id = r.Id,
                        EventId = r.EventId,
                        UserId = r.UserId,
                        Quantity = r.Quantity,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,

                        // Map attendees (Session 21 multi-attendee feature)
                        // Phase 6A.43: Updated to use AgeCategory instead of Age
                        // Phase 6A.48: AgeCategory now nullable in DTO to handle corrupted JSONB data
                        Attendees = r.Attendees != null ? r.Attendees.Select(a => new AttendeeDetailsDto
                        {
                            Name = a.Name,
                            AgeCategory = a.AgeCategory,
                            Gender = a.Gender
                        }).ToList() : new List<AttendeeDetailsDto>(),

                        // Contact information
                        ContactEmail = r.Contact != null ? r.Contact.Email : null,
                        ContactPhone = r.Contact != null ? r.Contact.PhoneNumber : null,
                        ContactAddress = r.Contact != null ? r.Contact.Address : null,

                        // Payment information
                        PaymentStatus = r.PaymentStatus,
                        TotalPriceAmount = r.TotalPrice != null ? r.TotalPrice.Amount : null,
                        TotalPriceCurrency = r.TotalPrice != null ? r.TotalPrice.Currency.ToString() : null,

                        // Phase 6A.81 Part 3: Checkout session data (URL populated below for Preliminary)
                        StripeCheckoutSessionId = r.StripeCheckoutSessionId,
                        CheckoutSessionExpiresAt = r.CheckoutSessionExpiresAt,
                        StripeCheckoutUrl = null  // Populated after query for Preliminary status
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                // Phase 6A.81 Part 3: Retrieve checkout URL from Stripe for Preliminary registrations
                if (registration != null &&
                    registration.Status == RegistrationStatus.Preliminary &&
                    !string.IsNullOrWhiteSpace(registration.StripeCheckoutSessionId))
                {
                    _logger.LogDebug(
                        "[Phase 6A.81-Part3] Retrieving checkout URL for Preliminary registration - RegistrationId={RegistrationId}, SessionId={SessionId}",
                        registration.Id, registration.StripeCheckoutSessionId);

                    var checkoutUrlResult = await _stripePaymentService.GetCheckoutSessionUrlAsync(
                        registration.StripeCheckoutSessionId,
                        cancellationToken);

                    if (checkoutUrlResult.IsSuccess)
                    {
                        registration = registration with { StripeCheckoutUrl = checkoutUrlResult.Value };

                        _logger.LogDebug(
                            "[Phase 6A.81-Part3] Checkout URL retrieved successfully - RegistrationId={RegistrationId}",
                            registration.Id);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[Phase 6A.81-Part3] Failed to retrieve checkout URL - RegistrationId={RegistrationId}, Error={Error}",
                            registration.Id, checkoutUrlResult.Error);
                    }
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetUserRegistrationForEvent COMPLETE: EventId={EventId}, UserId={UserId}, Found={Found}, RegistrationId={RegistrationId}, Status={Status}, Duration={ElapsedMs}ms",
                    request.EventId, request.UserId, registration != null, registration?.Id, registration?.Status, stopwatch.ElapsedMilliseconds);

                return Result<RegistrationDetailsDto?>.Success(registration);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetUserRegistrationForEvent FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
