using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetUserRegistrationForEvent;

public class GetUserRegistrationForEventQueryHandler
    : IQueryHandler<GetUserRegistrationForEventQuery, RegistrationDetailsDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IDonationRepository _donationRepository;
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly ILogger<GetUserRegistrationForEventQueryHandler> _logger;

    public GetUserRegistrationForEventQueryHandler(
        IApplicationDbContext context,
        IStripePaymentService stripePaymentService,
        IDonationRepository donationRepository,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        ICollectionRepository collectionRepository,
        ISponsorRepository sponsorRepository,
        ILogger<GetUserRegistrationForEventQueryHandler> logger)
    {
        _context = context;
        _stripePaymentService = stripePaymentService;
        _donationRepository = donationRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _collectionRepository = collectionRepository;
        _sponsorRepository = sponsorRepository;
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

                // Phase 6A.137F-Fix: Load bundled financial items for completed registrations
                if (registration != null &&
                    registration.PaymentStatus == PaymentStatus.Completed &&
                    !string.IsNullOrWhiteSpace(registration.StripeCheckoutSessionId))
                {
                    try
                    {
                        decimal donationAmount = 0m;
                        decimal addOnTotal = 0m;
                        decimal collectionTotal = 0m;
                        decimal sponsorTotal = 0m;

                        // Load bundled donation by registration ID
                        try
                        {
                            var donation = await _donationRepository.GetByRegistrationIdAsync(
                                registration.Id, cancellationToken);
                            if (donation != null && donation.Amount.Amount > 0)
                                donationAmount = donation.Amount.Amount;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "[Phase 6A.137F-Fix] Failed to load donation - RegistrationId={RegistrationId}",
                                registration.Id);
                        }

                        // Load add-ons by user+event (catches both bundled and standalone purchases)
                        try
                        {
                            var addOnPurchases = await _addOnPurchaseRepository.GetByUserIdAndEventIdAsync(
                                request.UserId, request.EventId, cancellationToken);
                            var completedAddOns = addOnPurchases?
                                .Where(p => p.Status == AddOnPurchaseStatus.Completed)
                                .ToList();
                            if (completedAddOns != null && completedAddOns.Count > 0)
                                addOnTotal = completedAddOns.Sum(p => p.TotalAmount.Amount);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "[Phase 6A.137F-Fix] Failed to load add-ons - RegistrationId={RegistrationId}",
                                registration.Id);
                        }

                        // Load bundled collection by checkout session ID
                        try
                        {
                            var collection = await _collectionRepository.GetByCheckoutSessionIdAsync(
                                registration.StripeCheckoutSessionId!, cancellationToken);
                            if (collection != null && collection.Amount.Amount > 0)
                                collectionTotal = collection.Amount.Amount;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "[Phase 6A.137F-Fix] Failed to load collection - RegistrationId={RegistrationId}",
                                registration.Id);
                        }

                        // Load bundled sponsor by checkout session ID
                        try
                        {
                            var sponsor = await _sponsorRepository.GetByCheckoutSessionIdAsync(
                                registration.StripeCheckoutSessionId!, cancellationToken);
                            if (sponsor != null && sponsor.Amount != null && sponsor.Amount.Amount > 0)
                                sponsorTotal = sponsor.Amount.Amount;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "[Phase 6A.137F-Fix] Failed to load sponsor - RegistrationId={RegistrationId}",
                                registration.Id);
                        }

                        // Compute grand total: tickets + all bundled items
                        var ticketAmount = registration.TotalPriceAmount ?? 0m;
                        var grandTotal = ticketAmount + donationAmount + addOnTotal + collectionTotal + sponsorTotal;

                        if (donationAmount > 0 || addOnTotal > 0 || collectionTotal > 0 || sponsorTotal > 0)
                        {
                            registration = registration with
                            {
                                DonationAmount = donationAmount > 0 ? donationAmount : null,
                                AddOnTotal = addOnTotal > 0 ? addOnTotal : null,
                                CollectionTotal = collectionTotal > 0 ? collectionTotal : null,
                                SponsorTotal = sponsorTotal > 0 ? sponsorTotal : null,
                                GrandTotal = grandTotal
                            };

                            _logger.LogInformation(
                                "[Phase 6A.137F-Fix] Financial breakdown loaded - RegistrationId={RegistrationId}, Tickets={Tickets}, Donation={Donation}, AddOns={AddOns}, Collection={Collection}, Sponsor={Sponsor}, GrandTotal={GrandTotal}",
                                registration.Id, ticketAmount, donationAmount, addOnTotal, collectionTotal, sponsorTotal, grandTotal);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "[Phase 6A.137F-Fix] Failed to load financial breakdown - RegistrationId={RegistrationId}",
                            registration.Id);
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
