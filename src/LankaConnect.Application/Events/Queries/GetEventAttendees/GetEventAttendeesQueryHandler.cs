using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Options;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LankaConnect.Application.Events.Queries.GetEventAttendees;

public class GetEventAttendeesQueryHandler
    : IQueryHandler<GetEventAttendeesQuery, EventAttendeesResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventRepository _eventRepository;
    private readonly CommissionSettings _commissionSettings;

    public GetEventAttendeesQueryHandler(
        IApplicationDbContext context,
        IEventRepository eventRepository,
        IOptions<CommissionSettings> commissionSettings)
    {
        _context = context;
        _eventRepository = eventRepository;
        _commissionSettings = commissionSettings.Value;
    }

    public async Task<Result<EventAttendeesResponse>> Handle(
        GetEventAttendeesQuery request,
        CancellationToken cancellationToken)
    {
        // Get event details using repository
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (@event == null)
        {
            return Result<EventAttendeesResponse>.Failure("Event not found");
        }

        // Phase 6A.55: Use direct LINQ projection to avoid materializing JSONB with null AgeCategory
        // .Include(r => r.Attendees) fails when JSONB has {"age_category": null}
        // This pattern projects directly to DTO, allowing nullable AgeCategory to be handled gracefully
        var attendeeDtos = await _context.Registrations
            .AsNoTracking()
            .Where(r => r.EventId == request.EventId)
            .Where(r => r.Status != RegistrationStatus.Cancelled &&
                       r.Status != RegistrationStatus.Refunded)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new EventAttendeeDto
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
                    Gender = a.Gender
                }).ToList(),

                TotalAttendees = r.Attendees.Count(),

                // Phase 6A.55: Count only non-null Adult/Child values
                // Null values are excluded from counts (safer than guessing)
                AdultCount = r.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult),
                ChildCount = r.Attendees.Count(a => a.AgeCategory == AgeCategory.Child),

                // Gender distribution with full names (Phase 6A.45 fix - avoid Excel formula interpretation)
                GenderDistribution = string.Join(", ",
                    r.Attendees
                        .Where(a => a.Gender.HasValue)
                        .GroupBy(a => a.Gender!.Value)
                        .Select(g => $"{g.Count()} {g.Key}")
                ),

                TotalAmount = r.TotalPrice != null ? r.TotalPrice.Amount : null,

                // Phase 6A.71: Calculate NET amount per registration (organizer's payout after 5% commission)
                // For free events or null amounts, NetAmount will be null
                NetAmount = r.TotalPrice != null
                    ? r.TotalPrice.Amount * (1 - _commissionSettings.EventTicketCommissionRate)
                    : null,

                Currency = r.TotalPrice != null ? r.TotalPrice.Currency.ToString() : null,

                // Ticket info - will be enhanced when Ticket entity integration is complete
                TicketCode = null,
                QrCodeData = null,
                HasTicket = false
            })
            .ToListAsync(cancellationToken);

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

        return Result<EventAttendeesResponse>.Success(new EventAttendeesResponse
        {
            EventId = request.EventId,
            EventTitle = @event.Title.Value,
            Attendees = attendeeDtos,
            TotalRegistrations = attendeeDtos.Count,
            TotalAttendees = attendeeDtos.Sum(a => a.TotalAttendees),

            // Phase 6A.71: Commission-aware revenue
            GrossRevenue = grossRevenue,
            CommissionAmount = commissionAmount,
            NetRevenue = netRevenue,
            CommissionRate = _commissionSettings.EventTicketCommissionRate,
            IsFreeEvent = isFreeEvent,

            // Deprecated (for backward compatibility)
#pragma warning disable CS0618 // Type or member is obsolete
            TotalRevenue = grossRevenue
#pragma warning restore CS0618 // Type or member is obsolete
        });
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
