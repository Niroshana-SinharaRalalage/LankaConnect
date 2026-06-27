using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetRegistrationById;

/// <summary>
/// Phase 6A.44: Handler to get registration details by ID
/// This allows anonymous users to view their registration details after payment
/// Phase 6A.137F-Fix: Added financial breakdown loading for bundled checkout items
/// </summary>
public class GetRegistrationByIdQueryHandler
    : IQueryHandler<GetRegistrationByIdQuery, RegistrationDetailsDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IDonationRepository _donationRepository;
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly ILogger<GetRegistrationByIdQueryHandler> _logger;

    public GetRegistrationByIdQueryHandler(
        IApplicationDbContext context,
        IDonationRepository donationRepository,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        ICollectionRepository collectionRepository,
        ISponsorRepository sponsorRepository,
        ILogger<GetRegistrationByIdQueryHandler> logger)
    {
        _context = context;
        _donationRepository = donationRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _collectionRepository = collectionRepository;
        _sponsorRepository = sponsorRepository;
        _logger = logger;
    }

    public async Task<Result<RegistrationDetailsDto?>> Handle(
        GetRegistrationByIdQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetRegistrationById"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("RegistrationId", request.RegistrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetRegistrationById START: RegistrationId={RegistrationId}",
                request.RegistrationId);

            try
            {
                // Validate request
                if (request.RegistrationId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetRegistrationById FAILED: Invalid RegistrationId - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);

                    return Result<RegistrationDetailsDto?>.Failure("Registration ID is required");
                }

                var registration = await _context.Registrations
                    .Where(r => r.Id == request.RegistrationId)
                    .Select(r => new RegistrationDetailsDto
                    {
                        Id = r.Id,
                        EventId = r.EventId,
                        UserId = r.UserId,
                        Quantity = r.Quantity,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,

                        // Map attendees
                        Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
                        {
                            Name = a.Name,
                            AgeCategory = a.AgeCategory,
                            Gender = a.Gender
                        }).ToList(),

                        // Contact information
                        ContactEmail = r.Contact != null ? r.Contact.Email : null,
                        ContactPhone = r.Contact != null ? r.Contact.PhoneNumber : null,
                        ContactAddress = r.Contact != null ? r.Contact.Address : null,

                        // Payment information
                        PaymentStatus = r.PaymentStatus,
                        TotalPriceAmount = r.TotalPrice != null ? r.TotalPrice.Amount : null,
                        TotalPriceCurrency = r.TotalPrice != null ? r.TotalPrice.Currency.ToString() : null,

                        // Phase 6A.137F-Fix: Checkout session ID for financial breakdown loading
                        StripeCheckoutSessionId = r.StripeCheckoutSessionId
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                // Phase 7F-E.2: load mode + lead-name + RegistrationBreakdown via the
                // shared projector so the FE event-detail card renders Mode A and Mode B
                // (B1/B2/B3/B4) registrations through one consistent shape.
                if (registration != null)
                {
                    try
                    {
                        var bd = await RegistrationBreakdownProjector.LoadAsync(
                            _context, registration.Id, cancellationToken);
                        registration = registration with
                        {
                            RegistrationMode = bd.Mode,
                            LeadAttendeeName = bd.LeadAttendeeName,
                            Breakdown = bd.Breakdown,
                        };
                    }
                    catch (Exception bdEx)
                    {
                        _logger.LogWarning(bdEx,
                            "[7F-E.2] Failed to load RegistrationBreakdown — RegistrationId={RegistrationId}; FE will fall back to legacy quantity-only display",
                            registration.Id);
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

                        // Load add-ons: prefer user+event query (catches bundled + standalone),
                        // fall back to checkout session ID for anonymous users without userId
                        try
                        {
                            IReadOnlyList<AddOnPurchase>? addOnPurchases = null;
                            if (registration.UserId.HasValue && registration.UserId.Value != Guid.Empty)
                            {
                                addOnPurchases = await _addOnPurchaseRepository.GetByUserIdAndEventIdAsync(
                                    registration.UserId.Value, registration.EventId, cancellationToken);
                            }
                            else
                            {
                                addOnPurchases = await _addOnPurchaseRepository.GetAllByCheckoutSessionIdAsync(
                                    registration.StripeCheckoutSessionId!, cancellationToken);
                            }
                            // Phase 6A.137F-Fix4: Include Pending bundled add-ons (defense-in-depth)
                            var completedAddOns = addOnPurchases?
                                .Where(p => p.Status == AddOnPurchaseStatus.Completed
                                         || (p.Status == AddOnPurchaseStatus.Pending
                                             && p.RegistrationId == registration.Id))
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

                if (registration == null)
                {
                    _logger.LogWarning(
                        "GetRegistrationById COMPLETE: Registration not found - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "GetRegistrationById COMPLETE: RegistrationId={RegistrationId}, EventId={EventId}, AttendeeCount={AttendeeCount}, PaymentStatus={PaymentStatus}, Duration={ElapsedMs}ms",
                        registration.Id, registration.EventId, registration.Attendees?.Count ?? 0, registration.PaymentStatus, stopwatch.ElapsedMilliseconds);
                }

                return Result<RegistrationDetailsDto?>.Success(registration);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetRegistrationById FAILED: Exception occurred - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.RegistrationId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
