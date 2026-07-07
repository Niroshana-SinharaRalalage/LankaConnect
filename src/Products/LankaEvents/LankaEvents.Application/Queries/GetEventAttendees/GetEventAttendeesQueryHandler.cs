using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Application.Common.Options;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventAttendees;

/// <summary>
/// Handler for retrieving event attendees with revenue breakdown
/// Includes on-the-fly revenue calculation for legacy registrations
/// </summary>
public class GetEventAttendeesQueryHandler
    : IQueryHandler<GetEventAttendeesQuery, EventAttendeesResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventRepository _eventRepository;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly CommissionSettings _commissionSettings;
    private readonly ILogger<GetEventAttendeesQueryHandler> _logger;

    public GetEventAttendeesQueryHandler(
        IApplicationDbContext context,
        IEventRepository eventRepository,
        IRevenueCalculatorService revenueCalculatorService,
        IOptions<CommissionSettings> commissionSettings,
        ILogger<GetEventAttendeesQueryHandler> logger)
    {
        _context = context;
        _eventRepository = eventRepository;
        _revenueCalculatorService = revenueCalculatorService;
        _commissionSettings = commissionSettings.Value;
        _logger = logger;
    }

    public async Task<Result<EventAttendeesResponse>> Handle(
        GetEventAttendeesQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventAttendees"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventAttendees START: EventId={EventId}",
                request.EventId);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventAttendees FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<EventAttendeesResponse>.Failure("Event ID is required");
                }

                // Get event details using repository
                // Phase 6A.X FIX: Use trackChanges: false for read-only query (better performance)
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: false, cancellationToken);

                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventAttendees FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<EventAttendeesResponse>.Failure("Event not found");
                }

                // Phase 6A.X DIAGNOSTIC: Log Location status for revenue breakdown calculation
                _logger.LogInformation(
                    "GetEventAttendees: Event loaded - EventId={EventId}, Title={Title}, HasLocation={HasLocation}",
                    @event.Id,
                    @event.Title.Value,
                    @event.Location != null);


                // Phase 6A.55: Use direct LINQ projection to avoid materializing JSONB with null AgeCategory
                // .Include(r => r.Attendees) fails when JSONB has {"age_category": null}
                // This pattern projects directly to DTO, allowing nullable AgeCategory to be handled gracefully
                // Phase 6A.81: CRITICAL - Only include CONFIRMED registrations (exclude Preliminary/Abandoned)
                // Phase 6A.X: LEFT JOIN with Tickets table to populate TicketCode and QrCodeData
                var attendeeDtos = await (
                    from r in _context.Registrations.AsNoTracking()
                    where r.EventId == request.EventId
                    where r.Status == RegistrationStatus.Confirmed ||
                          r.Status == RegistrationStatus.Waitlisted ||
                          r.Status == RegistrationStatus.CheckedIn ||
                          r.Status == RegistrationStatus.Attended ||
                          // Phase 7E follow-up: RefundRequested rows still consume capacity until
                          // Stripe confirms the refund (per RegistrationStatus enum doc). Surface
                          // them so organisers can spot stuck rows and force-cancel them.
                          r.Status == RegistrationStatus.RefundRequested
                    join t in _context.Tickets on r.Id equals t.RegistrationId into tickets
                    from ticket in tickets.DefaultIfEmpty()
                    orderby r.CreatedAt
                    select new EventAttendeeDto
                    {
                        RegistrationId = r.Id,
                        UserId = r.UserId,
                        Status = r.Status,
                        PaymentStatus = r.PaymentStatus,
                        CreatedAt = r.CreatedAt,

                        ContactEmail = r.Contact != null ? r.Contact.Email : string.Empty,
                        ContactPhone = r.Contact != null ? r.Contact.PhoneNumber : string.Empty,
                        ContactAddress = r.Contact != null ? r.Contact.Address : null,

                        // Phase 6A.55: Direct LINQ projection handles null AgeCategory gracefully
                        // DTO has nullable AgeCategory (Phase 6A.48), so null values don't crash
                        Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
                        {
                            Name = a.Name,
                            AgeCategory = a.AgeCategory, // DTO is nullable (Phase 6A.48)
                            Gender = a.Gender,
                            // Phase 6A.161: surface the per-attendee ticket tier (denormalized name
                            // lives in the same JSONB row, so this projects without any extra join).
                            // Null for single-tier/free/legacy registrations.
                            TicketTierId = a.TicketTierId,
                            TicketTierName = a.TicketTierName
                        }).ToList(),

                        TotalAttendees = r.Attendees.Count(),

                        // Phase 6A.55: Count only non-null Adult/Child values
                        // Null values are excluded from counts (safer than guessing)
                        AdultCount = r.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult),
                        ChildCount = r.Attendees.Count(a => a.AgeCategory == AgeCategory.Child),

                        // Phase 7E.8: explicit male/female counters so CSV/Excel exports stay
                        // accurate under Mode B (post-processing overrides these from the
                        // demographic axis when r.Attendees is empty).
                        MaleCount = r.Attendees.Count(a => a.Gender == Gender.Male),
                        FemaleCount = r.Attendees.Count(a => a.Gender == Gender.Female),

                        // Gender distribution with full names (Phase 6A.45 fix - avoid Excel formula interpretation)
                        GenderDistribution = string.Join(", ",
                            r.Attendees
                                .Where(a => a.Gender.HasValue)
                                .GroupBy(a => a.Gender!.Value)
                                .Select(g => $"{g.Count()} {g.Key}")
                        ),

                        // Phase 7E.7: RegistrationMode is a simple smallint column EF can translate.
                        // LeadAttendeeName is a simple text column. HeadCount itself uses a custom
                        // JSONB ValueConverter so its sub-fields can't be SQL-projected — Mode B
                        // overrides for TotalAttendees/AdultCount/ChildCount happen in the
                        // post-processing pass below where the entity is materialised.
                        RegistrationMode = r.RegistrationMode,
                        LeadAttendeeName = r.LeadAttendeeName,

                        TotalAmount = r.TotalPrice != null ? r.TotalPrice.Amount : null,

                        // Phase 6A.X: Use actual breakdown from Registration columns (set by SetRevenueBreakdown)
                        // For registrations without breakdown data, NetAmount uses legacy 5% calculation
                        SalesTaxAmount = r.SalesTaxAmount != null ? r.SalesTaxAmount.Amount : null,
                        StripeFeeAmount = r.StripeFeeAmount != null ? r.StripeFeeAmount.Amount : null,
                        PlatformCommissionAmount = r.PlatformCommissionAmount != null ? r.PlatformCommissionAmount.Amount : null,
                        OrganizerPayoutAmount = r.OrganizerPayoutAmount != null ? r.OrganizerPayoutAmount.Amount : null,
                        SalesTaxRate = r.SalesTaxRate,

                        // NetAmount: Use actual breakdown if available, otherwise legacy calculation
                        NetAmount = r.OrganizerPayoutAmount != null
                            ? r.OrganizerPayoutAmount.Amount
                            : (r.TotalPrice != null ? r.TotalPrice.Amount * (1 - _commissionSettings.EventTicketCommissionRate) : null),

                        Currency = r.TotalPrice != null ? r.TotalPrice.Currency.ToString() : null,

                        // Phase 6A.X: Ticket info from LEFT JOIN (null for free events or tickets not yet generated)
                        TicketCode = ticket != null ? ticket.TicketCode : null,
                        QrCodeData = ticket != null ? ticket.QrCodeData : null,
                        HasTicket = ticket != null
                    }
                ).ToListAsync(cancellationToken);

        // Phase 6A.161: Observability — how many registrations carry at least one ticket tier.
        // Helps diagnose "tier column is empty" reports (single-tier/free/legacy events legitimately
        // return zero here). Cheap in-memory scan over the already-materialised DTOs.
        var registrationsWithTier = attendeeDtos.Count(d =>
            d.Attendees.Any(a => !string.IsNullOrWhiteSpace(a.TicketTierName)));
        _logger.LogInformation(
            "GetEventAttendees: ticket-tier coverage - {WithTier}/{Total} registrations have a tier, EventId={EventId}",
            registrationsWithTier, attendeeDtos.Count, request.EventId);

        // Phase 7E.7: Post-processing override for Mode B registrations. The custom JSONB
        // ValueConverter on Registration.HeadCount prevents SQL-side sub-field access, so we
        // do a second tiny query that materialises HeadCount via the converter, then map fields
        // to the DTOs we already have. Mode A registrations are untouched (HeadCount is null).
        var bModeRows = await _context.Registrations
            .AsNoTracking()
            .Where(r => r.EventId == request.EventId &&
                        r.RegistrationMode != LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode.DetailedAttendees &&
                        (r.Status == RegistrationStatus.Confirmed ||
                         r.Status == RegistrationStatus.Waitlisted ||
                         r.Status == RegistrationStatus.CheckedIn ||
                         r.Status == RegistrationStatus.Attended ||
                         r.Status == RegistrationStatus.RefundRequested))
            .Select(r => new { r.Id, r.HeadCount })
            .ToListAsync(cancellationToken);

        if (bModeRows.Count > 0)
        {
            var bModeMap = bModeRows.ToDictionary(x => x.Id, x => x.HeadCount);
            foreach (var dto in attendeeDtos)
            {
                if (!bModeMap.TryGetValue(dto.RegistrationId, out var hc) || hc == null) continue;

                // Override demographic counts from the snapshotted HeadCountBreakdown.
                dto.TotalAttendees = hc.Total;
                if (hc.Demographics != null)
                {
                    var demo = hc.Demographics;
                    dto.AdultCount = (demo.Adults ?? 0)
                                     + (demo.AdultMales ?? 0)
                                     + (demo.AdultFemales ?? 0);
                    dto.ChildCount = (demo.Children ?? 0)
                                     + (demo.ChildMales ?? 0)
                                     + (demo.ChildFemales ?? 0);

                    // Reuse the shared formatter for consistent wording across emails + dashboard.
                    dto.HeadCountBreakdownLine = LankaConnect.Products.LankaEvents.Application.Common
                        .HeadCountEmailFormatter.FormatDemographicLine(demo);

                    // GenderDistribution column for Mode B mirrors the demographic line so the
                    // existing column in the AttendeeManagementTab is informative without UI changes.
                    var males = (demo.Males ?? 0) + (demo.AdultMales ?? 0) + (demo.ChildMales ?? 0);
                    var females = (demo.Females ?? 0) + (demo.AdultFemales ?? 0) + (demo.ChildFemales ?? 0);
                    dto.MaleCount = males;
                    dto.FemaleCount = females;
                    if (males > 0 || females > 0)
                    {
                        dto.GenderDistribution = $"{males} Male, {females} Female";
                    }
                }
                else
                {
                    // B1: Total only, no demographic axis.
                    dto.AdultCount = 0;
                    dto.ChildCount = 0;
                    dto.MaleCount = 0;
                    dto.FemaleCount = 0;
                    dto.GenderDistribution = string.Empty;
                    dto.HeadCountBreakdownLine = string.Empty;
                }
            }
        }

        // Phase 6A.X FIX: Calculate breakdown ON-THE-FLY for old registrations without breakdown data
        // This ensures ALL events show detailed breakdown (not just new ones created after fix deployment)
        // User validated: "regardless of new or old event, calculations should be reflected"
        _logger.LogInformation(
            "Starting on-the-fly revenue breakdown calculation: TotalRegistrations={Count}, EventLocation={HasLocation}",
            attendeeDtos.Count,
            @event.Location != null);

        int calculatedCount = 0;
        foreach (var attendeeDto in attendeeDtos)
        {
            // Only calculate if breakdown is missing AND we have necessary data
            if (!attendeeDto.SalesTaxAmount.HasValue &&
                attendeeDto.TotalAmount.HasValue &&
                attendeeDto.TotalAmount.Value > 0 &&
                @event.Location != null)
            {
                _logger.LogInformation(
                    "Attempting breakdown calculation for registration {RegistrationId}: Amount=${Amount}",
                    attendeeDto.RegistrationId,
                    attendeeDto.TotalAmount.Value);
                try
                {
                    // Create Money object from TotalAmount
                    var totalPriceMoney = Money.Create(attendeeDto.TotalAmount.Value, Currency.USD);
                    if (totalPriceMoney.IsFailure)
                    {
                        _logger.LogWarning(
                            "Failed to create Money for registration {RegistrationId}: {Error}",
                            attendeeDto.RegistrationId,
                            totalPriceMoney.Error);
                        continue;
                    }

                    // Calculate breakdown using Event.Location
                    var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                        totalPriceMoney.Value,
                        @event.Location,
                        cancellationToken);

                    if (breakdownResult.IsSuccess)
                    {
                        // Update DTO with calculated values (NOT database - read-only query)
                        var breakdown = breakdownResult.Value;
                        attendeeDto.SalesTaxAmount = breakdown.SalesTaxAmount.Amount;
                        attendeeDto.StripeFeeAmount = breakdown.StripeFeeAmount.Amount;
                        attendeeDto.PlatformCommissionAmount = breakdown.PlatformCommission.Amount;
                        attendeeDto.OrganizerPayoutAmount = breakdown.OrganizerPayout.Amount;
                        attendeeDto.SalesTaxRate = breakdown.SalesTaxRate;

                        // Phase 6A.X FIX: Update NetAmount to use calculated organizer payout
                        // Bug: NetAmount was set to legacy 5% calculation (line 116-118) BEFORE on-the-fly calculation
                        // This caused NetAmount to show $47.50 instead of $44.66 in Excel exports
                        attendeeDto.NetAmount = breakdown.OrganizerPayout.Amount;

                        calculatedCount++;
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Revenue breakdown calculation failed for registration {RegistrationId}: {Error}",
                            attendeeDto.RegistrationId,
                            breakdownResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Exception while calculating on-the-fly revenue breakdown for registration {RegistrationId}",
                        attendeeDto.RegistrationId);
                }
            }
        }

        if (calculatedCount > 0)
        {
            _logger.LogInformation(
                "Calculated revenue breakdown on-the-fly for {Count} old registrations in event {EventId}",
                calculatedCount,
                @event.Id);
        }

        // Phase 6A.71: Calculate revenue with commission
        var grossRevenue = attendeeDtos.Sum(a => a.TotalAmount ?? 0);
        var isFreeEvent = @event.IsFree() || grossRevenue == 0;

        decimal commissionAmount = 0m;
        decimal netRevenue = grossRevenue;

        if (!isFreeEvent)
        {
            commissionAmount = grossRevenue * _commissionSettings.EventTicketCommissionRate;
            netRevenue = grossRevenue - commissionAmount;
        }

        // Phase 6A.X: Calculate detailed breakdown totals by summing actual Registration breakdown columns
        // This uses the actual breakdown data stored when SetRevenueBreakdown() was called
        decimal totalSalesTax = attendeeDtos.Sum(a => a.SalesTaxAmount ?? 0m);
        decimal totalStripeFees = attendeeDtos.Sum(a => a.StripeFeeAmount ?? 0m);
        decimal totalPlatformCommission = attendeeDtos.Sum(a => a.PlatformCommissionAmount ?? 0m);
        decimal totalOrganizerPayout = attendeeDtos.Sum(a => a.OrganizerPayoutAmount ?? 0m);

        // Calculate average tax rate weighted by registration count
        var registrationsWithTax = attendeeDtos.Where(a => a.SalesTaxRate > 0).ToList();
        decimal averageTaxRate = registrationsWithTax.Any()
            ? registrationsWithTax.Average(a => a.SalesTaxRate)
            : 0m;

        // Phase 6A.X CRITICAL FIX: Check hasRevenueBreakdown AFTER on-the-fly calculation
        // Original bug: This check was done BEFORE on-the-fly calculation loop (lines 118-185)
        // Result: Even though we calculated breakdown on-the-fly, flag was already FALSE
        // Fix: Move this check to AFTER on-the-fly calculation so it sees the updated DTOs
        bool hasRevenueBreakdown = attendeeDtos.Any(a =>
            a.SalesTaxAmount.HasValue ||
            a.StripeFeeAmount.HasValue ||
            a.PlatformCommissionAmount.HasValue ||
            a.OrganizerPayoutAmount.HasValue);

                // Legacy fallback: If no registration has breakdown data, use commission-based calculation
                if (!hasRevenueBreakdown && !isFreeEvent)
                {
                    totalPlatformCommission = commissionAmount;
                    totalOrganizerPayout = netRevenue;
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventAttendees COMPLETE: EventId={EventId}, TotalRegistrations={TotalRegistrations}, TotalAttendees={TotalAttendees}, GrossRevenue={GrossRevenue}, IsFreeEvent={IsFreeEvent}, HasRevenueBreakdown={HasRevenueBreakdown}, Duration={ElapsedMs}ms",
                    request.EventId,
                    attendeeDtos.Count,
                    attendeeDtos.Sum(a => a.TotalAttendees),
                    grossRevenue,
                    isFreeEvent,
                    hasRevenueBreakdown,
                    stopwatch.ElapsedMilliseconds);

                return Result<EventAttendeesResponse>.Success(new EventAttendeesResponse
                {
                    EventId = request.EventId,
                    EventTitle = @event.Title.Value,
                    Attendees = attendeeDtos,
                    TotalRegistrations = attendeeDtos.Count(),
                    TotalAttendees = attendeeDtos.Sum(a => a.TotalAttendees),

                    // Phase 6A.71: Commission-aware revenue
                    GrossRevenue = grossRevenue,
                    CommissionAmount = commissionAmount,
                    NetRevenue = netRevenue,
                    CommissionRate = _commissionSettings.EventTicketCommissionRate,
                    IsFreeEvent = isFreeEvent,

                    // Phase 6A.X: Detailed revenue breakdown totals
                    TotalSalesTax = totalSalesTax,
                    TotalStripeFees = totalStripeFees,
                    TotalPlatformCommission = totalPlatformCommission,
                    TotalOrganizerPayout = totalOrganizerPayout,
                    AverageTaxRate = averageTaxRate,
                    HasRevenueBreakdown = hasRevenueBreakdown,

                    // Deprecated (for backward compatibility)
#pragma warning disable CS0618 // Type or member is obsolete
                    TotalRevenue = grossRevenue
#pragma warning restore CS0618 // Type or member is obsolete
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventAttendees FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    // Phase 6A.55 NOTE: MapToDto method removed and replaced with direct LINQ projection above
    // This avoids materializing domain entities with null AgeCategory values
    private EventAttendeeDto MapToDto_REMOVED_PHASE_6A55(Registration registration)
    {
        // Phase 6A.55: This method caused crashes when AgeCategory was null in JSONB
        // Lines below would throw InvalidOperationException during .Include() materialization
        var adultCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult);
        var childCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Child);

        // Calculate gender distribution (e.g., "2 Male, 1 Female")
        // Phase 6A.45 FIX: Use full names instead of short codes to avoid Excel formula interpretation
        var genderCounts = registration.Attendees
            .Where(a => a.Gender.HasValue)
            .GroupBy(a => a.Gender!.Value)
            .Select(g => $"{g.Count()} {g.Key}")
            .ToList();

        var genderDistribution = genderCounts.Any()
            ? string.Join(", ", genderCounts)
            : string.Empty;

        // Map attendees
        var attendeeDtos = registration.Attendees.Select(a => new AttendeeDetailsDto
        {
            Name = a.Name,
            AgeCategory = a.AgeCategory,
            Gender = a.Gender
        }).ToList();

        return new EventAttendeeDto
        {
            RegistrationId = registration.Id,
            UserId = registration.UserId,
            Status = registration.Status,
            PaymentStatus = registration.PaymentStatus,
            CreatedAt = registration.CreatedAt,

            ContactEmail = registration.Contact?.Email ?? string.Empty,
            ContactPhone = registration.Contact?.PhoneNumber ?? string.Empty,
            ContactAddress = registration.Contact?.Address,

            Attendees = attendeeDtos,
            TotalAttendees = registration.Attendees.Count,
            AdultCount = adultCount,
            ChildCount = childCount,
            GenderDistribution = genderDistribution,

            TotalAmount = registration.TotalPrice?.Amount,
            Currency = registration.TotalPrice?.Currency.ToString(),

            // Ticket info - will be enhanced when Ticket entity integration is complete
            TicketCode = null,
            QrCodeData = null,
            HasTicket = false
        };
    }

    private static string GetGenderShortCode(Gender gender)
    {
        return gender switch
        {
            Gender.Male => "M",
            Gender.Female => "F",
            Gender.Other => "O",
            _ => "?"
        };
    }
}
